using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Performance;
using System.Text;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
var loads = new List<string>();
var files = new Dictionary<string, byte[]> {
   ["/tmp/HMA Drop Tests/first.HMA"] = Encoding.UTF8.GetBytes("# Sample script\n\n@000020 12 34 "),
   ["/tmp/HMA Drop Tests/second.hma"] = Encoding.UTF8.GetBytes("@000020 99 "),
   ["/tmp/HMA Drop Tests/ordinary.bin"] = new byte[512],
};
var fs = new StubFileSystem {
   MetadataFor = _ => Array.Empty<string>(),
   LoadFile = path => { loads.Add(path); return files.TryGetValue(path, out var data) ? new LoadedFile(path, data.ToArray()) : null; },
};
Application.Current!.Resources["FileSystem"] = fs;
var editor = new EditorViewModel(fs, InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
var window = new MainWindow { Width = 1440, Height = 1000, FileSystem = fs, Editor = editor };
window.Resources["FileSystem"] = fs;
window.Show();
void Layout() { Dispatcher.UIThread.RunJobs(); window.Measure(new Size(1440,1000)); window.Arrange(new Rect(0,0,1440,1000)); }
DataObject FileData(string path) {
   // Avalonia deliberately prohibits implementing IStorageFile; use its real local-file
   // wrapper via reflection because its constructor is internal in the pinned 11.2.3 SDK.
   var type = typeof(IStorageFile).Assembly.GetType("Avalonia.Platform.Storage.FileIO.BclStorageFile", throwOnError: true)!;
   var file = (IStorageItem)Activator.CreateInstance(type,
      System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
      null, new object[] { new FileInfo(path) }, null)!;
   var data = new DataObject(); data.Set(DataFormats.Files, new[] { file }); return data;
}
DragEventArgs Drag(Control target, RoutedEvent<DragEventArgs> kind, IDataObject data,
   DragDropEffects effects = DragDropEffects.Copy | DragDropEffects.Move) {
   var e = new DragEventArgs(kind, data, target, new Avalonia.Point(10,10), KeyModifiers.None) { DragEffects = effects };
   target.RaiseEvent(e); Dispatcher.UIThread.RunJobs(); return e;
}
var first = FileData("/tmp/HMA Drop Tests/first.HMA");
Drag(window, DragDrop.DropEvent, first);
Check(editor.Count == 0 && editor.ErrorMessage.Contains("ROM"), "Dropping a script without a ROM must show an error rather than open it as binary data.");
Check(loads.Count == 0, "Reject missing-target scripts before reading them.");

var fixture = new SyntheticMapFixture(24,20); var model = fixture.Model; var token = new ModelDelta();
foreach (var at in new[] {0x104,0x108,0x10C}) model.WritePointer(token,at,0x110);
var format = model.GetNextRun(0x100).FormatString;
var parse = ArrayRun.TryParse(model,format[..^1]+"4",0x100,SortedSpan<int>.None,out var root);
Check(!parse.HasError, "Synthetic map banks must parse.");
model.ObserveAnchorWritten(new NoDataChangeDeltaModel(),"data.maps.banks",root);
((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new("synthetic.layout","data.maps.banks/3/maps/0/map/0/layout/","Maps"));
var viewport = new ViewPort("rom.bin",model,InstantDispatch.Instance,new Singletons(),tutorials:editor.MapTutorials,fs:fs);
viewport.InitializationWorkload.GetAwaiter().GetResult();
editor.Add(viewport);
var otherModel = new PokemonModel(Enumerable.Repeat((byte)5,512).ToArray(), singletons:new Singletons());
var other = new ViewPort("other.bin",otherModel,InstantDispatch.Instance,new Singletons(),fs:fs);
other.InitializationWorkload.GetAwaiter().GetResult(); editor.Add(other);
var otherBytes=otherModel.RawData.ToArray();
editor.Add(viewport.MapEditor); Layout();
var mapView=window.GetVisualDescendants().OfType<MapTab>().Single();
var pristine=model.RawData.ToArray();
model.WriteValue(viewport.ChangeHistory.CurrentChange,0x30,0x77); // Deliberately leave an earlier edit uncompleted.
var baseline=model.RawData.ToArray();
var metadata=model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
var over=Drag(mapView,DragDrop.DragOverEvent,first);
Check(over.Handled && over.DragEffects==DragDropEffects.Copy, "Local file drags must advertise copy without moving source files.");
Check(loads.Count==0 && model.RawData.SequenceEqual(baseline), "Drag-over must not load or apply a script.");
mapView.AddHandler(DragDrop.DragOverEvent,(_,e)=>{e.DragEffects=DragDropEffects.None;e.Handled=true;});
Check(Drag(mapView,DragDrop.DragOverEvent,first).DragEffects==DragDropEffects.Copy,
   "A child control rejecting file data must not block the window's copy target.");
viewport.UpdateInProgress=true;
Drag(window,DragDrop.DropEvent,first);
Check(loads.Count==0 && model.RawData.SequenceEqual(baseline) && editor.ErrorMessage.Contains("finish"),
   "A busy ROM must report a deferred import rather than silently discard a script.");
viewport.UpdateInProgress=false;
var childDrops=0;
mapView.AddHandler(DragDrop.DropEvent,(_,e)=>{childDrops++;e.Handled=true;});
var drop=Drag(mapView,DragDrop.DropEvent,first);
Check(drop.Handled && childDrops==1 && drop.DragEffects==DragDropEffects.Copy,
   "Child editor handling must not prevent imports, and accepted drops must report copy rather than move.");
Check(editor.Count==3 && ReferenceEquals(editor.SelectedTab,viewport), "A map drop must select its owning ROM without creating a script tab.");
Check(model[0x20]==0x12 && model[0x21]==0x34, "Dropped HMA must execute through the real script importer at its explicit address.");
Check(otherModel.RawData.SequenceEqual(otherBytes), "Import must not target the other open ROM.");
Check(metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()), "Byte-only HMA must preserve anchor/pointer metadata.");
Check(viewport.Undo.CanExecute(null), "Script edits must remain undoable.");
Drag(window,DragDrop.DropEvent,FileData("/tmp/HMA Drop Tests/second.hma"));
Check(model[0x20]==0x99 && model[0x21]==0x34 && editor.Count==3, "Repeated drops from the ROM tab must apply to the same model.");
viewport.Undo.Execute(null); Check(model[0x20]==0x12 && model[0x21]==0x34, "Undo must restore the preceding import.");
viewport.Undo.Execute(null); Check(model.RawData.SequenceEqual(baseline), "Undoing both scripts must restore every original byte.");
viewport.Redo.Execute(null); Check(model[0x20]==0x12 && model[0x21]==0x34, "Redo must restore the imported bytes.");
viewport.Undo.Execute(null);
viewport.Undo.Execute(null); Check(model.RawData.SequenceEqual(pristine), "Earlier unsaved edits must retain their own undo entry.");
viewport.Redo.Execute(null); Check(model.RawData.SequenceEqual(baseline), "Earlier unsaved edits must remain redoable separately from imports.");
editor.SelectedIndex=2; Layout();
Drag(window,DragDrop.DropEvent,FileData("/tmp/HMA Drop Tests/missing.hma"));
Check(ReferenceEquals(editor.SelectedTab,viewport.MapEditor) && model.RawData.SequenceEqual(baseline), "A failed file load must preserve the selected map and ROM bytes.");
var binaryScript = new ViewPort("already-open.hma",new PokemonModel(new byte[512],singletons:new Singletons()),InstantDispatch.Instance,new Singletons());
editor.Add(binaryScript);
var before=loads.Count;
Drag(window,DragDrop.DropEvent,first);
Check(loads.Count==before && model.RawData.SequenceEqual(baseline), "An accidentally opened HMA tab must not become a patch target.");
editor.SelectedIndex=0;
Drag(window,DragDrop.DropEvent,FileData("/tmp/HMA Drop Tests/ordinary.bin"));
Check(editor.Count==5 && ((ViewPort)editor.SelectedTab).FileName.EndsWith("ordinary.bin"), "Ordinary binary files must retain the existing Open behavior.");
var text=new DataObject();text.Set(DataFormats.Text,"ordinary text");
Check(!Drag(window,DragDrop.DragOverEvent,text).Handled, "Text drags must remain available to child editors.");
Check(files["/tmp/HMA Drop Tests/first.HMA"].SequenceEqual(Encoding.UTF8.GetBytes("# Sample script\n\n@000020 12 34 ")), "Dropped source scripts must not be modified.");
window.Editor=null;window.Close();
Console.WriteLine("PASS: routed local-file drops, map-to-ROM HMA import, repeated drops, metadata/other-ROM isolation, undo/redo, missing targets, read failure, and ordinary file/text behavior.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
