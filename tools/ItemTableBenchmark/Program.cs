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

if (args.Length < 1 || args.Length > 3) throw new ArgumentException("Expected output JSON path and optional local ROM path and --rom-only.");
if (args.Length == 3 && args[2] != "--rom-only") throw new ArgumentException("Unknown option; expected --rom-only.");
var output = Path.GetFullPath(args[0]); var rom = args.Length >= 2 ? Path.GetFullPath(args[1]) : null;
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory); Application.Current!.Resources["FileSystem"] = new StubFileSystem();
int checks = 0; int steps = 100;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
object Measure(string phase, Action action) {
   action(); action(); var times = new List<double>(); var bytes = new List<long>();
   for (int i = 0; i < 7; i++) {
      var before = GC.GetAllocatedBytesForCurrentThread(); var start = Stopwatch.GetTimestamp(); action();
      times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds); bytes.Add(GC.GetAllocatedBytesForCurrentThread() - before);
   }
   var medianMs = times.Order().ElementAt(3); var medianAllocated = bytes.Order().ElementAt(3);
   Console.WriteLine($"{phase}: {medianMs:F3} ms / {medianAllocated:N0} bytes per {steps}");
   return new { phase, steps, medianMs, medianAllocated, samplesMs = times, samplesAllocated = bytes };
}
void Layout(TabView view) { Dispatcher.UIThread.RunJobs(); view.Measure(new Size(1440, 1000)); view.Arrange(new Rect(0, 0, 1440, 1000)); }
var scenarios = new List<(string Name, Func<ItemFixture> Create)> {
   ("synthetic-375-no-icons", () => new ItemFixture(375, 40, false)),
   ("synthetic-375-icons", () => new ItemFixture(375, 40, true)),
   ("synthetic-1024-icons", () => new ItemFixture(1024, 40, true)),
   ("synthetic-375-long-descriptions", () => new ItemFixture(375, 1000, true)),
};
if (args.Length == 3 && args[2] == "--rom-only") scenarios.Clear();
if (rom != null) scenarios.Add(("supplied-rom", () => ItemFixture.FromRom(rom)));
var results = new List<object>();
foreach (var scenario in scenarios) {
   Console.WriteLine("Fixture: " + scenario.Name);
   steps = scenario.Name == "supplied-rom" ? 10 : 100;
   var fixture = scenario.Create(); var model = fixture.Model; var stats = fixture.Stats;
   Console.WriteLine($"Item rows: {stats.ElementCount}, bytes per row: {stats.ElementLength}");
   var original = Hash(model.RawData); var metadata = model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize();
   var expectedDescriptions = new List<string>(); var pixels = new List<string>();
   for (int row = 0; row < steps; row++) {
      fixture.Navigate(row); if (rom != null && scenario.Name == "supplied-rom") Console.WriteLine("Verified real-ROM row " + row); Check(fixture.ViewPort.Tools.TableTool.CurrentElementSelector.ModelValue == row, "Wrong item selection.");
      expectedDescriptions.Add(fixture.Description.Content);
      foreach (var sprite in fixture.Members.OfType<SpriteElementViewModel>()) {
         Check(sprite.PixelWidth == 24 && sprite.PixelHeight == 24 && sprite.PixelData?.Length == 576, "Incorrect item icon dimensions.");
         var data = new byte[sprite.PixelData.Length * 2]; Buffer.BlockCopy(sprite.PixelData, 0, data, 0, data.Length); pixels.Add(Hash(data));
      }
   }
   var phases = new List<object>();
   phases.Add(Measure("navigate without controls", () => { for (int row = 0; row < steps; row++) fixture.Navigate(row); }));
   if (scenario.Name == "supplied-rom") {
      phases.Add(Measure("refresh table directly", () => { for (int row = 0; row < steps; row++) fixture.ViewPort.Tools.TableTool.DataForCurrentRunChanged(); }));
      phases.Add(Measure("dependent table usage search", () => {
         for (int row = 0; row < steps; row++) foreach (var table in model.Arrays) {
            if (table.DependsOn(HardcodeTablesModel.ItemsTableName)) GC.KeepAlive(table.Search(model, HardcodeTablesModel.ItemsTableName, row).ToArray());
         }
      }));
      phases.Add(Measure("dependent stream usage search", () => {
         for (int row = 0; row < steps; row++) foreach (var stream in model.Streams) {
            if (!stream.DependsOn(HardcodeTablesModel.ItemsTableName)) continue;
            if (stream is TrainerPokemonTeamRun team) GC.KeepAlive(team.Search(HardcodeTablesModel.ItemsTableName, row).ToArray());
            if (stream is TableStreamRun table) GC.KeepAlive(table.Search(HardcodeTablesModel.ItemsTableName, row).ToArray());
         }
      }));
      phases.Add(Measure("table navigation discovery outside refresh", () => {
         for (int row = 0; row < steps; row++) { GC.KeepAlive(fixture.ViewPort.Tools.TableTool.TableSections); GC.KeepAlive(fixture.ViewPort.Tools.TableTool.TableList); }
      }));
   }
   phases.Add(Measure("construct description candidates", () => { for (int row = 0; row < steps; row++) GC.KeepAlive(new TextStreamElementViewModel(fixture.ViewPort, "description", fixture.FieldAddress("description", row), "\"\"")); }));
   foreach (var name in new[] { "holdeffect", "pocket" }) {
      var segment = stats.ElementContent.Single(s => s.Name == name);
      phases.Add(Measure("construct " + name + " dropdown candidates", () => { for (int row = 0; row < steps; row++) GC.KeepAlive(new ComboBoxArrayElementViewModel(fixture.ViewPort, null, name, fixture.FieldAddress(name, row), segment.Length)); }));
   }
   if (fixture.Icons != null) {
      phases.Add(Measure("construct icon candidates", () => {
         for (int row = 0; row < steps; row++) {
            var pointer = fixture.Icons.Start + row * fixture.Icons.ElementLength;
            var sprite = (ISpriteRun)model.GetNextRun(model.ReadPointer(pointer));
            GC.KeepAlive(new SpriteElementViewModel(fixture.ViewPort, "sprite", sprite.FormatString, sprite.SpriteFormat, pointer));
         }
      }));
      phases.Add(Measure("construct palette candidates", () => {
         for (int row = 0; row < steps; row++) {
            var pointer = fixture.Icons.Start + row * fixture.Icons.ElementLength + 4;
            var palette = (IPaletteRun)model.GetNextRun(model.ReadPointer(pointer));
            GC.KeepAlive(new PaletteElementViewModel(fixture.ViewPort, fixture.ViewPort.ChangeHistory, "palette", palette.FormatString, palette.PaletteFormat, pointer));
         }
      }));
   }
   var view = new TabView { DataContext = fixture.ViewPort }; var window = new Window { Width = 1440, Height = 1000, Content = view }; window.Show(); Layout(view);
   using var bitmap = new RenderTargetBitmap(new PixelSize(1440, 1000)); bitmap.Render(view);
   phases.Add(Measure("navigate with production controls and layout", () => { for (int row = 0; row < steps; row++) { fixture.Navigate(row); Layout(view); } }));
   var icon = fixture.Members.OfType<SpriteElementViewModel>().SingleOrDefault();
   if (icon != null) {
      phases.Add(Measure("refresh selected icon with production controls", () => {
         for (int row = 0; row < steps; row++) { icon.UpdateTiles(); Layout(view); }
      }));
   }
   var notifications = new Dictionary<string, int>();
   foreach (var field in fixture.Members) if (field is System.ComponentModel.INotifyPropertyChanged notify)
      notify.PropertyChanged += (_, e) => { var key = field.GetType().Name + "." + e.PropertyName; notifications[key] = notifications.GetValueOrDefault(key) + 1; };
   for (int row = 0; row < steps; row++) { fixture.Navigate(row); Layout(view); } // Untimed diagnostics.
   var boundPixelHashes = new List<string>();
   for (int row = 0; row < 3; row++) {
      fixture.Navigate(row); Layout(view);
      var textBox = view.GetVisualDescendants().OfType<TextBox>().Single(c => c.Name == "StreamTextBox" && ReferenceEquals(c.DataContext, fixture.Description));
      Check(textBox.Text == expectedDescriptions[row], "Bound item description differs.");
      foreach (var sprite in fixture.Members.OfType<SpriteElementViewModel>()) {
         var image = view.GetVisualDescendants().OfType<PixelImage>().Single(c => ReferenceEquals(c.DataContext, sprite));
         Check(image.Source is WriteableBitmap b && b.PixelSize == new PixelSize(24, 24), "Bound item icon is missing.");
         using var frame = ((WriteableBitmap)image.Source).Lock(); var rgba = new byte[frame.RowBytes * frame.Size.Height];
         Marshal.Copy(frame.Address, rgba, 0, rgba.Length); boundPixelHashes.Add(Hash(rgba));
      }
   }
   Check(original == Hash(model.RawData), "Read-only benchmark changed bytes.");
   Check(metadata.SequenceEqual(model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize()), "Read-only benchmark changed metadata.");
   Check(!fixture.ViewPort.Undo.CanExecute(null) && fixture.Errors.Count == 0, "Read-only benchmark created history/errors.");
   results.Add(new { scenario = scenario.Name, count = stats.ElementCount, statsFormat = stats.FormatString, sourceSha256 = original,
      descriptionHash = Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", expectedDescriptions))), pixelHashes = pixels, boundPixelHashes,
      fields = fixture.Members.GroupBy(m => m.GetType().Name).ToDictionary(g => g.Key, g => g.Count()), notifications, phases });
   window.Close(); Dispatcher.UIThread.RunJobs();
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new { runtime = RuntimeInformation.FrameworkDescription, architecture = RuntimeInformation.ProcessArchitecture.ToString(), correctnessChecks = checks, scope = "Item-only component timings and headless bindings; no native input latency claim", results }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"BENCHMARK_CHECKS_PASSED:{checks}");
