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
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;

if (args.Length != 1) throw new ArgumentException("Expected a JSON output path.");
var output = Path.GetFullPath(args[0]);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
Application.Current!.Resources["FileSystem"] = new StubFileSystem();
var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
object Measure(string phase, Action action) {
   action(); action();
   var samples = new List<double>();
   var allocations = new List<long>();
   for (int sample = 0; sample < 7; sample++) {
      var before = GC.GetAllocatedBytesForCurrentThread();
      var start = Stopwatch.GetTimestamp();
      action();
      samples.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
      allocations.Add(GC.GetAllocatedBytesForCurrentThread() - before);
   }
   var medianMs = samples.Order().ElementAt(3);
   var medianAllocated = allocations.Order().ElementAt(3);
   Console.WriteLine($"  {phase}: {medianMs:F3} ms, {medianAllocated:N0} bytes / 100 operations");
   return new { phase, steps = 100, medianMs, medianAllocated, samplesMs = samples, samplesAllocated = allocations };
}
void CheckSelection(SyntheticPokemonFixture fixture, int index) {
   var selector = fixture.ViewPort.Tools.TableTool.CurrentElementSelector;
   Check(selector.ModelValue == index, "Table selector index drifted.");
   Check(selector.DisplayText == $"MON{index:D4}", "Table selector label drifted.");
   Check(fixture.Errors.Count == 0, "Table operation raised an editor error: " + string.Join("; ", fixture.Errors));
}
string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
// GetTable creates this read-only token for each lookup. Measure its allocation separately;
// no shared-token or model implementation change is made by this benchmark.
GC.KeepAlive(new NoDataChangeDeltaModel());
var tokenAllocationStart = GC.GetAllocatedBytesForCurrentThread();
for (int i = 0; i < 1000; i++) GC.KeepAlive(new NoDataChangeDeltaModel());
var emptyLookupTokenBytes = (GC.GetAllocatedBytesForCurrentThread() - tokenAllocationStart) / 1000;
var results = new List<object>();
foreach (var (rows, auxiliary) in new[] { (412, 0), (412, 250), (1024, 250), (2048, 250) }) {
   Console.WriteLine($"Fixture: {rows} species, {auxiliary} auxiliary tables");
   var fixture = new SyntheticPokemonFixture(rows, auxiliary);
   var initialBytes = fixture.Model.RawData.ToArray();
   var start = Stopwatch.GetTimestamp();
   fixture.Navigate(0);
   var coldOpenMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
   CheckSelection(fixture, 0);
   var initialMetadata = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize();
   var table = fixture.ViewPort.Tools.TableTool;
   var phases = new List<object>();
   phases.Add(Measure("real table navigation, no controls attached", () => {
      for (int i = 0; i < 100; i++) fixture.Navigate(i);
   }));
   CheckSelection(fixture, 99);

   // A controlled component experiment, not an application change: same selector, choices,
   // indices, and comparison logic, with candidate construction included versus precomputed.
   var component = new FilteringComboOptions();
   var choices = ComboOption.Convert(fixture.StatsTable.ElementNames).ToArray();
   component.Update(choices, 0);
   phases.Add(Measure("selector plus fresh choice objects", () => {
      for (int i = 0; i < 100; i++) {
         var names = fixture.StatsTable.ElementNames.ToList();
         var candidates = names.Select((name, index) => new ComboOption(name, index)).ToList();
         component.Update(candidates, i);
      }
   }));
   phases.Add(Measure("selector with prebuilt choices, experiment only", () => {
      for (int i = 0; i < 100; i++) component.Update(choices, i);
   }));
   Check(component.ModelValue == 99 && component.DisplayText == "MON0099", "Component experiment changed selection.");
   var filter = new FilteringComboOptions();
   filter.Update(choices, 0);
   phases.Add(Measure("filter preparation, no popup/input rendering", () => {
      for (int i = 0; i < 100; i++) {
         filter.DisplayText = i % 2 == 0 ? "MON00" : "MON01";
         filter.SelectConfirm();
      }
   }));

   phases.Add(Measure("table section/list discovery, 100 paired reads", () => {
      for (int i = 0; i < 100; i++) {
         GC.KeepAlive(table.TableSections);
         GC.KeepAlive(table.TableList);
      }
   }));
   var itemOffset = 0;
   foreach (var field in fixture.StatsTable.ElementContent) {
      if (field.Name == "item1") break;
      itemOffset += field.Length;
   }
   phases.Add(Measure("one item dropdown constructor per entry", () => {
      for (int i = 0; i < 100; i++)
         GC.KeepAlive(new ComboBoxArrayElementViewModel(fixture.ViewPort, null, "item1",
            SyntheticPokemonFixture.Stats + i * fixture.StatsTable.ElementLength + itemOffset, 2));
   }));

   // Untimed counters quantify repeated source access and collection reuse in the real path.
   var profiled = new SyntheticPokemonFixture(rows, auxiliary, instrument: true);
   profiled.Navigate(101);
   var profiledTable = profiled.ViewPort.Tools.TableTool;
   var counted = (CountingPokemonModel)profiled.Model;
   var sectionsNotifications = 0; var listNotifications = 0; var choiceReplacements = 0; var memberChanges = 0;
   profiledTable.PropertyChanged += (_, e) => {
      if (e.PropertyName == nameof(TableTool.TableSections)) sectionsNotifications++;
      if (e.PropertyName == nameof(TableTool.TableList)) listNotifications++;
   };
   profiledTable.CurrentElementSelector.PropertyChanged += (_, e) => {
      if (e.PropertyName == nameof(FilteringComboOptions.AllOptions)) choiceReplacements++;
   };
   foreach (var group in profiledTable.Groups) group.Members.CollectionChanged += (_, _) => memberChanges++;
   counted.ResetCounters();
   for (int i = 0; i < 100; i++) { profiled.Navigate(i); CheckSelection(profiled, i); }
   var diagnostics = new { counted.AnchorListReads, counted.AnchorNameLookups, counted.RunLookups,
      sectionsNotifications, listNotifications, choiceReplacements, memberChanges,
      fieldMembers = profiledTable.Groups.Sum(group => group.Members.Count) };

   // Real production TabView, templates, field presenters, bindings, layout, and Skia drawing.
   // Headless supplies windowing/cursor services; no native macOS window is created.
   var view = new TabView { DataContext = fixture.ViewPort };
   var window = new Window { Width = 1200, Height = 800, Content = view };
   start = Stopwatch.GetTimestamp();
   window.Show(); Dispatcher.UIThread.RunJobs();
   view.Measure(new Size(1200, 800)); view.Arrange(new Rect(0, 0, 1200, 800));
   using var bitmap = new RenderTargetBitmap(new PixelSize(1200, 800));
   bitmap.Render(view);
   var coldUiMountMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
   var controls = view.GetVisualDescendants().OfType<Control>().ToArray();
   var mainSelector = controls.OfType<AngleComboBox>().Single(combo => ReferenceEquals(combo.DataContext, table.CurrentElementSelector));
   Check(mainSelector.SelectedIndex == 99, "Actual dropdown binding did not select the current row.");
   Check(mainSelector.IsEffectivelyVisible, "The table selector must be visible during UI measurements.");
   var hpPresenter = controls.OfType<TextBlock>().FirstOrDefault(block =>
      block.DataContext is FieldArrayElementViewModel field && field.Name == "hp" && block.Text == "139");
   Check(hpPresenter != null, "The production HP field presenter was not rendered with the current value.");
   Check(hpPresenter!.IsEffectivelyVisible, "The HP field presenter must be visible during UI measurements.");
   phases.Add(Measure("real navigation plus headless binding/layout, no forced bitmap draw", () => {
      for (int i = 0; i < 100; i++) {
         fixture.Navigate(i);
         Dispatcher.UIThread.RunJobs();
         view.Measure(new Size(1200, 800)); view.Arrange(new Rect(0, 0, 1200, 800));
      }
   }));
   phases.Add(Measure("static unchanged tab, 100 forced Skia draws", () => {
      for (int i = 0; i < 100; i++) bitmap.Render(view);
   }));
   phases.Add(Measure("real navigation plus headless layout and Skia draw", () => {
      for (int i = 0; i < 100; i++) {
         fixture.Navigate(i);
         Dispatcher.UIThread.RunJobs();
         view.Measure(new Size(1200, 800)); view.Arrange(new Rect(0, 0, 1200, 800));
         bitmap.Render(view);
      }
   }));
   CheckSelection(fixture, 99);
   Check(mainSelector.SelectedIndex == 99 && hpPresenter!.Text == "139", "Visible values diverged after UI navigation.");
   Check(initialBytes.SequenceEqual(fixture.Model.RawData), "Read-only navigation/filtering changed data bytes.");
   Check(initialMetadata.SequenceEqual(fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize()),
      "Read-only navigation changed metadata.");
   Check(!fixture.ViewPort.Undo.CanExecute(null), "Read-only navigation created undo history.");
   var uiSummary = new { coldUiMountMs, totalControls = controls.Length, lightweightTextPresenters = controls.OfType<TextBoxLookAlike>().Count(),
      actualTextBoxes = controls.OfType<TextBox>().Count(), mainSelectorCount = mainSelector.ItemCount };
   window.Close(); Dispatcher.UIThread.RunJobs();

   // Verify invalidation requirements through actual HMA edits and history, outside timings.
   fixture.ViewPort.Goto.Execute(SyntheticPokemonFixture.Names + 99 * 11);
   fixture.ViewPort.Edit("\"RENAMED\" ");
   fixture.ViewPort.ChangeHistory.ChangeCompleted();
   fixture.Navigate(99);
   Check(table.CurrentElementSelector.DisplayText == "RENAMED", "Actual name edit did not refresh selector choices.");
   fixture.ViewPort.Undo.Execute(null);
   fixture.Navigate(99); table.DataForCurrentRunChanged();
   CheckSelection(fixture, 99);
   Check(initialBytes.SequenceEqual(fixture.Model.RawData), "Undo did not restore exact fixture bytes.");
   fixture.ViewPort.Redo.Execute(null);
   fixture.Navigate(99); table.DataForCurrentRunChanged();
   Check(table.CurrentElementSelector.DisplayText == "RENAMED", "Redo did not refresh selector choices.");
   fixture.ViewPort.Undo.Execute(null);
   Check(initialBytes.SequenceEqual(fixture.Model.RawData), "Final undo did not restore fixture data.");
   Check(initialMetadata.SequenceEqual(fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize()),
      "Edit/undo checks did not restore fixture metadata.");
   results.Add(new { rows, auxiliaryTables = auxiliary, statsStride = fixture.StatsTable.ElementLength,
      fixtureSha256 = Hash(initialBytes), coldOpenMs, phases, diagnostics, uiSummary });
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new {
   runtime = RuntimeInformation.FrameworkDescription, platform = RuntimeInformation.OSDescription,
   architecture = RuntimeInformation.ProcessArchitecture.ToString(), configuration = "Release", samplesPerPhase = 7,
   emptyLookupTokenBytes,
   scope = "Synthetic names and real base-stats schema; no ROM, portraits, evolutions, native input or macOS compositor; the prebuilt-choice experiment does not modify editor behavior",
   correctnessChecks = checks, results,
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"BENCHMARK_CHECKS_PASSED:{checks}");
