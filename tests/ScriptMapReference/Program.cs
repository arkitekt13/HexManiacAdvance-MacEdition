using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;
using HavenSoft.HexManiac.Review;
using Point = Avalonia.Point;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
var fixture = new SyntheticMapFixture(80, 50, withNpcText: true);
fixture.Model.SetList(new NoDataChangeDeltaModel(), "FacingOptions", "NoMovement");
fixture.Model.SetList(new NoDataChangeDeltaModel(), "MapAttributeBehaviors", "Normal");
var source = fixture.ViewPort;
var bytes = fixture.Model.RawData.ToArray();
string[] Metadata() => fixture.Model.ExportMetadata(null, source.Singletons.MetadataInfo).Serialize();
var metadata = Metadata();
var allocationState = (source.Model.FreeSpaceStart, source.Model.NextExportID, source.Model.ChangeCount);
var watch = Stopwatch.StartNew();
var snapshot = ScriptMapSnapshot.Create(source, 0);
Console.WriteLine("SNAPSHOT_MS:" + watch.Elapsed.TotalMilliseconds);
Check(snapshot.Image.PixelWidth == 1280 && snapshot.Image.PixelHeight == 800, "Snapshot retains native map dimensions.");
Check(snapshot.Image.PixelData.Length == 1_024_000 && snapshot.Image.PixelData.Distinct().Count() > 1, "Snapshot contains rendered artwork.");
Check(bytes.SequenceEqual(fixture.Model.RawData) && metadata.SequenceEqual(Metadata()), "NPC discovery is isolated from live bytes and metadata.");
Check(source.ChangeHistory.IsSaved && !source.Undo.CanExecute(null), "Read-only map preparation cannot create live history or dirty the document.");
Check(allocationState == (source.Model.FreeSpaceStart, source.Model.NextExportID, source.Model.ChangeCount), "Snapshot preparation preserves allocation and byte-highlighting state.");
source.ScriptReferenceMapID = 0;
var duplicate = source.CreateDuplicate();
Check(duplicate.ScriptReferenceMapID == 0 && ReferenceEquals(duplicate.Model, source.Model), "Duplicate tabs retain source map identity on the same ROM.");
var unrelated = new ViewPort(new LoadedFile("other.gba", new byte[512]));
Check(unrelated.ScriptReferenceMapID == null, "Another ROM never inherits the map context.");

// Actual map event routes, including shared-script reuse and the default-map duplicate trap.
var routeFixture = new SyntheticMapFixture(12, 10, withNpcText: true);
var routeModel = routeFixture.Model;
routeModel.SetList(new NoDataChangeDeltaModel(), "FacingOptions", "NoMovement");
routeModel.SetList(new NoDataChangeDeltaModel(), "MapAttributeBehaviors", "Normal");
var routeToken = new ModelDelta();
foreach (var at in new[] { 0x104, 0x108, 0x10C }) routeModel.WritePointer(routeToken, at, 0x110);
var bankFormat = routeModel.GetNextRun(0x100).FormatString;
var bankError = HavenSoft.HexManiac.Core.Models.Runs.ArrayRun.TryParse(routeModel, bankFormat[..^1] + "4", 0x100,
   HavenSoft.HexManiac.Core.Models.Runs.SortedSpan<int>.None, out var bankRun);
