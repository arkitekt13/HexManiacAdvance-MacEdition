using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
var viewport=new ViewPort("event-editing-fixture.bin",model,InstantDispatch.Instance,new Singletons(),tutorials:editor.MapTutorials,fs:fs);
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
HavenSoft.HexManiac.Core.ViewModels.Map.ScriptEventViewModel Event() =>
   new(_=>{},mapper.PrimaryMap.GetMapModel().GetSubTable("events")[0].GetSubTable("scripts")[0],mapper.Templates);
var ev=Event();mapper.SelectedEvent=ev;ev.ScriptAddressText="<001800>";Layout();
var angle=view.GetVisualDescendants().OfType<AngleTextBox>().Single(a=>ReferenceEquals(a.DataContext,ev)
   && a.TextBinding is Avalonia.Data.Binding b && b.Path=="ScriptAddressText");
var box=angle.GetTextBox();Layout();
void Key(PhysicalKey key,RawInputModifiers modifiers=RawInputModifiers.None){
   window.KeyPressQwerty(key,modifiers);window.KeyReleaseQwerty(key,RawInputModifiers.None);Layout();
}
void Caret(int index){box.Focus();box.CaretIndex=index;box.SelectionStart=index;box.SelectionEnd=index;Layout();}
viewport.ChangeHistory.ClearHistory(false);
var original=model.RawData.ToArray();
var metadata=model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
var mapPosition=(mapper.PrimaryMap.LeftEdge,mapper.PrimaryMap.TopEdge);
void Preserved(string context){
   Check(ReferenceEquals(mapper.SelectedEvent,ev) && model[0x162]==1,context+" retains the Script Event.");
   Check(model.ReadPointer(0x1600C)==0x1800 && original.SequenceEqual(model.RawData),context+" preserves pointer and every ROM byte.");
   Check(metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),context+" preserves metadata.");
   Check(box.IsFocused && ReferenceEquals(angle.Content,box),context+" retains the same focused editor.");
   Check(mapPosition==(mapper.PrimaryMap.LeftEdge,mapper.PrimaryMap.TopEdge),context+" cannot pan the map while editing text.");
}
Caret(2);Key(PhysicalKey.Backspace);
Check(box.Text=="<01800>","Backspace removes one source character rather than the Script Event.");Preserved("Backspace");
box.Text="<001800>";Caret(1);Key(PhysicalKey.Delete);
Check(box.Text=="<01800>","Forward Delete removes the character after the caret.");Preserved("Delete");
// Deletion at the start/end must remain a harmless text operation too.
box.Text="<001800>";Caret(0);Key(PhysicalKey.Backspace);Preserved("Backspace at start");
Caret(box.Text!.Length);Key(PhysicalKey.Delete);Preserved("Delete at end");
Caret(2);Key(PhysicalKey.ArrowRight);
Check(box.CaretIndex==3,"Right arrow moves the text caret.");Preserved("Right arrow");
Key(PhysicalKey.ArrowLeft);
Check(box.CaretIndex==2,"Left arrow moves the text caret.");Preserved("Left arrow");
Key(PhysicalKey.ArrowUp);Key(PhysicalKey.ArrowDown);Preserved("Vertical arrows");
Key(PhysicalKey.Escape);Preserved("Escape in text");
// Other event fields use the same AngleTextBox editing path.
var trigger=view.GetVisualDescendants().OfType<AngleTextBox>().Single(a=>ReferenceEquals(a.DataContext,ev)
   && a.TextBinding is Avalonia.Data.Binding b && b.Path=="TriggerHex").GetTextBox();
trigger.Focus();trigger.SelectionStart=0;trigger.SelectionEnd=0;Key(PhysicalKey.Backspace);
Check(trigger.IsFocused && ReferenceEquals(mapper.SelectedEvent,ev) && model[0x162]==1,"Backspace in another event field retains the event.");
Check(original.SequenceEqual(model.RawData),"No-op field editing preserves the entire ROM.");
box.Focus();box.SelectAll();Key(PhysicalKey.Backspace);
Check(box.Text==string.Empty && ReferenceEquals(mapper.SelectedEvent,ev) && model[0x162]==1,"Clearing the address retains its Script Event.");
Check(model.ReadPointer(0x1600C)==HavenSoft.HexManiac.Core.ViewModels.DataFormats.Pointer.NULL,"Empty address retains the existing null-pointer parsing rule.");
viewport.ChangeHistory.ChangeCompleted();viewport.Undo.Execute(null);Layout();
Check(original.SequenceEqual(model.RawData),"Undo of clearing the address restores the original pointer and all bytes.");

// Once focus returns to the map, the same shortcuts retain their original commands.
foreach(var key in new[]{PhysicalKey.Backspace,PhysicalKey.Delete}){
   mapper.SelectedEvent=Event();Layout();view.Focus();Layout();
   Key(key);
   Check(mapper.SelectedEvent==null && model[0x162]==0,"Map-focused deletion still removes the Script Event.");
   Check(viewport.Undo.CanExecute(null),"Map event deletion creates an undoable history record.");
   viewport.Undo.Execute(null);Layout();
   Check(original.SequenceEqual(model.RawData),"Undo restores the event, pointers and all bytes.");
   Check(metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Undo restores event metadata.");
   viewport.Redo.Execute(null);Layout();
   Check(model[0x162]==0,"Redo removes the event again.");
   viewport.Undo.Execute(null);Layout();
}
mapper.SelectedEvent=Event();Layout();view.Focus();Key(PhysicalKey.Escape);
Check(mapper.SelectedEvent==null && model[0x162]==1 && original.SequenceEqual(model.RawData),"Map-focused Escape cancels selection without deleting data.");
view.Focus();Key(PhysicalKey.ArrowRight);
Check(mapPosition!=(mapper.PrimaryMap.LeftEdge,mapper.PrimaryMap.TopEdge),"Map-focused Right arrow still pans.");
Key(PhysicalKey.ArrowLeft);
Check(mapPosition==(mapper.PrimaryMap.LeftEdge,mapper.PrimaryMap.TopEdge),"Map-focused Left arrow reverses the pan.");
window.Editor=null;window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
