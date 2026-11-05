using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using HavenSoft.HexManiac.Core.ViewModels.Images;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>A bounded map-owned image preview that cannot intercept dropdown input.</summary>
public sealed class BlocksetHoverPreview {
   readonly Canvas layer;
   readonly Viewbox box;
   readonly PixelImage image;
   Control owner;

   public BlocksetHoverPreview(Canvas layer, Viewbox box, PixelImage image) {
      (this.layer, this.box, this.image) = (layer, box, image);
      layer.SizeChanged += (_, _) => { if (owner != null) Place(); };
      box.SizeChanged += (_, _) => { if (owner != null) Place(); };
   }

   public void Show(Control row, IPixelViewModel pixels) {
      Hide();
      if (pixels == null) return;
      owner = row;
      owner.PointerExited += Leave;
      owner.PointerWheelChanged += Scroll;
      owner.DataContextChanged += ContextChanged;
      owner.DetachedFromVisualTree += Detached;
      image.DataContext = pixels;
      box.IsVisible = true;
      Place();
   }

   public void Hide() {
      if (owner != null) {
         owner.PointerExited -= Leave;
         owner.PointerWheelChanged -= Scroll;
         owner.DataContextChanged -= ContextChanged;
         owner.DetachedFromVisualTree -= Detached;
      }
      owner = null;
      box.IsVisible = false;
      image.DataContext = null;
   }

   void Leave(object sender, PointerEventArgs e) => Hide();
   void Scroll(object sender, PointerWheelEventArgs e) => Hide();
   void ContextChanged(object sender, EventArgs e) => Hide();
   void Detached(object sender, VisualTreeAttachmentEventArgs e) => Hide();

   void Place() {
      const double margin = 4;
      double width = Math.Max(1, layer.Bounds.Width - margin * 2);
      double height = Math.Max(1, layer.Bounds.Height - margin * 2);
      box.MaxWidth = Math.Min(400, width);
      box.MaxHeight = height;
      box.Measure(new Size(width, height));
      // Dropdown rows may live in a separate popup root; convert through screen coordinates.
      var point = layer.PointToClient(owner.PointToScreen(default));
      Canvas.SetLeft(box, margin);
      Canvas.SetTop(box, Math.Clamp(point.Y, margin, Math.Max(margin, layer.Bounds.Height - box.DesiredSize.Height - margin)));
   }
}
