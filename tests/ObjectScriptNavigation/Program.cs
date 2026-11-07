using Avalonia;
using Pointer = HavenSoft.HexManiac.Core.ViewModels.DataFormats.Pointer;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Core.ViewModels.Images;
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
var fixture=new SyntheticMapFixture(24,20,withScriptEvent:true);var model=fixture.Model;
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
var viewport=new ViewPort("sprite-selection-fixture.bin",model,InstantDispatch.Instance,new Singletons(),tutorials:editor.MapTutorials,fs:fs);
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
model.FreeSpaceStart = 0x18000;
model.SetList(viewport.CurrentChange, "FacingOptions", "NoMovement", "LookAround", "Wander");
var objectFormat = new HavenSoft.HexManiac.Core.Models.Map.Format(model).ObjectsFormat;
objectFormat = objectFormat[..(objectFormat.LastIndexOf(']') + 1)] + "1";
var objectError = ArrayRun.TryParse(model, objectFormat, 0x17000, SortedSpan<int>.None, out var objectRun);
Check(!objectError.HasError, "Synthetic object table must parse.");
model.ObserveAnchorWritten(viewport.CurrentChange, "synthetic.object", objectRun);
var sprites = Enumerable.Range(0, 1).Select(i => {
   int w = i == 108 ? 64 : i == 150 ? 96 : i is 106 or 107 or 109 ? 32 : 16;
   int h = i == 108 ? 64 : i == 150 ? 128 : 32;
   var pixels=new short[w*h];
   for(int y=1;y<h-1;y++)for(int x=1;x<w-1;x++)pixels[y*w+x]=(short)(i+1);
   return new ReadonlyPixelViewModel(w,h,pixels,-1);
}).ToArray();
int lastGoto=-1;
ObjectEventViewModel Object() => new(mapper.PrimaryMap, address => lastGoto=address, model.GetTableModel("synthetic.object", () => viewport.CurrentChange)[0], mapper.Templates, sprites, sprites[0], null);



// Link the object table into the map so the real map Undo/Redo refresh can rebuild selection.
model[0x160]=1;model.WritePointer(viewport.CurrentChange,0x164,0x17000);
var obj=Object();model[0x18000]=2;model[0x18100]=2;
model.ObserveRunWritten(new NoDataChangeDeltaModel(),new XSERun(0x18000));
model.ObserveRunWritten(new NoDataChangeDeltaModel(),new XSERun(0x18100));
obj.ScriptAddress=Pointer.NULL;mapper.PrimaryMap.SelectedEvent=obj;Layout();
AngleButton Arrow()=>view.GetVisualDescendants().OfType<AngleButton>().First(b=>ReferenceEquals(b.DataContext,mapper.SelectedEvent)&&b.Command is HavenSoft.HexManiac.Core.MethodCommand c&&c.ExecuteName=="GotoScript");
string[] Metadata()=>model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
void Enabled(bool expected,string context){Layout();Check(Arrow().IsEffectivelyEnabled==expected,context+": actual arrow enabled state.");Check(Arrow().Command.CanExecute(null)==expected,context+": command state agrees with the arrow.");}
void Click(int expected){
 lastGoto=-1;var arrow=Arrow();var at=arrow.TranslatePoint(new Avalonia.Point(arrow.Bounds.Width/2,arrow.Bounds.Height/2),window).Value;
 var before=model.RawData.ToArray();window.MouseMove(at);window.MouseDown(at,MouseButton.Left);window.MouseUp(at,MouseButton.Left);Layout();
 Check(lastGoto==expected,"A real arrow click must deliver the current script address to navigation.");
 Check(before.SequenceEqual(model.RawData),"Following an existing script must preserve ROM bytes.");
}
Enabled(false,"Null script");
viewport.ChangeHistory.ClearHistory(false);var original=model.RawData.ToArray();var originalMetadata=Metadata();
var sameArrow=Arrow();obj.ScriptAddress=0x18000;viewport.ChangeHistory.ChangeCompleted();
Enabled(true,"Programmatic assignment without reselecting");Check(ReferenceEquals(sameArrow,Arrow()),"Refreshing enabled state must preserve the same button.");Click(0x18000);
var assigned=model.RawData.ToArray();var assignedMetadata=Metadata();
mapper.Undo.Execute(null);Layout();obj=Object();mapper.PrimaryMap.SelectedEvent=obj;Layout();Enabled(false,"Undo assignment");
Check(original.SequenceEqual(model.RawData)&&originalMetadata.SequenceEqual(Metadata()),"Undo must restore exact initial bytes and metadata.");
mapper.Redo.Execute(null);Layout();obj=Object();mapper.PrimaryMap.SelectedEvent=obj;Layout();Enabled(true,"Redo assignment");
Check(assigned.SequenceEqual(model.RawData)&&assignedMetadata.SequenceEqual(Metadata()),"Redo must restore exact assigned bytes and metadata.");
// Keep callback capture on the same displayed object for direct text/pointer transitions.
obj=Object();mapper.PrimaryMap.SelectedEvent=obj;Layout();
obj.ScriptAddressText="<null>";Enabled(false,"Clear address through text");
obj.ScriptAddressText="<018000>";Enabled(true,"Type a valid address");Click(0x18000);
obj.ScriptAddressText="<018100>";Enabled(true,"Replace an existing address");Click(0x18100);
obj.ScriptAddressText="<F00000>";Enabled(false,"Out-of-ROM address");
obj.ScriptAddressText="<018000>";Enabled(true,"Correct an out-of-ROM address");Click(0x18000);
obj.ScriptAddress=Pointer.NULL;Enabled(false,"Clear address programmatically");
// The existing Create Script and NPC template paths must enable navigation immediately.
model.FreeSpaceStart=0x1A000;
viewport.CurrentChange.ChangeData(model,0x1A000,Enumerable.Repeat((byte)0xFF,0x2000).ToArray());viewport.ChangeHistory.ClearHistory(false);
var beforeCreate=model.RawData.ToArray();obj.CreateScript();viewport.ChangeHistory.ChangeCompleted();Enabled(true,"Create Script");
Check(lastGoto==obj.ScriptAddress,"Create Script requests navigation to its allocated source.");
Check(model.RawData.Skip(obj.ScriptAddress).Take(4).SequenceEqual(new byte[]{0x6A,0x5A,0x6C,0x02}),"Create Script preserves its existing starting bytes.");Click(obj.ScriptAddress);
var created=model.RawData.ToArray();mapper.Undo.Execute(null);Layout();obj=Object();mapper.PrimaryMap.SelectedEvent=obj;Layout();Enabled(false,"Undo Create Script");
Check(beforeCreate.SequenceEqual(model.RawData),"Undo Create Script restores every byte.");mapper.Redo.Execute(null);Layout();obj=Object();mapper.PrimaryMap.SelectedEvent=obj;Layout();Enabled(true,"Redo Create Script");Check(created.SequenceEqual(model.RawData),"Redo Create Script restores every byte.");
obj=Object();mapper.PrimaryMap.SelectedEvent=obj;obj.ScriptAddress=Pointer.NULL;Layout();Enabled(false,"Before NPC template");
mapper.Templates.CreateNPC(obj,viewport.CurrentChange);viewport.ChangeHistory.ChangeCompleted();Enabled(true,"NPC template assignment");Click(obj.ScriptAddress);
window.Editor=null;window.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
