using System.Globalization;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.ViewModels;

// Exercise the real Skia text renderer in a headless window without loading a ROM.
AppBuilder.Configure<Application>().UseStandardRuntimePlatformSubsystem()
   .UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();

var font = new Typeface("Menlo, Consolas, monospace");
double Width(string text) => new FormattedText(text, CultureInfo.InvariantCulture,
   FlowDirection.LeftToRight, font, 12, Brushes.White).WidthIncludingTrailingWhitespace;
var checks = 0;
void Check(bool condition, string message) {
   checks++;
   if (!condition) throw new InvalidOperationException(message);
}

Check(Width("\0\0\0") == 0, "Expected to reproduce zero-width NUL placeholders.");
Check(Math.Abs(Width("compare") - Width("       ")) < .001, "Font must be monospaced.");
Check(SyntaxLayerTextConverter.Normalize("a\0\t\r\nb") == "a \t\r\nb",
   "Display conversion must preserve indices, tabs and line endings.");
Check(SyntaxLayerTextConverter.Normalize(null) == "", "Empty layer must be safe.");

const string content = "# NPC interaction\nlock\ncompare 0x4002 0x0002\nif1 == <section3>\n\nrelease\nend";
var vm = new TextEditorViewModel { LineCommentHeader = "#" };
foreach (var keyword in new[] { "lock", "compare", "if1", "release", "end" }) vm.Keywords.Add(keyword);
vm.Constants.Add("section3");
vm.Content = content;
var editor = new TextEditor { FontSize = 12, DataContext = vm };
var panel = new StackPanel { Children = { editor } };
var host = new Window { Content = panel, Width = 1200, Height = 800 }; host.Show();
Dispatcher.UIThread.RunJobs();

var layers = new Dictionary<string, string> {
   ["BasicLayer"] = vm.PlainContent, ["AccentLayer"] = vm.AccentContent,
   ["ConstantsLayer"] = vm.ConstantContent, ["NumericLayer"] = vm.NumericContent,
   ["TextLayer"] = vm.TextContent, ["CommentLayer"] = vm.CommentContent,
};
foreach (var (name, raw) in layers) {
   var layer = editor.FindControl<TextBlock>(name);
   var actual = name == "CommentLayer"
      ? string.Concat(layer.Inlines.OfType<Run>().Select(run => run.Text)) : layer.Text;
   var normalized = SyntaxLayerTextConverter.Normalize(raw);
   Check(actual == normalized, $"{name}: the control must display the normalized layer.");
   Check(layer.FontFamily.Equals(editor.EditBox.FontFamily), $"{name}: font must match the caret layer.");
   Check(layer.FontSize == editor.EditBox.FontSize, $"{name}: font size must match the caret layer.");
   Check(normalized.Length == content.Length, $"{name}: character indices changed.");
   for (int i = 0; i < content.Length; i++) {
      if (char.IsWhiteSpace(normalized[i])) continue;
      var lineStart = content.LastIndexOf('\n', i) + 1;
      Check(Math.Abs(Width(content[lineStart..i]) - Width(normalized[lineStart..i])) < .001,
         $"{name}: token at index {i} is horizontally misplaced.");
   }
}
Check(vm.Content == content && editor.EditBox.Text == content, "The editable script must remain unchanged.");
// A single validation pass can publish dozens of diagnostics synchronously.
// Count actual inline-list clears to detect repeated rebuilding of the visible document.
var diagnosticVm = new TextEditorViewModel { LineCommentHeader = "#" };
diagnosticVm.Content = string.Join("\n", Enumerable.Range(0, 32).Select(i => "# warning " + i));
var diagnosticEditor = new TextEditor { FontSize = 12, DataContext = diagnosticVm };
panel.Children.Add(diagnosticEditor);
Dispatcher.UIThread.RunJobs();
var commentLayer = diagnosticEditor.FindControl<TextBlock>("CommentLayer");
var decorationRebuilds = 0;
commentLayer.Inlines.CollectionChanged += (_, _) => {
   if (commentLayer.Inlines.Count == 0) decorationRebuilds++;
};
diagnosticVm.ErrorLocations.Clear();
for (int i = 0; i < 32; i++) diagnosticVm.ErrorLocations.Add(new TextSegment(i, 2, 7,
   i % 2 == 0 ? SegmentType.Error : SegmentType.Warning));
var diagnostics = diagnosticVm.ErrorLocations.ToArray();
Dispatcher.UIThread.RunJobs();
Console.WriteLine($"DIAGNOSTIC_REBUILDS:{decorationRebuilds}");
Check(decorationRebuilds == 1, "One diagnostic burst must rebuild error decorations once.");
var diagnosticLayer = diagnosticEditor.FindControl<DiagnosticUnderlineLayer>("DiagnosticLayer");
Check(diagnosticLayer.Diagnostics.Count == 32,
   "Every error and warning must remain decorated after coalescing.");
Check(string.Concat(commentLayer.Inlines.OfType<Run>().Select(run => run.Text)) == diagnosticVm.CommentContent,
   "Diagnostic decorations must preserve every character and line break.");
Check(diagnosticVm.ErrorLocations.SequenceEqual(diagnostics), "Rendering must preserve all diagnostic records.");
Check(diagnosticEditor.EditBox.Text == diagnosticVm.Content, "Rendering must not change editable text.");

decorationRebuilds = 0;
diagnosticVm.ErrorLocations.Clear();
Dispatcher.UIThread.RunJobs();
Check(decorationRebuilds == 1 && diagnosticLayer.Diagnostics.Count == 0,
   "Cleared diagnostics must remove all old underlines in one update.");

// Switch scripts while an update is queued: only the current model may be rendered.
decorationRebuilds = 0;
diagnosticVm.ErrorLocations.Add(new TextSegment(0, 2, 7, SegmentType.Warning));
var replacementVm = new TextEditorViewModel { LineCommentHeader = "#", Content = "# current script" };
replacementVm.ErrorLocations.Add(new TextSegment(0, 2, 7, SegmentType.Error));
diagnosticEditor.DataContext = replacementVm;
Dispatcher.UIThread.RunJobs();
Check(decorationRebuilds == 1, "A context switch during a diagnostic burst must render the current script once.");
Check(string.Concat(commentLayer.Inlines.OfType<Run>().Select(run => run.Text)) == replacementVm.CommentContent,
   "Queued updates must use the current script, not the previous model.");
Check(diagnosticLayer.Diagnostics.SequenceEqual(replacementVm.ErrorLocations),
   "The replacement model's diagnostic must be displayed.");
decorationRebuilds = 0;
diagnosticVm.ErrorLocations.Clear();
Dispatcher.UIThread.RunJobs();
Check(decorationRebuilds == 0, "The old script must be unsubscribed after switching models.");

host.Content = null; host.Close();
Console.WriteLine("PASS: checked script layout, diagnostic coalescing, complete underlines, unchanged content, and context switching.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
