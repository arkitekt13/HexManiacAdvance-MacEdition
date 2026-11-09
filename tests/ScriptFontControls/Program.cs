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
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Review;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
using var f=new WorkflowFixture();f.Edit(WorkflowFixture.Source(60,1));f.View.Tools.SelectedIndex=3;f.View.ChangeHistory.ClearHistory(false);f.View.ChangeHistory.TagAsSaved();
Application.Current.Resources["FileSystem"]=f.FileSystem;var editor=new EditorViewModel(f.FileSystem,InstantDispatch.Instance);editor.Add(f.View);App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
var window=new MainWindow{Width=1200,Height=800,FileSystem=f.FileSystem,Editor=editor};window.Resources["FileSystem"]=f.FileSystem;window.Show();editor.GotoViewModel.ControlVisible=false;
void Pump(){for(int i=0;i<3;i++){Dispatcher.UIThread.RunJobs();window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}}
int checks=0;void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
Pump();var tab=window.GetVisualDescendants().OfType<TabView>().Single(t=>ReferenceEquals(t.DataContext,f.View));var outer=tab.FindControl<ScrollViewer>("CodeToolMultiTextBox");var input=tab.GetVisualDescendants().OfType<TextEditor>().Single(t=>ReferenceEquals(t.DataContext,f.Body.Editor)).EditBox;
var minus=tab.FindControl<Button>("CodeFontDecrease");var plus=tab.FindControl<Button>("CodeFontIncrease");
Check(minus.IsEffectivelyVisible&&plus.IsEffectivelyVisible,"The explicit font controls must be available in the code header.");
input.Focus();input.SelectionStart=4;input.SelectionEnd=9;Pump();var selection=input.SelectedText;var start=input.SelectionStart;var end=input.SelectionEnd;var source=f.Body.Content;var before=f.State();var caret=input.CaretIndex;
void Click(Button button){var at=button.TranslatePoint(new Avalonia.Point(button.Bounds.Width/2,button.Bounds.Height/2),window).Value;window.MouseMove(at);window.MouseDown(at,MouseButton.Left);window.MouseUp(at,MouseButton.Left);Pump();}
var font=f.Tool.FontSize;Click(plus);Check(f.Tool.FontSize==font+1,"Clicking plus increases code text one step.");Click(minus);Check(f.Tool.FontSize==font,"Clicking minus decreases code text one step.");
Check(input.IsFocused&&input.SelectedText==selection&&input.SelectionStart==start&&input.SelectionEnd==end&&input.CaretIndex==caret,"Font button clicks preserve source focus, caret and selection.");
var point=input.TranslatePoint(new Avalonia.Point(100,100),window).Value;var command=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
foreach(var delta in new[]{new Vector(0,1),new Vector(0,-1),new Vector(0,.1),new Vector(1,0)}){
 window.MouseWheel(point,delta,command);Pump();Check(f.Tool.FontSize==font,"Command-modified trackpad/wheel events must never resize source text.");
}
outer.Offset=default;Pump();var offset=outer.Offset;window.MouseWheel(point,new Vector(0,-2),RawInputModifiers.None);Pump();Check(outer.Offset.Y>offset.Y,"Ordinary wheel/trackpad scrolling must still scroll the source.");
for(int i=0;i<25;i++)Click(plus);Check(f.Tool.FontSize==20,"Explicit zoom retains the existing upper text-size limit.");for(int i=0;i<25;i++)Click(minus);Check(f.Tool.FontSize==8,"Explicit zoom retains the existing lower text-size limit.");
Check(f.Body.Content==source&&before.EqualsModel(f)&&f.View.ChangeHistory.IsSaved&&!f.View.ChangeHistory.Undo.CanExecute(null)&&!f.Tool.HasPendingScriptChanges,"Font changes and scrolling cannot edit/compile source or change ROM, metadata or history.");
if(args.Length==1){f.Tool.FontSize=12;Pump();var header=(Control)tab.FindControl<AngleComboBox>("CodeModeSelector").Parent;using var image=new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)Math.Ceiling(header.Bounds.Width+8),(int)Math.Ceiling(header.Bounds.Height+8)));image.Render(header);image.Save(args[0]);}
window.Editor=null;window.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
