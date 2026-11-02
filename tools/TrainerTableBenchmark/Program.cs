using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;

if (args.Length != 1) throw new ArgumentException("Expected one JSON output path.");
var output = Path.GetFullPath(args[0]);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
Application.Current!.Resources["FileSystem"] = new StubFileSystem();
var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
object Measure(string phase, Action action) {
   action(); action();
   var times = new List<double>(); var allocated = new List<long>();
   for (int sample = 0; sample < 7; sample++) {
      var before = GC.GetAllocatedBytesForCurrentThread(); var start = Stopwatch.GetTimestamp();
      action();
      times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
      allocated.Add(GC.GetAllocatedBytesForCurrentThread() - before);
   }
   var medianMs = times.Order().ElementAt(3); var medianAllocated = allocated.Order().ElementAt(3);
   Console.WriteLine($"  {phase}: {medianMs:F3} ms, {medianAllocated:N0} bytes / 100 operations");
   return new { phase, steps = 100, medianMs, medianAllocated, samplesMs = times, samplesAllocated = allocated };
}
string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
string PixelHash(IReadOnlyList<IPixelViewModel> images) {
   var bytes = new List<byte>();
   foreach (var image in images) {
      var buffer = new byte[image.PixelData.Length * 2];
      Buffer.BlockCopy(image.PixelData, 0, buffer, 0, buffer.Length); bytes.AddRange(buffer);
   }
   return Hash(bytes.ToArray());
}
void CheckSelection(SyntheticTrainerFixture fixture, int row) {
   var selector = fixture.ViewPort.Tools.TableTool.CurrentElementSelector;
   Check(selector.ModelValue == row, "Trainer selector index drifted.");
   Check(fixture.TeamEditor.Content.Contains($"MON{row:D4}"), "Trainer text does not show its expected first Pokémon.");
   Check(fixture.TeamEditor.Run.Start == fixture.Team(row).Start, "Trainer editor points at the wrong team.");
   Check(fixture.Team(row).ElementCount == fixture.Count(row) && fixture.Team(row).StructType == fixture.Type(row), "Team count/format drifted.");
   Check(fixture.Errors.Count == 0, "Editor error: " + string.Join("; ", fixture.Errors));
}
var results = new List<object>();
foreach (var scenario in new[] {
   new Scenario("basic-one", 1, 0, false, false),
   new Scenario("custom-one", 1, 3, false, false),
   new Scenario("custom-six", 6, 3, false, false),
   new Scenario("custom-six-team-previews", 6, 3, true, false),
   new Scenario("custom-six-portrait-options", 6, 3, false, true),
   new Scenario("mixed-all-artwork", 1, 0, true, true, true),
}) {
   Console.WriteLine($"Fixture: {scenario.Id}");
   var fixture = new SyntheticTrainerFixture(scenario.Members, scenario.Type, scenario.Sprites, scenario.Portraits, scenario.Mixed);
   var initialBytes = fixture.Model.RawData.ToArray();
   fixture.Navigate(0); CheckSelection(fixture, 0);
   var table = fixture.ViewPort.Tools.TableTool;
   var metadata = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize();
   var anchors = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).NamedAnchors
      .Select(anchor => (anchor.Address, anchor.Name, anchor.Format)).ToArray();
   var phases = new List<object>();
   phases.Add(Measure("navigation without controls", () => { for (int i = 0; i < 100; i++) fixture.Navigate(i); }));
   CheckSelection(fixture, 99);
   phases.Add(Measure("serialize 100 teams", () => { for (int i = 0; i < 100; i++) GC.KeepAlive(fixture.Team(i).SerializeRun()); }));
   var serializedTeams = Enumerable.Range(0, 100).Select(row => fixture.Team(row).SerializeRun()).ToArray();
   var formatter = fixture.Team(99).PreFormatter;
   phases.Add(Measure("validate 100 serialized teams, serialization excluded", () => {
      for (int i = 0; i < 100; i++) GC.KeepAlive(formatter.FindErrors(serializedTeams[i]).ToArray());
   }));
   var moveNames = fixture.Model.GetOptions(HardcodeTablesModel.MoveNamesTable);
   phases.Add(Measure("match 100 prefixed move lines", () => {
      for (int i = 0; i < 100; i++) moveNames.TryMatch($"- MOVE{i + 1:D4}", out _);
   }));
   phases.Add(Measure("match 100 bare move names, diagnostic only", () => {
      for (int i = 0; i < 100; i++) moveNames.TryMatch($"MOVE{i + 1:D4}", out _);
   }));
   var prefixedLookup = moveNames.TryMatch("- MOVE0001", out var prefixedIndex);
   var bareLookup = moveNames.TryMatch("MOVE0001", out var bareIndex);
   Check(bareLookup && bareIndex == 1, "Synthetic exact move lookup failed.");
   phases.Add(Measure("construct 100 generic team stream candidates", () => {
      for (int i = 0; i < 100; i++) GC.KeepAlive(new TextStreamElementViewModel(fixture.ViewPort, "pokemon", fixture.Parent(i) + 36, TrainerPokemonTeamRun.SharedFormatString));
   }));
   phases.Add(Measure("construct 100 specialized team stream candidates", () => {
      for (int i = 0; i < 100; i++) GC.KeepAlive(new TrainerPokemonTeamElementViewModel(fixture.ViewPort, fixture.Team(i), "pokemon", fixture.Parent(i) + 36));
   }));
   phases.Add(Measure("construct four held-item dropdowns per trainer", () => {
      for (int i = 0; i < 100; i++) for (int item = 0; item < 4; item++)
         GC.KeepAlive(new ComboBoxArrayElementViewModel(fixture.ViewPort, null, $"item{item + 1}", fixture.Parent(i) + 16 + item * 2, 2));
   }));
   phases.Add(Measure("construct portrait dropdown per trainer", () => {
      for (int i = 0; i < 100; i++) GC.KeepAlive(new ComboBoxArrayElementViewModel(fixture.ViewPort, null, "sprite", fixture.Parent(i) + 3, 1));
   }));
   // Experiment only: excludes choice creation and does not add a cache to the application.
   var portrait = table.Groups.SelectMany(group => group.Members).OfType<ComboBoxArrayElementViewModel>().Single(field => field.Name == "sprite");
   var choices = portrait.FilteringComboOptions.AllOptions.ToArray();
   var component = new FilteringComboOptions(); component.Update(choices, 0);
   phases.Add(Measure("portrait selector with prebuilt choices, experiment only", () => {
      for (int i = 0; i < 100; i++) component.Update(choices, i % 64);
   }));
   phases.Add(Measure("read 100 team preview collections", () => {
      for (int i = 0; i < 100; i++) GC.KeepAlive(fixture.Team(i).Visualizations);
   }));
   var previews = fixture.Team(99).Visualizations;
   var repeatedPreviews = fixture.Team(99).Visualizations;
   Check(previews.Count == (scenario.Sprites ? fixture.Count(99) : 0), "Preview count differs from team size.");
   Check(previews.All(image => image.PixelWidth == 64 && image.PixelHeight == 64 && image.PixelData.Length == 4096), "Synthetic sprite size drifted.");
   Check(PixelHash(previews) == PixelHash(repeatedPreviews), "Repeated preview requests changed pixels.");

   // Untimed navigation checks measure reuse/notifications without polluting the timing loop.
   fixture.Navigate(101);
   var stream = fixture.TeamEditor; var portraitChoices = portrait.FilteringComboOptions.AllOptions;
   var visualNotifications = 0; var memberChanges = 0;
   stream.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TextStreamElementViewModel.Visualizations)) visualNotifications++; };
   foreach (var group in table.Groups) group.Members.CollectionChanged += (_, _) => memberChanges++;
   for (int i = 0; i < 100; i++) { fixture.Navigate(i); CheckSelection(fixture, i); }
   var diagnostics = new {
      visualNotifications, memberChanges, sameTeamEditor = ReferenceEquals(stream, fixture.TeamEditor),
      samePortraitChoices = ReferenceEquals(portraitChoices, portrait.FilteringComboOptions.AllOptions),
      freshPreviewObjects = previews.Count > 0 && !ReferenceEquals(previews[0], repeatedPreviews[0]),
      prefixedLookup, prefixedIndex, bareLookup, bareIndex,
      memberCount = table.Groups.Sum(group => group.Members.Count),
   };

   var view = new TabView { DataContext = fixture.ViewPort };
   var window = new Window { Width = 1440, Height = 1000, Content = view };
   window.Show(); Dispatcher.UIThread.RunJobs();
   void Layout() { view.Measure(new Size(1440, 1000)); view.Arrange(new Rect(0, 0, 1440, 1000)); }
   Layout();
   using var bitmap = new RenderTargetBitmap(new PixelSize(1440, 1000)); bitmap.Render(view);
   var controls = view.GetVisualDescendants().OfType<Control>().ToArray();
   var editor = controls.OfType<TextEditor>().SingleOrDefault(control => ReferenceEquals(control.DataContext, fixture.TeamEditor.ElementContent));
   var textBox = editor?.EditBox ?? controls.OfType<TextBox>().Single(control => control.Name == "StreamTextBox" && ReferenceEquals(control.DataContext, fixture.TeamEditor));
   var productionTemplate = editor == null ? "generic stream TextBox" : "specialized trainer TextEditor";
   Console.WriteLine($"  Production team template: {productionTemplate}");
   Check(textBox.IsEffectivelyVisible, "Production team text box is hidden.");
   Check(textBox.Text == fixture.TeamEditor.Content, "Production team text binding diverged.");
   var selector = controls.OfType<AngleComboBox>().Single(control => ReferenceEquals(control.DataContext, table.CurrentElementSelector));
   Check(selector.IsEffectivelyVisible && selector.SelectedIndex == 99, "Visible trainer selector binding diverged.");
   // Obtain the actual bound preview collection rather than calling Visualizations again.
   var previewItems = controls.OfType<ItemsControl>().SingleOrDefault(control => ReferenceEquals(control.DataContext, fixture.TeamEditor)
      && control.ItemsSource is IEnumerable<IPixelViewModel>);
   IPixelViewModel[] BoundPreviews() => previewItems == null ? Array.Empty<IPixelViewModel>() : ((IEnumerable<IPixelViewModel>)previewItems.ItemsSource).ToArray();
   PixelImage[] BoundImages() {
      var bound = BoundPreviews();
      return view.GetVisualDescendants().OfType<PixelImage>().Where(image => bound.Any(vm => ReferenceEquals(image.DataContext, vm))).ToArray();
   }
   var initialImages = BoundImages();
   var expectedBoundPreviews = previewItems == null ? 0 : previews.Count;
   Check(initialImages.Length == expectedBoundPreviews && initialImages.All(image => image.Source != null), "Bound previews lack published bitmaps.");
   fixture.Navigate(98); Dispatcher.UIThread.RunJobs(); Layout();
   var nextImages = BoundImages();
   var replacedPreviewControls = initialImages.Count(image => !nextImages.Contains(image));
   phases.Add(Measure("navigation plus production headless bindings/layout", () => {
      for (int i = 0; i < 100; i++) { fixture.Navigate(i); Dispatcher.UIThread.RunJobs(); Layout(); }
   }));
   CheckSelection(fixture, 99);
   Check(textBox.Text == fixture.TeamEditor.Content && selector.SelectedIndex == 99, "Visible values drifted after navigation.");
   Check(previewItems == null || PixelHash(BoundPreviews()) == PixelHash(previews), "Bound final preview pixels drifted.");
   Check(BoundImages().Length == expectedBoundPreviews && BoundImages().All(image => image.Source != null), "Final preview bitmaps are missing.");
   bitmap.Render(view); // actual Skia drawing is checked, not included in navigation timings
   Check(initialBytes.SequenceEqual(fixture.Model.RawData), "Read-only work modified trainer/team/sprite bytes.");
   Check(metadata.SequenceEqual(fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize()), "Read-only work modified metadata.");
   Check(!fixture.ViewPort.Undo.CanExecute(null), "Navigation created undo history.");
   var ui = new { productionTemplate, totalControls = controls.Length, replacedPreviewControls,
      boundPreviewCount = BoundPreviews().Length, boundTeamTextCharacters = textBox.Text.Length };
   object orderingProbe = null;
   if (scenario.Id == "mixed-all-artwork") {
      // Diagnostic experiment only, after production timings: reorder one in-memory template
      // collection and regenerate its presenters. No application XAML or model data is edited.
      var host = controls.OfType<ItemsControl>().Single(control => control.ItemsSource is IEnumerable<IArrayElementViewModel> fields
         && fields.Any(field => ReferenceEquals(field, fixture.TeamEditor)));
      var trainerTemplate = host.DataTemplates.OfType<DataTemplate>().Single(template => template.DataType == typeof(TrainerPokemonTeamElementViewModel));
      var source = host.ItemsSource; host.ItemsSource = null;
      host.DataTemplates.Remove(trainerTemplate); host.DataTemplates.Insert(0, trainerTemplate);
      host.ItemsSource = source; Dispatcher.UIThread.RunJobs(); Layout();
      var probeControls = view.GetVisualDescendants().OfType<Control>().ToArray();
      var specialized = probeControls.OfType<TextEditor>().Single(control => ReferenceEquals(control.DataContext, fixture.TeamEditor.ElementContent));
      var probePreviews = probeControls.OfType<ItemsControl>().Single(control => ReferenceEquals(control.DataContext, fixture.TeamEditor)
         && control.ItemsSource is IEnumerable<IPixelViewModel>);
      var probeImages = ((IEnumerable<IPixelViewModel>)probePreviews.ItemsSource).ToArray();
      Check(specialized.IsEffectivelyVisible && specialized.EditBox.Text == fixture.TeamEditor.Content, "Template-order probe did not reveal bound trainer editor.");
      Check(probeImages.Length == fixture.Count(99) && PixelHash(probeImages) == PixelHash(previews), "Template-order probe preview count/pixels diverged.");
      Check(probeControls.OfType<AngleButton>().Any(button => ReferenceEquals(button.Command, fixture.TeamEditor.SetDefaultMoves))
         && probeControls.OfType<AngleButton>().Any(button => ReferenceEquals(button.Command, fixture.TeamEditor.SetDefaultItems)), "Template-order probe did not reveal trainer commands.");
      bitmap.Render(view);
      Check(initialBytes.SequenceEqual(fixture.Model.RawData), "Template-order experiment wrote model data.");
      orderingProbe = new { specializedTrainerEditors = 1, previewCount = probeImages.Length, defaultCommandsPresent = true,
         modelBytesUnchanged = true, note = "In-memory template ordering experiment after baseline timings; application XAML unchanged" };
      Console.WriteLine($"  Ordering probe: specialized trainer editor + {probeImages.Length} previews + default commands present");
   }
   window.Close(); Dispatcher.UIThread.RunJobs();

   // Exercise real stream editing, parent updates, forced growth relocation, and history.
   var oldTeam = fixture.Team(99); var oldPointer = oldTeam.Start;
   var oldParents = Enumerable.Range(0, SyntheticTrainerFixture.Rows).Select(i => fixture.Model.ReadPointer(fixture.Parent(i) + 36)).ToArray();
   var oldParentPrefix = fixture.Model.RawData[fixture.Parent(99)..(fixture.Parent(99) + 32)];
   var blocks = fixture.TeamEditor.Content.Split(Environment.NewLine + Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
      .Select(block => block.Trim()).Where(block => block.Length > 0).ToArray();
   Check(blocks.Length == oldTeam.ElementCount, "Team block fixture does not match serialized members.");
   var newCount = blocks.Length == 6 ? 5 : blocks.Length + 1;
   var edited = string.Join(Environment.NewLine + Environment.NewLine,
      blocks.Length == 6 ? blocks.Take(5) : blocks.Concat(new[] { blocks[0] })) + Environment.NewLine;
   fixture.TeamEditor.Content = edited;
   fixture.ViewPort.ChangeHistory.ChangeCompleted();
   var editedTeam = fixture.Team(99);
   Check(editedTeam.ElementCount == newCount && fixture.Model.ReadMultiByteValue(fixture.Parent(99) + 32, 4) == newCount, "Editing did not update parent team count.");
   Check(editedTeam.StructType == oldTeam.StructType, "Team format flags changed unexpectedly.");
   Check(oldParentPrefix.SequenceEqual(fixture.Model.RawData[fixture.Parent(99)..(fixture.Parent(99) + 32)]), "Team editing changed unrelated parent fields.");
   if (newCount > oldTeam.ElementCount) Check(editedTeam.Start != oldPointer, "Occupied bytes did not force actual team relocation.");
   for (int i = 0; i < oldParents.Length; i++) if (i != 99)
      Check(oldParents[i] == fixture.Model.ReadPointer(fixture.Parent(i) + 36), "Editing changed another trainer's team pointer.");
   fixture.ViewPort.Undo.Execute(null);
   Check(initialBytes.SequenceEqual(fixture.Model.RawData), "Undo did not restore exact fixture bytes.");
   Check(anchors.SequenceEqual(fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).NamedAnchors
      .Select(anchor => (anchor.Address, anchor.Name, anchor.Format))), "Undo did not restore anchor definitions.");
   fixture.ViewPort.Redo.Execute(null);
   Check(fixture.Team(99).ElementCount == newCount && fixture.Team(99).Start == editedTeam.Start, "Redo did not restore edited team/count/pointer.");
   fixture.ViewPort.Undo.Execute(null);
   Check(initialBytes.SequenceEqual(fixture.Model.RawData) && fixture.Errors.Count == 0, "Final restoration/editor error check failed.");

   results.Add(new { scenario, rows = SyntheticTrainerFixture.Rows, species = 412, itemOptions = 512, moveOptions = 512,
      trainerPortraitOptions = 64, fixtureSha256 = Hash(initialBytes), teamTextSha256 = Hash(System.Text.Encoding.UTF8.GetBytes(oldTeam.SerializeRun())),
      previewPixelSha256 = PixelHash(previews), phases, diagnostics, ui, orderingProbe });
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new {
   runtime = RuntimeInformation.FrameworkDescription, platform = RuntimeInformation.OSDescription,
   architecture = RuntimeInformation.ProcessArchitecture.ToString(), configuration = "Release", samplesPerPhase = 7,
   scope = "Synthetic trainer schema, text teams, uncompressed artwork, production headless bindings/layout; no ROM, native input/compositor, default-move legality or compiler coverage; no application optimization implemented",
   correctnessChecks = checks, results,
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"BENCHMARK_CHECKS_PASSED:{checks}");

record Scenario(string Id, int Members, byte Type, bool Sprites, bool Portraits, bool Mixed = false);
