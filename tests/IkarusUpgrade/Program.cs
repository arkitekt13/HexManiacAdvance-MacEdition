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
using Upgrade = HavenSoft.HexManiac.AvaloniaUI.Project.IkarusUpgrade;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
 .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
void Reject(Action action, string message) {
 try { action(); } catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException) { Check(true,message); return; }
 throw new Exception("Expected rejection: " + message);
}
var upgrade = Upgrade.Load(Upgrade.DefaultDirectory, out var reason);
Check(upgrade != null && reason == null, "bundled exact patch and credits load");
Check(upgrade.Credits.Contains("Ikarus") && upgrade.Credits.Contains("Liberty"), "creator credits preserved");
Check(Upgrade.Load(Path.GetTempPath(), out _) == null, "missing assets disabled");
Check(Upgrade.DisabledReason(null) != null, "no ROM blocked");
using (var f = new WorkflowFixture()) {
 Check(Upgrade.DisabledReason(f.View) != null, "undersized fabricated file blocked");
 f.Model.ExpandData(new NoDataChangeDeltaModel(), 16*1024*1024-1);
 Check(Upgrade.DisabledReason(f.View) == null, "edited English FireRed accepted without clean CRC");
 foreach (string code in new[] { "BPEE", "BPGE", "BPRJ", "ABCD" }) {
  System.Text.Encoding.ASCII.GetBytes(code).CopyTo(f.Model.RawData, 0xAC);
  Check(Upgrade.DisabledReason(f.View) != null, "non supported game/language blocked: " + code);
 }
 System.Text.Encoding.ASCII.GetBytes("BPRE").CopyTo(f.Model.RawData, 0xAC);
 f.Model[0xBC] = 1;Check(Upgrade.DisabledReason(f.View) == null, "revision 1 allowed with untested warning");
 f.Model[0xBC] = 0;
 f.View.ChangeHistory.ClearHistory(false); f.View.ChangeHistory.TagAsSaved();
 var original = f.Model.RawData.ToArray(); var originalDisk = File.ReadAllBytes(f.FileName); var originalToml = File.ReadAllBytes(f.MetadataFile);
 f.View.ChangeHistory.CurrentChange.ChangeData(f.Model, 0x8000, (byte)0x42);f.View.ChangeHistory.ChangeCompleted();
 var before = f.Model.RawData.ToArray(); var namedBefore = f.Model.ExportMetadata(null, f.Services.MetadataInfo).NamedAnchors;
 var prepared = upgrade.Prepare(f.View);
 Check(prepared.MatchesCurrent() && before.SequenceEqual(f.Model.RawData), "prepare leaves live bytes intact");
 Check(!f.View.ChangeHistory.IsSaved, "prepare preserves prior unsaved state");
 var path = Path.Combine(f.Folder, "named backup.gba");
 Reject(() => upgrade.Apply(prepared, null), "missing backup stops live patch");
 foreach (string invalid in new[] { f.FileName, f.MetadataFile, Path.Combine(f.Folder,"wrong.bin"), Path.Combine(f.Folder,"missing","backup.gba") })
  Reject(() => upgrade.WriteBackup(prepared, invalid), "backup path protection: " + Path.GetFileName(invalid));
 var collision = Path.Combine(f.Folder,"sidecar.gba");File.WriteAllText(Path.ChangeExtension(collision,".toml"),"keep me");
 Reject(() => upgrade.WriteBackup(prepared,collision), "existing metadata never overwritten");
 Check(File.ReadAllText(Path.ChangeExtension(collision,".toml")) == "keep me", "existing sidecar preserved");
 foreach (bool sidecarDirectory in new[] { false,true }) {
  var directoryPath=Path.Combine(f.Folder,sidecarDirectory?"dir-sidecar.gba":"dir-rom.gba");
  Directory.CreateDirectory(sidecarDirectory?Path.ChangeExtension(directoryPath,".toml"):directoryPath);
  Reject(()=>upgrade.WriteBackup(prepared,directoryPath),"backup directory collision refused");
 }
 var race = Path.Combine(f.Folder,"race.gba");
 Reject(() => upgrade.WriteBackup(prepared,race,()=>File.WriteAllText(race,"foreign ROM")), "publication collision aborts");
 Check(File.ReadAllText(race)=="foreign ROM" && !File.Exists(Path.ChangeExtension(race,".toml")), "collision keeps foreign ROM and removes owned sidecar");
 Check(!Directory.GetFiles(f.Folder,".hma-ikarus-*").Any(), "staging files cleaned");
 var backup = upgrade.WriteBackup(prepared,path);
 Check(backup.Verify() && File.ReadAllBytes(path).SequenceEqual(before), "backup captures current unsaved ROM bytes");
 var backupMetadata = new StoredMetadata(File.ReadAllLines(Path.ChangeExtension(path,".toml")));
 Check(namedBefore.All(a=>backupMetadata.NamedAnchors.Any(b=>a.Name==b.Name && a.Address==b.Address && a.Format==b.Format)), "backup includes current named definitions");
 var exportID=f.Model.NextExportID;
 f.Model.NextExportID++;
 Reject(()=>upgrade.Apply(prepared,backup),"metadata changes during backup prevent patch");
 f.Model.NextExportID=exportID;
 var different = upgrade.Prepare(f.View);
 Reject(()=>upgrade.Apply(different,backup), "unmatched backup token rejected");
 File.AppendAllText(Path.ChangeExtension(path,".toml"),"tamper");
 Reject(()=>upgrade.Apply(prepared,backup), "tampered backup prevents patch");
 File.WriteAllLines(Path.ChangeExtension(path,".toml"),prepared.Metadata);
 Check(backup.Verify(), "restored backup verifies");
 File.Move(path,path+".hold");Reject(()=>upgrade.Apply(prepared,backup),"missing backup prevents patch");File.Move(path+".hold",path);
 f.Model[0x8001] ^= 1; Reject(()=>upgrade.Apply(prepared,backup), "live changes during backup prevent patch"); f.Model[0x8001] ^= 1;
 upgrade.Apply(prepared,backup);
 var after = f.Model.RawData.ToArray();
 Check(!after.SequenceEqual(before) && !f.View.ChangeHistory.IsSaved, "upgrade changes live ROM and leaves unsaved");
 Check(f.Model.ExportMetadata(null,f.Services.MetadataInfo).NamedAnchors.All(a=>namedBefore.Any(b=>a.Name==b.Name && a.Address==b.Address && a.Format==b.Format)), "live named definitions survive reload");
 Check(File.ReadAllBytes(f.FileName).SequenceEqual(originalDisk) && File.ReadAllBytes(f.MetadataFile).SequenceEqual(originalToml), "original ROM and TOML never implicitly saved");
 f.View.ChangeHistory.Undo.Execute(null);
 Check(f.Model.RawData.SequenceEqual(before) && !f.View.ChangeHistory.IsSaved, "one Undo restores exact pre-patch bytes and unsaved state");
 f.View.ChangeHistory.Undo.Execute(null);
 Check(f.Model.RawData.SequenceEqual(original) && f.View.ChangeHistory.IsSaved, "earlier edit Undo still works and reaches saved state");
 f.View.ChangeHistory.Redo.Execute(null);f.View.ChangeHistory.Redo.Execute(null);
 Check(f.Model.RawData.SequenceEqual(after), "Redo reapplies prior edit then exact upgrade");
 f.View.ChangeHistory.CurrentChange.ChangeData(f.Model,0x8002,(byte)7);f.View.ChangeHistory.ChangeCompleted();
 f.View.ChangeHistory.Undo.Execute(null);Check(f.Model.RawData.SequenceEqual(after), "later edit has independent Undo");
 Reject(()=>upgrade.Prepare(f.View), "already applied upgrade refused");
 f.View.ChangeHistory.Undo.Execute(null);f.View.Refresh();f.Dispatch.Flush();
 f.Edit("not_a_real_command",settle:false);
 Check(!f.Tool.FlushPendingScriptChanges(), "fixture has a rejected script draft");
 var rejected = f.Body.Editor.Content;
 Reject(()=>upgrade.Prepare(f.View), "rejected draft prevents preparation");
 Check(f.Body.Editor.Content==rejected && f.Tool.HasUncompiledScriptChanges, "rejected source and diagnostics retained");
}
// A derived formatting refresh must neither create Undo steps nor clear Redo/saved state.
using (var f=new WorkflowFixture()) {
 var history=f.View.ChangeHistory;
 history.CurrentChange.ChangeData(f.Model,0x8000,(byte)4);history.ChangeCompleted();
 history.Undo.Execute(null);
 Check(history.IsSaved && history.Redo.CanExecute(null),"transient refresh starts at saved state with Redo available");
 var outer=new NoDataChangeDeltaModel();var inner=new NoDataChangeDeltaModel();
 using(history.UseTransientChange(outer)) {
  Check(ReferenceEquals(history.CurrentChange,outer),"formatting receives isolated token");
  f.Model.ObserveAnchorWritten(history.CurrentChange,"ikarus.transient",new HavenSoft.HexManiac.Core.Models.Runs.NoInfoRun(0x8008));
  history.ChangeCompleted();
  using(history.UseTransientChange(inner)) Check(ReferenceEquals(history.CurrentChange,inner),"nested transient scope is isolated");
  Check(ReferenceEquals(history.CurrentChange,outer),"nested scope restores prior formatting token");
  Reject(()=>history.CurrentChange.ChangeData(f.Model,0x8000,(byte)9),"derived refresh cannot write ROM bytes");
 }
 Check(history.IsSaved && history.Redo.CanExecute(null),"derived formatting retains saved state and Redo");
 history.Redo.Execute(null);Check(f.Model[0x8000]==4,"Redo remains functional after derived formatting");
}
// Related script tabs must settle together: another tab's rejected source cannot be hidden by selection.
using (var f = new WorkflowFixture()) {
 f.Model.ExpandData(new NoDataChangeDeltaModel(),16*1024*1024-1);
 var owner=new EditorViewModel(f.FileSystem,f.Dispatch);owner.Add(f.View);
 var sibling=new ViewPort(f.FileName,f.Model,f.Dispatch,f.Services,changeHistory:f.View.ChangeHistory,fs:f.FileSystem);
 owner.Add(sibling);sibling.InitializationWorkload.GetAwaiter().GetResult();
 sibling.Goto.Execute(WorkflowFixture.Other);sibling.Tools.CodeTool.Mode=HavenSoft.HexManiac.Core.ViewModels.Tools.CodeMode.Script;
 sibling.Tools.CodeTool.IsSelected=true;f.Dispatch.Flush();
 Check(MainWindow.IkarusRelatedViews(owner,f.View).Length==2,"all related data tabs found");
 var body=sibling.Tools.CodeTool.Contents[0];body.Editor.Content="invalid_command";
 var bytes=f.Model.RawData.ToArray();
 Check(!MainWindow.FlushIkarusDrafts(owner,f.View),"rejected sibling draft blocks utility");
 Check(body.Editor.Content=="invalid_command" && f.Model.RawData.SequenceEqual(bytes),"sibling rejection retains source and bytes");
 sibling.Undo.Execute(null);
 Check(MainWindow.FlushIkarusDrafts(owner,f.View),"discarded rejected sibling permits retry");
 f.Edit(WorkflowFixture.Source(1,3),settle:false);
 Check(f.Tool.HasPendingScriptChanges && MainWindow.FlushIkarusDrafts(owner,f.View) && !f.Tool.HasUncompiledScriptChanges,"valid pending source settles before backup");
 var corrected=upgrade.Prepare(f.View);
 Check(corrected.MatchesCurrent(),"corrected project prepares normally");
}
// A formatting failure after byte writes must restore bytes, metadata and edited-byte tracking.
var rollbackServices = new Singletons(InstantDispatch.Instance);
var failing = new FailingReloadModel(Enumerable.Repeat((byte)0xFF,0x10000).ToArray(),rollbackServices);
failing[0x6000]=5;
var rollbackBytes=failing.RawData.ToArray();var tracked=failing.ChangeCount;
var candidate=rollbackBytes.ToArray();candidate[0x6001]=9;failing.FailNextLoad=true;
Reject(()=>ReloadingModelDelta.Apply(failing,candidate,rollbackServices.MetadataInfo),"formatting failure rolls back");
Check(failing.RawData.SequenceEqual(rollbackBytes) && failing.ChangeCount==tracked && failing.HasChanged(0x6000) && !failing.HasChanged(0x6001),"failure restores bytes and exact edited-byte tracking");
var success=ReloadingModelDelta.Apply(failing,candidate,rollbackServices.MetadataInfo);
var redo=success.Revert(failing);
Check(failing.RawData.SequenceEqual(rollbackBytes) && failing.ChangeCount==tracked,"successful Undo restores edited-byte tracking");
redo.Revert(failing);Check(failing.RawData.SequenceEqual(candidate),"successful Redo restores candidate");
// Actual modal controls: cancellation is inert and continue returns consent.
var menuEditor=new EditorViewModel(new StubFileSystem(),InstantDispatch.Instance);
Application.Current.Resources["FileSystem"]=new StubFileSystem();
App.UpdateThemeDictionary(Application.Current.Resources,menuEditor.Theme);
var menuWindow=new MainWindow { Width=1000,Height=700, FileSystem=new StubFileSystem(),Editor=menuEditor };
menuWindow.Show();Dispatcher.UIThread.RunJobs();
IEnumerable<NativeMenuItem> Items(NativeMenu menu)=>menu.Items.OfType<NativeMenuItem>().SelectMany(item=>new[]{item}.Concat(item.Menu==null?Enumerable.Empty<NativeMenuItem>():Items(item.Menu)));
var menuItem=Items(NativeMenu.GetMenu(menuWindow)).Single(item=>Equals(item.Header,"Ikarus Visual Upgrade…"));
Check(menuItem.IsVisible && !menuItem.IsEnabled && menuItem.ToolTip.Contains("FireRed"),"native utility menu is visible and disabled without FireRed");
menuWindow.Editor=null;menuWindow.Close();
var host = new Window { Width=800,Height=700 };host.Show();
foreach (bool accept in new[] { false,true }) {
 var dialog = new IkarusUpgradeDialog(upgrade.Credits);
 var task = dialog.ShowDialog<bool>(host);Dispatcher.UIThread.RunJobs();
 Check(dialog.FindControl<TextBlock>("RecommendationText").Text.Contains("Edited projects are allowed"), "dialog recommends without enforcing clean base");
 Check(dialog.FindControl<TextBlock>("CompatibilityText").Text.Contains("further testing"), "dialog displays compatibility limits");
 Check(dialog.FindControl<TextBlock>("ChangesText").Text.Contains("map layouts"), "dialog discloses non-graphics changes");
 if (accept && Environment.GetEnvironmentVariable("HMA_IKARUS_DIALOG_PREVIEW") is { Length: > 0 } preview) {
  using var bitmap=new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize((int)dialog.Bounds.Width,(int)dialog.Bounds.Height));
  bitmap.Render(dialog);bitmap.Save(preview);
 }
 dialog.FindControl<Button>(accept?"ContinueButton":"CancelButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
 Check(task.GetAwaiter().GetResult()==accept,"actual dialog result matches clicked button");
}
host.Close();
if (args.Length==1) checks += RealVerification.Run(args[0],upgrade);
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class FailingReloadModel : PokemonModel {
 public bool FailNextLoad {get;set;}
 public FailingReloadModel(byte[] data,Singletons services):base(data,singletons:services) {InitializationWorkload.GetAwaiter().GetResult();}
 public override void Load(byte[] data,StoredMetadata metadata) {
  if(FailNextLoad) {FailNextLoad=false;throw new InvalidOperationException("Injected formatting failure");}
  base.Load(data,metadata);
 }
}
