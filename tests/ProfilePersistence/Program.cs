using System.Diagnostics;
using Avalonia;using Avalonia.Headless;using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;using HavenSoft.HexManiac.Core.Models;using HavenSoft.HexManiac.Core.ViewModels;using HavenSoft.HexManiac.Review;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
int checks=0;void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
var root=Path.Combine(Path.GetTempPath(),"hma-profile-audit-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
try{
 var profile=Path.Combine(root,"isolated profile");var names=new[]{"Tutorial Village.bin","Village [tutorial].bin","Village, v2.bin","Village \"notes\".bin","Haven's POKéMON 日本語.bin"};var paths=names.Select(n=>Path.Combine(root,n)).ToArray();
 var data=Enumerable.Repeat((byte)255,512).ToArray();data[0x100]=2;foreach(var path in paths)File.WriteAllBytes(path,data);
 using(var fs=new MacFileSystem(()=>null,appSupportDirectory:profile)){
  var first=new EditorViewModel(fs,InstantDispatch.Instance);Check(first.Count==0&&first.RecentFiles.Count==0&&!first.PreferScriptMapView&&!first.ShowScriptReferenceGrid,"Fresh isolated settings start at welcome with safe defaults.");
  first.PreferScriptMapView=true;first.ShowScriptReferenceGrid=true;first.InsertAutoActive=false;first.ZoomLevel=2;foreach(var path in paths)first.RecentFiles.Add(path);first.WriteAppLevelMetadata();
 }
 string[] saved;
 using(var fs=new MacFileSystem(()=>null,appSupportDirectory:profile)){
  var next=new EditorViewModel(fs,InstantDispatch.Instance);Check(next.Count==0&&next.PreferScriptMapView&&next.ShowScriptReferenceGrid&&!next.InsertAutoActive&&next.ZoomLevel==2,"A fresh editor instance restores map/grid/assistance/zoom preferences without auto-opening a ROM.");
  Check(paths.SequenceEqual(next.RecentFiles),"Recent paths with spaces, brackets, commas, quotes, apostrophes and Unicode must survive restart exactly.");
  saved=fs.MetadataFor(EditorViewModel.ApplicationName);Check(saved!=null&&fs.AppMetadataWarning==null,"The profile is readable without startup warnings.");
  Application.Current.Resources["FileSystem"]=fs;App.UpdateThemeDictionary(Application.Current.Resources,next.Theme);
  var window=new MainWindow{Editor=next,FileSystem=fs};Check(window.Width==1200&&window.Height==800,"Window geometry uses the existing safe default size.");window.Width=1400;window.Height=900;window.Editor=null;window.Close();
  var secondWindow=new MainWindow{Editor=next,FileSystem=fs};Check(secondWindow.Width==1200&&secondWindow.Height==800,"Window geometry is not currently persisted; reopening uses defaults.");secondWindow.Editor=null;secondWindow.Close();
 }
 // Reopen each exact persisted menu entry through its existing command, using only disposable files.
 foreach(var path in paths){
  var requested=new List<string>();var fs=new StubFileSystem{MetadataFor=name=>name==EditorViewModel.ApplicationName?saved:Array.Empty<string>(),Exists=File.Exists,LoadFile=name=>{requested.Add(name);return new LoadedFile(name,File.ReadAllBytes(name));},SaveMetadata=(_,_)=>true};
  var vm=new EditorViewModel(fs,InstantDispatch.Instance);vm.RecentFileViewModels.Single(entry=>entry.LongName==path).Open.Execute(null);
  var timer=Stopwatch.StartNew();while(vm.Count==0&&timer.Elapsed.TotalSeconds<10){Dispatcher.UIThread.RunJobs();Thread.Sleep(1);}
  Check(vm.SelectedTab is ViewPort opened&&opened.FullFileName==path&&requested.SequenceEqual(new[]{path}),"Recent menu reopens the exact selected disposable file.");
  Check(File.ReadAllBytes(path).SequenceEqual(data),"Reopening cannot edit file bytes.");vm.SelectedTab.Close.Execute(fs);
 }
 // Existing literal/single-quoted and Windows-path profile entries retain their exact meaning.
 foreach(var legacy in new[]{"RecentFiles = [\"/roms/Village [demo], v1.bin\"]","RecentFiles = ['C:\\roms\\test.bin']","RecentFiles = [\"C:\\roms\\test.bin\"]"}){
  var expected=legacy.Contains("/roms/")?"/roms/Village [demo], v1.bin":"C:\\roms\\test.bin";
  var vm=new EditorViewModel(new StubFileSystem{MetadataFor=_=>new[]{legacy},Exists=path=>path==expected},InstantDispatch.Instance);
  Check(vm.RecentFiles.SequenceEqual(new[]{expected}),"Legacy quoted paths remain exact without interpreting Windows backslashes as escapes.");
 }
 using(var f=new WorkflowFixture()){
  var prefs=Path.Combine(profile,"external-emulator.txt");var application=Path.Combine(root,"Applications with spaces","mGBA.app");Directory.CreateDirectory(Path.Combine(application,"Contents"));File.WriteAllText(Path.Combine(application,"Contents","Info.plist"),"synthetic application");
  int picks=0,starts=0;var errors=new List<string>();var bytes=f.Model.RawData.ToArray();
  var launcher=new ExternalEmulatorLauncher(prefs,()=>{picks++;return Task.FromResult(application);},info=>{starts++;Check(info.ArgumentList.SequenceEqual(new[]{"-a",application,f.View.FullFileName}),"Remembered emulator and ROM are separate launch arguments.");return Task.FromResult<string>(null);});
  UiSync.Run(()=>launcher.LaunchAsync(f.View,f.FileSystem,()=>false,errors.Add));
  launcher=new ExternalEmulatorLauncher(prefs,()=>{picks++;return Task.FromResult<string>(null);},_=>{starts++;return Task.FromResult<string>(null);});UiSync.Run(()=>launcher.LaunchAsync(f.View,f.FileSystem,()=>false,errors.Add));
  Check(picks==1&&starts==2&&errors.Count==0&&bytes.SequenceEqual(f.Model.RawData),"A new launcher reuses the saved emulator without touching the ROM or starting a real process.");
 }
 Check(paths.All(path=>File.ReadAllBytes(path).SequenceEqual(data)),"Every disposable opened file remains unchanged at the end of the profile audit.");
}finally{Directory.Delete(root,true);}
Console.WriteLine($"CHECKS_PASSED:{checks}");
