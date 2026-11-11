using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Review;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
 .UseHeadless(new AvaloniaHeadlessPlatformOptions {UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
using var first=new WorkflowFixture();using var second=new WorkflowFixture();
void Text(WorkflowFixture f,int address,string name,string text) {
 var data=f.Model.TextConverter.Convert(text,out var invalid);if(invalid)throw new Exception("Invalid fabricated text");
 new NoTrackChange().ChangeData(f.Model,address,data);
 f.Model.ObserveAnchorWritten(new NoDataChangeDeltaModel(),name,new PCSRun(f.Model,address,data.Count));
}
Text(first,0x7000,"find.poliwhirl","POLIWHIRL");Text(first,0x7020,"find.magneton.one","MAGNETON");Text(first,0x7040,"find.magneton.two","MAGNETON");
Text(second,0x7000,"find.electabuzz","ELECTABUZZ");
new NoTrackChange().ChangeData(first.Model,0x7100,new byte[]{0xCA,0xFE,0xF0,0x0D,0x17,0x23});
first.View.Tools.SelectedIndex=0;second.View.Tools.SelectedIndex=0;
first.Dispatch.Flush();second.Dispatch.Flush();
var beforeFirst=first.State();var beforeSecond=second.State();
var diskFirst=File.ReadAllBytes(first.FileName);var diskSecond=File.ReadAllBytes(second.FileName);
var owner=new EditorViewModel(first.FileSystem,InstantDispatch.Instance);owner.Add(first.View);owner.Add(second.View);owner.SelectedIndex=0;owner.SearchAllFiles=false;
Application.Current.Resources["FileSystem"]=first.FileSystem;App.UpdateThemeDictionary(Application.Current.Resources,owner.Theme);
var window=new MainWindow{Width=1200,Height=800,FileSystem=first.FileSystem,Editor=owner};window.Resources["FileSystem"]=first.FileSystem;window.Show();owner.GotoViewModel.ControlVisible=false;
void Pump(){Dispatcher.UIThread.RunJobs();window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();}
void Key(PhysicalKey key,RawInputModifiers modifiers=RawInputModifiers.None){window.KeyPressQwerty(key,modifiers);window.KeyReleaseQwerty(key,modifiers);Pump();}
var modifier=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
var box=window.FindControl<TextBox>("FindBox");Pump();
void Query(string text){Key(PhysicalKey.F,modifier);Check(box.IsFocused&&owner.FindControlVisible,"Command-F focuses the Find field");box.SelectAll();window.KeyTextInput(text);Pump();}
int Address(ViewPort v)=>v.ConvertViewPointToAddress(v.SelectionStart);
var originalAddress=Address(first.View);
Query("poliwhirl");
Check(Address(first.View)==originalAddress,"typing alone does not navigate before Return");
Key(PhysicalKey.Enter);
Check(!owner.FindControlVisible,"Return executes Find and closes the field");
Check(ReferenceEquals(owner.SelectedTab,first.View)&&Address(first.View)==0x7000,"case-insensitive Pokemon-name search selects the existing name");
Query("CA FE F0 0D 17 23");Key(PhysicalKey.Enter);
Check(Address(first.View)==0x7100,"Return also executes the existing hex-byte search");
owner.MatchExactCase=true;Query("poliwhirl");Key(PhysicalKey.Enter);
Check(owner.ShowError&&owner.ErrorMessage.Contains("Could not find"),"exact-case option remains effective");
owner.ClearError.Execute(null);owner.MatchExactCase=false;
Query("NO_SUCH_POKEMON");Key(PhysicalKey.Enter);
Check(owner.ShowError&&owner.ErrorMessage.Contains("Could not find"),"a missing query reports the existing no-results message");owner.ClearError.Execute(null);
Query("electabuzz");Key(PhysicalKey.Enter);
Check(owner.ShowError&&ReferenceEquals(owner.SelectedTab,first.View),"current-ROM scope excludes other open files");owner.ClearError.Execute(null);
owner.SearchAllFiles=true;Query("electabuzz");Key(PhysicalKey.Enter);
Check(ReferenceEquals(owner.SelectedTab,second.View)&&Address(second.View)==0x7000,"all-files scope can navigate to a matching second ROM");
owner.SelectedIndex=0;owner.SearchAllFiles=false;Pump();Query("magneton");Key(PhysicalKey.Enter);
Check(owner.SelectedTab is SearchResultsViewPort&&owner.InformationMessage.Contains("2 matches"),"multiple matches use the existing results view");
owner.SelectedIndex=0;Pump();Key(PhysicalKey.F3);var a=Address(first.View);Key(PhysicalKey.F3);var b=Address(first.View);
Check(a!=b&&new[]{0x7020,0x7040}.Contains(a)&&new[]{0x7020,0x7040}.Contains(b),"F3 still cycles through the matching locations");
Key(PhysicalKey.F3,RawInputModifiers.Shift);Check(Address(first.View)==a,"Shift-F3 moves back through results");
Query("poliwhirl");Key(PhysicalKey.Escape);Check(!owner.FindControlVisible,"Escape still dismisses Find without submitting");
Query("poliwhirl");
var other=new TextBox{Text="other field",AcceptsReturn=true,Width=150};window.FindControl<StackPanel>("FindPanel").Children.Add(other);Pump();other.Focus();Key(PhysicalKey.Enter);
Check(owner.FindControlVisible&&other.Text.Contains('\n'),"Return remains normal text input outside the Find field");
window.FindControl<StackPanel>("FindPanel").Children.Remove(other);box.Focus();Key(PhysicalKey.Escape);
Check(beforeFirst.EqualsModel(first)&&beforeSecond.EqualsModel(second),"all searches preserve both ROMs and their project metadata");
Check(first.View.ChangeHistory.IsSaved&&second.View.ChangeHistory.IsSaved&&!first.View.Undo.CanExecute(null)&&!second.View.Undo.CanExecute(null),"searching creates no edit or Undo entry");
Check(first.Saves==0&&second.Saves==0&&File.ReadAllBytes(first.FileName).SequenceEqual(diskFirst)&&File.ReadAllBytes(second.FileName).SequenceEqual(diskSecond),"search input never saves or changes project files");
window.Editor=null;window.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