Check(!bankError.HasError, "Synthetic map routes have four valid banks.");
routeModel.ObserveAnchorWritten(new NoDataChangeDeltaModel(), "data.maps.banks", bankRun);
((IList<GotoShortcutModel>)routeModel.GotoShortcuts).Add(new("synthetic.layout", "data.maps.banks/3/maps/0/map/0/layout/", "Maps"));
var routeFs = new StubFileSystem { MetadataFor = _ => Array.Empty<string>() };
var routeEditor = new EditorViewModel(routeFs, InstantDispatch.Instance);
var routeView = new ViewPort("routes.gba", routeModel, InstantDispatch.Instance, new Singletons(), tutorials: routeEditor.MapTutorials, fs: routeFs);
routeView.InitializationWorkload.GetAwaiter().GetResult();
var mapper = routeView.MapEditor;
Check(mapper != null, "Real mapper initializes for script-route checks.");
routeEditor.Add(routeView); routeEditor.Add(mapper);
var routeEvent = mapper.PrimaryMap.EventGroup.Objects.Single();
mapper.EventDown(routeEvent, PrimaryInteractionStart.DoubleClick);
Check(routeView.ScriptReferenceMapID == 3000, "Double-clicking an event captures its source map.");
routeModel.ObserveRunWritten(new NoDataChangeDeltaModel(), new HavenSoft.HexManiac.Core.Models.Runs.XSERun(0x18000));
routeView.Goto.Execute(0x18000); routeView.Tools.CodeTool.Mode = CodeMode.Script; routeView.Tools.SelectedIndex = 3;
mapper.PrimaryMap = new BlockMapViewModel(routeFs, routeEditor.MapTutorials, routeView,
   new HavenSoft.HexManiac.Core.Models.Map.Format(routeModel), mapper.Templates, 0, 0);
routeEditor.SelectedIndex = 1;
routeModel[0x18800] = 2;
routeModel.ObserveRunWritten(new NoDataChangeDeltaModel(), new HavenSoft.HexManiac.Core.Models.Runs.XSERun(0x18800));
routeEvent = mapper.PrimaryMap.EventGroup.Objects.Single(); routeEvent.ScriptAddress = 0x18800;
routeModel.ObserveRunWritten(new NoDataChangeDeltaModel(), new HavenSoft.HexManiac.Core.Models.Runs.XSERun(0x18000));
mapper.EventDown(routeEvent, PrimaryInteractionStart.DoubleClick);
var secondScript = routeEditor.SelectedTab as ViewPort;
Check(secondScript != null && secondScript.ScriptReferenceMapID == 0 && !ReferenceEquals(secondScript, routeView),
   "A new script tab inherits map 0-0 rather than its new mapper's default 3-0.");
routeEditor.SelectedIndex = 1;
routeEvent.ScriptAddress = 0x18000;
mapper.EventDown(routeEvent, PrimaryInteractionStart.DoubleClick);
Check(ReferenceEquals(routeEditor.SelectedTab, routeView) && routeView.ScriptReferenceMapID == 0,
   "Reusing a shared script updates its explicit source-map context.");
