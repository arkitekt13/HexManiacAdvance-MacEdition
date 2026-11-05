using System.Reflection;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Headless;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException(message);}
void Complete(Task task) {
 var timer=System.Diagnostics.Stopwatch.StartNew();
 while(!task.IsCompleted&&timer.Elapsed.TotalSeconds<10){Dispatcher.UIThread.RunJobs();Thread.Sleep(1);}
 if(!task.IsCompleted)throw new TimeoutException("UI operation did not finish.");
 task.GetAwaiter().GetResult();
}
T Result<T>(Task<T> task){Complete(task);return task.GetAwaiter().GetResult();}
var root=Path.Combine(Path.GetTempPath(),"hma-first-launch-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
var parse=typeof(App).GetMethod("ParseArgs",BindingFlags.Static|BindingFlags.NonPublic)!;
(string path,int address,bool[] options) Parse(params string[] args)=>((string,int,bool[]))parse.Invoke(null,new object[]{args})!;
try {
 foreach(var name in new[]{"Tutorial Village.gba","Tour.gba backup.gba","Tour: Village.gba","Tour:ABCD","Tour.gba ABCD"}) {
  var path=Path.Combine(root,name);File.WriteAllBytes(path,new byte[512]);
  var result=Parse(path);
  Check(result.path==path&&result.address==-1,"Literal existing filenames must survive spaces, colons and hexadecimal-looking suffixes.");
 }
 foreach(var suffix in new[]{":71BF40"," 71BF40"}) {
  var result=Parse(Path.Combine(root,"Tutorial Village.gba")+suffix,"--no-metadata","--dev-menu","-psn_0_123");
  Check(result.path==Path.Combine(root,"Tutorial Village.gba")&&result.address==0x71BF40&&!result.options[0]&&result.options[1],"Legacy address syntax and options must survive.");
 }
 foreach(var path in new[]{"/missing/Tour.gba backup.gba","/missing/Tour: Village.gba"})Check(Parse(path).path==path,"Invalid address suffixes must not truncate missing filenames.");
 var missing=Parse();Check(missing.path==""&&missing.address==-1&&missing.options[0]&&!missing.options[1],"No arguments use the ordinary welcome defaults.");
 var pathToOpen=Path.Combine(root,"rejected.gba");File.WriteAllBytes(pathToOpen,new byte[512]);
 string[] metadata=Array.Empty<string>();int writes=0,pickers=0;
 var fs=new StubFileSystem{MetadataFor=name=>name==pathToOpen?metadata:Array.Empty<string>(),SaveMetadata=(_,_)=>{writes++;return true;},OpenFile=(_,_)=>{pickers++;return null;}};
 var editor=new EditorViewModel(fs,InstantDispatch.Instance);
 foreach(var bad in new[]{new[]{"[[NamedAnchors]]","Address = 0x100"},new[]{"[[NamedAnchors]]","Name = '''bad'''","Address = 0xNOTHEX"},new[]{"[[NamedAnchors]]","Name = '''bad'''","Address = 0xFFFFFFFFFFFFFFFFFFFFFFFF"}}) {
  metadata=bad;
  using var mac=new MacFileSystem(()=>null);
  var task=(Task)typeof(App).GetMethod("TryOpenFile",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{editor,mac,pathToOpen,0x100})!;
  Complete(task);
  Check(editor.Count==0&&!string.IsNullOrEmpty(editor.ErrorMessage),"Rejected startup metadata must remain a visible error without indexing a nonexistent tab.");
  Check(writes==0&&pickers==0,"Rejected metadata must never be overwritten or invoke another picker.");
 }
 metadata=Array.Empty<string>();editor.ClearError.Execute(null);
 var opened=Result(editor.OpenFileAsync(new LoadedFile(pathToOpen,File.ReadAllBytes(pathToOpen))));
 Check(opened!=null&&editor.Count==1&&ReferenceEquals(editor.SelectedTab,opened),"Correcting metadata permits a successful retry.");
 Check(opened!.ChangeHistory.IsSaved&&!opened.Undo.CanExecute(null),"Opening a ROM creates no edit history.");
 metadata=new[]{"[[NamedAnchors]]","Address = 0x100"};var bytes=opened.Model.RawData.ToArray();
 Check(Result(editor.OpenFileAsync(new LoadedFile(pathToOpen,new byte[512])))==null&&editor.Count==1&&ReferenceEquals(editor.SelectedTab,opened),"Failure with another tab open must retain that tab and return no new tab.");
 Check(bytes.SequenceEqual(opened.Model.RawData)&&!opened.Undo.CanExecute(null),"Failed opening preserves existing bytes and history.");
 Check(Result(editor.OpenFileAsync(null))==null&&pickers==0,"An explicit canceled open does not summon a picker.");
 var canceledEditor=new EditorViewModel(fs,InstantDispatch.Instance);canceledEditor.Open.Execute(null);
 Check(canceledEditor.Count==0&&pickers==1,"Ordinary Open still invokes the picker and respects cancellation.");
 metadata=Array.Empty<string>();opened.Close.Execute(fs);
 Check(editor.Count==0,"Closing the last saved ROM returns to the empty-editor state.");
 var heldModel=new HeldModel(new byte[512],editor.Singletons);
 var heldView=new ViewPort("initializing.bin",heldModel,InstantDispatch.Instance,editor.Singletons,fs:fs);
 editor.Add(heldView);heldModel.Hold();
 var pending=editor.OpenFileAsync(new LoadedFile(pathToOpen,new byte[512]));
 Check(!pending.IsCompleted,"Opening another file waits for the selected model initialization.");
 heldView.Close.Execute(fs);heldModel.Release();
 var delayedResult=Result(pending);
 Check(delayedResult!=null&&ReferenceEquals(editor.SelectedTab,delayedResult)&&editor.Count==1,"Closing the original tab while initialization waits cannot redirect import or prevent a new file from opening.");
 Check(heldModel.ReferenceCount==0&&delayedResult!.ChangeHistory.IsSaved,"Delayed opening neither revives the closed model nor dirties the new document.");
 delayedResult.Close.Execute(fs);
 foreach(var transition in new[]{"stay","navigate","close"}) {
  var addressEditor=new EditorViewModel(fs,InstantDispatch.Instance);
  var data=new byte[512];data[0x100]=2;File.WriteAllBytes(pathToOpen,data);
  using var mac=new MacFileSystem(()=>null);
  var addressTask=(Task)typeof(App).GetMethod("TryOpenFile",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{addressEditor,mac,pathToOpen,0x100})!;
  var target=(ViewPort)addressEditor.SelectedTab;
  var other=new ViewPort("other.bin",new PokemonModel(new byte[512],singletons:addressEditor.Singletons),InstantDispatch.Instance,addressEditor.Singletons,fs:fs);
  var originalMode=other.Tools.CodeTool.Mode;
  if(transition=="navigate")addressEditor.Add(other);
  if(transition=="close")target.Close.Execute(fs);
  var timer=System.Diagnostics.Stopwatch.StartNew();
  while(!addressTask.IsCompleted&&timer.Elapsed.TotalSeconds<10){Dispatcher.UIThread.RunJobs();Thread.Sleep(1);}
  Check(addressTask.IsCompleted,"Address-navigation dispatch must finish.");addressTask.GetAwaiter().GetResult();
  if(transition=="stay")Check(target.Tools.CodeTool.Mode==HavenSoft.HexManiac.Core.ViewModels.Tools.CodeMode.Script,"A successful startup address opens its script on the actual new tab.");
  if(transition=="navigate")Check(ReferenceEquals(addressEditor.SelectedTab,other)&&other.Tools.CodeTool.Mode==originalMode,"Queued address navigation cannot target another selected tab.");
  if(transition=="close")Check(addressEditor.Count==0&&target.Model.ReferenceCount==0,"Queued address navigation cannot revive a closed ROM.");
 }
 var stale=new EditorViewModel(new StubFileSystem{MetadataFor=_=>new[]{"RecentFiles = [\"/missing/helper.gba\"]"},Exists=_=>false},InstantDispatch.Instance);
 Check(stale.Count==0&&stale.RecentFiles.Count==0,"Stale recent paths do not open tabs or prevent startup.");
} finally {Directory.Delete(root,true);}
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class HeldModel : PokemonModel {
 readonly TaskCompletionSource gate=new();
 public HeldModel(byte[] data,Singletons services):base(data,singletons:services){}
 public void Hold()=>InitializationWorkload=gate.Task;
 public void Release()=>gate.SetResult();
}
