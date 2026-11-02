using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>
/// Keep NPC previews scoped to the map surface rather than its surrounding panels.
/// Core returns null for an unchanged cell, so hiding a preview must retain its content
/// for re-entry while removing the attached tip that Avalonia could reopen elsewhere.
/// </summary>
public sealed class MapHoverPreview {
   private readonly Control surface;
   private object[] lastContent = Array.Empty<object>();

   public MapHoverPreview(Control surface) => this.surface = surface;

   public bool OwnsSource(object source) => source is Visual visual &&
      (ReferenceEquals(visual, surface) || visual.GetVisualAncestors().Contains(surface));

   public void Hide() {
      ToolTip.SetIsOpen(surface, false);
      ToolTip.SetTip(surface, null);
   }

   public void Update(object content) {
      var tooltip = content == null ? lastContent : content as object[];
      if (tooltip == null) return;
      if (ContentMatches(tooltip) && ToolTip.GetIsOpen(surface)) return;
      lastContent = tooltip;
      if (tooltip.Length == 0) {
         Hide();
         return;
      }
      if (ToolTip.GetTip(surface) is HexContentToolTip existing) {
         existing.DataContext = tooltip;
      } else {
         ToolTip.SetTip(surface, new HexContentToolTip { DataContext = tooltip });
      }
      ToolTip.SetIsOpen(surface, true);
   }

   private bool ContentMatches(object[] tooltip) {
      if (lastContent.Length != tooltip.Length) return false;
      for (int i = 0; i < tooltip.Length; i++) {
         if (!Equals(lastContent[i], tooltip[i])) return false;
      }
      return true;
   }
}
