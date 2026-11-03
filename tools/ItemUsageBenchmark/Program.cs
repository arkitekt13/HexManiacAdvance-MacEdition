using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

if (args.Length != 2) throw new ArgumentException("Expected output JSON path and explicit local ROM path.");
var output = Path.GetFullPath(args[0]); var romPath = Path.GetFullPath(args[1]);
if (!output.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || string.Equals(output, romPath, StringComparison.OrdinalIgnoreCase))
   throw new ArgumentException("Use a separate .json output path.");
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
Application.Current!.Resources["FileSystem"] = new StubFileSystem();
int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
var originalBytes = File.ReadAllBytes(romPath); var originalHash = Hash(originalBytes);
IDataModel model = new HardcodeTablesModel(new Singletons(), originalBytes.ToArray(), new StoredMetadata(Array.Empty<string>()));
model.InitializationWorkload.GetAwaiter().GetResult();
var dispatch = new MeasuredDispatch(); var singletons = new Singletons(dispatch);
var viewport = new ViewPort("item-usage-benchmark", model, dispatch, singletons, fs: new StubFileSystem());
viewport.InitializationWorkload.GetAwaiter().GetResult();
var errors = new List<string>(); viewport.OnError += (_, e) => errors.Add(e);
var table = model.GetTable(HardcodeTablesModel.ItemsTableName);
var tool = viewport.Tools.TableTool;
void Navigate(int row) => viewport.Goto.Execute(table.Start + row * table.ElementLength);
void PumpUntil(Func<bool> done) {
   var deadline = Stopwatch.StartNew();
   while (!done()) {
      Dispatcher.UIThread.RunJobs();
      if (deadline.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException("Delayed map search did not complete.");
      Thread.Sleep(5);
   }
   Dispatcher.UIThread.RunJobs();
}
MapOptionsArrayElementViewModel Owner() => tool.UsageChildren.OfType<MapOptionsArrayElementViewModel>().Single();
var bankField = typeof(GotoMapButton).GetField("bank", BindingFlags.Instance | BindingFlags.NonPublic)!;
var mapField = typeof(GotoMapButton).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic)!;
var eventField = typeof(GotoMapButton).GetField("eventModel", BindingFlags.Instance | BindingFlags.NonPublic)!;
string[] Published(MapOptionsArrayElementViewModel owner) => owner.MapPreviews.Select(button => {
   var ev = (IEventModel)eventField.GetValue(button);
   return $"{bankField.GetValue(button)}:{mapField.GetValue(button)}:{ev?.Element.Start ?? -1}";
}).ToArray();
string[] PixelHashes(MapOptionsArrayElementViewModel owner) => owner.MapPreviews.Select(button => {
   var pixels = button.Image.PixelData; var bytes = new byte[pixels.Length * 2];
   Buffer.BlockCopy(pixels, 0, bytes, 0, bytes.Length); return Hash(bytes);
}).ToArray();
var metadata = model.ExportMetadata(null, singletons.MetadataInfo).Serialize();
var results = new List<object>();
foreach (var item in new[] { 13, 1, 50, 0 }) {
   Console.WriteLine("Item: " + item);
   var previous = dispatch.MapCalls.Count;
   var navigationStart = Stopwatch.GetTimestamp(); Navigate(item);
   var navigationMs = Stopwatch.GetElapsedTime(navigationStart).TotalMilliseconds;
   var owner = Owner();
   Check(dispatch.MapCalls.Count == previous, "Navigation must schedule, not immediately execute, map search.");
   Check(owner.MapPreviews.Count == 0, "Deferred preview owner should start empty.");
   PumpUntil(() => dispatch.MapCalls.Count > previous);
   var cold = dispatch.MapCalls.Last(); var keys = Published(owner); var hashes = PixelHashes(owner);
   Check(cold.DelayMs >= 450, "Map search did not respect the real half-second delay.");
   Check(cold.UiThread, "The Mac delay callback should resume on its captured UI context.");
   Check(!tool.UsageOptionsOpen, "The benchmark should expose searches scheduled while the usage menu is closed.");
   var reference = ReferenceMatches(model, viewport.Tools.CodeTool.ScriptParser, item).ToArray();
   var visible = reference.Where(match => viewport.MapEditor.GetMapPreview(match.Bank, match.Map, match.Event.X, match.Event.Y) != null).ToArray();
   Check(keys.SequenceEqual(visible.Select(match => match.Key)), "Published ordered map/event results differ from uncached reference search.");
   var phases = new List<object>();
   phases.Add(Measure("map/event enumeration only", () => Enumerate(model).Count()));
   phases.Add(Measure("script and hidden-item matching without previews", () => ReferenceMatches(model, viewport.Tools.CodeTool.ScriptParser, item).Count()));
   var experimental = ExperimentMatches(model, viewport.Tools.CodeTool.ScriptParser, item).ToArray();
   Check(reference.Select(m => m.Key).SequenceEqual(experimental.Select(m => m.Key)), "Query-local root reuse experiment changed ordered matching results.");
   phases.Add(Measure("query-local repeated-root reuse experiment, matching only", () => ExperimentMatches(model, viewport.Tools.CodeTool.ScriptParser, item).Count()));
   phases.Add(Measure("generate previews for matched events only", () => {
      foreach (var match in visible) GC.KeepAlive(viewport.MapEditor.GetMapPreview(match.Bank, match.Map, match.Event.X, match.Event.Y));
      return visible.Length;
   }));
   var repeated = new List<CallbackSample>();
   for (int i = 0; i < 3; i++) {
      var prior = dispatch.MapCalls.Count;
      tool.DataForCurrentRunChanged(); owner = Owner();
      PumpUntil(() => dispatch.MapCalls.Count > prior);
      repeated.Add(dispatch.MapCalls.Last());
      Check(repeated.Last().DelayMs >= 450 && repeated.Last().UiThread, "Repeated search must wait and resume on the UI thread.");
      Check(keys.SequenceEqual(Published(owner)), "Repeated delayed searches changed ordered results.");
      Check(hashes.SequenceEqual(PixelHashes(owner)), "Repeated delayed searches changed thumbnail pixels.");
   }
   var events = Enumerate(model).ToArray();
   var roots = events.Where(e => e.Event is IScriptEventModel && e.Event is not SignpostEventModel).Select(e => ((IScriptEventModel)e.Event).ScriptAddress).ToArray();
   Console.WriteLine($"Delayed callback: {cold.ElapsedMs:F3} ms, {cold.Allocated:N0} bytes; {keys.Length} results; UI={cold.UiThread}");
   results.Add(new { item, navigationMs, cold, repeated, resultKeys = keys, pixelHashes = hashes,
      events = events.Length, scriptRoots = roots.Length, uniqueScriptRoots = roots.Distinct().Count(),
      referenceMatches = reference.Length, hiddenMatches = reference.Count(m => m.Event is SignpostEventModel), phases });
}
// Normal rapid browsing: share the real timer, cancel earlier requests, and complete only the last.
var beforeRapid = dispatch.MapCalls.Count;
for (int item = 13; item < 23; item++) Navigate(item);
var finalOwner = Owner();
Check(dispatch.MapCalls.Count == beforeRapid, "Rapid browsing unexpectedly executed a delayed callback inline.");
PumpUntil(() => dispatch.MapCalls.Count > beforeRapid);
Check(dispatch.MapCalls.Last().DelayMs >= 450, "Coalesced search must retain the normal delay.");
Check(dispatch.MapCalls.Count == beforeRapid + 1, "Rapid browsing should coalesce to one completed map scan.");
var finalReference = ReferenceMatches(model, viewport.Tools.CodeTool.ScriptParser, 22)
   .Where(m => viewport.MapEditor.GetMapPreview(m.Bank, m.Map, m.Event.X, m.Event.Y) != null).Select(m => m.Key).ToArray();
