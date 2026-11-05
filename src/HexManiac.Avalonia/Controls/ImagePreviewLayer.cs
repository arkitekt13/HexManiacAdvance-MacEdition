using System;
using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.Core.ViewModels.Images;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>Window-owned image hints that fit complete content and pass input to their source.</summary>
public sealed class ImagePreviewLayer : Canvas {
   private static readonly AttachedProperty<ImagePreviewLayer> LayerProperty =
      AvaloniaProperty.RegisterAttached<ImagePreviewLayer, TopLevel, ImagePreviewLayer>("ImagePreviewLayer");
   private readonly HexContentToolTip content = new() { IsHitTestVisible = false, DataContext = null };
   private readonly Viewbox box;
   private Control owner;
   private Rect anchor;
   private Point? placedAt;
   private bool anchorToBounds;
   private Action dismissed;
   private TopLevel top;

   public ImagePreviewLayer() {
      IsHitTestVisible = false;
      ClipToBounds = true;
      box = new Viewbox { Child = content, Stretch = Stretch.Uniform,
         StretchDirection = StretchDirection.DownOnly,
         IsHitTestVisible = false, IsVisible = false };
      Children.Add(box);
      SizeChanged += (_, _) => { if (owner != null) Place(); };
      box.SizeChanged += (_, _) => { if (owner != null) Place(); };
      AttachedToVisualTree += (_, _) => Attach();
      DetachedFromVisualTree += (_, _) => Detach();
   }

   public static ImagePreviewLayer For(Control control) => TopLevel.GetTopLevel(control)?.GetValue(LayerProperty);
   public static bool HasImages(object value) => value is IEnumerable items && items.Cast<object>().Any(item => item is IPixelViewModel);
   public bool IsShowing(Control control) => ReferenceEquals(owner, control) && box.IsVisible;

   public void Show(Control source, object value, Rect? sourceAnchor = null, Action onDismissed = null) {
      if (!HasImages(value) || !source.IsEffectivelyVisible) { Hide(source); return; }
      if (!ReferenceEquals(owner, source)) {
         Hide();
         owner = source;
         owner.PointerExited += OwnerExited;
         owner.DataContextChanged += OwnerContextChanged;
         owner.DetachedFromVisualTree += OwnerDetached;
         owner.LayoutUpdated += OwnerLayout;
      }
      anchorToBounds = sourceAnchor == null;
      anchor = sourceAnchor ?? new Rect(source.Bounds.Size);
      dismissed = onDismissed;
      content.DataContext = value;
      box.IsVisible = true;
      Place();
   }

   public void Hide(Control source = null) {
      if (source != null && !ReferenceEquals(source, owner)) return;
      var callback = dismissed;
      dismissed = null;
      if (owner != null) {
         owner.PointerExited -= OwnerExited;
         owner.DataContextChanged -= OwnerContextChanged;
         owner.DetachedFromVisualTree -= OwnerDetached;
         owner.LayoutUpdated -= OwnerLayout;
      }
      owner = null;
      placedAt = null;
      box.IsVisible = false;
      content.DataContext = null;
      callback?.Invoke();
   }

   private void Place() {
      if (owner == null) return;
      if (anchorToBounds) anchor = new Rect(owner.Bounds.Size);
      var point = owner.TranslatePoint(anchor.Position, this);
      if (point == null || !owner.IsEffectivelyVisible) { Hide(); return; }
      placedAt = point;
      const double margin = 4, gap = 12;
      var width = Math.Max(1, Bounds.Width - margin * 2);
      var height = Math.Max(1, Bounds.Height - margin * 2);
      // Leave images at natural size inside the Viewbox; limiting the content itself clips
      // wide/tall children instead of uniformly fitting the complete text/image collection.
      box.MaxWidth = Math.Min(400, width);
      box.MaxHeight = height;
      box.Measure(new Size(width, height));
      var size = box.DesiredSize;
      double x = point.Value.X + anchor.Width + gap;
      if (x + size.Width > Bounds.Width - margin) x = point.Value.X - gap - size.Width;
      x = Math.Clamp(x, margin, Math.Max(margin, Bounds.Width - size.Width - margin));
      double y = Math.Clamp(point.Value.Y, margin, Math.Max(margin, Bounds.Height - size.Height - margin));
      SetLeft(box, x);
      SetTop(box, y);
   }

   private void Attach() {
      top = TopLevel.GetTopLevel(this);
      if (top == null) return;
      top.SetValue(LayerProperty, this);
      top.AddHandler(PointerPressedEvent, DismissPointer, RoutingStrategies.Tunnel, true);
      top.AddHandler(PointerWheelChangedEvent, DismissWheel, RoutingStrategies.Tunnel, true);
      top.AddHandler(KeyDownEvent, DismissKey, RoutingStrategies.Tunnel, true);
      if (top is Window window) window.Deactivated += Deactivated;
   }

   private void Detach() {
      Hide();
      if (top == null) return;
      if (ReferenceEquals(top.GetValue(LayerProperty), this)) top.ClearValue(LayerProperty);
      top.RemoveHandler(PointerPressedEvent, DismissPointer);
      top.RemoveHandler(PointerWheelChangedEvent, DismissWheel);
      top.RemoveHandler(KeyDownEvent, DismissKey);
      if (top is Window window) window.Deactivated -= Deactivated;
      top = null;
   }

   private void DismissPointer(object sender, PointerPressedEventArgs e) => Hide();
   private void DismissWheel(object sender, PointerWheelEventArgs e) => Hide();
   private void DismissKey(object sender, KeyEventArgs e) => Hide();
   private void Deactivated(object sender, EventArgs e) => Hide();
   private void OwnerExited(object sender, PointerEventArgs e) => Hide();
   private void OwnerContextChanged(object sender, EventArgs e) => Hide();
   private void OwnerDetached(object sender, VisualTreeAttachmentEventArgs e) => Hide();
   private void OwnerLayout(object sender, EventArgs e) {
      if (owner == null) return;
      if (!owner.IsEffectivelyVisible) { Hide(); return; }
      if (placedAt != owner.TranslatePoint(anchor.Position, this) || anchorToBounds && anchor.Size != owner.Bounds.Size) Place();
   }
}
