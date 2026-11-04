using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Performance;
using System.Xml.Linq;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
var fs = new StubFileSystem(); var editor = new EditorViewModel(fs, InstantDispatch.Instance);
Application.Current!.Resources["FileSystem"] = fs;
App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
var surface = new Grid { Background = Brushes.Green };
var layer = new Canvas();
var root = new Grid { Children = { surface, layer } };
var window = new Window { Width = 700, Height = 600, Content = root }; window.Show();
void Pump(Window target) {
   Dispatcher.UIThread.RunJobs(); target.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick();
}
Pump(window);
var controller = new MapHoverPreview(surface, layer);
var box = (Viewbox)layer.Children.Single();
int exits = 0, clicks = 0;
surface.PointerExited += (_, _) => { exits++; controller.Hide(); };
surface.PointerPressed += (_, _) => { clicks++; controller.Hide(); };
var point = new Avalonia.Point(400, 350); var cell = new Rect(point.X - 8, point.Y - 8, 16, 16);
surface.PointerMoved += (_, _) => controller.Update(null, cell);
window.MouseMove(point); Pump(window);
var small = new object[] { "Small dialogue" };
var complex = Enumerable.Range(0, 5).SelectMany(i => new object[] {
   "Dialogue " + i + ": " + new string('a', 55),
   new ReadonlyPixelViewModel(64, 64, Enumerable.Repeat((short)(0x1000 + i * 20), 4096).ToArray(), -1) { SpriteScale = 2 },
}).ToArray();
void Fits(Canvas host, Control preview) {
   var location = preview.TranslatePoint(default, host)!.Value;
   Check(location.X >= 0 && location.Y >= 0 && location.X + preview.Bounds.Width <= host.Bounds.Width + .01
      && location.Y + preview.Bounds.Height <= host.Bounds.Height + .01, $"The complete preview must fit inside the map viewport (position={location}, size={preview.Bounds.Size}, host={host.Bounds.Size}, desired={preview.DesiredSize}).");
}
foreach (var content in new[] { small, complex }) {
   controller.Update(content, cell); Pump(window);
   var visual = box.Child;
   Check(box.IsVisible && visual is HexContentToolTip, "A preview must be shown inside the map-owned layer.");
   Check(!layer.IsHitTestVisible && !box.IsHitTestVisible && !visual!.IsHitTestVisible, "All preview input must pass through to the map.");
   Check(ReferenceEquals(TopLevel.GetTopLevel(visual), window) && ToolTip.GetTip(surface) == null && !ToolTip.GetIsOpen(surface), "NPC hover must not create an automatic or native tooltip popup.");
   Check(ReferenceEquals(visual.DataContext, content), "The complete original summary must remain available.");
   int transitions = 0;
   void Changed(object sender, AvaloniaPropertyChangedEventArgs e) { if (e.Property == Visual.IsVisibleProperty) transitions++; }
   box.PropertyChanged += Changed;
   int initialExits = exits;
   // The old large pointer tooltip produced 38 open/close transitions for these 20 movements.
   for (int i = 0; i < 20; i++) {
      window.MouseMove(point + new Vector(i % 2, 0)); Pump(window);
      Check(box.IsVisible && ReferenceEquals(box.Child, visual) && ReferenceEquals(visual.DataContext, content), "Same-cell pointer motion must retain one visible preview and its content.");
      Check(ReferenceEquals(window.InputHitTest(point), surface), "Even a preview covering the cursor cannot steal the map's hit target.");
   }
   Check(transitions == 0 && exits == initialExits, "Complex previews must not cause map exits or visibility oscillation.");
   box.PropertyChanged -= Changed;
   Fits(layer, box);
   if (content == complex) {
      var rows = ((HexContentToolTip)visual).FindControl<ItemsControl>("ToolTipContent")!;
      Check(rows.Items.Count == 10, "Fitting a tall preview must retain all five dialogue rows and all five images.");
      Check(visual.GetVisualDescendants().OfType<PixelImage>().Count(i => i.Source is WriteableBitmap) == 5, "All complex-preview images must publish real bitmaps.");
   }
   foreach (var corner in new[] { new Rect(0, 0, 16, 16), new Rect(684, 0, 16, 16), new Rect(0, 584, 16, 16), new Rect(684, 584, 16, 16) }) {
      controller.Update(null, corner); Pump(window); Fits(layer, box);
   }
   controller.Hide(); Check(!box.IsVisible, "Leaving a map must hide the preview.");
   controller.Update(null, cell); Pump(window);
   Check(box.IsVisible && ReferenceEquals(box.Child, visual) && ReferenceEquals(visual.DataContext, content), "Same-cell re-entry reuses the existing content/control.");
   window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Pump(window);
   Check(!box.IsVisible, "A click through the preview reaches the map and dismisses it.");
   controller.Update(null, cell); Pump(window);
   controller.Update(Array.Empty<object>()); controller.Update(null, cell); Pump(window);
   Check(!box.IsVisible, "An empty cell clears the cached summary and cannot reopen it.");
}
Check(clicks == 2, "Both simple and complex previews must pass actual pointer clicks through.");
controller.Update(complex, cell); Pump(window);
window.Width = 240; window.Height = 180; Pump(window);
point = new Avalonia.Point(120, 90); cell = new Rect(112, 82, 16, 16);
window.MouseMove(point); Pump(window); Fits(layer, box);
Check(box.IsVisible && box.Bounds.Width > 0 && box.Bounds.Height > 0, "Very small viewports keep a fitted preview.");
window.Content = null; Pump(window); Check(!box.IsVisible, "Detaching a map must hide the preview.");
window.Content = root; Pump(window); controller.Update(null, new Rect(10, 10, 16, 16)); Pump(window);
Check(box.IsVisible, "Reattaching the same map supports hover re-entry.");
surface.DataContext = new object(); controller.Update(null); Pump(window);
Check(!box.IsVisible, "Switching map contexts clears abandoned hover content.");
window.Close();