Check(Published(finalOwner).SequenceEqual(finalReference), "Coalesced search must publish the final selected item's results.");
Check(Hash(model.RawData) == originalHash && Hash(File.ReadAllBytes(romPath)) == originalHash, "Investigation changed ROM bytes.");
Check(metadata.SequenceEqual(model.ExportMetadata(null, singletons.MetadataInfo).Serialize()), "Investigation changed metadata.");
Check(!viewport.Undo.CanExecute(null) && errors.Count == 0, "Read-only search created history/errors.");
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new { runtime = RuntimeInformation.FrameworkDescription, architecture = RuntimeInformation.ProcessArchitecture.ToString(), game = model.GetGameCode(), sourceSha256 = originalHash,
   correctnessChecks = checks, scope = "Actual Mac delay timer and UI synchronization context; no native window/compositor or input-latency claim; in-process component experiments do not replace production scans", results, rapid = dispatch.MapCalls.Last() }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"BENCHMARK_CHECKS_PASSED:{checks}");

object Measure(string phase, Func<int> action) {
   action(); action(); var times = new List<double>(); var bytes = new List<long>(); var count = 0;
   for (int i = 0; i < 5; i++) {
      var before = GC.GetAllocatedBytesForCurrentThread(); var start = Stopwatch.GetTimestamp(); count = action();
      times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds); bytes.Add(GC.GetAllocatedBytesForCurrentThread() - before);
   }
   var ms = times.Order().ElementAt(2); var allocated = bytes.Order().ElementAt(2);
   Console.WriteLine($"{phase}: {ms:F3} ms / {allocated:N0} bytes, count {count}");
   return new { phase, count, medianMs = ms, medianAllocated = allocated, samplesMs = times, samplesAllocated = bytes };
}
IEnumerable<Match> Enumerate(IDataModel data) {
   var maps = AllMapsModel.Create(data);
   for (int bank = 0; bank < maps.Count; bank++) {
      var currentBank = maps[bank];
      for (int map = 0; map < currentBank.Count; map++) {
         var current = currentBank[map]; if (current == null) continue;
         foreach (var ev in current.Events.Objects.Concat<IScriptEventModel>(current.Events.Scripts)) yield return new(bank, map, ev);
         foreach (var ev in current.Events.Signposts) if (ev.IsHiddenItem) yield return new(bank, map, ev);
      }
   }
}
IEnumerable<Match> ReferenceMatches(IDataModel data, ScriptParser parser, int item) {
   var filter = new List<byte>();
   foreach (var line in parser.DependsOn(HardcodeTablesModel.ItemsTableName)) {
      if (line is MacroScriptLine macro && macro.Args[0] is SilentMatchArg silent) filter.Add(silent.ExpectedValue);
      if (line is ScriptLine sl) filter.Add(sl.LineCode[0]);
   }
   foreach (var match in Enumerate(data)) {
      if (match.Event is SignpostEventModel hidden) { if (hidden.ItemValue == item) yield return match; continue; }
      var ev = (IScriptEventModel)match.Event; var found = false;
      foreach (var spot in Flags.GetAllScriptSpots(data, parser, new[] { ev.ScriptAddress }, filter.ToArray())) {
         int address = spot.Address + spot.Line.LineCode.Count;
         foreach (var arg in spot.Line.Args) {
            var length = arg.Length(data, address);
            if (arg.EnumTableName == HardcodeTablesModel.ItemsTableName && data.ReadMultiByteValue(address, length) == item) { found = true; break; }
            address += length;
         }
         if (found) break;
      }
      if (found) yield return match;
   }
}
IEnumerable<Match> ExperimentMatches(IDataModel data, ScriptParser parser, int item) {
   // Experiment only: this dictionary exists for one enumeration and never outlives a query.
   var outcomes = new Dictionary<int, bool>(); var filter = new List<byte>();
   foreach (var line in parser.DependsOn(HardcodeTablesModel.ItemsTableName)) {
      if (line is MacroScriptLine macro && macro.Args[0] is SilentMatchArg silent) filter.Add(silent.ExpectedValue);
      if (line is ScriptLine sl) filter.Add(sl.LineCode[0]);
   }
   var codes = filter.ToArray();
   foreach (var match in Enumerate(data)) {
      if (match.Event is SignpostEventModel hidden) { if (hidden.ItemValue == item) yield return match; continue; }
      var address = ((IScriptEventModel)match.Event).ScriptAddress;
      if (!outcomes.TryGetValue(address, out var found)) outcomes[address] = found = ContainsItem(address);
      if (found) yield return match;
   }
   bool ContainsItem(int root) {
      foreach (var spot in Flags.GetAllScriptSpots(data, parser, new[] { root }, codes)) {
         int address = spot.Address + spot.Line.LineCode.Count;
         foreach (var arg in spot.Line.Args) {
            var length = arg.Length(data, address);
            if (arg.EnumTableName == HardcodeTablesModel.ItemsTableName && data.ReadMultiByteValue(address, length) == item) return true;
            address += length;
         }
      }
      return false;
   }
}

