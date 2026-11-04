using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

// Investigation only. Time the real width handler and remove it only in disposable controls
// for same-width A/B experiments. No application instrumentation, compiler or ROM is used.
if (args.Length != 1) throw new ArgumentException("Expected output JSON path.");
var output = Path.GetFullPath(args[0]); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var fs = new StubFileSystem(); var host = new EditorViewModel(fs, InstantDispatch.Instance);
Application.Current!.Resources["FileSystem"] = fs; App.UpdateThemeDictionary(Application.Current.Resources, host.Theme);
var candidates = typeof(TextEditor).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
   .Where(m => m.Name.Contains(".ctor") && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(object), typeof(TextChangedEventArgs) })).ToArray();
if (candidates.Length != 1) throw new InvalidOperationException("Expected exactly one constructor TextChanged width handler; inspect source before using this probe.");
var cases = new[] { ("short", 100), ("short", 500), ("short", 2000), ("dialogue", 500), ("long", 500) };
var rows = new List<object>(); var endpoints = new Dictionary<string, (string[] Hashes, double[] Widths, double[] Corner)>();
int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
double Median(IEnumerable<double> values) => values.Order().ElementAt(values.Count() / 2);
string Source(string kind, int units, int digit) {
   var line = kind == "dialogue" ? "Hello é traveler 😀 — take your time." : "setvar temp1 1 # NPC note";
   return $"lock\nsetvar temp1 {digit}\n" + (kind == "long" ? string.Concat(Enumerable.Repeat("Long dialogue text. ", units)) : string.Join("\n", Enumerable.Repeat(line, units))) + "\nend";
}
foreach (var (kind, units) in cases) foreach (bool omitWidth in new[] { false, true }) {
   var model = new TextEditorViewModel { LineCommentHeader = "#" };
   foreach (var word in new[] { "lock", "setvar", "end" }) model.Keywords.Add(word);
   model.Constants.Add("temp1");
   var editor = new TextEditor { DataContext = model, FontSize = 14 };
   var window = new Window { Width = 1000, Height = 750, Content = editor }; window.Show();
   void Drain() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); }
   var layers = new[] { "BasicLayer", "AccentLayer", "ConstantsLayer", "NumericLayer", "CommentLayer", "TextLayer" }.Select(n => editor.FindControl<TextBlock>(n)!).ToArray();
   // Warm the unmodified control, including fonts/layout and fixed-width geometry, in BOTH arms.
   for (int i = 0; i < 6; i++) { model.Content = Source(kind, units, 1 + i % 2); Drain(); }
   editor.EditBox.Focus(); editor.EditBox.CaretIndex = 2; Drain();
   var original = candidates[0].CreateDelegate<EventHandler<TextChangedEventArgs>>(editor);
   editor.EditBox.TextChanged -= original;
   int widthCalls = 0; double widthMs = 0; long widthBytes = 0;
   var widthSources = new List<string>();
   EventHandler<TextChangedEventArgs> wrapped = (sender, e) => {
      long before = GC.GetAllocatedBytesForCurrentThread(), at = Stopwatch.GetTimestamp();
      original(sender, e);
      widthMs += Stopwatch.GetElapsedTime(at).TotalMilliseconds; widthBytes += GC.GetAllocatedBytesForCurrentThread() - before; widthCalls++;
      widthSources.Add(editor.EditBox.Text ?? "");
   };
   if (!omitWidth) editor.EditBox.TextChanged += wrapped;
   void Reset() { widthCalls = 0; widthMs = 0; widthBytes = 0; widthSources.Clear(); }
   var samples = new List<(double Set, double Drain, double Width, long Bytes, int Calls, int SyncCalls)>();
   for (int iteration = 0; iteration < 10; iteration++) {
      Reset(); string source = Source(kind, units, 1 + iteration % 2);
      long at = Stopwatch.GetTimestamp(); model.Content = source; double set = Stopwatch.GetElapsedTime(at).TotalMilliseconds; int syncCalls = widthCalls;
      at = Stopwatch.GetTimestamp(); Drain(); double drain = Stopwatch.GetElapsedTime(at).TotalMilliseconds;
      Check(editor.EditBox.Text == source && model.Content == source, "The probe must preserve complete editable source.");
      var expected = new[] { model.PlainContent, model.AccentContent, model.ConstantContent, model.NumericContent, model.CommentContent, model.TextContent }.Select(SyntaxLayerTextConverter.Normalize).ToArray();
      var displayed = layers.Select((layer, i) => i == 4 ? string.Concat(layer.Inlines.OfType<Run>().Select(r => r.Text)) : layer.Text ?? "").ToArray();
      Check(displayed.SequenceEqual(expected), "All six rendered layers must match production highlighting.");
      Check(omitWidth ? widthCalls == 0 : widthCalls > 0, "The intended real width handler must be wrapped or omitted.");
      if (iteration >= 3) samples.Add((set, drain, widthMs, widthBytes, widthCalls, syncCalls));
   }
   var widths = layers.Select(l => l.Width).ToArray();
   var cover = editor.FindControl<Control>("CornerCover")!; var corner = new[] { cover.Width, cover.Height };
   var hashes = new[] { model.PlainContent, model.AccentContent, model.ConstantContent, model.NumericContent, model.CommentContent, model.TextContent }.Select(Hash).ToArray();
   string key = kind + units; bool geometryMatches = true;
   if (!omitWidth) endpoints[key] = (hashes, widths, corner);
   else {
      Check(endpoints[key].Hashes.SequenceEqual(hashes), "Same-size A/B source layers must agree.");
      geometryMatches = endpoints[key].Widths.Zip(widths).All(p => Math.Abs(p.First - p.Second) < .001) && endpoints[key].Corner.SequenceEqual(corner);
   }
   Reset();
   for (int i = 0; i < 10; i++) model.Content = Source(kind, units, 1 + i % 2);
   Drain(); var burst = new { edits = 10, widthCalls, widthMs, widthBytes, distinctWidthSources = widthSources.Distinct(StringComparer.Ordinal).Count() };
   Reset(); model.ErrorLocations.Clear(); for (int i = 0; i < 32; i++) model.ErrorLocations.Add(new TextSegment(i, 0, 1, SegmentType.Warning)); Drain();
   var diagnostics = new { count = 32, widthCalls, widthMs };
   Check(widthCalls == 0, "Diagnostics-only changes must not invoke the source width handler.");
   rows.Add(new { kind, units, lines = model.Content.Split('\n').Length, characters = model.Content.Length, omitWidth, geometryMatches,
      setMedianMs = Median(samples.Select(s => s.Set)), drainMedianMs = Median(samples.Select(s => s.Drain)),
      widthMedianMs = Median(samples.Select(s => s.Width)), widthMedianBytes = samples.Select(s => s.Bytes).Order().ElementAt(3),
      widthCalls = samples.Select(s => s.Calls), synchronousCalls = samples.Select(s => s.SyncCalls),
      samples = samples.Select(s => new { setMs = s.Set, drainMs = s.Drain, widthMs = s.Width, widthBytes = s.Bytes, calls = s.Calls }),
      widths, corner, hashes, burst, diagnostics });
   Console.Error.WriteLine($"{key}, omit={omitWidth}: set={Median(samples.Select(s => s.Set)):F2}, drain={Median(samples.Select(s => s.Drain)):F2}, width={Median(samples.Select(s => s.Width)):F2}, geometry={geometryMatches}");
   editor.EditBox.TextChanged -= wrapped; window.Content = null; window.Close(); Drain();
}
File.WriteAllText(output, JsonSerializer.Serialize(new { checks, runtime = Environment.Version.ToString(),
   scope = "Standalone production TextEditor; exact width-handler wrapper and same-size omission prototype; no compiler, native input or ROM; phase medians are not additive", rows }, new JsonSerializerOptions { WriteIndented = true }));
