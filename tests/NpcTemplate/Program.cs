using Avalonia;
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
var sprites = Enumerable.Range(0, 152).Select(i => {
   int w = i == 108 ? 64 : i == 150 ? 96 : i is 106 or 107 or 109 ? 32 : 16;
   int h = i == 108 ? 64 : i == 150 ? 128 : 32;
   var pixels=new short[w*h];
   for(int y=1;y<h-1;y++)for(int x=1;x<w-1;x++)pixels[y*w+x]=(short)(i+1);
   return new ReadonlyPixelViewModel(w,h,pixels,-1);
}).ToArray();
ObjectEventViewModel Object() => new(mapper.PrimaryMap, _ => {}, model.GetTableModel("synthetic.object", () => viewport.CurrentChange)[0], mapper.Templates, sprites, sprites[0], null);

var templates=mapper.Templates;
templates.RefreshLists(sprites);templates.SelectedTemplate=TemplateType.Npc;
mapper.ShowTemplateSettings=true;Layout();
var popup=view.GetLogicalDescendants().OfType<Popup>().Single(p=>ReferenceEquals(p.DataContext,templates));
var picker=((Control)popup.Child!).GetVisualDescendants().OfType<AngleComboBox>().Single(c=>c.Name=="NpcTemplateGraphics");
Check(picker.IsEffectivelyVisible,"The NPC template must expose a visible pre-placement sprite picker.");
viewport.ChangeHistory.ClearHistory(false);
var source=model.RawData.ToArray();var metadata=model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
void Choose(int index) {
 picker.IsDropDownOpen=true;Layout();
 var item=picker.ContainerFromIndex(index)!;item.BringIntoView();Layout();
 var pointer=new Pointer(12,PointerType.Mouse,true);
 item.RaiseEvent(new PointerPressedEventArgs(item,pointer,TopLevel.GetTopLevel(item),new Avalonia.Point(4,4),0,new PointerPointProperties(RawInputModifiers.LeftMouseButton,PointerUpdateKind.LeftButtonPressed),KeyModifiers.None,1));
 item.RaiseEvent(new PointerReleasedEventArgs(item,pointer,TopLevel.GetTopLevel(item),new Avalonia.Point(4,4),1,new PointerPointProperties(RawInputModifiers.None,PointerUpdateKind.LeftButtonReleased),KeyModifiers.None,MouseButton.Left));
 Layout();
 Check(templates.NpcGraphics==index&&picker.SelectedIndex==index,"A real popup item selection must update the NPC preference.");
 Check(templates.ObjectTemplateImage.PixelData.Contains((short)(index+1)),"The toolbar image must use the selected NPC sprite.");
 Check(templates.TrainerGraphics==0,"NPC choices must not call the trainer sprite/preference path.");
 Check(picker.Bounds.Height<=70,"Large sprites must not stretch the selected picker.");
}
foreach(int index in new[]{1,107,108,109,150})Choose(index);
Check(source.SequenceEqual(model.RawData)&&metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize())&&!viewport.Undo.CanExecute(null),"Choosing a template sprite must not edit ROM bytes, metadata or history.");
picker.IsDropDownOpen=false;
var expectedIcon=templates.ObjectTemplateImage.PixelData.ToArray();
templates.SelectedTemplate=TemplateType.None;Layout();templates.SelectedTemplate=TemplateType.Npc;Layout();
Check(templates.NpcGraphics==150&&templates.ObjectTemplateImage.PixelData.SequenceEqual(expectedIcon),"Switching away and back must retain NPC choice and icon.");
mapper.ShowTemplateSettings=false;Layout();mapper.ShowTemplateSettings=true;Layout();
Check(picker.SelectedIndex==150,"Reopening template settings must retain the selected sprite.");
mapper.NavigateTo(0,0,8,8);Layout();
Check(ReferenceEquals(templates,mapper.Templates)&&templates.NpcGraphics==150,"Navigating maps in one ROM must retain the NPC preference.");
mapper.NavigateTo(3,0,8,8);Layout();
templates.RefreshLists(sprites);Layout();
Check(templates.NpcGraphics==150&&picker.SelectedIndex==150,$"Refreshing sprite options must retain a still-valid choice: model={templates.NpcGraphics}, picker={picker.SelectedIndex}, template={templates.SelectedTemplate}.");
var icon=view.FindControl<PixelImage>("ObjectTemplateIcon")!;
Check(icon.Parent is Viewbox fitted&&fitted.Bounds.Width<=22&&fitted.Bounds.Height<=22,"Even large chosen sprites must fit the toolbar icon box.");
// A minimal trainer preview table permits testing the independent trainer preference path.
model.WritePointer(new ModelDelta(),0x14000,0x14500);
var trainerError=ArrayRun.TryParse(model,"[sprite<>]1",0x14000,SortedSpan<int>.None,out var trainerSprites);
Check(!trainerError.HasError,"Trainer preview fixture must parse.");
model.ObserveAnchorWritten(new NoDataChangeDeltaModel(),HardcodeTablesModel.TrainerSpritesName,trainerSprites);
foreach(var type in new[]{TemplateType.Trainer,TemplateType.Legendary}) {
 templates.SelectedTemplate=type;templates.TrainerGraphics=type==TemplateType.Trainer?5:7;Layout();
 Check(!picker.IsEffectivelyVisible&&templates.NpcGraphics==150,"Trainer/Legendary choices must not replace the remembered NPC sprite.");
 templates.SelectedTemplate=TemplateType.Npc;Layout();
 Check(picker.IsEffectivelyVisible&&templates.ObjectTemplateImage.PixelData.SequenceEqual(expectedIcon),"Returning from another template restores the NPC icon.");
}
var remembered=templates.NpcGraphics;
templates.NpcGraphics=-1;templates.NpcGraphics=int.MaxValue;
Check(templates.NpcGraphics==remembered,"Transient invalid selection indices must not overwrite the last choice.");
mapper.ShowTemplateSettings=false;Layout();
// The generated NPC remains byte-for-byte the existing template except for its graphics ID.
model.FreeSpaceStart=0x18000;
viewport.CurrentChange.ChangeData(model,0x18000,Enumerable.Repeat((byte)0xFF,0x8000).ToArray());
viewport.ChangeHistory.ClearHistory(false);
var baseline=model.RawData.ToArray();var baselineMetadata=model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
var obj=Object();templates.CreateNPC(obj,viewport.CurrentChange);viewport.ChangeHistory.ChangeCompleted();
Check(obj.Graphics==150,"Creating an NPC must assign the remembered sprite ID.");
var script=obj.ScriptAddress;
var expected=Convert.FromHexString("0F0000000000090202C3B4E100D5E200C8CABDABFF");
BitConverter.GetBytes(script+9+0x08000000).CopyTo(expected,2);
Check(model.RawData.Skip(script).Take(expected.Length).SequenceEqual(expected),"NPC script and default dialogue bytes must remain unchanged.");
Check(obj.Elevation==3&&obj.MoveType==2&&obj.RangeXY=="(3, 3)"&&obj.TrainerType==0&&obj.TrainerRangeOrBerryID==0&&obj.Flag==0,"All other NPC template settings must retain their previous defaults.");
var created=model.RawData.ToArray();
byte[] saved=null;string[] savedMetadata=null;
fs.Save=f=>{saved=f.Contents.ToArray();return true;};fs.SaveMetadata=(_,lines)=>{savedMetadata=lines.ToArray();return true;};
viewport.Save.Execute(fs);
Check(created.SequenceEqual(saved),"Save must include the chosen graphics and normal generated script.");
var reloaded=new PokemonModel(saved,new StoredMetadata(savedMetadata),viewport.Singletons);
Check(created.SequenceEqual(reloaded.RawData),"Reload preserves the created NPC exactly.");
mapper.Undo.Execute(null);Layout();
Check(baseline.SequenceEqual(model.RawData)&&baselineMetadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"One Undo restores all script/resource/pointer bytes and metadata.");
Check(templates.NpcGraphics==150,"Undoing placement must retain the template preference for the next NPC.");
mapper.Redo.Execute(null);Layout();Check(created.SequenceEqual(model.RawData),"Redo restores the NPC with the selected sprite.");
templates.RefreshLists(sprites.Take(8).ToArray());Layout();
Check(templates.NpcGraphics==0,"If the chosen sprite is removed, fall back to a valid default.");
window.Editor=null;window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
