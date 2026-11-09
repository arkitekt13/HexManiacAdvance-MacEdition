using System;
using System.Linq;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI.Implementations;
using HavenSoft.HexManiac.AvaloniaUI.Project;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>A static image with local panning; owns no live map editor or ROM commands.</summary>
public sealed class ScriptMapReferenceView : Grid {
   private readonly TextBlock caption;
   private readonly TextBlock eventInfo;
   private IReadOnlyList<ScriptMapEvent> events = Array.Empty<ScriptMapEvent>();
   private ContextMenu eventMenu;
   private int snapshotGeneration;
   private bool dragged;
   private readonly Image image;
   private readonly ScrollViewer scroll;
   private readonly CheckBox gridCheckBox;
   private readonly ReferenceTileGrid tileGrid;
   private WriteableBitmap bitmap;
   private IPointer pointer;
   private Point start;
   private Vector startOffset;
   public event EventHandler InteractionStarted;
   public event EventHandler GridPreferenceChanged;
   public event EventHandler<ScriptMapEvent> EventRequested;
   public bool ContainsEvent(ScriptMapEvent target) => events.Contains(target);
   public bool ShowGrid { get => gridCheckBox.IsChecked == true; set => gridCheckBox.IsChecked = value; }
   public Vector Offset { get => scroll.Offset; set => scroll.Offset = value; }
   public bool IsPanning => pointer != null;

   public ScriptMapReferenceView() {
      RowDefinitions = new RowDefinitions("Auto,Auto,*");
      ClipToBounds = true;
      Focusable = false;
      caption = new TextBlock { Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap };
      eventInfo = new TextBlock { Name = "ReferenceEventInfo", Margin = new Thickness(8, 0, 8, 4), MinHeight = 20, TextWrapping = TextWrapping.Wrap };
      image = new Image { Stretch = Stretch.None, HorizontalAlignment = HorizontalAlignment.Left,
         VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false, Focusable = false };
      RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.None);
      tileGrid = new ReferenceTileGrid { IsVisible = false, IsHitTestVisible = false, Focusable = false };
      var artwork = new Grid { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
      artwork.Children.Add(image); artwork.Children.Add(tileGrid);
      gridCheckBox = new CheckBox { Name = "ReferenceGridCheckBox", Content = "Grid", Focusable = false, Margin = new Thickness(8, 0) };
      gridCheckBox.PropertyChanged += (_, e) => {
         if (e.Property != ToggleButton.IsCheckedProperty) return;
         tileGrid.IsVisible = ShowGrid;
         GridPreferenceChanged?.Invoke(this, EventArgs.Empty);
      };
      var header = new DockPanel();
      DockPanel.SetDock(gridCheckBox, Dock.Right); header.Children.Add(gridCheckBox); header.Children.Add(caption);
      scroll = new ScrollViewer { Content = artwork, Focusable = false, Background = Brushes.Transparent,
         HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
      Children.Add(header); Children.Add(eventInfo); SetRow(eventInfo, 1); Children.Add(scroll); SetRow(scroll, 2);
      AddHandler(PointerPressedEvent, BeginPan, RoutingStrategies.Tunnel);
      AddHandler(PointerMovedEvent, Pan, RoutingStrategies.Tunnel);
      AddHandler(PointerReleasedEvent, EndPan, RoutingStrategies.Tunnel);
      PointerCaptureLost += (_, _) => CancelPan();
      AddHandler(PointerWheelChangedEvent, (_, e) => {
         CancelPan(); CloseEventMenu();
         InteractionStarted?.Invoke(this, EventArgs.Empty);
         scroll.Offset -= new Vector(e.Delta.X * 32, e.Delta.Y * 32);
         e.Handled = true;
      }, RoutingStrategies.Tunnel);
      DetachedFromVisualTree += (_, _) => Clear();
   }

   public void ShowSnapshot(ScriptMapSnapshot snapshot) {
      CancelPan(); CloseEventMenu(); snapshotGeneration++; events = snapshot.Events;
      var next = PixelBuffer.EnsureSize(null, snapshot.Image.PixelWidth, snapshot.Image.PixelHeight);
      PixelBuffer.Write555(next, snapshot.Image.PixelData, snapshot.Image.PixelWidth, snapshot.Image.PixelHeight);
      image.Width = snapshot.Image.PixelWidth; image.Height = snapshot.Image.PixelHeight;
      tileGrid.Width = image.Width; tileGrid.Height = image.Height;
      image.Source = next;
      bitmap?.Dispose(); bitmap = next;
      gridCheckBox.IsEnabled = true;
      caption.Text = snapshot.Caption + " — drag or scroll to pan";
      ShowEventInfo("Click an object, script event or signpost to view its code.");
   }

   public void Clear(string message = "") {
      CancelPan(); CloseEventMenu(); snapshotGeneration++; events = Array.Empty<ScriptMapEvent>();
      eventInfo.Text = string.Empty; eventInfo.IsVisible = false;
      image.Source = null; bitmap?.Dispose(); bitmap = null;
      tileGrid.Width = tileGrid.Height = 0; gridCheckBox.IsEnabled = false;
      caption.Text = message;
   }

   private void BeginPan(object sender, PointerPressedEventArgs e) {
      if (e.GetPosition(this).Y < scroll.Bounds.Y) return;
      // Scrollbar thumbs keep their normal behavior; artwork clicks never reach hex input.
      if (e.Source is Visual visual && (visual is ScrollBar || visual.GetVisualAncestors().OfType<ScrollBar>().Any())) return;
      if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
      CloseEventMenu();
      InteractionStarted?.Invoke(this, EventArgs.Empty);
      dragged = false;
      start = e.GetPosition(this); startOffset = scroll.Offset; pointer = e.Pointer;
      pointer.Capture(this); e.Handled = true;
   }

   private void Pan(object sender, PointerEventArgs e) {
      if (pointer != e.Pointer) return;
      if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) { CancelPan(); return; }
      var delta = e.GetPosition(this) - start;
      if (Math.Abs(delta.X) >= 4 || Math.Abs(delta.Y) >= 4) dragged = true;
      if (dragged) scroll.Offset = startOffset - new Vector(delta.X, delta.Y);
      e.Handled = true;
   }

