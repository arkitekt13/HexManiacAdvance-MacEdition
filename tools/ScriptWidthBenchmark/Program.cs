using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

// Time actual production measurements, including queued work rather than request events.
// No compiler, ROM or native input is used.
if (args.Length != 1) throw new ArgumentException("Expected output JSON path.");
var output = Path.GetFullPath(args[0]); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var fs = new StubFileSystem(); var host = new EditorViewModel(fs, InstantDispatch.Instance);
Application.Current!.Resources["FileSystem"] = fs; App.UpdateThemeDictionary(Application.Current.Resources, host.Theme);
var cases = new[] { ("short", 100), ("short", 500), ("short", 2000), ("dialogue", 500), ("long", 500) };
var rows = new List<object>();
int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
double Median(IEnumerable<double> values) => values.Order().ElementAt(values.Count() / 2);
string Source(string kind, int units, int digit) {
   var line = kind == "dialogue" ? "Hello é traveler 😀 — take your time." : "setvar temp1 1 # NPC note";
   return $"lock\nsetvar temp1 {digit}\n" + (kind == "long" ? string.Concat(Enumerable.Repeat("Long dialogue text. ", units)) : string.Join("\n", Enumerable.Repeat(line, units))) + "\nend";
}
foreach (var (kind, units) in cases) {
   var model = new TextEditorViewModel { LineCommentHeader = "#" };
   foreach (var word in new[] { "lock", "setvar", "end" }) model.Keywords.Add(word);
   model.Constants.Add("temp1");
   var editor = new MeasuredEditor { DataContext = model, FontSize = 14 };
   var window = new Window { Width = 1000, Height = 750, Content = editor }; window.Show();
   void Drain() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); }
   var layers = new[] { "BasicLayer", "AccentLayer", "ConstantsLayer", "NumericLayer", "CommentLayer", "TextLayer" }.Select(n => editor.FindControl<TextBlock>(n)!).ToArray();
   // Warm fonts/layout and stable geometry identically in the before and after builds.
   for (int i = 0; i < 6; i++) { model.Content = Source(kind, units, 1 + i % 2); Drain(); }
   editor.EditBox.Focus(); editor.EditBox.CaretIndex = 2; Drain();
   void Reset() => editor.Reset();
   var samples = new List<(double Set, double Drain, double Width, long Bytes, int Calls, int SyncCalls)>();
   for (int iteration = 0; iteration < 10; iteration++) {
      Reset(); string source = Source(kind, units, 1 + iteration % 2);
      long at = Stopwatch.GetTimestamp(); model.Content = source; double set = Stopwatch.GetElapsedTime(at).TotalMilliseconds; int syncCalls = editor.Calls;
      at = Stopwatch.GetTimestamp(); Drain(); double drain = Stopwatch.GetElapsedTime(at).TotalMilliseconds;
      Check(editor.EditBox.Text == source && model.Content == source, "The probe must preserve complete editable source.");
      var expected = new[] { model.PlainContent, model.AccentContent, model.ConstantContent, model.NumericContent, model.CommentContent, model.TextContent }.Select(SyntaxLayerTextConverter.Normalize).ToArray();
      var displayed = layers.Select((layer, i) => i == 4 ? string.Concat(layer.Inlines.OfType<Run>().Select(r => r.Text)) : layer.Text ?? "").ToArray();
      Check(displayed.SequenceEqual(expected), "All six rendered layers must match production highlighting.");
      Check(editor.Calls > 0, "Actual width measurement must run for a settled source edit.");
      if (iteration >= 3) samples.Add((set, drain, editor.Ms, editor.Bytes, editor.Calls, syncCalls));
   }
   var widths = layers.Select(l => l.Width).ToArray();
   var cover = editor.FindControl<Control>("CornerCover")!; var corner = new[] { cover.Width, cover.Height };
   var hashes = new[] { model.PlainContent, model.AccentContent, model.ConstantContent, model.NumericContent, model.CommentContent, model.TextContent }.Select(Hash).ToArray();
   string key = kind + units;
   var bursts = new List<object>();
   for (int sample = 0; sample < 7; sample++) {
      Reset(); long at = Stopwatch.GetTimestamp(), bytes = GC.GetAllocatedBytesForCurrentThread();
      for (int i = 0; i < 10; i++) model.Content = Source(kind, units, 1 + i % 2);
      double setMs = Stopwatch.GetElapsedTime(at).TotalMilliseconds;
      at = Stopwatch.GetTimestamp(); Drain(); double drainMs = Stopwatch.GetElapsedTime(at).TotalMilliseconds;
      bursts.Add(new { edits = 10, calls = editor.Calls, widthMs = editor.Ms, widthBytes = editor.Bytes,
         distinctWidthSources = editor.Sources.Distinct(StringComparer.Ordinal).Count(), setMs, drainMs,
         totalBytes = GC.GetAllocatedBytesForCurrentThread() - bytes });
      Check(editor.EditBox.Text == model.Content, "Burst must retain the latest complete source.");
   }
   Reset(); model.ErrorLocations.Clear(); for (int i = 0; i < 32; i++) model.ErrorLocations.Add(new TextSegment(i, 0, 1, SegmentType.Warning)); Drain();
   var diagnostics = new { count = 32, widthCalls = editor.Calls, widthMs = editor.Ms };
   Check(editor.Calls == 0, "Diagnostics-only changes must not invoke the source width handler.");
   rows.Add(new { kind, units, lines = model.Content.Split('\n').Length, characters = model.Content.Length,
      setMedianMs = Median(samples.Select(s => s.Set)), drainMedianMs = Median(samples.Select(s => s.Drain)),
      widthMedianMs = Median(samples.Select(s => s.Width)), widthMedianBytes = samples.Select(s => s.Bytes).Order().ElementAt(3),
      widthCalls = samples.Select(s => s.Calls), synchronousCalls = samples.Select(s => s.SyncCalls),
      samples = samples.Select(s => new { setMs = s.Set, drainMs = s.Drain, widthMs = s.Width, widthBytes = s.Bytes, calls = s.Calls }),
      widths, corner, hashes, bursts, diagnostics });
   Console.Error.WriteLine($"{key}, set={Median(samples.Select(s => s.Set)):F2}, drain={Median(samples.Select(s => s.Drain)):F2}, width={Median(samples.Select(s => s.Width)):F2}");
   window.Content = null; window.Close(); Drain();
}
File.WriteAllText(output, JsonSerializer.Serialize(new { checks, runtime = Environment.Version.ToString(),
   scope = "Standalone production TextEditor; actual UpdateDocumentWidth observer, single edits and queued bursts; total drain includes queue/extent restoration; no compiler, native input or ROM; phase medians are not additive", rows }, new JsonSerializerOptions { WriteIndented = true }));

sealed class MeasuredEditor : TextEditor {
   public int Calls; public double Ms; public long Bytes; public readonly List<string> Sources = new();
   public void Reset() { Calls = 0; Ms = 0; Bytes = 0; Sources.Clear(); }
   protected override void UpdateDocumentWidth() {
      string source = EditBox.Text ?? "";
      long at = Stopwatch.GetTimestamp(), bytes = GC.GetAllocatedBytesForCurrentThread();
      base.UpdateDocumentWidth();
      Ms += Stopwatch.GetElapsedTime(at).TotalMilliseconds; Bytes += GC.GetAllocatedBytesForCurrentThread() - bytes; Calls++;
      Sources.Add(source);
   }
}
