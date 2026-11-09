using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Review;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
 .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message) {checks++;if(!ok)throw new Exception(message);}
using var f=new WorkflowFixture();
var launches=new List<(string File,string Args)>();
f.FileSystem.LaunchProcess=(file,args)=>launches.Add((file,args));
var owner=new EditorViewModel(f.FileSystem,InstantDispatch.Instance);owner.Add(f.View);
Application.Current.Resources["FileSystem"]=f.FileSystem;App.UpdateThemeDictionary(Application.Current.Resources,owner.Theme);
MainWindow Window(EditorViewModel model) {
 var w=new MainWindow {Width=1100,Height=760,FileSystem=f.FileSystem,Editor=model};
 w.Resources["FileSystem"]=f.FileSystem;w.Show();model.GotoViewModel.ControlVisible=false;return w;
}
void Pump() {Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
void Click(MainWindow w,Button button) {
 var at=button.TranslatePoint(new Avalonia.Point(button.Bounds.Width/2,button.Bounds.Height/2),w).Value;
 w.MouseMove(at);w.MouseDown(at,MouseButton.Left);w.MouseUp(at,MouseButton.Left);Pump();
}
IEnumerable<NativeMenuItem> Items(NativeMenu menu)=>menu.Items.OfType<NativeMenuItem>().SelectMany(item=>new[]{item}.Concat(item.Menu==null?Enumerable.Empty<NativeMenuItem>():Items(item.Menu)));
var window=Window(owner);Pump();
var notice=window.FindControl<Border>("UpdateNotice");var view=window.FindControl<Button>("ViewUpdateButton");var dismiss=window.FindControl<Button>("DismissUpdateButton");
var menu=Items(NativeMenu.GetMenu(window)).Single(i=>Equals(i.Header,"Update"));
Check(!notice.IsVisible&&!menu.IsEnabled,"no positive update result means no notice or enabled menu");
var input=window.GetVisualDescendants().OfType<TextEditor>().Single(c=>ReferenceEquals(c.DataContext,f.Body.Editor)).EditBox;
input.Focus();input.CaretIndex=4;input.SelectionStart=1;input.SelectionEnd=4;Pump();
var state=f.State();var source=f.Body.Content;var caret=input.CaretIndex;var start=input.SelectionStart;var end=input.SelectionEnd;
owner.IsNewVersionAvailable=true;Pump();
Check(notice.IsEffectivelyVisible&&menu.IsEnabled,"positive compatible update makes notice and menu available");
Check(input.IsFocused&&input.CaretIndex==caret&&input.SelectionStart==start&&input.SelectionEnd==end,"asynchronous notice never takes source focus, caret or selection");
Check(launches.Count==0,"showing a notice never launches or downloads anything");
if(Environment.GetEnvironmentVariable("HMA_UPDATE_NOTICE_PREVIEW") is {Length:>0} preview) {
 using var image=new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)notice.Bounds.Width,(int)notice.Bounds.Height));image.Render(notice);image.Save(preview);
}
Click(window,view);
Check(launches.Count==1&&launches[0]==("open",App.ReleaseUrl),"actual View Update button opens only this fork's releases page");
Check(input.IsFocused&&input.CaretIndex==caret&&input.SelectionStart==start&&input.SelectionEnd==end,"view button preserves editing focus and selection");
Click(window,dismiss);
Check(!notice.IsVisible&&menu.IsEnabled&&owner.IsNewVersionAvailable,"dismissal hides the notice without clearing update availability");
Check(f.State().EqualsModel(f)&&f.Body.Content==source&&f.View.ChangeHistory.IsSaved,"notice and actions leave ROM, metadata, source and history unchanged");
owner.IsNewVersionAvailable=false;owner.IsNewVersionAvailable=true;Pump();
Check(!notice.IsVisible,"dismissal lasts for this window session");
window.Editor=null;window.Close();
window=Window(owner);Pump();notice=window.FindControl<Border>("UpdateNotice");
Check(notice.IsEffectivelyVisible,"a new window can remind about an update that remains available");
f.Edit(WorkflowFixture.Source(1,9),settle:false);var pendingState=f.State();var pendingSource=f.Body.Content;
Click(window,window.FindControl<Button>("DismissUpdateButton"));
Check(f.Tool.HasPendingScriptChanges&&f.Body.Content==pendingSource&&pendingState.EqualsModel(f),"dismissing an update never flushes, discards or mutates a pending script edit");
f.Tool.FlushPendingScriptChanges();f.Dispatch.Flush();f.View.ChangeHistory.ChangeCompleted();
window.Editor=null;window.Close();window=Window(owner);Pump();notice=window.FindControl<Border>("UpdateNotice");
f.Edit("invalid_command",settle:true);var rejected=f.Body.Content;var rejectedState=f.State();
owner.IsNewVersionAvailable=false;owner.IsNewVersionAvailable=true;Pump();
Click(window,window.FindControl<Button>("DismissUpdateButton"));
Check(f.Body.Content==rejected&&f.Tool.HasUncompiledScriptChanges&&f.Body.HasError&&rejectedState.EqualsModel(f),"rejected drafts and diagnostics survive showing/dismissing the notice");
var replacement=new EditorViewModel(f.FileSystem,InstantDispatch.Instance);
window.Editor=replacement;owner.IsNewVersionAvailable=false;owner.IsNewVersionAvailable=true;Pump();
Check(!notice.IsVisible,"detached editor update events cannot display a notice in a replacement editor");
replacement.IsNewVersionAvailable=true;Pump();
Check(notice.IsEffectivelyVisible&&window.FindControl<StartScreen>("WelcomeScreen").IsEffectivelyVisible,"update notice also works on the welcome screen without an open ROM");
window.Editor=null;window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
