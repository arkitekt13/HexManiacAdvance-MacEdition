using System.Diagnostics;
using System.Reflection;
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
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;
using HavenSoft.HexManiac.Review;
using Point = Avalonia.Point;
using Pointer = HavenSoft.HexManiac.Core.ViewModels.DataFormats.Pointer;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
 .UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
int checks=0;
void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
var fixture=new SyntheticMapFixture(90,60,withNpcText:true,withScriptEvent:true,withSignpost:true);var model=fixture.Model;var setup=new ModelDelta();
model.SetList(new NoDataChangeDeltaModel(),"FacingOptions","NoMovement");model.SetList(new NoDataChangeDeltaModel(),"MapAttributeBehaviors","Normal");
model[0x17000]=9;model.WriteMultiByteValue(0x17014,2,new ModelDelta(),0);model.WriteMultiByteValue(0x17004,2,setup,8);model.WriteMultiByteValue(0x17006,2,setup,8);
model[0x162]=1;model.WritePointer(setup,0x16C,0x16000);model.WriteMultiByteValue(0x16000,2,setup,12);model.WriteMultiByteValue(0x16002,2,setup,8);model.WritePointer(setup,0x1600C,0x18800);
var parse=ArrayRun.TryParse(model,"[x: y: elevation: trigger:|h index:: script<`xse`>]1",0x16000,SortedSpan.One(0x16C),out var scripts);
Check(!parse.HasError,"Synthetic script event table parses.");model.ObserveAnchorWritten(setup,"synthetic.script.events",scripts);
setup.ChangeData(model,0x18800,new byte[]{0x16,1,0x40,7,0,2});setup.ChangeData(model,0x18A00,new byte[]{0x16,1,0x40,8,0,2});
model.ObserveRunWritten(setup,new XSERun(0x18000,SortedSpan.One(0x17010)));
model.ObserveRunWritten(setup,new XSERun(0x18800,SortedSpan.One(0x1600C)));
// 18A00 remains unformatted: navigation must decode it without annotating the live ROM.
var fs=new StubFileSystem{MetadataFor=_=>Array.Empty<string>()};var dispatch=new WorkflowDispatch();
var source=new ViewPort("map-navigation.bin",model,dispatch,new Singletons(dispatch),fs:fs){ScriptReferenceMapID=0};source.InitializationWorkload.GetAwaiter().GetResult();
source.Goto.Execute(0x18000);source.Tools.CodeTool.Mode=CodeMode.Script;source.Tools.SelectedIndex=3;dispatch.Flush();
var tool=source.Tools.CodeTool;source.ChangeHistory.ClearHistory(false);source.ChangeHistory.TagAsSaved();
Application.Current.Resources["FileSystem"]=fs;var editor=new EditorViewModel(fs,InstantDispatch.Instance){PreferScriptMapView=true};App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);editor.Add(source);editor.GotoViewModel.ControlVisible=false;
var window=new MainWindow{Width=1400,Height=900,Editor=editor,FileSystem=fs};window.Resources["FileSystem"]=fs;window.Show();
void Pump(){Dispatcher.UIThread.RunJobs();window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();}
void Until(Func<bool> condition){var time=Stopwatch.StartNew();while(!condition()){Pump();Thread.Sleep(1);if(time.Elapsed.TotalSeconds>25)throw new TimeoutException("Map navigation timed out.");}Pump();}
Pump();var tab=window.GetVisualDescendants().OfType<TabView>().Single(t=>ReferenceEquals(t.DataContext,source));
var pane=tab.FindControl<ScriptMapReferenceView>("ScriptMapPane");
Image Image()=>pane.GetVisualDescendants().OfType<Image>().Single();
TextBox Box()=>tab.GetVisualDescendants().OfType<TextEditor>().First(c=>ReferenceEquals(c.DataContext,tool.Contents[0].Editor)).EditBox;
string[] Metadata()=>model.ExportMetadata(null,source.Singletons.MetadataInfo).Serialize();
string Info()=>pane.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Name=="ReferenceEventInfo").Text;
Point At(double x,double y)=>Image().TranslatePoint(new Point(x,y),window).Value;
void ClickAt(double x,double y){var at=At(x,y);window.MouseMove(at);window.MouseDown(at,MouseButton.Left);window.MouseUp(at,MouseButton.Left);Pump();}
void ShowMapAgain(){var count=tab.ReferenceRenderCount;tab.FindControl<Button>("ScriptHexViewButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();tab.FindControl<Button>("ScriptMapViewButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Until(()=>tab.ReferenceRenderCount>count);}
Until(()=>Image().Source!=null);
var bytes=model.RawData.ToArray();var metadata=Metadata();var snapshot=ScriptMapSnapshot.Create(source,0);
Check(snapshot.Events.Count==3,$"Snapshot includes objects, script events and signposts: {string.Join(",",snapshot.Events.Select(e=>e.Kind))}.");
var obj=snapshot.Events.Single(e=>e.Kind==ScriptMapEventKind.Object);var script=snapshot.Events.Single(e=>e.Kind==ScriptMapEventKind.Script);
Check(obj.ObjectID==9&&obj.Flag==0&&obj.X==8&&obj.Y==8,"Snapshot captures object ID and unassigned flag and coordinates.");
Check(obj.TryResolve(source,out var initial)&&initial.Address==0x18000,"Object lookup resolves current script.");
Check(bytes.SequenceEqual(model.RawData)&&metadata.SequenceEqual(Metadata())&&source.ChangeHistory.IsSaved&&!source.ChangeHistory.Undo.CanExecute(null),"Snapshot and hit metadata leave all live ROM/metadata/history state untouched.");
pane.Offset=new Vector(30,20);pane.ShowGrid=true;Pump();var offset=pane.Offset;var image=Image().Source;var renders=tab.ReferenceRenderCount;
ClickAt(8*16+8,8*16+8);Check(Info().Contains("Object ID: 9")&&Info().Contains("Flag: none"),"Object click shows read-only Object ID and Flag.");
if(args.Length==1){using var preview=new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(1400,900));preview.Render(tab);preview.Save(args[0]);}
ClickAt(12*16+8,8*16+8);Until(()=>tool.Contents[0].Address==0x18800);
Check(tool.Contents[0].Content.Contains("7")&&editor.Count==1&&ReferenceEquals(editor.SelectedTab,source),"Script event click loads its code in the same tab.");
Check(Box().IsFocused&&tab.IsScriptMapVisible&&!tab.FindControl<DockPanel>("HexPane").IsEnabled,"Navigation focuses source with hidden hex still disabled.");
Check(pane.Offset==offset&&pane.ShowGrid&&ReferenceEquals(image,Image().Source)&&tab.ReferenceRenderCount==renders,"Script navigation retains pan/grid and the exact map image without rendering again.");
Check(bytes.SequenceEqual(model.RawData)&&metadata.SequenceEqual(Metadata())&&source.ChangeHistory.IsSaved,"Navigation alone cannot edit bytes, metadata or saved state.");
// Read current pointers rather than an address cached into the image.
model.WritePointer(new ModelDelta(),0x1600C,0x18A00);bytes=model.RawData.ToArray();metadata=Metadata();
ClickAt(12*16+8,8*16+8);Until(()=>tool.Contents[0].Address==0x18A00);
Check(tool.Contents[0].Content.Contains("8")&&bytes.SequenceEqual(model.RawData)&&metadata.SequenceEqual(Metadata()),"A changed pointer opens current unformatted script bytes without live formatting writes.");
// Empty-map clicks, button releases and actual drags never request navigation or map editing.
int requests=0;pane.EventRequested+=(_,_)=>requests++;
var beforeBody=tool.Contents[0];var field=Box();field.Focus();field.CaretIndex=2;field.SelectionStart=1;field.SelectionEnd=2;var selection=(field.CaretIndex,field.SelectionStart,field.SelectionEnd);
ClickAt(20*16+8,12*16+8);Check(requests==0&&ReferenceEquals(beforeBody,tool.Contents[0])&&selection==(field.CaretIndex,field.SelectionStart,field.SelectionEnd),"Blank tiles do not select tiles or disturb source selection.");
var from=At(8*16+8,8*16+8);window.MouseMove(from);window.MouseDown(from,MouseButton.Left);window.MouseMove(from+new Vector(-24,-18),RawInputModifiers.LeftMouseButton);window.MouseUp(from+new Vector(-24,-18),MouseButton.Left);Pump();
Check(requests==0&&pane.Offset!=offset,$"Dragging from an object pans instead of opening its script: requests={requests}, offset={pane.Offset}, original={offset}, active={pane.IsPanning}.");
pane.Offset=offset;Pump();from=At(8*16+8,8*16+8);window.MouseDown(from,MouseButton.Left);window.MouseMove(from+new Vector(2,1),RawInputModifiers.LeftMouseButton);window.MouseUp(from+new Vector(2,1),MouseButton.Left);Pump();
Check(requests==1&&pane.Offset==offset,"Small click jitter opens an event without shifting the map.");
from=At(8*16+8,8*16+8);window.MouseDown(from,MouseButton.Left);pane.CancelPan();window.MouseUp(from,MouseButton.Left);Pump();Check(requests==1,"Canceled capture cannot turn its release into navigation.");
from=At(8*16+8,8*16+8);window.MouseDown(from,MouseButton.Right);window.MouseUp(from,MouseButton.Right);Pump();Check(requests==1,"Secondary clicks add no map editing commands.");window.KeyPressQwerty(PhysicalKey.Escape,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.Escape,RawInputModifiers.None);Pump();
// Missing pointers show IDs and leave the left editor untouched.
model.WritePointer(new ModelDelta(),0x17010,Pointer.NULL);var unchanged=tool.Contents[0];bytes=model.RawData.ToArray();metadata=Metadata();
ClickAt(8*16+8,8*16+8);Check(ReferenceEquals(unchanged,tool.Contents[0])&&Info().Contains("No script")&&Info().Contains("Flag: none"),"An object without a script still shows its IDs without creating or navigating anything.");
Check(bytes.SequenceEqual(model.RawData)&&metadata.SequenceEqual(Metadata()),"Missing-script clicks perform no writes.");model.WritePointer(new ModelDelta(),0x17010,0x18000);
// Incomplete/rejected source blocks navigation and preserves the precise draft/diagnostics.
ClickAt(8*16+8,8*16+8);Until(()=>tool.Contents[0].Address==0x18000);
var body=tool.Contents[0];var originalSource=body.Content;source.ChangeHistory.ClearHistory(false);source.ChangeHistory.TagAsSaved();Pump();Until(()=>Image().Source!=null);
var rejected="msgbox.npc <auto>\n{\n"+new string('A',180)+"\n}\nnot_a_command\nend";body.Editor.Content=rejected;Pump();field=Box();field.Focus();field.CaretIndex=10;field.SelectionStart=8;field.SelectionEnd=10;Pump();selection=(field.CaretIndex,field.SelectionStart,field.SelectionEnd);
bytes=model.RawData.ToArray();metadata=Metadata();offset=pane.Offset;image=Image().Source;
var pending=dispatch.CapturePendingCallbacks();ClickAt(12*16+8,8*16+8);
Check(ReferenceEquals(body,tool.Contents[0])&&body.Content==rejected&&body.HasError&&body.Editor.ErrorLocations.Count>0,$"A failed pending compile blocks script switching and retains rejected source plus diagnostics: same={ReferenceEquals(body,tool.Contents[0])} error={body.HasError} pending={tool.HasPendingScriptChanges} info={Info()} image={Image().Source!=null} requests={requests} offset={pane.Offset} at={At(200,136)} bounds={pane.Bounds}.");
Check(Info().Contains("Fix the errors")&&pane.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Name=="ReferenceEventInfo").Foreground is Avalonia.Media.ISolidColorBrush red&&red.Color.R>red.Color.G,"Blocked navigation has a red inline notice.");
Check(Box().IsFocused&&selection==(field.CaretIndex,field.SelectionStart,field.SelectionEnd)&&pane.Offset==offset&&ReferenceEquals(image,Image().Source),"Blocked navigation retains caret/selection/focus/pan and pixels.");
Check(bytes.SequenceEqual(model.RawData)&&metadata.SequenceEqual(Metadata())&&source.ChangeHistory.IsSaved&&!source.ChangeHistory.Undo.CanExecute(null),"Failed resource compilation and map clicks preserve bytes, metadata, saved state and model history.");
foreach(var callback in pending)callback();Check(body.Content==rejected&&bytes.SequenceEqual(model.RawData),"Canceled pending callbacks cannot later change a rejected source or another script.");
ClickAt(20*16+8,8*16+8);Check(ReferenceEquals(body,tool.Contents[0])&&body.Content==rejected&&Info().Contains("Fix the errors"),"Signpost navigation shares rejected-draft protection.");
body.Editor.Content=originalSource;ClickAt(12*16+8,8*16+8);Until(()=>tool.Contents[0].Address==0x18A00);Check(!tool.HasActiveScriptDraft,"Correction and retry permit navigation.");
// Compile a growing script before resolving the click, including a shared target.
ClickAt(8*16+8,8*16+8);Until(()=>tool.Contents[0].Address==0x18000);
model.ObserveAnchorWritten(new ModelDelta(),"synthetic.script.guard",new NoInfoRun(0x1800C,SortedSpan<int>.None));model.FreeSpaceStart=0x1C000;new ModelDelta().ChangeData(model,0x1C000,Enumerable.Repeat((byte)0xFF,0x4000).ToArray());
source.ChangeHistory.ClearHistory(false);source.ChangeHistory.TagAsSaved();Pump();Until(()=>Image().Source!=null);var original=model.RawData.ToArray();var originalMetadata=Metadata();
body=tool.Contents[0];body.Editor.Content=string.Concat(Enumerable.Repeat("setvar 0x4001 4\n",30))+"end";Pump();
ClickAt(8*16+8,8*16+8);Pump();source.ChangeHistory.ChangeCompleted();
int moved=model.ReadPointer(0x17010);Check(moved!=0x18000&&model.GetNextRun(moved) is XSERun,$"A valid oversized script relocates and updates the event pointer before switching: moved={moved:X} run={model.GetNextRun(moved).GetType()} active={tool.Contents[0].Address:X} draft={tool.HasActiveScriptDraft}.");
Check(tool.Contents[0].Address==moved&&Info().Contains(moved.ToString("X6")),"The click that settles relocation resolves the destination again before navigation.");
ClickAt(12*16+8,8*16+8);Until(()=>tool.Contents[0].Address==0x18A00);ClickAt(8*16+8,8*16+8);Until(()=>tool.Contents[0].Address==moved);
Check(Info().Contains(moved.ToString("X6"))&&tool.Contents[0].Content.Contains("setvar"),"Clicking the same snapshot after relocation opens the updated address and refreshes details.");
var grown=model.RawData.ToArray();var grownMetadata=Metadata();source.Undo.Execute(null);dispatch.Flush();Until(()=>Image().Source!=null);
Check(original.SequenceEqual(model.RawData)&&originalMetadata.SequenceEqual(Metadata())&&source.Redo.CanExecute(null),"One Undo restores all growth/pointer/metadata changes without read-only refresh destroying Redo.");
source.Redo.Execute(null);dispatch.Flush();Until(()=>Image().Source!=null);
Check(grown.SequenceEqual(model.RawData)&&grownMetadata.SequenceEqual(Metadata()),"Redo restores the relocated script exactly.");
// Pointer targets may be shared. Switching by map click must still reuse one editor tab.
model.WritePointer(new ModelDelta(),0x1600C,moved);ClickAt(12*16+8,8*16+8);Check(editor.Count==1&&Info().Contains("Script event 1"),"Shared scripts update event details without creating tabs.");
// Actual overlapping rows produce a chooser; choosing its event loads the appropriate code.
model.WriteMultiByteValue(0x16000,2,new ModelDelta(),8);model.WritePointer(new ModelDelta(),0x1600C,0x18A00);ShowMapAgain();
ClickAt(8*16+8,8*16+8);var menu=(ContextMenu)typeof(ScriptMapReferenceView).GetField("eventMenu",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pane);
Check(menu?.IsOpen==true,"Overlapping object/script events offer a chooser.");
var choice=((IEnumerable<MenuItem>)menu.ItemsSource).Single(i=>i.Header.ToString().StartsWith("Script event"));Pump();var root=TopLevel.GetTopLevel(choice);var point=choice.TranslatePoint(new Point(choice.Bounds.Width/2,choice.Bounds.Height/2),root).Value;
root.MouseMove(point);root.MouseDown(point,MouseButton.Left);root.MouseUp(point,MouseButton.Left);Pump();Until(()=>tool.Contents[0].Address==0x18A00);
Check(Info().Contains("Script event 1"),"The real chooser click opens the chosen event, not the object underneath.");
ClickAt(8*16+8,8*16+8);menu=(ContextMenu)typeof(ScriptMapReferenceView).GetField("eventMenu",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pane);var stale=((IEnumerable<MenuItem>)menu.ItemsSource).First();
pane.Clear();stale.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Pump();Check(tool.Contents[0].Address==0x18A00&&!menu.IsOpen,"Stale chooser actions cannot navigate after the snapshot is cleared.");
ShowMapAgain();
// Large sprite pixels outside the anchor tile remain clickable, without retaining event VMs.
var large = snapshot with { Events = new[] { obj with { Bounds = new Rect(124,112,32,32) } } };
pane.ShowSnapshot(large);Pump();bytes=model.RawData.ToArray();metadata=Metadata();
ClickAt(150,116);Until(()=>tool.Contents[0].Address==moved);
Check(Info().Contains("Object ID: 9")&&bytes.SequenceEqual(model.RawData)&&metadata.SequenceEqual(Metadata()),"A large sprite can be clicked outside its tile without editing the map.");
// Old hit records cannot silently resolve another row after deletion, movement or map changes.
model.WriteMultiByteValue(0x17004,2,new ModelDelta(),9);
Check(!obj.TryResolve(source,out _),"A moved/replaced object rejects its stale snapshot hit record.");
model.WriteMultiByteValue(0x17004,2,new ModelDelta(),8);model.WriteMultiByteValue(0x17014,2,new ModelDelta(),0x2AB);
Check(obj.TryResolve(source,out var changedFlag)&&changedFlag.Flag==0x2AB,"Object details read the current flag rather than a cached value.");model.WriteMultiByteValue(0x17014,2,new ModelDelta(),0);
model.WriteMultiByteValue(0x17014,2,new ModelDelta(),0x2AB);ClickAt(8*16+8,8*16+8);Check(Info().Contains("Flag: 0x02AB")&&!Info().Contains("Sprite"),"Object click displays the assigned flag in hex instead of Sprite ID.");
model.WriteMultiByteValue(0x17014,2,new ModelDelta(),0);ClickAt(8*16+8,8*16+8);Check(Info().Contains("Flag: none"),"Removing the flag displays none on the next click without a render.");
source.ScriptReferenceMapID=1000;Check(!obj.TryResolve(source,out _),"Hit records cannot cross to another map context.");source.ScriptReferenceMapID=0;Pump();Until(()=>Image().Source!=null);
// Dismissing an overlapping-event chooser must not navigate.
ShowMapAgain();ClickAt(8*16+8,8*16+8);menu=(ContextMenu)typeof(ScriptMapReferenceView).GetField("eventMenu",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pane);
var beforeDismiss=tool.Contents[0];pane.CancelPan();Pump();
Check(menu?.IsOpen==false&&ReferenceEquals(beforeDismiss,tool.Contents[0]),"Canceling a chooser leaves the active script unchanged.");
// Signpost kind determines whether arg contains a script pointer or packed item/base data.
for(int kind=0;kind<=4;kind++){
 model[0x1B005]=(byte)kind;model.WritePointer(new ModelDelta(),0x1B008,0x18800);
 pane.ShowSnapshot(ScriptMapSnapshot.Create(source,0));Pump();bytes=model.RawData.ToArray();metadata=Metadata();
 ClickAt(20*16+8,8*16+8);Until(()=>tool.Contents[0].Address==0x18800);
 if(kind==0)Check(Box().SelectionStart==Box().SelectionEnd,"Map View signpost navigation does not select all source.");
 Check(Info().Contains("Signpost 1")&&editor.Count==1,"Script-bearing signpost kind "+kind+" opens in the same left editor.");
 Check(bytes.SequenceEqual(model.RawData)&&metadata.SequenceEqual(Metadata()),"Signpost navigation does not lazily format the live ROM.");
}
model.WritePointer(new ModelDelta(),0x1B008,0x18A00);ClickAt(20*16+8,8*16+8);Until(()=>tool.Contents[0].Address==0x18A00);
Check(Info().Contains("018A00"),"Signpost navigation resolves a changed pointer from the live row.");
var signBody=tool.Contents[0];var signSource=signBody.Content;signBody.Editor.Content=signSource+"\n# pending";Pump();
foreach(int kind in new[]{5,6,7,8,255}){
 model[0x1B005]=(byte)kind; // arg intentionally still resembles a valid ROM pointer.
 pane.ShowSnapshot(ScriptMapSnapshot.Create(source,0));Pump();bytes=model.RawData.ToArray();metadata=Metadata();var callbacks=dispatch.CapturePendingCallbacks();
 ClickAt(20*16+8,8*16+8);
 Check(ReferenceEquals(signBody,tool.Contents[0])&&tool.HasPendingScriptChanges&&callbacks.SequenceEqual(dispatch.CapturePendingCallbacks()),"Non-script signpost kind "+kind+" cannot navigate or settle source.");
 Check(Info().Contains("no script")&&(kind is >=5 and <=7?Info().Contains("Hidden item"):kind==8?Info().Contains("Secret base"):Info().Contains("Unsupported")),"Non-script signposts display their purpose.");
 Check(bytes.SequenceEqual(model.RawData)&&metadata.SequenceEqual(Metadata()),"Packed signpost data stays unchanged.");
}
signBody.Editor.Content=signSource;tool.FlushPendingScriptChanges();
model[0x1B005]=0;pane.ShowSnapshot(ScriptMapSnapshot.Create(source,0));Pump();
var signHit=ScriptMapSnapshot.Create(source,0).Events.Single(e=>e.Kind==ScriptMapEventKind.Signpost);
model[0x1B005]=5;Check(!signHit.TryResolve(source,out _),"A stale script-signpost hit cannot reinterpret a newly changed hidden-item union.");
model[0x1B005]=0;model.WritePointer(new ModelDelta(),0x1B008,Pointer.NULL);
ClickAt(20*16+8,8*16+8);Check(Info().Contains("No script")&&ReferenceEquals(signBody,tool.Contents[0]),"A null signpost pointer shows its status without creating a script.");
model[0x163]=0;Check(!signHit.TryResolve(source,out _),"A deleted signpost cannot be selected from old image metadata.");model[0x163]=1;
// Clipboard shortcuts following a map click stay in source, never hidden hex.
model.WriteMultiByteValue(0x16000,2,new ModelDelta(),12);ShowMapAgain();ClickAt(12*16+8,8*16+8);Until(()=>tool.Contents[0].Address==0x18A00);
field=Box();field.CaretIndex=0;field.SelectionStart=field.SelectionEnd=0;field.Focus();bytes=model.RawData.ToArray();
UiSync.Run(()=>window.Clipboard.SetTextAsync("# copied between event scripts\n"));var modifier=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
window.KeyPressQwerty(PhysicalKey.V,modifier);window.KeyReleaseQwerty(PhysicalKey.V,RawInputModifiers.None);Pump();
Check(tool.Contents[0].Content.Contains("copied between event scripts")&&bytes.SequenceEqual(model.RawData),"Command-V after event navigation pastes into script source without writing hidden hex bytes.");
window.Editor=null;Pump();
var subscribers=(EventHandler)typeof(CodeTool).GetField("ScriptStateChanged",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tool);
Check(subscribers?.GetInvocationList().Any(callback=>ReferenceEquals(callback.Target,tab))!=true,"Detached tabs release the new script-state subscription.");
Check(Image().Source==null,"Closing the document releases its map image.");window.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
