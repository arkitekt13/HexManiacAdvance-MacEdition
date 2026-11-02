using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.ViewModels;

// Exercise the real Skia text renderer without starting a window or loading a ROM.
AppBuilder.Configure<Application>().UseStandardRuntimePlatformSubsystem()
   .UseWindowingSubsystem(() => { }, "offscreen").UseSkia().SetupWithoutStarting();

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
Console.WriteLine("PASS: reproduced zero-width placeholders; checked monospaced font, all six display layers, token positions, comments, line endings, and unchanged editable text.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