   private void EndPan(object sender, PointerReleasedEventArgs e) {
      if (pointer != e.Pointer) return;
      var delta = e.GetPosition(this) - start;
      bool click = !dragged && Math.Abs(delta.X) < 4 && Math.Abs(delta.Y) < 4 &&
         e.InitialPressMouseButton == MouseButton.Left && e.GetPosition(this).Y >= scroll.Bounds.Y;
      var at = e.GetPosition(image);
      CancelPan(); e.Handled = true;
      if (!click || !new Rect(image.Bounds.Size).Contains(at)) return;
      var hits = events.Where(target => target.Bounds.Contains(at)).ToArray();
      if (hits.Length == 1) EventRequested?.Invoke(this, hits[0]);
      else if (hits.Length > 1) {
         int generation = snapshotGeneration;
         var menu = new ContextMenu();
         var items = hits.Select(target => {
            var item = new MenuItem { Header = target.Label };
            item.Click += (_, _) => {
               if (generation == snapshotGeneration && ContainsEvent(target)) EventRequested?.Invoke(this, target);
            };
            return item;
         }).ToArray();
         menu.ItemsSource = items; eventMenu = menu;
         menu.Closed += (_, _) => {
            menu.ItemsSource = null;
            if (ReferenceEquals(eventMenu, menu)) eventMenu = null;
            InteractionStarted?.Invoke(this, EventArgs.Empty);
         };
         menu.Open(this);
      }
   }

   public void ShowEventInfo(string text, bool error = false) {
      eventInfo.Text = text; eventInfo.IsVisible = true;
      if (error) eventInfo.Foreground = Brushes.IndianRed;
      else eventInfo.ClearValue(TextBlock.ForegroundProperty);
   }

   private void CloseEventMenu() {
      var menu = eventMenu; eventMenu = null;
      if (menu != null) { menu.Close(); menu.ItemsSource = null; }
   }

   public void CancelPan() {
      CloseEventMenu();
      var old = pointer; pointer = null; dragged = false;
      if (old?.Captured == this) old.Capture(null);
   }
}

/// <summary>Visual overlay in the image's coordinate space; scrolling moves both together.</summary>
public sealed class ReferenceTileGrid : Control {
   public const int TileSize = 16;
   private static readonly Pen line = new(new SolidColorBrush(Color.FromArgb(64, 255, 255, 255)), 1);
   public override void Render(DrawingContext context) {
      base.Render(context);
      for (int x = TileSize; x < Bounds.Width; x += TileSize)
         context.DrawLine(line, new Point(x + .5, 0), new Point(x + .5, Bounds.Height));
      for (int y = TileSize; y < Bounds.Height; y += TileSize)
         context.DrawLine(line, new Point(0, y + .5), new Point(Bounds.Width, y + .5));
   }
}
