using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;
using HavenSoft.HexManiac.Review;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
void Pump(Window window){Dispatcher.UIThread.RunJobs();window.Measure(new Size(1200,800));window.Arrange(new Rect(0,0,1200,800));AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();}
void Collect(Window window){var time=System.Diagnostics.Stopwatch.StartNew();do{Pump(window);GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();Thread.Sleep(20);}while(time.ElapsedMilliseconds<500);}
int Observers(object source,object target){
 int count=0;
 for(var type=source.GetType();type!=null;type=type.BaseType)
 foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly))
  if(field.GetValue(source) is MulticastDelegate callback) count+=callback.GetInvocationList().Count(h=>ReferenceEquals(h.Target,target));
 return count;
}
ViewPort Source(){
 var f=new SyntheticMapFixture(12,10);var view=f.ViewPort;
 f.Model[0x18000]=2;f.Model.ObserveRunWritten(new NoDataChangeDeltaModel(),new XSERun(0x18000));
 view.Goto.Execute(0x18000);view.Tools.CodeTool.Mode=CodeMode.Script;view.Tools.SelectedIndex=3;view.ScriptReferenceMapID=0;
 view.ChangeHistory.ClearHistory(false);view.ChangeHistory.TagAsSaved();return view;
}
TabView Active(MainWindow window,EditorViewModel editor)=>window.GetVisualDescendants().OfType<TabView>().First(t=>ReferenceEquals(t.DataContext,editor.SelectedTab));
void Ready(MainWindow window,EditorViewModel editor,bool map){
 var watch=System.Diagnostics.Stopwatch.StartNew();
 while(true){Pump(window);var tab=Active(window,editor);if(!map||tab.FindControl<ScriptMapReferenceView>("ScriptMapPane").GetVisualDescendants().OfType<Image>().FirstOrDefault()?.Source!=null)return;
  if(watch.Elapsed.TotalSeconds>20)throw new TimeoutException("Map did not become ready.");Thread.Sleep(1);}
}
[MethodImpl(MethodImplOptions.NoInlining)]
WeakReference[] Switch(MainWindow window,EditorViewModel editor){
 var old=Active(window,editor);
 var refs=new[]{new WeakReference(old),new WeakReference(old.FindControl<HexContent>("HexContent")),
  new WeakReference(old.GetVisualDescendants().OfType<TextEditor>().First()),new WeakReference(old.GetVisualDescendants().OfType<HorizontalSlantedTextControl>().Single())};
 editor.SelectedIndex=1-editor.SelectedIndex;return refs;
}
foreach(bool map in new[]{false,true}){
 var a=Source();var b=Source();var hashes=new[]{SHA256.HashData(a.Model.RawData),SHA256.HashData(b.Model.RawData)};
 var fs=new StubFileSystem{MetadataFor=_=>Array.Empty<string>()};Application.Current.Resources["FileSystem"]=fs;
 var editor=new EditorViewModel(fs,InstantDispatch.Instance){PreferScriptMapView=map};App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
 editor.Add(a);editor.Add(b);editor.GotoViewModel.ControlVisible=false;
 var window=new MainWindow{Width=1200,Height=800,Editor=editor,FileSystem=fs};window.Resources["FileSystem"]=fs;window.Show();Ready(window,editor,map);
 var watched=new List<WeakReference>();
 for(int i=0;i<6;i++){
  watched.AddRange(Switch(window,editor));Ready(window,editor,map);
  Active(window,editor).GetVisualDescendants().OfType<TextEditor>().First(t=>t.IsEffectivelyVisible).EditBox.Focus();
  Collect(window);
  if(watched[0].Target is TabView retained) {Console.WriteLine("RETAINING_PATH_B: "+RetentionTrace.Find(b,retained));Console.WriteLine("RETAINING_PATH_WINDOW: "+RetentionTrace.Find(window,retained));}
  Check(watched.All(r=>!r.IsAlive),$"Detached tab/hex/text/header controls must collect while their documents remain open (Map={map}, cycle={i}, alive={watched.Count(r=>r.IsAlive)}).");
 }
 Check(hashes[0].SequenceEqual(SHA256.HashData(a.Model.RawData))&&hashes[1].SequenceEqual(SHA256.HashData(b.Model.RawData)),"Tab lifetimes preserve every source byte.");
 Check(a.ChangeHistory.IsSaved&&b.ChangeHistory.IsSaved&&!a.Undo.CanExecute(null)&&!b.Undo.CanExecute(null),"Tab lifetimes preserve saved state and empty model history.");
 window.Editor=null;window.Close();
}

