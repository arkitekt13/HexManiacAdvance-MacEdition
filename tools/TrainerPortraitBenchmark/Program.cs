using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;

if (args.Length != 1) throw new ArgumentException("Expected a JSON output path.");
var output = Path.GetFullPath(args[0]);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
Application.Current!.Resources["FileSystem"] = new StubFileSystem();
var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
string PixelHash(IPixelViewModel image) {
   var bytes = new byte[image.PixelData.Length * 2]; Buffer.BlockCopy(image.PixelData, 0, bytes, 0, bytes.Length);
   return Hash(bytes);
}
VisualComboOption[] Choices(ComboBoxArrayElementViewModel field) => field.FilteringComboOptions.AllOptions.OfType<VisualComboOption>().ToArray();
object Measure(string phase, int steps, Action action) {
   action(); action(); var elapsed = new List<double>(); var allocated = new List<long>();
   for (int sample = 0; sample < 7; sample++) {
      var before = GC.GetAllocatedBytesForCurrentThread(); var start = Stopwatch.GetTimestamp(); action();
      elapsed.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
      allocated.Add(GC.GetAllocatedBytesForCurrentThread() - before);
   }
   var medianMs = elapsed.Order().ElementAt(3); var medianAllocated = allocated.Order().ElementAt(3);
   Console.WriteLine($"{phase}: {medianMs:F3} ms, {medianAllocated:N0} bytes / {steps} operations");
   return new { phase, steps, medianMs, medianAllocated, samplesMs = elapsed, samplesAllocated = allocated };
}
void Layout(TabView view) { Dispatcher.UIThread.RunJobs(); view.Measure(new Size(1000, 700)); view.Arrange(new Rect(0, 0, 1000, 700)); }
var results = new List<object>();
foreach (var (count, compressed) in new[] { (16, false), (64, false), (128, false), (64, true) }) {
   Console.WriteLine($"Fixture: {count} portraits, compressed={compressed}");
   var fixture = new PortraitFixture(count, compressed);
   var original = fixture.Model.RawData.ToArray();
   var metadata = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize();
   var candidate = fixture.Candidate(); var initial = Choices(candidate);
   Check(initial.Length == count && initial.All(image => image.PixelWidth == 64 && image.PixelHeight == 64), "Portrait count/dimensions drifted.");
   var hashes = initial.Select(PixelHash).ToArray();
   var repeated = Choices(fixture.Candidate());
   Check(!ReferenceEquals(initial[0], repeated[0]) && hashes.SequenceEqual(repeated.Select(PixelHash)), "Fresh candidates must reproduce the same pixels.");
   var phases = new List<object> {
      Measure("construct fresh portrait field candidates", 100, () => { for (int i = 0; i < 100; i++) GC.KeepAlive(fixture.Candidate(i)); }),
      Measure("navigate portrait-only table, no controls", 100, () => { for (int i = 0; i < 100; i++) fixture.Navigate(i); }),
   };
   // Evaluate an existing model cache as a component only, never install a new editor cache.
   var cache = fixture.Model.CurrentCacheScope;
   var cached = Enumerable.Range(0, count).Select(i => cache.GetImage(fixture.Sprite(i))).ToArray();
   Check(hashes.SequenceEqual(cached.Select(PixelHash)), "Existing model image cache differs from portrait pixels in this fixture.");
   phases.Add(Measure("existing model image-cache reads, experiment only", 100, () => {
      for (int step = 0; step < 100; step++) for (int i = 0; i < count; i++) GC.KeepAlive(cache.GetImage(fixture.Sprite(i)));
   }));
   var retained = fixture.Field.FilteringComboOptions.AllOptions;
   fixture.Navigate(0);
   Check(ReferenceEquals(retained, fixture.Field.FilteringComboOptions.AllOptions), "Navigation unexpectedly replaced unchanged choices.");
   var view = new TabView { DataContext = fixture.ViewPort };
   var window = new Window { Width = 1000, Height = 700, Content = view }; window.Show(); Layout(view);
   var combo = view.GetVisualDescendants().OfType<AngleComboBox>().Single(control => ReferenceEquals(control.DataContext, fixture.Field.FilteringComboOptions));
   Check(combo.IsEffectivelyVisible && combo.SelectedIndex == 0, "Production portrait selector is not bound/visible.");
   phases.Add(Measure("navigate with production closed selector bindings/layout", 100, () => {
      for (int i = 0; i < 100; i++) { fixture.Navigate(i); Layout(view); }
   }));
   fixture.Navigate(0); Layout(view);
   combo.IsDropDownOpen = true; Layout(view);
   Check(combo.IsDropDownOpen && fixture.Field.FilteringComboOptions.DropDownIsOpen, "Popup open state must reach the bound selector.");
   combo.IsDropDownOpen = false; Layout(view);
   Check(!combo.IsDropDownOpen && !fixture.Field.FilteringComboOptions.DropDownIsOpen, "Popup close state must reach the bound selector.");
   phases.Add(Measure("open and close production portrait popup", 10, () => {
      for (int i = 0; i < 10; i++) { combo.IsDropDownOpen = true; Layout(view); combo.IsDropDownOpen = false; Layout(view); }
   }));
   Check(combo.SelectedIndex == 0 && fixture.Model[PortraitFixture.Parents] == 0, "Popup interactions changed selection/data.");
   using var bitmap = new RenderTargetBitmap(new PixelSize(1000, 700)); bitmap.Render(view);
   Check(original.SequenceEqual(fixture.Model.RawData), "Read-only portrait operations changed source bytes.");
   Check(metadata.SequenceEqual(fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize()), "Read-only portrait operations changed metadata.");
   Check(!fixture.ViewPort.Undo.CanExecute(null) && fixture.Errors.Count == 0, "Read-only operations created history/errors.");
   results.Add(new { count, compressed, fixtureSha256 = Hash(original), pixelHashes = hashes, phases,
      sameLiveChoices = ReferenceEquals(retained, fixture.Field.FilteringComboOptions.AllOptions) });
   window.Close(); Dispatcher.UIThread.RunJobs();
}

