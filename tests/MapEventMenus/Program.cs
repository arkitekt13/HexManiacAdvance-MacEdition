using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using Avalonia.Interactivity;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks=0;
void Check(bool ok,string message) { checks++;if(!ok)throw new InvalidOperationException(message); }
var fixture=new SyntheticMapFixture(24,20,withWarp:true);var model=fixture.Model;
model.SetList(new NoDataChangeDeltaModel(),"MapAttributeBehaviors","Normal");
var token=new ModelDelta();
foreach(var at in new[]{0x104,0x108,0x10C})model.WritePointer(token,at,0x110);
var format=model.GetNextRun(0x100).FormatString;
var error=ArrayRun.TryParse(model,format[..^1]+"4",0x100,SortedSpan<int>.None,out var root);
Check(!error.HasError,"Synthetic map banks must parse.");
model.ObserveAnchorWritten(new NoDataChangeDeltaModel(),"data.maps.banks",root);
((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new("synthetic.layout","data.maps.banks/3/maps/0/map/0/layout/","Maps"));
var fs=new StubFileSystem{MetadataFor=_=>Array.Empty<string>()};
Application.Current!.Resources["FileSystem"]=fs;
var editor=new EditorViewModel(fs,InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
var viewport=new ViewPort("event-menus-fixture.bin",model,InstantDispatch.Instance,new Singletons(),tutorials:editor.MapTutorials,fs:fs);
viewport.InitializationWorkload.GetAwaiter().GetResult();
var mapper=viewport.MapEditor;
var window=new MainWindow{Width=1440,Height=1000,FileSystem=fs,Editor=editor};
window.Resources["FileSystem"]=fs;window.Show();
editor.Add(viewport);editor.Add(mapper);
void Layout(){
   Dispatcher.UIThread.RunJobs();window.Measure(new Size(window.Width,window.Height));window.Arrange(new Rect(0,0,window.Width,window.Height));
   // Refresh the headless hit-test scene after zooming, as the native render timer would.
   AvaloniaHeadlessPlatform.ForceRenderTimerTick();
}

Layout();
editor.GotoViewModel.ControlVisible=false;
var view=window.GetVisualDescendants().OfType<MapTab>().Single();
var surface = view.FindControl<ItemsControl>("MapView")!;
var menu = (ContextMenu)view.Resources["MapEventMenu"]!;
var image = view.GetVisualDescendants().OfType<PixelImage>().Single(i => ReferenceEquals(i.DataContext, mapper.PrimaryMap));
IPointer pointer = null;
surface.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel, handledEventsToo: true);
Avalonia.Point Cell(int x, int y) {
   var border = mapper.PrimaryMap.GetBorderThickness(); var scale = mapper.PrimaryMap.SpriteScale;
   return image.TranslatePoint(new Avalonia.Point((border.West + x + .5) * 16 * scale, (border.North + y + .5) * 16 * scale), window)!.Value;
}
int prompts = 0;
fs.ShowOptions = (title, prompt, details, options) => {
   Check(title == "Pick a bank" && details.Count > 0, "The warp's Create New Map must reach its real bank dialog.");
   prompts++; return -1;
};
foreach (var scale in new[] { 1d, 2d }) foreach (var button in new[] { MouseButton.Right, MouseButton.Left }) {
   if (button == MouseButton.Left && !OperatingSystem.IsMacOS()) continue;
   mapper.PrimaryMap.SpriteScale = scale; mapper.HideEvents = false; Layout(); view.Focus();
   viewport.ChangeHistory.ClearHistory(false);
   var before = model.RawData.ToArray(); var metadata = model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize();
   var position = (mapper.PrimaryMap.LeftEdge, mapper.PrimaryMap.TopEdge);
   var point = Cell(2, 3); var modifiers = button == MouseButton.Left ? RawInputModifiers.Control : RawInputModifiers.None;
   window.MouseDown(point, button, modifiers); Layout();
   Check(menu.IsOpen, $"Secondary/Control-click on a warp must open its event menu before selecting the underlying tile ({button}/{scale}, point={point}, hit={window.InputHitTest(point)}, events={mapper.PrimaryMap.ShowEvents}, eventCount={model[0x161]}).");
   Check(pointer?.Captured == null, "Opening an event menu must release map pointer capture.");
   window.MouseUp(point, button, RawInputModifiers.None); Layout();
   Check(menu.ItemsSource == mapper.ContextItems, "The menu displays the shared event commands.");
   var items = menu.GetLogicalDescendants().OfType<MenuItem>().ToArray();
   Check(items.Select(i => i.Header).SequenceEqual(new[] { "Create New Map", "Open in New Tab", "Delete Warp" }), "Restore all three original warp commands.");
   Check(items.All(i => ReferenceEquals(i.Command, i.DataContext)), "Menu labels and executable commands must bind to the same shared entries.");
   var create = items[0]; create.Command!.Execute(create.CommandParameter); menu.Close(); Layout();
   Check(before.SequenceEqual(model.RawData) && metadata.SequenceEqual(model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()), "Opening/canceling map creation preserves bytes and metadata.");
   Check(!viewport.ChangeHistory.Undo.CanExecute(null), "Opening/canceling the menu must not create ROM history.");
   window.MouseMove(point + new Vector(24, 24)); Layout();
   Check(position == (mapper.PrimaryMap.LeftEdge, mapper.PrimaryMap.TopEdge), "Pointer motion after menu dismissal must not pan the map.");
   Check(before.SequenceEqual(model.RawData), "Pointer motion after menu dismissal must not paint tiles.");
   // Context menu deletion must use the warp under the cursor, even if nothing was selected.
   window.MouseDown(point, button, modifiers); window.MouseUp(point, button, RawInputModifiers.None); Layout();
   var delete = menu.GetLogicalDescendants().OfType<MenuItem>().Single(i => Equals(i.Header, "Delete Warp"));
   delete.Command!.Execute(delete.CommandParameter); menu.Close(); Layout(); viewport.ChangeHistory.ChangeCompleted();
   Check(model[0x161] == 0, "Delete Warp acts on the event under the context click.");
   mapper.Undo.Execute(null); Layout();
   Check(before.SequenceEqual(model.RawData), "Undo restores the warp and all bytes.");
   Check(metadata.SequenceEqual(model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()), "Undo restores warp metadata.");
   mapper.Redo.Execute(null); Layout(); Check(model[0x161] == 0, "Redo repeats warp deletion.");
   mapper.Undo.Execute(null); Layout();
   // Empty cells keep their secondary drag-selection gesture and never show stale warp commands.
   var empty = Cell(10, 10); window.MouseMove(empty); Layout(); window.MouseDown(empty, button, modifiers); Layout(); window.MouseMove(Cell(11, 11), modifiers | (button == MouseButton.Left ? RawInputModifiers.LeftMouseButton : RawInputModifiers.RightMouseButton));
   window.MouseUp(Cell(11, 11), button, RawInputModifiers.None); Layout();
   Check(!menu.IsOpen, $"Empty-cell secondary dragging must not show a stale event menu (open={menu.IsOpen}, brush={mapper.DrawMultipleTiles}, button={button}, scale={scale}).");
   Check(before.SequenceEqual(model.RawData), "Tile selection following a context menu preserves ROM bytes.");
}
Check(prompts == (OperatingSystem.IsMacOS() ? 4 : 2), "Every Create New Map invocation must reach the bank dialog once.");
var tabsBefore = editor.Count;
var warpPoint = Cell(2, 3);
window.MouseDown(warpPoint, MouseButton.Right); window.MouseUp(warpPoint, MouseButton.Right); Layout();
var openTab = menu.GetLogicalDescendants().OfType<MenuItem>().Single(i => Equals(i.Header, "Open in New Tab"));
openTab.Command!.Execute(openTab.CommandParameter); menu.Close(); Layout();
Check(editor.Count == tabsBefore + 1 && editor.OfType<MapEditorViewModel>().Count() == 2, "Open in New Tab opens the warp destination in a separate map tab.");
Check(editor.OfType<MapEditorViewModel>().All(m => ReferenceEquals(m.ViewPort.Model, model)), "Destination tabs share the same ROM model.");
window.Editor = null; window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
