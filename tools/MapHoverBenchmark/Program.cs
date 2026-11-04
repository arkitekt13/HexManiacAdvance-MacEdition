using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Performance;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var results = new List<object>();
int checks = 0, sink = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
foreach (var workload in new[] { "null", "dialogue", "branches" }) foreach (int count in new[] { 1, 8, 32, 64 }) {
   var seed = new SyntheticMapFixture(32, 32, withNpcText: true);
   var data = new byte[0x100000]; seed.Model.RawData.CopyTo(data, 0);
   void Word(int at, int value) => BitConverter.GetBytes(value).CopyTo(data, at);
   void Ptr(int at, int value) => Word(at, 0x08000000 + value);
   data[0x160] = (byte)count;
   for (int i = 0; i < count; i++) {
      int row = 0x17000 + i * 24, script = 0x20000 + i * 0x100, texts = 0x40000 + i * 0x200;
      data[row] = (byte)(i + 1); data[row + 4] = (byte)(10 + i % 8); data[row + 6] = (byte)(10 + i / 8);
      Word(row + 16, workload == "null" ? 0 : 0x08000000 + (workload == "dialogue" ? 0x18000 : script));
      if (workload != "branches") continue;
      void Load(int at, int index) { data[at] = 0x0F; data[at + 1] = 0; Ptr(at + 2, texts + index * 64); }
      Load(script, 0); data[script + 6] = 6; data[script + 7] = 1; Ptr(script + 8, script + 40);
      data[script + 12] = 4; Ptr(script + 13, script + 60); data[script + 17] = 2;
      Load(script + 40, 1); Load(script + 46, 2); data[script + 52] = 2;
      Load(script + 60, 3); Load(script + 66, 4); data[script + 72] = 3;
      for (int text = 0; text < 5; text++) PCSString.Convert($"\"NPC {i} branch {text}: synthetic dialogue.\"").CopyTo(data, texts + text * 64);
   }
   var model = new PokemonModel(data, seed.Model.ExportMetadata(null, seed.ViewPort.Singletons.MetadataInfo), new Singletons());
   model.SetList(new NoDataChangeDeltaModel(), "FacingOptions", "NoMovement");
   model.SetList(new NoDataChangeDeltaModel(), "MapAttributeBehaviors", "Normal");
   var token = new ModelDelta();
   foreach (int at in new[] { 0x104, 0x108, 0x10C }) model.WritePointer(token, at, 0x110);
   var format = model.GetNextRun(0x100).FormatString;
   Check(!ArrayRun.TryParse(model, format[..^1] + "4", 0x100, SortedSpan<int>.None, out var banks).HasError, "Map bank parse");
   model.ObserveAnchorWritten(new NoDataChangeDeltaModel(), "data.maps.banks", banks);
   ((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new("synthetic.layout", "data.maps.banks/3/maps/0/map/0/layout/", "Maps"));
   var viewport = new ViewPort("hover-benchmark.bin", model, InstantDispatch.Instance, new Singletons(), fs: new StubFileSystem());
   viewport.InitializationWorkload.GetAwaiter().GetResult();
   var mapper = viewport.MapEditor; var map = mapper.PrimaryMap; mapper.HideEvents = false;
   _ = map.PixelData;
   var summary = typeof(MapEditorViewModel).GetMethod("SummarizeScript", BindingFlags.Instance | BindingFlags.NonPublic)!
      .CreateDelegate<Func<int, IEnumerable<object>>>(mapper);
   (double x, double y) Point(int x, int y) { var p = mapper.ToMapPosition(map, x, y); return (p.x + 8 * map.SpriteScale, p.y + 8 * map.SpriteScale); }
   var empty = Point(1, 1); var hit = Point(10, 10);
   IReadOnlyList<IEventViewModel> At((double x, double y) p) => map.EventsUnderCursor(p.x, p.y, false);
   // Investigation-only candidate: reacquire current rows; instantiate only the matching NPC.
   // This is deliberately not a replacement for all event types, callbacks, or selection wiring.
   List<ObjectEventViewModel> Filter(int x, int y) {
      var rows = map.GetMapModel().GetSubTable("events")[0].GetSubTable("objects");
      var matches = new List<ObjectEventViewModel>();
      for (int i = 0; i < rows.Count; i++) {
         var row = rows[i];
         if (row.GetValue("x") == x && row.GetValue("y") == y)
            matches.Add(new ObjectEventViewModel(map, _ => { }, row, mapper.Templates, map.AllOverworldSprites, BlockMapViewModel.GetDefaultOW(model), map.BerryInfo));
      }
      return matches;
   }
   Check(map.EventGroup.Objects.Count == count, "Expected NPC count");
   Check(At(empty).Count == 0 && At(hit).Count == 1, "Hit and empty tile coordinates");
   var expected = (ObjectEventViewModel)At(hit).Single();
   var candidate = Filter(10, 10).Single();
   Check(candidate.X == expected.X && candidate.Y == expected.Y && candidate.ScriptAddress == expected.ScriptAddress, "Candidate NPC matches current row");
   var expectedText = summary(expected.ScriptAddress).OfType<string>().ToArray();
   Check(expectedText.Length == (workload == "branches" ? 5 : 1) && (workload != "null" || expectedText[0] == "(no script)"), $"Actual branch/text traversal: {workload}, address {expected.ScriptAddress:X}, [{string.Join(" | ", expectedText)}]");
   Check(expectedText.SequenceEqual(summary(candidate.ScriptAddress).OfType<string>()), "Candidate summary parity");
   // Warm discovery/JIT before timing; cold rendering and metadata discovery are outside this measurement.
   for (int i = 0; i < 10; i++) { _ = At(empty); _ = At(hit); _ = Filter(10, 10); _ = summary(expected.ScriptAddress).ToArray(); }
   viewport.ChangeHistory.ClearHistory(false);
   var before = model.RawData.ToArray(); var metadata = model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize();
   int step = 0;
   void Measure(string phase, Action action, int operations = 40) {
      for (int i = 0; i < 5; i++) action();
      var samples = new List<object>();
      for (int sample = 0; sample < 5; sample++) {
         long bytes = GC.GetAllocatedBytesForCurrentThread(); long start = Stopwatch.GetTimestamp();
         for (int i = 0; i < operations; i++) action();
         double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds / operations;
         samples.Add(new { ms, bytes = (GC.GetAllocatedBytesForCurrentThread() - bytes) / operations });
      }
      results.Add(new { workload, count, phase, operations, samples });
   }
   Measure("event-group", () => sink += map.EventGroup.Objects.Count);
   Measure("lookup-empty", () => sink += At(empty).Count);
   Measure("lookup-npc", () => sink += At(hit).Count);
   Measure("summary", () => sink += summary(expected.ScriptAddress).Count());
   Measure("hover-empty", () => { var p = Point(1 + step++ % 6, 1); _ = mapper.Hover(p.x, p.y); });
   // One measured operation includes two tile transitions, one empty then one NPC.
   Measure("hover-npc-pair", () => { _ = mapper.Hover(empty.x, empty.y); _ = mapper.Hover(hit.x, hit.y); });
   _ = mapper.Hover(hit.x, hit.y);
   Check(mapper.Hover(hit.x, hit.y) == null, "Same-cell hover reuses summary");
   Measure("hover-same-cell", () => { _ = mapper.Hover(hit.x, hit.y); });
   Measure("candidate-empty", () => sink += Filter(1, 1).Count);
   Measure("candidate-npc", () => sink += Filter(10, 10).Count);
   Check(before.SequenceEqual(model.RawData), "Warm hover leaves ROM bytes unchanged");
   Check(metadata.SequenceEqual(model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()), "Warm hover leaves metadata unchanged");
   Check(!viewport.Undo.CanExecute(null) && !viewport.Redo.CanExecute(null), "Warm hover leaves clean history");
   Console.Error.WriteLine($"Measured {workload}, {count} NPCs");
}
Console.WriteLine(JsonSerializer.Serialize(new { checks, sink, runtime = Environment.Version.ToString(), results }, new JsonSerializerOptions { WriteIndented = true }));
