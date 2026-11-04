using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var fs = new StubFileSystem(); var host = new EditorViewModel(fs, InstantDispatch.Instance);
Application.Current!.Resources["FileSystem"] = fs; App.UpdateThemeDictionary(Application.Current.Resources, host.Theme);
int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
var actual = new CountedEditor { FontSize = 14 }; var legacy = new ImmediateEditor { FontSize = 14 };
var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") }; Grid.SetColumn(legacy, 1); grid.Children.Add(actual); grid.Children.Add(legacy);
var window = new Window { Width = 1400, Height = 650, Content = grid }; window.Show();
void Drain() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); }
TextEditorViewModel Model(string text) { var vm = new TextEditorViewModel { LineCommentHeader = "#" }; vm.Keywords.Add("lock"); vm.Constants.Add("CONST"); vm.Content = text; return vm; }
var model = Model("lock # initial"); var reference = Model(model.Content); actual.DataContext = model; legacy.DataContext = reference; Drain();
string[] Names = { "BasicLayer", "AccentLayer", "ConstantsLayer", "NumericLayer", "CommentLayer", "TextLayer" };
TextBlock[] Layers(TextEditor editor) => Names.Select(n => editor.FindControl<TextBlock>(n)!).ToArray();
ScrollViewer Scroll(TextEditor editor) => editor.EditBox.GetVisualDescendants().OfType<ScrollViewer>().First(v => v.Name == "PART_ScrollViewer");
bool Near(double a, double b) => Math.Abs(a - b) < .01 || double.IsNaN(a) && double.IsNaN(b);
void Compare(string context) {
   Check(actual.EditBox.Text == legacy.EditBox.Text && actual.EditBox.Text == model.Content, context + ": exact source.");
   var left = Layers(actual); var right = Layers(legacy);
   for (int i = 0; i < 6; i++) {
      string Display(TextBlock layer) => i == 4 ? string.Concat(layer.Inlines.OfType<Run>().Select(r => r.Text)) : layer.Text ?? "";
      Check(Display(left[i]) == Display(right[i]), context + ": displayed layer " + i);
      Check(Near(left[i].Width, right[i].Width), $"{context}: width layer {i}: {left[i].Width} / {right[i].Width}");
      var a = (TranslateTransform)left[i].RenderTransform!; var b = (TranslateTransform)right[i].RenderTransform!;
      Check(Near(a.X, b.X) && Near(a.Y, b.Y), context + ": scroll transform " + i);
   }
   var ac = actual.FindControl<Control>("CornerCover")!; var bc = legacy.FindControl<Control>("CornerCover")!;
   Check(Near(ac.Width, bc.Width) && Near(ac.Height, bc.Height), context + ": scrollbar corner.");
   Check(Scroll(actual).Offset == Scroll(legacy).Offset && Scroll(actual).Extent == Scroll(legacy).Extent, context + ": native scroll geometry.");
   Check(actual.EditBox.CaretIndex == legacy.EditBox.CaretIndex && actual.EditBox.SelectionStart == legacy.EditBox.SelectionStart && actual.EditBox.SelectionEnd == legacy.EditBox.SelectionEnd, context + ": caret and selection.");
}
// A counter surrounds actual measurements, not TextChanged notifications or queued requests.
actual.Sources.Clear();
for (int i = 0; i < 10; i++) { model.Content = "lock # burst " + i; reference.Content = model.Content; }
Drain();
Check(actual.Sources.Count == 1, $"Ten queued source updates must measure once, observed {actual.Sources.Count}.");
Check(actual.Sources.Single() == model.Content, "The coalesced measurement must read the latest text."); Compare("Burst");
foreach (var source in new[] { "", "lock", "CONST 42\t  ", "éclair 😀\r\nlock\t# trailing  ", new string('W', 800), string.Join("\n", Enumerable.Repeat("lock # line", 200)), new string('界', 100) + "\n" + new string('i', 900), "short again" }) {
   model.Content = source; reference.Content = source; Drain(); Compare("Grow/shrink/Unicode");
}
model.Content = new string('W', 900) + "\n" + string.Join("\n", Enumerable.Repeat("lock", 160)); reference.Content = model.Content; Drain();
Scroll(actual).Offset = new Vector(150, 100); Scroll(legacy).Offset = new Vector(150, 100); Drain(); Compare("Both scrollbars");
actual.Request(); legacy.Request(); Scroll(actual).Offset = new Vector(210, 150); Scroll(legacy).Offset = new Vector(210, 150); Drain(); Compare("Scroll after queued measure request");
actual.Request(); legacy.Request(); Scroll(actual).Offset = new Vector(100, 60); Scroll(legacy).Offset = new Vector(100, 60);
model.Content += " last"; reference.Content = model.Content; Drain(); Compare("Source and scroll interleaved");
window.Width = 1100; window.Height = 500; Drain(); Compare("Resize");
foreach (double size in new[] { 12.0, 18.0 }) {
   actual.FontSize = size; legacy.FontSize = size;
   model.Content += "x"; reference.Content = model.Content; Drain(); Compare("Font size");
}
actual.FontFamily = legacy.FontFamily = new FontFamily("Arial"); model.Content = "WWWW iii éé 😀\t  "; reference.Content = model.Content; Drain(); Compare("Proportional/fallback font");
var culture = CultureInfo.CurrentCulture;
try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR"); model.Content += " 123"; reference.Content = model.Content; Drain(); Compare("Culture"); }
finally { CultureInfo.CurrentCulture = culture; }
actual.Sources.Clear(); actual.Request(); legacy.Request();
var template = actual.EditBox.Template; actual.EditBox.Template = null; actual.EditBox.ApplyTemplate(); actual.EditBox.Template = template; actual.EditBox.ApplyTemplate();
var oldTemplate = legacy.EditBox.Template; legacy.EditBox.Template = null; legacy.EditBox.ApplyTemplate(); legacy.EditBox.Template = oldTemplate; legacy.EditBox.ApplyTemplate();
Drain(); Check(actual.Sources.Count == 1 && actual.Sources[0] == model.Content, "Template replacement discards old queued work and measures the current template."); Compare("Template replacement");
actual.Sources.Clear(); actual.Request(); model.Content = "old abandoned " + new string('W', 200);
model = Model("lock # replacement"); reference = Model(model.Content); actual.DataContext = model; legacy.DataContext = reference; Drain();
Check(actual.Sources.Count == 1 && actual.Sources.All(s => s == model.Content), "Context replacement cannot measure abandoned source."); Compare("Context replacement");
actual.Sources.Clear(); actual.Request(); grid.Children.Remove(actual); model.Content = "lock # detached new source"; reference.Content = model.Content; Drain();
Check(actual.Sources.Count == 0, "Detached queued work must not measure.");
grid.Children.Add(actual); Drain(); Check(actual.Sources.Count == 1 && actual.Sources[0] == model.Content, "Reattachment must measure current content once."); Compare("Reattach");
actual.Sources.Clear(); for (int i = 0; i < 32; i++) model.ErrorLocations.Add(new TextSegment(0, 0, 1, SegmentType.Warning)); Drain();
Check(actual.Sources.Count == 0, "Diagnostics-only updates cannot request widths.");
actual.Request(); actual.DataContext = null; Drain(); Check(actual.EditBox.Text is null or "", "Clearing context must clear editable content.");
window.Content = null; window.Close(); Drain(); Console.WriteLine($"CHECKS_PASSED:{checks}");

class CountedEditor : TextEditor {
   public readonly List<string> Sources = new();
   public void Request() => RequestWidthUpdate();
   protected override void UpdateDocumentWidth() { Sources.Add(EditBox.Text ?? ""); base.UpdateDocumentWidth(); }
}
sealed class ImmediateEditor : CountedEditor {
   protected override void RequestWidthUpdate() => UpdateDocumentWidth();
}
