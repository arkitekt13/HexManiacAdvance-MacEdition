using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
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
using HexManiac.Core.Models.Runs.Sprites;
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
      model.ObserveRunWritten(new NoDataChangeDeltaModel(),
         new BlockmapRun(model,SyntheticMapFixture.Cells,new SortedSpan<int>(0x14C),24,20));
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
   var dimensions = view.FindControl<TextBlock>("MapDimensionsText")!;
   string ExpectedSize() {
      var layout = mapper.PrimaryMap.GetLayout();
      return $"Map: {layout.GetValue("width")} × {layout.GetValue("height")} tiles";
   }
   Check(dimensions.Text == ExpectedSize(), "Status dimensions must use the current map layout, excluding borders.");
   var zoomIn = view.FindControl<Button>("ZoomInButton")!;
   var zoomOut = view.FindControl<Button>("ZoomOutButton")!;
   void ClickZoom(Button button, double expected) {
      var old = mapper.PrimaryMap;
      var (scale,left,top,width,height) = (old.SpriteScale,old.LeftEdge,old.TopEdge,old.PixelWidth,old.PixelHeight);
      var point = button.TranslatePoint(new Avalonia.Point(button.Bounds.Width/2,button.Bounds.Height/2),window)!.Value;
      Check(button.IsEnabled, "A valid zoom step must remain enabled.");
      window.MouseDown(point,MouseButton.Left);window.MouseUp(point,MouseButton.Left);Layout();
      Check(mapper.PrimaryMap.SpriteScale == expected, "A status button click must use exactly one existing zoom step.");
      Check(Math.Abs(mapper.PrimaryMap.LeftEdge-(left*(expected/scale)-(mapper.PrimaryMap.PixelWidth-width)/2))<=1 &&
         Math.Abs(mapper.PrimaryMap.TopEdge-(top*(expected/scale)-(mapper.PrimaryMap.PixelHeight-height)/2))<=1,
         "Button zoom must use the view-center anchor and existing border-visibility compensation.");
      Check(dimensions.Text == ExpectedSize(), "Zoom and border toggling must not change native map dimensions.");
   }
   if (startup == 0) {
      foreach(var scale in new[]{.5,.25,.125,.0625}) ClickZoom(zoomOut,scale);
      Check(!zoomOut.IsEnabled && zoomIn.IsEnabled, "Zoom out must disable at 1/16x.");
      foreach(var scale in new[]{.125,.25,.5,1d,2,3,4,5,6,7,8,9,10}) ClickZoom(zoomIn,scale);
      Check(!zoomIn.IsEnabled && zoomOut.IsEnabled, "Zoom in must disable at 10x.");
      for(int scale=9;scale>=1;scale--) ClickZoom(zoomOut,scale);
      var layout=mapper.PrimaryMap.GetLayout();
      var expectedTip=$"{(long)layout.GetValue("width")*16} × {(long)layout.GetValue("height")*16} pixels · 16 × 16 pixels per tile";
      Check((string)ToolTip.GetTip(dimensions)==expectedTip,"Dimensions tooltip must expose native pixel size independent of zoom.");
      if(romBytes==null) {
         mapper.NavigateTo(0,0,10,10);Layout();
         Check(dimensions.Text==ExpectedSize(),"Map navigation must bind the newly selected map dimensions.");
         mapper.NavigateTo(3,0,10,10);Layout();
      }
   }

   var arrow = view.GetVisualDescendants().OfType<Avalonia.Controls.Border>().First(border =>
      border.Name == "PathBackground" && border.DataContext is ExpansionSlider candidate &&
      candidate.ContextItems.Any(command => command.Text == "Create New Map"));
   var slider = (ExpansionSlider)arrow.DataContext!;
   var menu = arrow.ContextMenu!;
   IPointer pointer = null;
   arrow.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer,
      RoutingStrategies.Tunnel, handledEventsToo: true);
   var arrowPoint = arrow.TranslatePoint(new Avalonia.Point(10, 10), window)!.Value;
   var menuInvocations = 0;
   var bankPrompts = 0;
   fileSystem.ShowOptions = (title, prompt, details, options) => {
      Check(title == "Pick a bank" && details.Count > 0, "Create New Map must reach its bank-selection dialog.");
      bankPrompts++;
      return -1; // Cancel before any map allocation, pointer writes, or connection changes.
   };
   var probe = new MenuCommand("Test connection-menu routing", () => menuInvocations++);
   slider.ContextItems.Add(probe);
   foreach (var (button, modifiers) in new[] {
      (MouseButton.Right, RawInputModifiers.None),
      (MouseButton.Left, RawInputModifiers.Control),
   }) {
      if (modifiers == RawInputModifiers.Control && !OperatingSystem.IsMacOS()) continue;
      window.MouseDown(arrowPoint, button, modifiers);
      window.MouseUp(arrowPoint, button, modifiers);
      Layout();
      Check(menu.IsOpen, "Secondary-click and macOS Control-click must open the arrow's connection menu.");
      Check(!ReferenceEquals(pointer?.Captured, arrow), "Context clicks must not capture the arrow for resizing.");
      Check(menu.ItemsSource == slider.ContextItems, "Arrow menu must retain its real connection commands.");
      var items = menu.GetLogicalDescendants().OfType<MenuItem>().ToArray();
      var probeItem = items.Single(item => ReferenceEquals(item.DataContext, probe));
      Check(ReferenceEquals(probeItem.Command, probe) && Equals(probeItem.Header, probe.Text),
         "Connection menu items must bind their command and visible label.");
      probeItem.Command!.Execute(probeItem.CommandParameter);
      var createItem = items.Single(item => item.DataContext is IMenuCommand { Text: "Create New Map" });
      Check(createItem.Command?.CanExecute(createItem.CommandParameter) == true,
         "The real Create New Map menu item must receive its connection parameter.");
      createItem.Command!.Execute(createItem.CommandParameter);
      menu.Close();
      window.MouseMove(arrowPoint + new Vector(32, 0));
      Layout();
      Check(modelBytes.SequenceEqual(SHA256.HashData(viewport.Model.RawData)),
         "Context clicks and subsequent pointer motion must not resize a map or create a connection.");
   }
   Check(menuInvocations == (OperatingSystem.IsMacOS() ? 2 : 1), "Menu commands must remain executable.");
   Check(bankPrompts == menuInvocations, "Each real Create New Map invocation must reach the bank prompt.");
   // A primary click remains a drag gesture, with its release returning to idle.
   window.MouseDown(arrowPoint, MouseButton.Left);
   Check(ReferenceEquals(pointer?.Captured, arrow), "Plain left-click must still capture the arrow for dragging.");
   window.MouseUp(arrowPoint, MouseButton.Left);
   Check(!ReferenceEquals(pointer?.Captured, arrow), "Finishing a left drag must release the arrow.");
   Layout();
   Check(!menu.IsOpen, "Plain left-click must not open the connection menu.");
   Check(modelBytes.SequenceEqual(SHA256.HashData(viewport.Model.RawData)), "Stationary arrow clicks must preserve map bytes.");
   slider.ContextItems.Remove(probe);
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
   if(startup==0 && romBytes==null) {
      var originalText=dimensions.Text;
      mapper.PrimaryMap.GetMapSliders().Single(s=>s.Icon==MapSliderIcons.ExtendRight).Move(1,0);
      Layout();
      Check(dimensions.Text=="Map: 25 × 20 tiles","Resizing must refresh status width immediately.");
      mapper.PrimaryMap.GetMapSliders().Single(s=>s.Icon==MapSliderIcons.ExtendDown).Move(0,1);
      viewport.ChangeHistory.ChangeCompleted();Layout();
      Check(dimensions.Text=="Map: 25 × 21 tiles","Resizing must refresh status height immediately.");
      Check((string)ToolTip.GetTip(dimensions)=="400 × 336 pixels · 16 × 16 pixels per tile","Resizing must refresh the native pixel tooltip.");
      mapper.Undo.Execute(null);Layout();
      Check(dimensions.Text==originalText,"Undo must restore the original dimension readout.");
      mapper.Redo.Execute(null);Layout();
      Check(dimensions.Text=="Map: 25 × 21 tiles","Redo must restore resized dimensions.");
      mapper.Undo.Execute(null);Layout();
      Check(modelBytes.SequenceEqual(SHA256.HashData(viewport.Model.RawData)),"Resize verification must restore all original ROM bytes.");
      Check(metadata.SequenceEqual(viewport.Model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Resize Undo must restore pointer and anchor metadata.");
   }
   if (romBytes != null) Check(maxNeighbors > 3, "Local FireRed scenario must load deeper neighboring maps.");
   Console.WriteLine($"ZOOM_COMPLETE:{zoomCount} elapsed_ms={timer.Elapsed.TotalMilliseconds:F1} max_visible_maps={maxNeighbors}");
   if(startup==0 && Environment.GetEnvironmentVariable("HMA_MAP_STATUS_PREVIEW") is string preview) {
      using var bitmap=new RenderTargetBitmap(new PixelSize((int)view.Bounds.Width,(int)view.Bounds.Height));
      bitmap.Render(view);bitmap.Save(preview);
   }
   // Detach before closing so MainWindow does not persist application settings or prompt for edits.
   window.Editor = null;
   window.Close();
   Dispatcher.UIThread.RunJobs();
}
if (romPath != null) Check(romBytes!.SequenceEqual(File.ReadAllBytes(romPath)), "Source ROM must remain unchanged.");
Console.WriteLine("PASS: fresh full-editor tabs, routed wheel zoom, borders, pending rendering, UI heartbeats, and data integrity.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
