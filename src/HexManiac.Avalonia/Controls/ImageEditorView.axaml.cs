using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.Core.ViewModels;
using ModelPoint = HavenSoft.HexManiac.Core.Models.Point;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>
/// Ported from HexManiac.WPF/Controls/ImageEditorView.xaml.cs.
///
/// WPF had separate MouseLeftButtonDown / MouseRightButtonDown / MouseDown routed events;
/// Avalonia raises one PointerPressed, so the button is read off the pointer's properties and
/// dispatched to the same three view model paths (tool / pan / eye-dropper).
/// The KeyBindings WPF declared in XAML are built here so Ctrl maps to Cmd on macOS.
/// </summary>
public partial class ImageEditorView : UserControl {
   private ImageEditorViewModel ViewModel => DataContext as ImageEditorViewModel;
   private bool isPointerCaptured;
   private MouseButton capturedButton;
   private MouseButton physicalButton;

   public ImageEditorView() {
      InitializeComponent();
      AttachedToVisualTree += (sender, e) => Focus();
      DetachedFromVisualTree += (sender, e) => ClearPopups();

      ImageContainer.PointerPressed += ImagePointerPressed;
      ImageContainer.PointerMoved += (sender, e) => ViewModel?.Hover(Point(e));
      ImageContainer.PointerReleased += ImagePointerReleased;
      ImageContainer.PointerWheelChanged += WheelMouse;

      BuildKeyBindings();
   }

   private void BuildKeyBindings() {
      var cmd = HexContent.CommandModifier;

      bool CanvasOwnsKeys() {
         var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
         if (focused is TextBox || focused is ComboBox || focused is Slider || focused is PaletteControl) return false;
         return focused is not Visual visual || !visual.GetVisualAncestors().OfType<PaletteControl>().Any();
      }
      void Add(Key key, KeyModifiers modifiers, Func<ImageEditorViewModel, ICommand> select,
         object parameter = null, bool clipboard = false) {
         object Argument() => clipboard ? FileSystem : parameter;
         KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(key, modifiers), Command = new StubCommand {
            CanExecute = _ => CanvasOwnsKeys() && ViewModel != null && (!clipboard || FileSystem != null) && select(ViewModel).CanExecute(Argument()),
            Execute = _ => {
               if (CanvasOwnsKeys() && ViewModel != null && (!clipboard || FileSystem != null) && select(ViewModel).CanExecute(Argument()))
                  select(ViewModel).Execute(Argument());
            },
         }});
      }

      Add(Key.OemPlus, KeyModifiers.None, vm => vm.ZoomInCommand);
      Add(Key.OemMinus, KeyModifiers.None, vm => vm.ZoomOutCommand);
      Add(Key.Delete, KeyModifiers.None, vm => vm.DeleteCommand);

      Add(Key.A, KeyModifiers.None, vm => vm.SelectTool, ImageEditorTools.Pan);
      Add(Key.S, KeyModifiers.None, vm => vm.SelectTool, ImageEditorTools.Select);
      Add(Key.D, KeyModifiers.None, vm => vm.SelectTool, ImageEditorTools.Draw);
      Add(Key.F, KeyModifiers.None, vm => vm.SelectTool, ImageEditorTools.Fill);
      Add(Key.G, KeyModifiers.None, vm => vm.SelectTool, ImageEditorTools.EyeDropper);
      Add(Key.H, KeyModifiers.None, vm => vm.SelectTool, ImageEditorTools.TilePalette);

      Add(Key.C, cmd, vm => vm.Copy, clipboard: true);
      Add(Key.V, cmd, vm => vm.Paste, clipboard: true);
      Add(Key.A, cmd, vm => vm.SelectAll);

      Add(Key.Space, KeyModifiers.None, vm => vm.SelectColor, 0);
      for (int i = 0; i <= 7; i++) {
         Add(Key.D0 + i, KeyModifiers.None, vm => vm.SelectColor, i);
      }
      Add(Key.D1, KeyModifiers.Shift, vm => vm.SetCursorSize, 1);
      Add(Key.D2, KeyModifiers.Shift, vm => vm.SetCursorSize, 2);
      Add(Key.D3, KeyModifiers.Shift, vm => vm.SetCursorSize, 4);
      Add(Key.D4, KeyModifiers.Shift, vm => vm.SetCursorSize, 8);
   }

   private Core.Models.IFileSystem FileSystem {
      get {
         var top = TopLevel.GetTopLevel(this);
         if (top != null && top.TryFindResource("FileSystem", out var resource)) return resource as Core.Models.IFileSystem;
         return null;
      }
   }

   private ModelPoint Point(PointerEventArgs e) {
      var p = e.GetPosition(ImageContainer);
      p = new Avalonia.Point(p.X - ImageContainer.Bounds.Width / 2, p.Y - ImageContainer.Bounds.Height / 2);
      return new ModelPoint((int)p.X, (int)p.Y);
   }

   private void ImagePointerPressed(object sender, PointerPressedEventArgs e) {
      if (ViewModel == null) return;
      var properties = e.GetCurrentPoint(ImageContainer).Properties;
      if (isPointerCaptured) return;
      if (!properties.IsLeftButtonPressed && !properties.IsRightButtonPressed && !properties.IsMiddleButtonPressed) return;
      physicalButton = properties.IsMiddleButtonPressed ? MouseButton.Middle :
         properties.IsRightButtonPressed ? MouseButton.Right : MouseButton.Left;
      e.Pointer.Capture(ImageContainer);
      isPointerCaptured = true;

      if (properties.IsMiddleButtonPressed) {
         capturedButton = MouseButton.Middle;
         ViewModel.PanDown(Point(e));
      } else if (properties.IsRightButtonPressed || (OperatingSystem.IsMacOS() &&
         properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Control))) {
         capturedButton = MouseButton.Right;
         ViewModel.EyeDropperDown(Point(e));
      } else if (properties.IsLeftButtonPressed) {
         capturedButton = MouseButton.Left;
         PaletteControl.ClosePopup();
         ViewModel.ToolDown(Point(e), e.KeyModifiers == HexContent.CommandModifier);
         Focus();
      }
   }

   private void ImagePointerReleased(object sender, PointerReleasedEventArgs e) {
      if (!isPointerCaptured || ViewModel == null) return;
      if (e.InitialPressMouseButton != physicalButton) return;
      var point = Point(e);
      if (capturedButton == MouseButton.Middle) ViewModel.PanUp(point);
      else if (capturedButton == MouseButton.Right) ViewModel.EyeDropperUp(point);
      else ViewModel.ToolUp(point);
      e.Pointer.Capture(null);
      isPointerCaptured = false;
   }

   private void WheelMouse(object sender, PointerWheelEventArgs e) {
      if (ViewModel == null) return;
      var delta = e.Delta.Y;
      if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) {
         if (delta > 0) ViewModel.SpritePage = (ViewModel.SpritePage + 1) % ViewModel.SpritePages;
         if (delta < 0) ViewModel.SpritePage = ViewModel.SpritePage == 0 ? ViewModel.SpritePages - 1 : ViewModel.SpritePage - 1;
      } else {
         if (delta > 0) ViewModel.ZoomIn(Point(e));
         if (delta < 0) ViewModel.ZoomOut(Point(e));
      }
      e.Handled = true;
   }

   private void ClearPopups() {
      PaletteControl.ClosePopup();
      PaletteControl.SingleSelect();
      PaletteMixer.ClosePopup();
      PaletteMixer.SingleSelect();
   }
}
