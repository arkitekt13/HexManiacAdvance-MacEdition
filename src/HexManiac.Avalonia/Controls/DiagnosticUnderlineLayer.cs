using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI.Resources;
using HavenSoft.HexManiac.Core.ViewModels;
using CoreTheme = HavenSoft.HexManiac.Core.ViewModels.Theme;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// Paint diagnostics independently of glyphs: Avalonia omits text decorations on the
/// trailing placeholder spaces used by the editor's syntax layers.
public sealed class DiagnosticUnderlineLayer : Control {
   public TextBox Target { get; set; }
   public IReadOnlyList<TextSegment> Diagnostics { get; private set; } = Array.Empty<TextSegment>();

   public void SetDiagnostics(IEnumerable<TextSegment> diagnostics) {
      Diagnostics = diagnostics.ToArray();
      InvalidateVisual();
   }

   public override void Render(DrawingContext context) {
      base.Render(context);
      if (Target == null || Diagnostics.Count == 0) return;
      var presenter = Target.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
      var origin = presenter?.TranslatePoint(default, this);
      if (origin == null) return;
      var viewport = presenter.GetVisualAncestors().OfType<ScrollContentPresenter>().FirstOrDefault();
      var viewportOrigin = viewport?.TranslatePoint(default, this);
      var clip = viewportOrigin is Point p ? new Rect(p, viewport.Bounds.Size) : new Rect(Bounds.Size);
      var text = Target.Text ?? string.Empty;
      // Preserve source offsets for LF, CRLF and mixed input.
      var lines = new List<(int Start, int Length)>();
      int start = 0;
      for (int i = 0; i < text.Length; i++) {
         if (text[i] != '\r' && text[i] != '\n') continue;
         lines.Add((start, i - start));
         if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
         start = i + 1;
      }
      lines.Add((start, text.Length - start));
      using (context.PushClip(clip)) foreach (var diagnostic in Diagnostics) {
         if (diagnostic.Line < 0 || diagnostic.Line >= lines.Count || diagnostic.Start < 0 || diagnostic.Length <= 0) continue;
         var line = lines[diagnostic.Line];
         if (diagnostic.Start >= line.Length) continue;
         var length = Math.Min(diagnostic.Length, line.Length - diagnostic.Start);
         var first = presenter.TextLayout.HitTestTextPosition(line.Start + diagnostic.Start);
         var last = presenter.TextLayout.HitTestTextPosition(line.Start + diagnostic.Start + length - 1);
         var color = diagnostic.Type switch {
            SegmentType.Warning => nameof(CoreTheme.Data1), SegmentType.Error => nameof(CoreTheme.Error), _ => null,
         };
         if (color == null) continue;
         var pen = new Pen(ThemeDictionary.Brush(color), 1.5);
         double left = origin.Value.X + first.Left, right = origin.Value.X + last.Right;
         double y = origin.Value.Y + first.Bottom - 2;
         // A real path also remains visible under blank text and trailing whitespace.
         for (double x = left; x < right; x += 4) {
            context.DrawLine(pen, new Point(x, y), new Point(Math.Min(x + 2, right), y + 1));
            if (x + 2 < right) context.DrawLine(pen, new Point(x + 2, y + 1), new Point(Math.Min(x + 4, right), y));
         }
      }
   }
}
