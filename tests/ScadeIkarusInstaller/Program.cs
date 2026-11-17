using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Review;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
 .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
void Reject(Action action, string message) {
 try { action(); } catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException) { Check(true,message); return; }
 throw new Exception("Expected rejection: " + message);
}
string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
byte[] Number(ulong value) {
 var bytes = new List<byte>();
 while (true) { byte low = (byte)(value & 127); value >>= 7; if(value == 0) { bytes.Add((byte)(low|128));return bytes.ToArray(); } bytes.Add(low);value--; }
}
byte[] Join(params byte[][] bytes) => bytes.SelectMany(b=>b).ToArray();
byte[] Literal(byte[] source, byte[] target) {
 var prefix = Join("BPS1"u8.ToArray(),Number((ulong)source.Length),Number((ulong)target.Length),Number(0),Number(((ulong)(target.Length-1)<<2)|1),target);
 var withCrc = Join(prefix,BitConverter.GetBytes((uint)Patcher.CalcCRC32(source)),BitConverter.GetBytes((uint)Patcher.CalcCRC32(target)));
 return Join(withCrc,BitConverter.GetBytes((uint)Patcher.CalcCRC32(withCrc)));
}
IEnumerable<NativeMenuItem> Items(NativeMenu menu) => menu.Items.OfType<NativeMenuItem>().SelectMany(i=>new[]{i}.Concat(i.Menu == null ? Enumerable.Empty<NativeMenuItem>() : Items(i.Menu)));
void Wait(ViewPort view) { view.Model.InitializationWorkload.GetAwaiter().GetResult();view.InitializationWorkload.GetAwaiter().GetResult(); }
var bundled = ScadeIkarusPackage.Load(ScadeIkarusPackage.DefaultDirectory,out var reason);
Check(bundled != null,"bundled package verifies: " + reason);
var supplied = new StoredMetadata(File.ReadAllLines(Path.Combine(ScadeIkarusPackage.DefaultDirectory,"Ikarus-SCADE.toml")));
var scadeMetadata = new StoredMetadata(File.ReadAllLines(Path.Combine(ScadePackage.DefaultDirectory,"SCADE.toml")));
Check(supplied.NamedAnchors.Count == 639 && supplied.Lists.Count == 79,"matching metadata contains full definition set");
Check(scadeMetadata.NamedAnchors.All(a=>supplied.NamedAnchors.Any(b=>a.Name==b.Name && a.Address==b.Address && a.Format==b.Format)),"all original SCADE anchor addresses and formats retained");
Check(File.ReadAllText(Path.Combine(ScadeIkarusPackage.DefaultDirectory,"Ikarus-SCADE.toml")).Replace("\r\n","\n").StartsWith(File.ReadAllText(Path.Combine(ScadePackage.DefaultDirectory,"SCADE.toml")).Replace("\r\n","\n")),"every original metadata line retained before appended relocation definitions");
Check(bundled.Credits.Contains("ShinyMiner") && bundled.Credits.Contains("Ikarus"),"both creators credited");
Check(bundled.DisabledReason(null)!=null,"no project rejected");
using var fixture = new WorkflowFixture();
var source = fixture.Model.RawData.ToArray();var target=source.ToArray();target[0x9000]^=1;
var root=Path.Combine(Path.GetTempPath(),"hma-scade-ikarus-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
try {
 var patch=Literal(source,target);
 var toml=Encoding.UTF8.GetBytes("[General]\nApplicationVersion = '''99.0.0'''\n\n[[NamedAnchors]]\nName = '''compatibility.test'''\nAddress = 0x009000\nFormat = '''[value.]4'''\n");
 var credits=Encoding.UTF8.GetBytes("Test-only credits");
 void WritePackage() {
  File.WriteAllBytes(Path.Combine(root,"Ikarus-SCADE.bps"),patch);
  File.WriteAllBytes(Path.Combine(root,"Ikarus-SCADE.toml"),toml);
  File.WriteAllBytes(Path.Combine(root,"Creator-Credits.txt"),credits);
  File.WriteAllText(Path.Combine(root,"ikarus-scade.json"),JsonSerializer.Serialize(new {
   version="test",sourceScadeVersion=ScadeIkarusPackage.SourceVersion,sourceLength=source.Length,
   sourceSha256=Hash(source),outputSha256=Hash(target),patchSha256=Hash(patch),tomlSha256=Hash(toml),creditsSha256=Hash(credits)
  }));
 }
 Check(ScadeIkarusPackage.Load(root,out reason)==null,"missing package disabled");
 WritePackage();
 Check(ScadeIkarusPackage.Load(root,out reason)==null,"fabricated identity cannot replace shipped reviewed identities");
 var package=ScadeIkarusPackage.LoadTestPackage(root,source,target,out reason);
 Check(package!=null,"synthetic package valid: "+reason);
 Check(package.MatchesBase(source) && package.DisabledReason(fixture.View)==null,"exact valid source eligible");
 Check(!package.MatchesBase(target),"already-patched source rejected");
 Reject(()=>package.Create(target),"already patched input cannot be patched again");
 foreach(var offset in new[]{0,0xAC,0xAF,0xBC,source.Length-1}) {
  var bad=source.ToArray();bad[offset]^=1;
  Check(!package.MatchesBase(bad),"modified input rejected at "+offset);
  Reject(()=>package.Create(bad),"create enforces identity at "+offset);
 }
 Check(!package.MatchesBase(source[..^1]),"wrong length rejected");
 var output=package.Create(source);
 Check(output.SequenceEqual(target) && fixture.Model.RawData.SequenceEqual(source),"patch matches independent output and preserves input");
 var path=Path.Combine(root,"Chosen name.gba");var sidecar=Path.ChangeExtension(path,".toml");
 package.SaveNewCopy(path,output,()=>Check(File.ReadAllBytes(sidecar).SequenceEqual(toml) && !File.Exists(path),"TOML published before ROM"));
 Check(File.ReadAllBytes(path).SequenceEqual(target) && File.ReadAllBytes(sidecar).SequenceEqual(toml),"chosen basename applied to exact ROM/TOML pair");
 Reject(()=>package.SaveNewCopy(path,output),"existing pair protected");
 var orphan=Path.Combine(root,"orphan.gba");File.WriteAllText(Path.ChangeExtension(orphan,".toml"),"keep");
 Reject(()=>package.SaveNewCopy(orphan,output),"existing TOML protected");
 Check(!File.Exists(orphan) && File.ReadAllText(Path.ChangeExtension(orphan,".toml"))=="keep","orphan remains untouched");
 Reject(()=>package.SaveNewCopy(Path.Combine(root,"wrong.bin"),output),"wrong extension rejected");
 Reject(()=>package.SaveNewCopy(Path.Combine(root,"invalid.gba"),source),"unverified output refused");
 foreach(var extension in new[]{".gba",".toml"}) {
  var collision=Path.Combine(root,"directory"+extension+".gba");Directory.CreateDirectory(Path.ChangeExtension(collision,extension));
  Reject(()=>package.SaveNewCopy(collision,output),"directory collision "+extension);
 }
 var failed=Path.Combine(root,"failed.gba");
 Reject(()=>package.SaveNewCopy(failed,output,()=>throw new IOException("Injected disk failure")),"publication failure handled");
 Check(!File.Exists(failed) && !File.Exists(Path.ChangeExtension(failed,".toml")),"failure rolls back own TOML");
 var race=Path.Combine(root,"race.gba");
 Reject(()=>package.SaveNewCopy(race,output,()=>File.WriteAllText(race,"foreign file")),"racing destination protected");
 Check(File.ReadAllText(race)=="foreign file" && !File.Exists(Path.ChangeExtension(race,".toml")),"racing ROM kept and own TOML rolled back");
 Check(!Directory.GetFiles(root,".hma-scade-ikarus-*").Any(),"staging removed after every outcome");
 var fs=new StubFileSystem {
  MetadataFor=name=>File.Exists(Path.ChangeExtension(name,".toml"))?File.ReadAllLines(Path.ChangeExtension(name,".toml")):Array.Empty<string>(),
  Save=file=>{File.WriteAllBytes(file.Name,file.Contents);return true;},
  SaveMetadata=(name,lines)=>{File.WriteAllLines(Path.ChangeExtension(name,".toml"),lines);return true;}
 };
 var editor=new EditorViewModel(fs,InstantDispatch.Instance);
 var view=editor.OpenFileAsync(new LoadedFile(path,File.ReadAllBytes(path))).GetAwaiter().GetResult();Wait(view);
 Check(view.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(),-1,"compatibility.test")==0x9000,"normal loader uses matching custom TOML");
 Check(File.ReadAllBytes(sidecar).SequenceEqual(toml),"initial open does not regenerate TOML");
 view.ChangeHistory.CurrentChange.ChangeData(view.Model,0x9000,(byte)(view.Model[0x9000]^2));view.ChangeHistory.ChangeCompleted();view.Save.Execute(fs);
 editor.CloseAll.Execute(fs);
 view=editor.OpenFileAsync(new LoadedFile(path,File.ReadAllBytes(path))).GetAwaiter().GetResult();Wait(view);
 Check(view.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(),-1,"compatibility.test")==0x9000 && view.Model[0x9000]==(byte)(target[0x9000]^2),"save/reopen retains matching definitions and edit");
 editor.CloseAll.Execute(fs);
 void CopyBundled() {
  foreach(var name in new[]{"ikarus-scade.json","Ikarus-SCADE.bps","Ikarus-SCADE.toml","Creator-Credits.txt"})
   File.Copy(Path.Combine(ScadeIkarusPackage.DefaultDirectory,name),Path.Combine(root,name),true);
 }
 CopyBundled();Check(ScadeIkarusPackage.Load(root,out reason)!=null,"copied production package valid");
 foreach(var name in new[]{"Ikarus-SCADE.bps","Ikarus-SCADE.toml","Creator-Credits.txt"}) {
  CopyBundled();File.AppendAllText(Path.Combine(root,name),"tampered");
  // Production loader rejects both changes and attempts to supply a different reviewed identity.
  Check(ScadeIkarusPackage.Load(root,out reason)==null,"tampered asset disabled: "+name);
 }
 File.WriteAllText(Path.Combine(root,"ikarus-scade.json"),"bad json");
 Check(ScadeIkarusPackage.Load(root,out reason)==null,"malformed manifest disabled");

 // Production menu bindings and confirmation content.
 var menuFs=new StubFileSystem();var menuEditor=new EditorViewModel(menuFs,InstantDispatch.Instance);
 menuEditor.Add(fixture.View);
 Application.Current.Resources["FileSystem"]=menuFs;App.UpdateThemeDictionary(Application.Current.Resources,menuEditor.Theme);
 var window=new MainWindow { FileSystem=menuFs,Editor=menuEditor };window.Show();Dispatcher.UIThread.RunJobs();
 var vanilla=Items(NativeMenu.GetMenu(window)).Single(i=>Equals(i.Header,"Ikarus Visual Upgrade — Vanilla FireRed…"));
 var experimental=Items(NativeMenu.GetMenu(window)).Single(i=>Equals(i.Header,ScadeIkarusPackage.Title+"…"));
 Check(!vanilla.IsEnabled && !experimental.IsEnabled,"undersized fake ROM cannot enable either production route");
 fixture.Model.ExpandData(new ModelDelta(),16*1024*1024-1);
 window.Editor=null;window.Editor=menuEditor;Dispatcher.UIThread.RunJobs();
 Check(vanilla.IsEnabled && !experimental.IsEnabled,"vanilla-sized edited project enables only vanilla route");
 fixture.Model.ExpandData(new ModelDelta(),32*1024*1024-1);
 window.Editor=null;window.Editor=menuEditor;Dispatcher.UIThread.RunJobs();
 Check(!vanilla.IsEnabled && !experimental.IsEnabled,"unsupported expanded base enables neither route");
 var dialog=new IkarusUpgradeDialog(bundled.Credits,true);
 Check(dialog.Title==ScadeIkarusPackage.Title && dialog.FindControl<TextBlock>("CompatibilityText").Text.Contains("unverified"),"experimental warning visible");
 Check(dialog.FindControl<TextBlock>("WorkflowText").Text.Contains("source ROM and TOML stay unchanged"),"new copy workflow explained");
 var cancelTask=dialog.ShowDialog<bool>(window);Dispatcher.UIThread.RunJobs();
 dialog.FindControl<Button>("CancelButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
 Check(!cancelTask.GetAwaiter().GetResult() && fixture.Model.RawData.Length==32*1024*1024,"experimental dialog cancellation leaves source untouched");
 window.Editor=null;window.Close();

 if(args.Length==2 && args[0]=="--real-rom") {
  var clean=File.ReadAllBytes(args[1]);var originalHash=Hash(clean);
  var scade=ScadePackage.Load(ScadePackage.DefaultDirectory,out reason);var fresh=scade.Create(clean);var freshBefore=Hash(fresh);
  Check(bundled.MatchesBase(fresh),"real current SCADE base eligible");
  Check(!bundled.MatchesBase(clean),"real vanilla rejected by SCADE path");
  var corrected=bundled.Create(fresh);Check(Hash(corrected)==ScadeIkarusPackage.OutputHash,"real compatibility output exact reviewed hash");
  Check(!bundled.MatchesBase(corrected),"real patched output rejected as source");
  var realPath=Path.Combine(root,"Real chosen name.gba");bundled.SaveNewCopy(realPath,corrected);
  var realFs=new StubFileSystem {
   MetadataFor=name=>File.Exists(Path.ChangeExtension(name,".toml"))?File.ReadAllLines(Path.ChangeExtension(name,".toml")):Array.Empty<string>(),
   Save=file=>{File.WriteAllBytes(file.Name,file.Contents);return true;},
   SaveMetadata=(name,lines)=>{File.WriteAllLines(Path.ChangeExtension(name,".toml"),lines);return true;}
  };
  var realEditor=new EditorViewModel(realFs,InstantDispatch.Instance);
  var freshView=realEditor.OpenFileAsync(new LoadedFile(Path.Combine(root,"Fresh.gba"),fresh)).GetAwaiter().GetResult();Wait(freshView);
  Check(IkarusUpgrade.DisabledReason(freshView)!=null && bundled.DisabledReason(freshView)==null,"fresh SCADE enables only compatible route");
  Application.Current.Resources["FileSystem"]=realFs;
  var realWindow=new MainWindow {FileSystem=realFs,Editor=realEditor};realWindow.Show();Dispatcher.UIThread.RunJobs();
  Check(Items(NativeMenu.GetMenu(realWindow)).Single(i=>Equals(i.Header,ScadeIkarusPackage.Title+"…")).IsEnabled,"native experimental menu enabled for real fresh base");
  Check(!Items(NativeMenu.GetMenu(realWindow)).Single(i=>Equals(i.Header,"Ikarus Visual Upgrade — Vanilla FireRed…")).IsEnabled,"native vanilla menu disabled for expanded SCADE");
  var realView=realEditor.OpenFileAsync(new LoadedFile(realPath,File.ReadAllBytes(realPath))).GetAwaiter().GetResult();Wait(realView);
  var initial=realView.Model.ExportMetadata(null,realEditor.Singletons.MetadataInfo);
  Check(supplied.NamedAnchors.All(a=>initial.NamedAnchors.Any(b=>a.Name==b.Name && a.Address==b.Address && a.Format==b.Format)),"all 639 definitions survive normal real-ROM opening");
  Check(File.ReadAllBytes(Path.ChangeExtension(realPath,".toml")).SequenceEqual(File.ReadAllBytes(Path.Combine(ScadeIkarusPackage.DefaultDirectory,"Ikarus-SCADE.toml"))),"real initial TOML never regenerated");
  realView.ChangeHistory.CurrentChange.ChangeData(realView.Model,0x700000,(byte)(realView.Model[0x700000]^1));realView.ChangeHistory.ChangeCompleted();realView.Save.Execute(realFs);
  var saved=new StoredMetadata(File.ReadAllLines(Path.ChangeExtension(realPath,".toml")));
  Check(supplied.NamedAnchors.All(a=>saved.NamedAnchors.Any(b=>a.Name==b.Name && a.Address==b.Address && a.Format==b.Format)),"real Save retains all 639 definitions");
  realWindow.Editor=null;realWindow.Close();realEditor.CloseAll.Execute(realFs);
  realView=realEditor.OpenFileAsync(new LoadedFile(realPath,File.ReadAllBytes(realPath))).GetAwaiter().GetResult();Wait(realView);
  Check(supplied.NamedAnchors.All(a=>realView.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(),-1,a.Name)==a.Address),"all real definitions reopen at correct addresses");
  Check(realView.Model[0x700000]==(byte)(corrected[0x700000]^1),"real saved edit persists");
  Check(Hash(fresh)==freshBefore && Hash(File.ReadAllBytes(args[1]))==originalHash,"real source bytes and original file unchanged");
  realEditor.CloseAll.Execute(realFs);
 }
} finally { Directory.Delete(root,true); }
Console.WriteLine($"CHECKS_PASSED:{checks}");
