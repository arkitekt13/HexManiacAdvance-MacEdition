using System.Reflection;
using System.Text.Json;
using Avalonia;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseWindowingSubsystem(() => {}, "offscreen").UseSkia().WithInterFont().SetupWithoutStarting();
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var observations=new List<object>();
var parse=typeof(App).GetMethod("ParseArgs",BindingFlags.Static|BindingFlags.NonPublic)!;
foreach(var input in new[]{"/tmp/Tutorial Village.gba","/tmp/Tour.gba backup.gba","/tmp/Tour: Village.gba","/tmp/Tutorial Village.gba:71BF40","/tmp/Tutorial Village.gba 71BF40"}) {
 var result=((string path,int address,bool[] options))parse.Invoke(null,new object[]{new[]{input}})!;
 observations.Add(new{test="startup-arguments",input,actual=result.path,address=result.address});
}
var folder=Path.Combine(Path.GetTempPath(),"hma-first-launch-review-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
try {
 var path=Path.Combine(folder,"rejected.gba");File.WriteAllBytes(path,new byte[512]);
 File.WriteAllLines(Path.ChangeExtension(path,".toml"),new[]{"[[NamedAnchors]]","Address = 0x100"});
 var editor=new EditorViewModel(new StubFileSystem{MetadataFor=file=>file==path?File.ReadAllLines(Path.ChangeExtension(path,".toml")):Array.Empty<string>()},InstantDispatch.Instance);
 using var fs=new MacFileSystem(()=>null);
 string failure=null;
 try {typeof(App).GetMethod("TryOpenFile",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{editor,fs,path,-1});}
 catch(TargetInvocationException ex){failure=ex.InnerException!.GetType().Name+": "+ex.InnerException.Message;}
 observations.Add(new{test="rejected-metadata-startup",tabs=editor.Count,error=editor.ErrorMessage,exception=failure});
 if (!OperatingSystem.IsWindows()) {
 var blocked=Path.Combine(folder,"blocked.toml");File.WriteAllText(blocked,"original settings");File.SetUnixFileMode(blocked,UnixFileMode.None);
 try {
  string readFailure=null;
  try {typeof(MacFileSystem).GetMethod("ReadAppMetadata",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(fs,new object[]{Path.Combine(folder,"blocked")});}
  catch(TargetInvocationException ex){readFailure=ex.InnerException!.GetType().Name;}
  observations.Add(new{test="unreadable-settings-read",exception=readFailure});
 } finally {File.SetUnixFileMode(blocked,UnixFileMode.UserRead|UnixFileMode.UserWrite);}
}
 var clean=new EditorViewModel(new StubFileSystem{MetadataFor=_=>Array.Empty<string>()},InstantDispatch.Instance);
 observations.Add(new{test="clean-profile",tabs=clean.Count,recentFiles=clean.RecentFiles.Count,version=AppIdentity.VersionNumber});
 var stale=new EditorViewModel(new StubFileSystem{MetadataFor=_=>new[]{"RecentFiles = [\"/missing/tutorial.gba\"]"},Exists=_=>false},InstantDispatch.Instance);
 observations.Add(new{test="missing-recent-file",tabs=stale.Count,recentFiles=stale.RecentFiles.Count});
} finally {Directory.Delete(folder,true);}
Console.WriteLine(JsonSerializer.Serialize(observations,new JsonSerializerOptions{WriteIndented=true}));