Check(secondScript.ScriptReferenceMapID == 0, "Another open script tab keeps its own map context.");
routeView.ScriptReferenceMapID = null;
routeEvent.GotoScript();
Check(routeView.ScriptReferenceMapID == 0, "The event script-address navigation route carries map context.");
routeModel[0x1B000] = 1; routeModel.WritePointer(routeToken, 0x1B001, 0x18000);
routeView.ScriptReferenceMapID = null;
new MapScriptViewModel(routeView, mapper.Templates, 0x1B000).Goto();
Check(routeView.ScriptReferenceMapID == 0, "Map-header scripts carry source-map context too.");

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
var fs = new StubFileSystem { MetadataFor = _ => Array.Empty<string>() };
Application.Current.Resources["FileSystem"] = fs;
var editor = new EditorViewModel(fs, InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
fixture.Model.ObserveRunWritten(new NoDataChangeDeltaModel(), new HavenSoft.HexManiac.Core.Models.Runs.XSERun(0x18000));
editor.Add(source); editor.GotoViewModel.ControlVisible = false;
source.Goto.Execute(0x18000); source.Tools.CodeTool.Mode = CodeMode.Script; source.Tools.SelectedIndex = 3;
metadata = Metadata();
var window = new MainWindow { Width = 1400, Height = 900, Editor = editor, FileSystem = fs };
window.Resources["FileSystem"] = fs; window.Show();
void Pump() {
   Dispatcher.UIThread.RunJobs(); window.Measure(new Size(1400, 900)); window.Arrange(new Rect(0, 0, 1400, 900));
   AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
}
void Click(Button button) {
   var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window).Value;
   window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Pump();
}
void Until(Func<bool> predicate) {
   var time = Stopwatch.StartNew();
   while (!predicate()) { Pump(); Thread.Sleep(1); if (time.Elapsed.TotalSeconds > 30) throw new TimeoutException("Reference UI timed out."); }
   Pump();
}
Pump();
var tab = window.GetVisualDescendants().OfType<TabView>().First(t => ReferenceEquals(t.DataContext, source));
Check(!tab.IsScriptMapVisible, "Hex remains the default view.");
var field = tab.GetVisualDescendants().OfType<TextEditor>().First(t => t.IsEffectivelyVisible).EditBox;
field.Focus(); field.CaretIndex = 2; field.SelectionStart = 1; field.SelectionEnd = 2;
var caret = field.CaretIndex; var selection = (field.SelectionStart, field.SelectionEnd);
var address = source.SelectionStart;
Click(tab.FindControl<Button>("ScriptMapViewButton"));
Until(() => tab.ReferenceRenderCount == 1);
Check(tab.IsScriptMapVisible && editor.PreferScriptMapView, "Map toggle opens the reference pane and remembers the preference.");
Check(field.IsFocused, "Switching views retains script keyboard focus.");
Check(!tab.FindControl<DockPanel>("HexPane").IsEnabled, "Hidden hex controls cannot accept stray keyboard input.");
Check(source.SelectionStart == address && field.CaretIndex == caret && (field.SelectionStart, field.SelectionEnd) == selection,
   "View switching preserves script/hex selection.");
var mapPane = tab.FindControl<ScriptMapReferenceView>("ScriptMapPane");
mapPane.Offset = new Vector(50, 40); Pump();
Check(tab.ReferenceRenderCount == 1, "Panning never redraws the map.");
var point = mapPane.TranslatePoint(new Point(100, 100), window).Value;
window.MouseDown(point, MouseButton.Left); window.MouseMove(point + new Vector(-35, -25)); Pump();
window.MouseUp(point + new Vector(-35, -25), MouseButton.Left); Pump();
Check(!mapPane.IsPanning && field.IsFocused, "Dragging the reference pans without taking focus from script input.");
window.MouseDown(point, MouseButton.Left); Pump();
Check(mapPane.IsPanning, "Reference artwork starts a local pan.");
window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None); window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None); Pump();
Check(!mapPane.IsPanning, "Escape ends a reference pan without entering map editing.");
window.MouseUp(point, MouseButton.Left); Pump();
Check(bytes.SequenceEqual(fixture.Model.RawData) && metadata.SequenceEqual(Metadata()), "Panning and toggles cannot write map data.");
var body = source.Tools.CodeTool.Contents[0]; var originalText = body.Content;
body.Editor.Content = originalText + "\n# reference typing"; Pump();
Check(tab.ReferenceRenderCount == 1, "Script typing does not rebuild the reference map.");
body.Editor.Content = originalText; Pump();
Click(tab.FindControl<Button>("ScriptHexViewButton")); Pump();
Check(!tab.IsScriptMapVisible && !editor.PreferScriptMapView, "Hex toggle restores the hex pane.");
tab.FindControl<HexContent>("HexContent").Focus();
Click(tab.FindControl<Button>("ScriptMapViewButton"));
Until(() => tab.ReferenceRenderCount == 2);
Check(field.IsFocused, "Switching from a focused hex pane hands the keyboard to script source.");
Check(mapPane.Offset.X > 0 && mapPane.Offset.Y > 0, "Returning to Map preserves its pan position.");
source.ScriptReferenceMapID = null; Pump();
Check(mapPane.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains("No reference map") == true), "Unknown map context has an explicit placeholder.");
Check(tab.IsScriptMapVisible, "Unavailable context never guesses the default map.");
source.ScriptReferenceMapID = 0; Until(() => tab.ReferenceRenderCount == 3);
if (args.Length == 1) {
   using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(1400, 900));
   bitmap.Render(tab); bitmap.Save(args[0]);
}

