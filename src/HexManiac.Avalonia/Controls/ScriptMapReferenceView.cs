using System;
using System.Linq;
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
   private readonly Image image;
   private readonly ScrollViewer scroll;
   private WriteableBitmap bitmap;
   private IPointer pointer;
   private Point start;
   private Vector startOffset;
   public event EventHandler InteractionStarted;
   public Vector Offset { get => scroll.Offset; set => scroll.Offset = value; }
   public bool IsPanning => pointer != null;

   public ScriptMapReferenceView() {
      RowDefinitions = new RowDefinitions("Auto,*");
      ClipToBounds = true;
      Focusable = false;
      caption = new TextBlock { Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap };
      image = new Image { Stretch = Stretch.None, HorizontalAlignment = HorizontalAlignment.Left,
         VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false, Focusable = false };
      RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.None);
      scroll = new ScrollViewer { Content = image, Focusable = false, Background = Brushes.Transparent,
         HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
      Children.Add(caption); Children.Add(scroll); SetRow(scroll, 1);
      AddHandler(PointerPressedEvent, BeginPan, RoutingStrategies.Tunnel);
      AddHandler(PointerMovedEvent, Pan, RoutingStrategies.Tunnel);
      AddHandler(PointerReleasedEvent, (_, e) => { if (pointer == e.Pointer) { CancelPan(); e.Handled = true; } }, RoutingStrategies.Tunnel);
      PointerCaptureLost += (_, _) => CancelPan();
      AddHandler(PointerWheelChangedEvent, (_, e) => {
         InteractionStarted?.Invoke(this, EventArgs.Empty);
         scroll.Offset -= new Vector(e.Delta.X * 32, e.Delta.Y * 32);
         e.Handled = true;
      }, RoutingStrategies.Tunnel);
      DetachedFromVisualTree += (_, _) => Clear();
   }

   public void ShowSnapshot(ScriptMapSnapshot snapshot) {
      var next = PixelBuffer.EnsureSize(null, snapshot.Image.PixelWidth, snapshot.Image.PixelHeight);
      PixelBuffer.Write555(next, snapshot.Image.PixelData, snapshot.Image.PixelWidth, snapshot.Image.PixelHeight);
      image.Width = snapshot.Image.PixelWidth; image.Height = snapshot.Image.PixelHeight;
      image.Source = next;
      bitmap?.Dispose(); bitmap = next;
      caption.Text = snapshot.Caption + " — drag or scroll to pan";
   }

   public void Clear(string message = "") {
      CancelPan(); image.Source = null; bitmap?.Dispose(); bitmap = null;
      caption.Text = message;
   }

   private void BeginPan(object sender, PointerPressedEventArgs e) {
      // Scrollbar thumbs keep their normal behavior; clicking artwork only starts a pan.
      if (e.Source is Visual visual && (visual is ScrollBar || visual.GetVisualAncestors().OfType<ScrollBar>().Any())) return;
      if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
      InteractionStarted?.Invoke(this, EventArgs.Empty);
      start = e.GetPosition(this); startOffset = scroll.Offset; pointer = e.Pointer;
      pointer.Capture(this); e.Handled = true;
   }

   private void Pan(object sender, PointerEventArgs e) {
      if (pointer != e.Pointer) return;
      if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) { CancelPan(); return; }
      var delta = e.GetPosition(this) - start;
      scroll.Offset = startOffset - new Vector(delta.X, delta.Y);
      e.Handled = true;
   }

   public void CancelPan() {
      var old = pointer; pointer = null;
      if (old?.Captured == this) old.Capture(null);
   }
}
