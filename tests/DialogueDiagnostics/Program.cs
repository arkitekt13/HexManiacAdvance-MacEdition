using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Review;
using Point = Avalonia.Point;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
using var f = new WorkflowFixture();
Application.Current!.Resources["FileSystem"] = f.FileSystem;
App.UpdateThemeDictionary(Application.Current.Resources, new EditorViewModel(f.FileSystem, InstantDispatch.Instance).Theme);
// Distinct colors make the regression check pixels rather than diagnostic/decorated-run counts.
Application.Current.Resources["Data1"] = Brushes.Yellow;
Application.Current.Resources["Error"] = Brushes.Red;
var editor = new TextEditor { FontSize = 20, DataContext = f.Body.Editor };
var window = new Window { Content = editor, Width = 900, Height = 300 }; window.Show();
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
void Pump() { Dispatcher.UIThread.RunJobs(); window.Measure(new Size(window.Width, window.Height)); window.Arrange(new Rect(0, 0, window.Width, window.Height)); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
byte[] Pixels() {
   Pump();
   using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Width, (int)window.Height)); bitmap.Render(editor);
   var bytes = new byte[(int)window.Width * (int)window.Height * 4]; var pin = GCHandle.Alloc(bytes, GCHandleType.Pinned);
   try { bitmap.CopyPixels(new PixelRect(0, 0, (int)window.Width, (int)window.Height), pin.AddrOfPinnedObject(), bytes.Length, (int)window.Width * 4); }
   finally { pin.Free(); }
   return bytes;
}
List<Point> Colored(byte[] pixels, bool warning) {
   var result = new List<Point>();
   for (int i = 0; i < pixels.Length; i += 4) {
      // Accept Skia's RGBA/BGRA surfaces: saturated red/yellow differs from syntax colors.
      if (Math.Min(pixels[i], pixels[i + 2]) < 70 && Math.Max(pixels[i], pixels[i + 2]) > 180 && (warning ? pixels[i + 1] > 180 : pixels[i + 1] < 70))
         result.Add(new Point(i / 4 % (int)window.Width, i / 4 / (int)window.Width));
   }
   return result;
}
const string dialogue = "Talk to me! It's great to meet you Michael my friend!";
foreach (var separator in new[] { "\n", "\r\n" }) {
   var source = ("section0: # 71BF40\n  msgbox.npc <auto>\n{\n" + dialogue + "\n}\n  end").Replace("\n", separator);
   f.Edit(source); editor.DataContext = f.Body.Editor; Pump();
   var state = f.State(); var diagnostics = f.Body.Editor.ErrorLocations.ToArray(); var retainedSource = f.Body.Content;
   Check(diagnostics.Any(d => d.Type == SegmentType.Warning) && !f.Body.HasError, "The screenshot's exact script has a width warning, not a compile error.");
   var warning = diagnostics.Single(d => d.Type == SegmentType.Warning);
   var pixels = Colored(Pixels(), true);
   Check(pixels.Count > 30, "Overflow must produce visible yellow pixels over the previously blank syntax placeholders.");
   var presenter = editor.EditBox.GetVisualDescendants().OfType<TextPresenter>().Single();
   var index = editor.EditBox.Text!.IndexOf(dialogue, StringComparison.Ordinal) + warning.Start;
   var rect = presenter.TextLayout.HitTestTextPosition(index);
   var origin = presenter.TranslatePoint(default, editor)!.Value;
   Check(pixels.All(p => p.Y >= origin.Y + rect.Bottom - 4 && p.Y <= origin.Y + rect.Bottom + 1), "The underline belongs below the overflowing dialogue row.");
   Check(pixels.Min(p => p.X) >= origin.X + rect.Left - 2, "The underline starts at the overflowing suffix, not at column zero.");
   Check(state.EqualsModel(f) && diagnostics.SequenceEqual(f.Body.Editor.ErrorLocations) && f.Body.Content == retainedSource, "Rendering preserves ROM bytes, metadata, diagnostics and source.");
   Check(!editor.FindControl<DiagnosticUnderlineLayer>("DiagnosticLayer")!.IsHitTestVisible, "Warnings cannot intercept typing or selection.");
   window.Width = 500; window.Height = 120; Pump();
   var unscrolled = Colored(Pixels(), true);
   var scroll = editor.EditBox.GetVisualDescendants().OfType<ScrollViewer>().Single();
   scroll.Offset = new Vector(100, 20); Pump();
   var moved = Colored(Pixels(), true);
   Check(moved.Count > 10 && Math.Abs(moved.Min(p => p.X) - (unscrolled.Min(p => p.X) - scroll.Offset.X)) < 3,
      "Horizontal scrolling keeps the underline attached to its text.");
   Check(Math.Abs(moved.Min(p => p.Y) - (unscrolled.Min(p => p.Y) - scroll.Offset.Y)) < 3,
      "Vertical scrolling keeps the underline attached to its row.");
   window.Width = 900; window.Height = 300; scroll.Offset = default; Pump();
   f.Edit(source.Replace(dialogue, "Hello!")); Pump();
   Check(Colored(Pixels(), true).Count == 0 && !f.Body.Editor.ErrorLocations.Any(d => d.Type == SegmentType.Warning), "Correction removes both the warning and visible underline.");
   f.Edit(source.Replace(dialogue, "Talk to me! It's great" + separator + "to meet you Michael" + separator + "my friend!")); Pump();
   Check(Colored(Pixels(), true).Count == 0, "Manual Enter breaks resolve overflow without a popup or automatic rewriting.");
}

// A whitespace-only line, overlapping/unsorted diagnostics and stale ranges cannot hide or
// crash the painter; errors share the same glyph-independent rendering path.
var vm = new TextEditorViewModel { Content = "lock\r\n          \nend" };
vm.ErrorLocations.Add(new TextSegment(1, 2, 6, SegmentType.Warning));
vm.ErrorLocations.Add(new TextSegment(0, 0, 4, SegmentType.Error));
vm.ErrorLocations.Add(new TextSegment(99, 0, 100, SegmentType.Error));
vm.ErrorLocations.Add(new TextSegment(0, -1, 3, SegmentType.Error));
editor.DataContext = vm; Pump();
Check(Colored(Pixels(), true).Count > 10, "Whitespace-only diagnostic ranges remain visible.");
Check(Colored(Pixels(), false).Count > 10, "Compile-error ranges produce visible red pixels.");
var before = vm.Content; editor.EditBox.Focus();
vm.ErrorLocations.Clear(); Pump();
Check(Colored(Pixels(), true).Count == 0 && Colored(Pixels(), false).Count == 0, "Clearing diagnostics removes every old underline.");
Check(vm.Content == before && editor.EditBox.IsFocused, "Updating underlines preserves text and keyboard focus.");

// Characterization of existing PCS encoding: no changes to the game's line/page controls.
var single = PCSString.Convert("First\nSecond");
var scrolling = PCSString.Convert("First\nSecond\nThird");
var paragraph = PCSString.Convert("First\n\nSecond");
Check(single.Contains((byte)0xFE) && !single.Contains((byte)0xFB), "A single Enter encodes a line break.");
Check(scrolling.Contains((byte)0xFA), "Subsequent single breaks encode scrolling lines.");
Check(paragraph.Contains((byte)0xFB), "A blank line encodes a paragraph/page break.");
window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