// Real controlled script debounce: toggling and rendering must never flush source.
var dispatch = new WorkflowDispatch();
var delayed = new ViewPort("delayed.gba", source.Model, dispatch, new Singletons(dispatch), changeHistory: source.ChangeHistory) { ScriptReferenceMapID = 0 };
editor.Add(delayed); editor.GotoViewModel.ControlVisible = false;
delayed.Goto.Execute(0x18000); delayed.Tools.CodeTool.Mode = CodeMode.Script; delayed.Tools.SelectedIndex = 3; dispatch.Flush(); Pump();
var delayedTab = window.GetVisualDescendants().OfType<TabView>().First(t => ReferenceEquals(t.DataContext, delayed));
Until(() => delayedTab.ReferenceRenderCount > 0);
var delayedBody = delayed.Tools.CodeTool.Contents[0];
var delayedOriginal = delayedBody.Content;
delayedBody.Editor.Content = delayedOriginal + "\n# pending draft";
Check(delayed.Tools.CodeTool.HasPendingScriptChanges, "Controlled fixture has a pending compile.");
var pendingBytes = source.Model.RawData.ToArray(); var pendingMetadata = Metadata();
var pendingCallbacks = dispatch.CapturePendingCallbacks();
Click(delayedTab.FindControl<Button>("ScriptHexViewButton")); Pump();
Click(delayedTab.FindControl<Button>("ScriptMapViewButton"));
Until(() => delayedTab.ReferenceRenderCount > 1);
Check(delayed.Tools.CodeTool.HasPendingScriptChanges && pendingCallbacks.SequenceEqual(dispatch.CapturePendingCallbacks()),
   "View toggles and snapshot preparation retain the exact queued callback without flushing it.");
Check(pendingBytes.SequenceEqual(source.Model.RawData) && pendingMetadata.SequenceEqual(Metadata()), "Pending-source preview preserves every live byte/metadata entry.");
dispatch.Flush(); Pump();
delayedBody = delayed.Tools.CodeTool.Contents[0];
delayedBody.Editor.Content = delayedOriginal + "\nnot_a_command"; dispatch.Flush(); Pump();
Check(delayedBody.HasError && delayed.Tools.CodeTool.HasUncompiledScriptChanges, "Rejected source remains a draft with errors.");
var rejectedSource = delayedBody.Content; var diagnosticCount = delayedBody.Editor.ErrorLocations.Count;
var rejectedBytes = source.Model.RawData.ToArray(); var rejectedMetadata = Metadata();
var rejectedSnapshot = ScriptMapSnapshot.Create(delayed, 0);
Check(rejectedSnapshot.Image.PixelWidth == 1280 && delayedBody.Content == rejectedSource && delayedBody.Editor.ErrorLocations.Count == diagnosticCount,
   "Preview rendering retains rejected source and all diagnostics.");
Check(rejectedBytes.SequenceEqual(source.Model.RawData) && rejectedMetadata.SequenceEqual(Metadata()), "Rejected-source rendering remains read-only.");

