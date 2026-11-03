using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Performance;
using System.Diagnostics;
using System.Security.Cryptography;

// The external watchdog is essential: an in-thread timeout cannot detect a blocked UI thread.
var romPath = args.Length == 1 ? Path.GetFullPath(args[0]) : null;
if (args.Length > 1) throw new ArgumentException("Usage: MapZoom.dll [local ROM path]");
var romBytes = romPath == null ? null : File.ReadAllBytes(romPath);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
void PumpUntil(Func<bool> ready) {
   var clock = Stopwatch.StartNew();
   while (!ready()) {
      Dispatcher.UIThread.RunJobs();
      if (clock.Elapsed > TimeSpan.FromSeconds(20)) throw new TimeoutException("Map startup/frame did not finish.");
      Thread.Sleep(1);
   }
}

foreach (var startup in Enumerable.Range(0, 3)) {
   Console.WriteLine($"STARTUP:{startup} fixture={(romBytes == null ? "synthetic" : "local ROM")}");
   var window = new MainWindow { Width = 1440, Height = 1000 };
   using var dispatcher = new MacFileSystem(() => window);
   // Never read/write user settings, ROM sidecars, or file watchers in a test.
   var fileSystem = new StubFileSystem { MetadataFor = _ => Array.Empty<string>() };
   Application.Current!.Resources["FileSystem"] = fileSystem;
   var editor = new EditorViewModel(fileSystem, dispatcher);
   App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
   window.Resources["FileSystem"] = fileSystem;
   window.FileSystem = fileSystem;
   window.Editor = editor;
   window.Show();
   ViewPort viewport;
   if (romBytes != null) {
      // Production Open starts model initialization on the actual Mac dispatcher background path.
      editor.Open.Execute(new LoadedFile("zoom-fixture.gba", romBytes.ToArray()));
      Check(editor.Count == 1 && editor[0] is ViewPort, "Open must create a ROM tab.");
      viewport = (ViewPort)editor[0];
   } else {
      var fixture = new SyntheticMapFixture(24, 20);
      var model = fixture.Model;
      var token = new ModelDelta();
      foreach (var address in new[] { 0x104, 0x108, 0x10C }) model.WritePointer(token, address, 0x110);
      var format = model.GetNextRun(0x100).FormatString;
      var error = ArrayRun.TryParse(model, format[..^1] + "4", 0x100, SortedSpan<int>.None, out var root);
      Check(!error.HasError, "Four fabricated banks must parse.");
      model.ObserveAnchorWritten(new NoDataChangeDeltaModel(), "data.maps.banks", root);
      ((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new GotoShortcutModel(
         "synthetic.layout", "data.maps.banks/3/maps/0/map/0/layout/", "Maps"));
      viewport = new ViewPort("zoom-fixture.bin", model, dispatcher, new Singletons(dispatcher),
         tutorials: editor.MapTutorials, fs: fileSystem);
      editor.Add(viewport);
   }
   PumpUntil(() => viewport.MapEditor != null);
   var mapper = viewport.MapEditor;
   Check(mapper.IsValidState, "Map editor must initialize successfully.");
   mapper.NavigateTo(3, 0, 10, 10);
   editor.Add(mapper);
   // Do not wait for the first map bitmap: start zooming as soon as the tab is laid out.
   void Layout() {
      Dispatcher.UIThread.RunJobs();
      window.Measure(new Size(1440, 1000));
      window.Arrange(new Rect(0, 0, 1440, 1000));
   }
   Layout();
   var view = window.GetVisualDescendants().OfType<MapTab>().Single();
   var surface = view.FindControl<ItemsControl>("MapView")!;
   var modelBytes = SHA256.HashData(viewport.Model.RawData);
   var metadata = viewport.Model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize();
   var initialUndo = viewport.Undo.CanExecute(null);
   var maxNeighbors = mapper.VisibleMaps.Count;
   var zoomCount = 0;
   var timer = Stopwatch.StartNew();
   foreach (var enlarge in Enumerable.Range(0, 10).SelectMany(_ =>
      new[] { true, true, true, false, false, false, false, false, false, true, true, true })) {
      var oldScale = mapper.PrimaryMap.SpriteScale;
      var point = surface.TranslatePoint(new Avalonia.Point(surface.Bounds.Width / 2, surface.Bounds.Height / 2), window);
      Check(point.HasValue, "Map surface must be attached to the editor window.");
      window.MouseWheel(point.Value, new Vector(0, enlarge ? 1 : -1), RawInputModifiers.None);
      Check(enlarge ? mapper.PrimaryMap.SpriteScale > oldScale : mapper.PrimaryMap.SpriteScale < oldScale,
         "A routed wheel event must zoom exactly in the requested direction.");
      Check(mapper.PrimaryMap.IncludeBorders == (mapper.PrimaryMap.SpriteScale <= 3),
         "Crossing 3x zoom must toggle border rendering.");
      maxNeighbors = Math.Max(maxNeighbors, mapper.VisibleMaps.Count);
      Layout();
      var heartbeat = false;
      Dispatcher.UIThread.Post(() => heartbeat = true);
      PumpUntil(() => heartbeat);
      zoomCount++;
   }
   PumpUntil(() => view.GetVisualDescendants().OfType<PixelImage>()
      .Where(image => ReferenceEquals(image.DataContext, mapper.PrimaryMap))
      .Any(image => image.Source is WriteableBitmap bitmap && bitmap.PixelSize.Width == mapper.PrimaryMap.PixelWidth));
   Check(mapper.PrimaryMap.SpriteScale == 1, "Repeated zoom must return to 1x.");
   Check(modelBytes.SequenceEqual(SHA256.HashData(viewport.Model.RawData)), "Zoom must preserve every model byte.");
   Check(metadata.SequenceEqual(viewport.Model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()),
      "Zoom must preserve pointer/anchor metadata.");
   Check(initialUndo == viewport.Undo.CanExecute(null), "Zoom must not create edit history.");
   if (romBytes != null) Check(maxNeighbors > 3, "Local FireRed scenario must load deeper neighboring maps.");
   Console.WriteLine($"ZOOM_COMPLETE:{zoomCount} elapsed_ms={timer.Elapsed.TotalMilliseconds:F1} max_visible_maps={maxNeighbors}");
   // Detach before closing so MainWindow does not persist application settings or prompt for edits.
   window.Editor = null;
   window.Close();
   Dispatcher.UIThread.RunJobs();
}
if (romPath != null) Check(romBytes!.SequenceEqual(File.ReadAllBytes(romPath)), "Source ROM must remain unchanged.");
Console.WriteLine("PASS: fresh full-editor tabs, routed wheel zoom, borders, pending rendering, UI heartbeats, and data integrity.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