// Verify the actual MapTab wiring, transformed tile anchor, and existing ROM/history invariants.
var fixture = new SyntheticMapFixture(24, 20, withNpcText: true);
var model = fixture.Model; var token = new ModelDelta();
model.SetList(new NoDataChangeDeltaModel(), "FacingOptions", "NoMovement");
model.SetList(new NoDataChangeDeltaModel(), "MapAttributeBehaviors", "Normal");
foreach (var at in new[] { 0x104, 0x108, 0x10C }) model.WritePointer(token, at, 0x110);
var format = model.GetNextRun(0x100).FormatString;
var parse = ArrayRun.TryParse(model, format[..^1] + "4", 0x100, SortedSpan<int>.None, out var banks);
Check(!parse.HasError, "The native-view fixture must parse.");
model.ObserveAnchorWritten(new NoDataChangeDeltaModel(), "data.maps.banks", banks);
((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new("synthetic.layout", "data.maps.banks/3/maps/0/map/0/layout/", "Maps"));
var hostEditor = new EditorViewModel(fs, InstantDispatch.Instance);
var viewport = new ViewPort("hover-fixture.bin", model, InstantDispatch.Instance, new Singletons(), tutorials: hostEditor.MapTutorials, fs: fs);
viewport.InitializationWorkload.GetAwaiter().GetResult();
var mapper = viewport.MapEditor;
var nativeWindow = new MainWindow { Width = 1440, Height = 1000, FileSystem = fs, Editor = hostEditor };
nativeWindow.Resources["FileSystem"] = fs; nativeWindow.Show(); hostEditor.Add(viewport); hostEditor.Add(mapper);
hostEditor.GotoViewModel.ControlVisible = false; Pump(nativeWindow);
var view = nativeWindow.GetVisualDescendants().OfType<MapTab>().Single();
var mapSurface = view.FindControl<ItemsControl>("MapView")!;
var mapLayer = view.FindControl<Canvas>("HoverPreviewLayer")!;
var liveBox = (Viewbox)mapLayer.Children.Single();
var liveMapImage = view.GetVisualDescendants().OfType<PixelImage>().Single(i => ReferenceEquals(i.DataContext, mapper.PrimaryMap));
Avalonia.Point Npc() {
   var border = mapper.PrimaryMap.GetBorderThickness(); double scale = mapper.PrimaryMap.SpriteScale;
   return liveMapImage.TranslatePoint(new Avalonia.Point((border.West + 2.5) * 16 * scale, (border.North + .5) * 16 * scale), nativeWindow)!.Value;
}
var original = model.RawData.ToArray();
var metadata = model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize();
viewport.ChangeHistory.ClearHistory(false);
foreach (var scale in new[] { 1d, 2d }) {
   mapper.PrimaryMap.SpriteScale = scale; mapper.HideEvents = false; Pump(nativeWindow);
   nativeWindow.MouseMove(Npc()); Pump(nativeWindow);
   Check(liveBox.IsVisible && liveBox.Child!.DataContext is object[] content && content.OfType<string>().Any(s => s.Contains("Discovered NPC text")), "The actual map must summarize its NPC into the hover overlay.");
   Fits(mapLayer, liveBox);
   var identity = liveBox.Child;
   for (int i = 0; i < 10; i++) { nativeWindow.MouseMove(Npc() + new Vector(i % 2, 0)); Pump(nativeWindow); }
   Check(liveBox.IsVisible && ReferenceEquals(liveBox.Child, identity), "Real map pointer movement and zoom retain one stable preview control.");
   var tutorial = view.FindControl<TutorialControl>("TutorialsControl")!;
   var tutorialPoint = tutorial.TranslatePoint(new Avalonia.Point(tutorial.Bounds.Width - 5, 5), nativeWindow)!.Value;
   nativeWindow.MouseMove(tutorialPoint); Pump(nativeWindow);
   Check(!liveBox.IsVisible, "Tutorial input dismisses an NPC preview instead of showing it over the tutorial.");
   nativeWindow.MouseMove(Npc()); Pump(nativeWindow);
   Check(liveBox.IsVisible && ReferenceEquals(liveBox.Child, identity), "Returning to the same NPC after a tutorial reuses cached content.");
   nativeWindow.MouseWheel(Npc(), new Vector(0, 1), RawInputModifiers.None); Pump(nativeWindow);
   Check(!liveBox.IsVisible, "Wheel zoom dismisses a preview tied to the old tile position.");
   Check(original.SequenceEqual(model.RawData), "NPC hover and zoom preserve all ROM bytes.");
}
Check(metadata.SequenceEqual(model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()), "Hover presentation preserves discovered metadata and pointers.");
Check(!viewport.ChangeHistory.Undo.CanExecute(null), "Hovering creates no ROM history entries.");
var markup = XDocument.Load(Path.GetFullPath("../../../../../src/HexManiac.Avalonia/Controls/MapTab.axaml"));
var previewMarkup = markup.Descendants().Single(n => (string)n.Attribute("Name") == "HoverPreviewLayer");
XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
var tutorialMarkup = markup.Descendants().Single(n => (string)n.Attribute(x + "Name") == "TutorialsControl");
Check((int)previewMarkup.Attribute("ZIndex")! < (int)tutorialMarkup.Attribute("ZIndex")!, "Tutorials must remain above the input-transparent hover layer.");
nativeWindow.Editor = null; nativeWindow.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