// Reusing a detached view must reconnect once and retain the exact pending draft.
using(var f=new WorkflowFixture()){
 var fs=f.FileSystem;Application.Current.Resources["FileSystem"]=fs;
 var tab=new TabView{DataContext=f.View};var window=new Window{Width=1200,Height=800,Content=tab};window.Show();Pump(window);
 var body=f.Body;
 var hex=tab.FindControl<HexContent>("HexContent");var sourceObservers=Observers(f.View,tab);
 var before=f.State();f.Edit(WorkflowFixture.Source(1,2),settle:false);var callbacks=f.Dispatch.CapturePendingCallbacks();
 window.Content=null;Pump(window);
 Check(Observers(f.View,tab)==0&&Observers(f.View,hex)==0,"Detached views remove general source/hex observers.");
 Check(f.Tool.HasPendingScriptChanges&&callbacks.SequenceEqual(f.Dispatch.CapturePendingCallbacks())&&before.EqualsModel(f),"Detach must not settle or replace pending source work.");
 for(int i=0;i<3;i++){
  window.Content=tab;Pump(window);
  Check(Observers(f.View,tab)==sourceObservers,"Reattach restores source listeners exactly once.");
  body.Editor.PushCaretUpdate(3);body.Editor.FocusKeyboard();Pump(window);
  var box=tab.GetVisualDescendants().OfType<TextEditor>().Single(t=>ReferenceEquals(t.DataContext,body.Editor)).EditBox;
  Check(box.IsFocused&&box.CaretIndex==3,"Reattached source retains keyboard-focus/caret commands.");
  window.Content=null;Pump(window);
 }
 window.Content=tab;Pump(window);f.Dispatch.Flush();f.View.ChangeHistory.ChangeCompleted();
 Check(!body.HasError&&f.Model[WorkflowFixture.Root+4]==2,"Pending source still settles successfully after reattachment.");
 var compiled=f.State();f.View.Undo.Execute(null);f.Dispatch.Flush();Check(before.EqualsModel(f),"Undo after reattachment restores original bytes/metadata.");
 f.View.Redo.Execute(null);f.Dispatch.Flush();Check(compiled.EqualsModel(f),"Redo after reattachment restores the compiled edit.");
 f.Edit("not_a_command\nend");var rejected=body.Content;var diagnostic=body.Editor.ErrorLocations.ToArray();
 window.Content=null;Pump(window);window.Content=tab;Pump(window);
 Check(body.Content==rejected&&body.HasError&&diagnostic.SequenceEqual(body.Editor.ErrorLocations),"Rejected draft and diagnostics survive detach/reattach.");
 window.Content=null;window.Close();
}

// Hidden panels may be constructed without ever joining the visual tree.
var hiddenDocument=Source();
var imageModel=hiddenDocument.Tools.SpriteTool;
var comboModel=hiddenDocument.Tools.TableTool.CurrentElementSelector;
var pixel=new PixelImage{DataContext=imageModel};
var palette=new PaletteControl{DataContext=imageModel.Colors};
var combo=new AngleComboBox{DataContext=comboModel};
Check(Observers(imageModel,pixel)==0&&Observers(imageModel.Colors.Elements,palette)==0&&Observers(comboModel,combo)==0,"Never-attached hidden panels must not subscribe to document models.");
var hiddenHost=new Window{Content=new StackPanel{Children={pixel,palette,combo}},Width=1200,Height=800};hiddenHost.Show();Pump(hiddenHost);
for(int i=0;i<2;i++){
 Check(Observers(imageModel,pixel)==1&&Observers(imageModel.Colors.Elements,palette)==1&&Observers(comboModel,combo)==1,"Visible preview/filter controls restore their observers once.");
 var content=hiddenHost.Content;hiddenHost.Content=null;Pump(hiddenHost);
 Check(Observers(imageModel,pixel)==0&&Observers(imageModel.Colors.Elements,palette)==0&&Observers(comboModel,combo)==0,"Detached preview/filter controls remove their observers.");
 hiddenHost.Content=content;Pump(hiddenHost);
}
hiddenHost.Content=null;hiddenHost.Close();

// A reused editor preserves native selection unless its model requested another caret.
var selectionModel=new TextEditorViewModel{Content="lock\nend\n"};
var selectionEditor=new TextEditor{DataContext=selectionModel};
var selectionHost=new Window{Content=selectionEditor,Width=1200,Height=800};selectionHost.Show();Pump(selectionHost);
selectionEditor.EditBox.SelectionStart=1;selectionEditor.EditBox.SelectionEnd=4;
var selectionStart=selectionEditor.EditBox.SelectionStart;var selectionEnd=selectionEditor.EditBox.SelectionEnd;
selectionHost.Content=null;Pump(selectionHost);selectionHost.Content=selectionEditor;Pump(selectionHost);
Check(selectionEditor.EditBox.SelectionStart==selectionStart&&selectionEditor.EditBox.SelectionEnd==selectionEnd,"Unchanged model preserves native selection on editor reattachment.");
selectionHost.Content=null;Pump(selectionHost);selectionModel.PushCaretUpdate(6);
selectionHost.Content=selectionEditor;Pump(selectionHost);
Check(selectionEditor.EditBox.CaretIndex==6,"Reattachment honors a model caret changed while detached.");
selectionHost.Content=null;selectionHost.Close();

// Header item ownership must handle remove/reset/replacement and detach/reattach.
var first=new ColumnHeaderRow("first",1);var second=new ColumnHeaderRow("second",1);
var rows=new ObservableCollection<ColumnHeaderRow>{first};
var headerControl=new HorizontalSlantedTextControl{HeaderRows=rows,FontSize=16};
var host=new Window{Content=headerControl,Width=1200,Height=800};host.Show();Pump(host);
Check(Observers(first.ColumnHeaders[0],headerControl)==1,"An attached header listens to each item once.");
rows.Remove(first);Pump(host);Check(Observers(first.ColumnHeaders[0],headerControl)==0,"Removed header items release their observers.");
rows.Add(second);rows.Clear();Pump(host);Check(Observers(second.ColumnHeaders[0],headerControl)==0,"Reset releases removed item observers.");
rows.Add(first);headerControl.HeaderRows=new ObservableCollection<ColumnHeaderRow>{second};Pump(host);
Check(Observers(first.ColumnHeaders[0],headerControl)==0&&Observers(second.ColumnHeaders[0],headerControl)==1,"Replacing header rows switches item subscriptions.");
host.Content=null;Pump(host);Check(Observers(second.ColumnHeaders[0],headerControl)==0,"Detached headers release item observers.");
host.Content=headerControl;Pump(host);Check(Observers(second.ColumnHeaders[0],headerControl)==1,"Reattached headers rebuild their subscriptions once.");
host.Content=null;host.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