record Match(int Bank, int Map, IEventModel Event) { public string Key => $"{Bank}:{Map}:{Event.Element.Start}"; }
record CallbackSample(string Action, double DelayMs, double ElapsedMs, long Allocated, bool UiThread);
sealed class MeasuredTimer(MeasuredDispatch dispatch) : IDelayWorkTimer {
   private readonly DelayWorkTimer timer = new();
   public bool HasScheduledWork => timer.HasScheduledWork;
   public void Reset() => timer.Reset();
   public DelayWorkResult DelayCall(TimeSpan delay, Action action) {
      var scheduled = Stopwatch.GetTimestamp();
      var name = action.Method.DeclaringType!.Name;
      return timer.DelayCall(delay, () => {
         var start = Stopwatch.GetTimestamp(); var before = GC.GetAllocatedBytesForCurrentThread();
         action();
         if (name == nameof(MapOptionsArrayElementViewModel)) dispatch.MapCalls.Add(new(name,
            Stopwatch.GetElapsedTime(scheduled, start).TotalMilliseconds,
            Stopwatch.GetElapsedTime(start).TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - before,
            Dispatcher.UIThread.CheckAccess()));
      });
   }
}
sealed class MeasuredDispatch : IWorkDispatcher {
   public List<CallbackSample> MapCalls { get; } = new();
   public Task WaitForRenderingAsync() => Task.CompletedTask;
   public void BlockOnUIWork(Action action) => action();
   public Task DispatchWork(Action action) { action?.Invoke(); return Task.CompletedTask; }
   public Task RunBackgroundWork(Action action) => DispatchWork(action);
   public IDelayWorkTimer CreateDelayTimer() => new MeasuredTimer(this);
}