// Simulate focus leaving text while the map pane is selected, then use the real global paste command.
var delayedField = delayedTab.GetVisualDescendants().OfType<TextEditor>().First(e => ReferenceEquals(e.DataContext, delayedBody.Editor)).EditBox;
delayedField.Focus(); delayedField.SelectionStart = 0; delayedField.SelectionEnd = 4;
HavenSoft.HexManiac.AvaloniaUI.MacPlatform.UiSync.Run(() => window.Clipboard.SetTextAsync("AA BB"));
var delayedPane = delayedTab.FindControl<ScriptMapReferenceView>("ScriptMapPane");
delayedPane.Focusable = true;
Check(delayedPane.Focus(), "A non-text reference surface can be used to test lost script focus.");
var modifier = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;
window.KeyPressQwerty(PhysicalKey.G, modifier); window.KeyReleaseQwerty(PhysicalKey.G, RawInputModifiers.None); Pump();
Check(editor.GotoViewModel.ControlVisible, "Command-G from reference focus opens Go To rather than a palette action.");
editor.GotoViewModel.ControlVisible = false; Pump();
delayedField.SelectionStart = 0; delayedField.SelectionEnd = 4; delayedPane.Focus();
window.KeyPressQwerty(PhysicalKey.V, modifier); window.KeyReleaseQwerty(PhysicalKey.V, RawInputModifiers.None);
Until(() => delayedField.Text.StartsWith("AA BB"));
Check(delayedField.IsFocused && rejectedBytes.SequenceEqual(source.Model.RawData), "Paste restores script focus and cannot paste bytes into hidden hex.");
delayedPane.Focusable = false;
window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None); window.KeyReleaseQwerty(PhysicalKey.Delete, RawInputModifiers.None); Pump();
Check(rejectedBytes.SequenceEqual(source.Model.RawData), "Delete in Map View cannot clear hidden ROM data.");
delayedBody.Editor.Content = delayedOriginal; dispatch.Flush(); Pump();
Check(!delayed.Tools.CodeTool.HasUncompiledScriptChanges, "Correction/retry still clears the rejected draft.");

// Undo/Redo notifications refresh static map pixels without opening a token from the preview.
var renderBeforeUndo = delayedTab.ReferenceRenderCount;
var editToken = delayed.ChangeHistory.CurrentChange;
editToken.ChangeData(source.Model, SyntheticMapFixture.Cells, (byte)1); delayed.ChangeHistory.ChangeCompleted();
delayed.Undo.Execute(null); dispatch.Flush();
Until(() => delayedTab.ReferenceRenderCount > renderBeforeUndo);
Check(delayed.Redo.CanExecute(null), "Preview refresh after Undo preserves Redo.");
var renderBeforeRedo = delayedTab.ReferenceRenderCount;
delayed.Redo.Execute(null); dispatch.Flush();
Until(() => delayedTab.ReferenceRenderCount > renderBeforeRedo);
Check(source.Model[SyntheticMapFixture.Cells] == 1, "Redo restores map data and refreshes the reference image.");
var previewDuringClose = delayedTab.ReferenceRenderCount;
delayed.ScriptReferenceMapID = 999999; Pump();
Until(() => delayedTab.FindControl<ScriptMapReferenceView>("ScriptMapPane").GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains("unavailable") == true));
Check(delayedTab.IsScriptMapVisible, "Invalid/removed map stays an explanatory reference pane without crashing.");

// A detached/reused script tab must discard old rendering rather than showing another ROM's map.
delayed.ScriptReferenceMapID = 0; Pump(); editor.Add(unrelated); Pump();
Check(!ReferenceEquals(editor.SelectedTab, delayed), "Tab changes can proceed while a reference render is pending.");
Check(unrelated.ScriptReferenceMapID == null && unrelated.Model.RawData.All(b => b == 0), "A stale render cannot change or assign context to another ROM.");
editor.PreferScriptMapView = true;
string[] settings = null; var settingsFs = new StubFileSystem { MetadataFor = _ => Array.Empty<string>(), SaveMetadata = (_, lines) => { settings = lines; return true; } };
var settingsEditor = new EditorViewModel(settingsFs, InstantDispatch.Instance) { PreferScriptMapView = true };
settingsEditor.WriteAppLevelMetadata();
settingsFs.MetadataFor = _ => settings;
Check(new EditorViewModel(settingsFs, InstantDispatch.Instance).PreferScriptMapView, "Map preference survives app-settings serialization/reload.");
Check(new EditorViewModel(new StubFileSystem { MetadataFor = _ => new[] { "PreferScriptMapView = invalid" } }, InstantDispatch.Instance).PreferScriptMapView == false,
   "Invalid settings retain the safe Hex default.");
window.Editor = null; window.Close(); dispatch.Clear();
Console.WriteLine($"CHECKS_PASSED:{checks}");