// Freshness is observed, not repaired: compare live retained choices with a newly built candidate.
var freshness = new List<object>();
foreach (var operation in new[] { "sprite-edit", "palette-edit", "repoint-then-edit", "model-reload" }) {
   var fixture = new PortraitFixture(64, false);
   if (operation == "repoint-then-edit") {
      // RepointToNewCopy correctly requires a shared source; create a second fixture
      // pointer before the history/byte snapshots, using normal model registration.
      var token = new ModelDelta();
      fixture.Model.WritePointer(token, 0x2800, fixture.Model.ReadPointer(PortraitFixture.Pointers));
      fixture.Model.ObserveRunWritten(token, new PointerRun(0x2800));
      fixture.ViewPort.Refresh();
      Check(fixture.Sprite().PointerSources.Count == 2, "Repoint fixture must have two registered pointer sources.");
   }
   var before = fixture.Model.RawData.ToArray();
   var liveChoices = fixture.Field.FilteringComboOptions.AllOptions;
   var originalHash = PixelHash(Choices(fixture.Field)[0]); var oldScope = fixture.Model.CurrentCacheScope;
   var oldPointer = fixture.Model.ReadPointer(PortraitFixture.Pointers);
   var originalAnchors = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).NamedAnchors.ToArray();
   if (operation == "palette-edit") {
      var palette = (IPaletteRun)fixture.Model.GetNextRun(PortraitFixture.Palette);
      var colors = palette.GetPalette(fixture.Model, 0).ToArray();
      for (int i = 1; i < colors.Length; i++) colors[i] = (short)(31 << 5);
      var updated = palette.SetPalette(fixture.Model, fixture.ViewPort.CurrentChange, 0, colors);
      fixture.Model.ObserveRunWritten(fixture.ViewPort.CurrentChange, updated);
   } else if (operation == "model-reload") {
      var data = fixture.Model.RawData.ToArray(); Array.Fill(data, (byte)0x99, oldPointer, 2048);
      var stored = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo);
      fixture.Model.Load(data, stored);
   } else {
      if (operation == "repoint-then-edit") {
         fixture.ViewPort.RepointToNewCopy(PortraitFixture.Pointers);
         fixture.ViewPort.ChangeHistory.ChangeCompleted();
         Check(fixture.Model.ReadPointer(PortraitFixture.Pointers) != oldPointer, "Repoint probe must actually move the sprite.");
      }
      var pixels = new int[64, 64];
      for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) pixels[x, y] = 9;
      var updated = fixture.Sprite().SetPixels(fixture.Model, fixture.ViewPort.CurrentChange, 0, pixels);
      fixture.Model.ObserveRunWritten(fixture.ViewPort.CurrentChange, updated);
   }
   fixture.ViewPort.ChangeHistory.ChangeCompleted(); fixture.ViewPort.Refresh();
   var freshHash = PixelHash(Choices(fixture.Candidate())[0]);
   var liveHash = PixelHash(Choices(fixture.Field)[0]);
   var cacheHash = PixelHash(fixture.Model.CurrentCacheScope.GetImage(fixture.Sprite()));
   Check(freshHash != originalHash && cacheHash == freshHash, "Mutation probe must change pixels and invalidate the existing model image cache.");
   var sameChoices = ReferenceEquals(liveChoices, fixture.Field.FilteringComboOptions.AllOptions);
   var afterBytes = fixture.Model.RawData.ToArray();
   object history = null;
   if (operation != "model-reload") {
      int undoCount = 0;
      while (fixture.ViewPort.Undo.CanExecute(null) && undoCount < 10) { fixture.ViewPort.Undo.Execute(null); undoCount++; }
      fixture.ViewPort.Refresh();
      Check(before.SequenceEqual(fixture.Model.RawData), "Undo must restore exact source bytes after portrait probe.");
      Check(originalAnchors.SequenceEqual(fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).NamedAnchors), "Undo must restore portrait anchor definitions.");
      int redoCount = 0;
      while (fixture.ViewPort.Redo.CanExecute(null) && redoCount < 10) { fixture.ViewPort.Redo.Execute(null); redoCount++; }
      fixture.ViewPort.Refresh();
      Check(afterBytes.SequenceEqual(fixture.Model.RawData), "Redo must restore exact edited portrait bytes/pointer.");
      var redoLive = PixelHash(Choices(fixture.Field)[0]); var redoFresh = PixelHash(Choices(fixture.Candidate())[0]);
      history = new { undoCount, redoCount, staleAfterRedo = redoLive != redoFresh };
   }
   Check(fixture.Errors.Count == 0, "Portrait mutation probe raised editor errors.");
   freshness.Add(new { operation, sameChoices, cacheScopeReplaced = !ReferenceEquals(oldScope, fixture.Model.CurrentCacheScope),
      freshPixelsChanged = freshHash != originalHash, staleLiveChoices = liveHash != freshHash, history });
   Console.WriteLine($"Freshness {operation}: sameChoices={sameChoices}, staleLiveChoices={liveHash != freshHash}");
}
var invalid = new PortraitFixture(64, false);
invalid.Model.WriteMultiByteValue(PortraitFixture.Parents, 1, invalid.ViewPort.CurrentChange, 255);
invalid.ViewPort.ChangeHistory.ChangeCompleted(); invalid.ViewPort.Refresh();
var invalidBytes = invalid.Model.RawData.ToArray();
Check(invalid.Field.FilteringComboOptions.ModelValue == 255 && invalid.Field.FilteringComboOptions.DisplayText == "255", "An out-of-range stored portrait must retain its numeric value.");
Check(Choices(invalid.Field).Length == 64 && invalid.Field.FilteringComboOptions.AllOptions.Count == 65, "Invalid enum preservation must retain all image choices plus its numeric fallback.");
Check(invalidBytes.SequenceEqual(invalid.Model.RawData), "Reading invalid enum fallback must not normalize ROM bytes.");
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new {
   runtime = RuntimeInformation.FrameworkDescription, platform = RuntimeInformation.OSDescription, configuration = "Release",
   architecture = RuntimeInformation.ProcessArchitecture.ToString(), correctnessChecks = checks,
   scope = "Portrait-only real enum/view/binding paths; fabricated 64x64 graphics; no application cache changes; import/reload orchestration audited separately",
   results, freshness, invalidPortraitPreserved = true,
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"BENCHMARK_CHECKS_PASSED:{checks}");
