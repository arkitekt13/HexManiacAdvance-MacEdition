using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>
/// A map-owned, input-transparent preview. A pointer-positioned ToolTip can cover the
/// pointer after fitting a tall summary, dismiss itself on map exit, then reopen forever.
/// Core returns null for the same cell; keep that content for deliberate map re-entry.
/// </summary>
public sealed class MapHoverPreview {
   private readonly Control surface;
   private readonly Canvas overlay;
   private readonly HexContentToolTip content = new() { IsHitTestVisible = false };
   private readonly Viewbox preview;
   private object[] lastContent = Array.Empty<object>();
   private Rect anchor;

   public MapHoverPreview(Control surface, Canvas overlay) {
      this.surface = surface;
      this.overlay = overlay;
      overlay.IsHitTestVisible = false;
      overlay.ClipToBounds = true;
      preview = new Viewbox { Child = content, Stretch = Stretch.Uniform,
         StretchDirection = StretchDirection.DownOnly, IsHitTestVisible = false, IsVisible = false };
      overlay.Children.Add(preview);
      overlay.SizeChanged += (_, _) => { if (preview.IsVisible) Place(); };
      preview.SizeChanged += (_, _) => { if (preview.IsVisible) Place(); };
      surface.DetachedFromVisualTree += (_, _) => Hide();
      surface.DataContextChanged += (_, _) => { lastContent = Array.Empty<object>(); Hide(); };
   }

   public bool OwnsSource(object source) => source is Visual visual &&
      (ReferenceEquals(visual, surface) || visual.GetVisualAncestors().Contains(surface));

   public void Hide() => preview.IsVisible = false;

   public void Update(object value, Rect? cell = null) {
      var tooltip = value == null ? lastContent : value as object[];
      if (tooltip == null) return;
      bool changed = !ContentMatches(tooltip);
      bool moved = cell is Rect next && next != anchor;
      if (cell is Rect bounds) anchor = bounds;
      lastContent = tooltip;
      if (tooltip.Length == 0) { Hide(); return; }
      if (changed) content.DataContext = tooltip;
      if (changed || moved || !preview.IsVisible) {
         preview.IsVisible = true;
         Place();
      }
   }

   private void Place() {
      const double margin = 4, gap = 12;
      var width = Math.Max(1, overlay.Bounds.Width - margin * 2);
      var height = Math.Max(1, overlay.Bounds.Height - margin * 2);
      content.MaxWidth = Math.Min(400, width);
      // Fit the complete summary, including every image/text row, without cropping it.
      preview.MaxWidth = width;
      preview.MaxHeight = height;
      preview.Measure(new Size(width, height));
      var size = preview.DesiredSize;
      double x = anchor.Right + gap;
      if (x + size.Width > overlay.Bounds.Width - margin) x = anchor.Left - gap - size.Width;
      x = Math.Clamp(x, margin, Math.Max(margin, overlay.Bounds.Width - margin - size.Width));
      double y = Math.Clamp(anchor.Top, margin, Math.Max(margin, overlay.Bounds.Height - margin - size.Height));
      Canvas.SetLeft(preview, x);
      Canvas.SetTop(preview, y);
   }

   private bool ContentMatches(object[] tooltip) {
      if (lastContent.Length != tooltip.Length) return false;
      for (int i = 0; i < tooltip.Length; i++) {
         if (!Equals(lastContent[i], tooltip[i])) return false;
      }
      return true;
   }
}
