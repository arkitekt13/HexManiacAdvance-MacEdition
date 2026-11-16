using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
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
Text(first,0x7200,"find.shared","SHARED FIND MATCH");Text(second,0x7200,"find.shared","SHARED FIND MATCH");
for(int i=0;i<40;i++)Text(first,0x7400+i*0x80,$"find.scroll.{i}","SCROLL FIND MATCH");
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
var resultTab=window.GetVisualDescendants().OfType<TabView>().SingleOrDefault(t=>ReferenceEquals(t.DataContext,owner.SelectedTab));
Check(resultTab!=null&&resultTab.IsEffectivelyVisible,"multiple matches display a real results viewer instead of the unported placeholder");
TabView ResultsTab(SearchResultsViewPort results)=>window.GetVisualDescendants().OfType<TabView>().Single(t=>ReferenceEquals(t.DataContext,results));
SearchResultsViewPort Results(){Check(owner.SelectedTab is SearchResultsViewPort,"search selects its results tab");return (SearchResultsViewPort)owner.SelectedTab;}
Avalonia.Point At(SearchResultsViewPort results,int x,int y){var hex=ResultsTab(results).FindControl<HexContent>("HexContent");return hex.TranslatePoint(new Avalonia.Point((x+.5)*hex.CellWidth,(y+.5)*hex.CellHeight),window).Value;}
(int x,int y) FirstMatch(SearchResultsViewPort results,IDataModel model=null){
 for(int y=0;y<results.Height;y++)for(int x=0;x<results.Width;x++)
  if(results.IsSelected(new HavenSoft.HexManiac.Core.Models.Point(x,y))&&(model==null||ReferenceEquals(results.ModelFor(new(x,y)),model)))return(x,y);
 throw new Exception("No visible highlighted search match");
}
void ClickMatch(SearchResultsViewPort results,bool command=false,bool twice=false,IDataModel model=null){
 // Separate a fresh double-click from earlier tests at the same screen position.
 if(twice)Thread.Sleep(600);
 var cell=FirstMatch(results,model);var at=At(results,cell.x,cell.y);window.MouseMove(at);
 for(int i=0;i<(twice?2:1);i++){window.MouseDown(at,MouseButton.Left,command?modifier:RawInputModifiers.None);window.MouseUp(at,MouseButton.Left);Pump();}
}
void ReturnTo(SearchResultsViewPort results){owner.SelectedIndex=Enumerable.Range(0,owner.Count).Single(i=>ReferenceEquals(owner[i],results));Pump();}
var matches=Results();
var hexResults=resultTab.FindControl<HexContent>("HexContent");
Check(ReferenceEquals(hexResults.ViewPort,matches)&&hexResults.Bounds.Width>100&&hexResults.Bounds.Height>100,"results are bound to a laid-out hex preview");
Check(!resultTab.FindControl<Grid>("ToolPanel").IsEffectivelyVisible&&!resultTab.IsScriptMapVisible,"results show previews without editable tools or Map View");
Check(matches[3,0].Format is HavenSoft.HexManiac.Core.ViewModels.DataFormats.UnderEdit heading&&heading.CurrentText.Contains("Results from "),"results render their source-file heading");
Check(matches.ResultCount==2&&matches.Headers.Any(h=>!string.IsNullOrEmpty(h.Content)),"both matches retain address headers");
if(args.Length==1){using var image=new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(1200,800));image.Render(window);image.Save(args[0]);}
ClickMatch(matches);Check(ReferenceEquals(owner.SelectedTab,matches),"a single plain click keeps the read-only results open");
hexResults.Focus();window.KeyTextInput("FF");Key(PhysicalKey.Backspace);Key(PhysicalKey.Delete);Key(PhysicalKey.V,modifier);
Check(beforeFirst.EqualsModel(first)&&beforeSecond.EqualsModel(second),"typing, deletion and paste in results cannot edit either source");
ClickMatch(matches,command:true);
Check(ReferenceEquals(owner.SelectedTab,first.View)&&Address(first.View)==0x7020,"Command-click opens the first highlighted match in its source tab");
Check(first.View.ConvertViewPointToAddress(first.View.SelectionEnd)==0x7027,"navigation selects the full matched text");
ReturnTo(matches);ClickMatch(matches,twice:true);
Check(ReferenceEquals(owner.SelectedTab,first.View)&&Address(first.View)==0x7020,"double-click opens a highlighted match");
ReturnTo(matches);var blank=At(matches,0,matches.Height-1);window.MouseDown(blank,MouseButton.Left,modifier);window.MouseUp(blank,MouseButton.Left);Pump();
Check(ReferenceEquals(owner.SelectedTab,matches),"clicking empty space does not navigate");
window.Width=700;window.Height=480;Pump();
Check(ResultsTab(matches).FindControl<HexContent>("HexContent").Bounds.Width>100&&matches.Width>0&&matches.Height>0,"results remain usable in a smaller window");
window.Width=1200;window.Height=800;Pump();
owner.SelectedIndex=0;Pump();owner.SearchAllFiles=true;Query("SHARED FIND MATCH");Key(PhysicalKey.Enter);
var shared=Results();Check(shared.ResultCount==2,"all-files results include both source files");
ClickMatch(shared,command:true,model:second.Model);
Check(ReferenceEquals(owner.SelectedTab,second.View)&&Address(second.View)==0x7200,"a second-file result opens that file at the correct address");
ReturnTo(shared);ClickMatch(shared,command:true,model:first.Model);
Check(ReferenceEquals(owner.SelectedTab,first.View)&&Address(first.View)==0x7200,"a first-file result opens its own file even at an identical address");
owner.SearchAllFiles=false;Query("SCROLL FIND MATCH");Key(PhysicalKey.Enter);
var scrolling=Results();Check(scrolling.ResultCount==40&&scrolling.MaximumScroll>scrolling.Height,"many matches create a scrollable results view");
var wheelAt=At(scrolling,1,2);window.MouseWheel(wheelAt,new Vector(0,-3),RawInputModifiers.None);Pump();
Check(scrolling.ScrollValue>0,"mouse wheel scrolls actual result previews");
scrolling.ScrollValue=scrolling.MaximumScroll;Pump();
// The final preview includes context after its match. Scroll back through those rows.
bool VisibleMatch()=>Enumerable.Range(0,scrolling.Height).Any(y=>Enumerable.Range(0,scrolling.Width).Any(x=>scrolling.IsSelected(new HavenSoft.HexManiac.Core.Models.Point(x,y))));
for(int i=0;i<scrolling.Height&&!VisibleMatch();i++){scrolling.ScrollValue--;Pump();}
ClickMatch(scrolling,command:true);
Check(ReferenceEquals(owner.SelectedTab,first.View)&&Address(first.View)==0x7400+39*0x80,"the last match remains navigable after scrolling");
ReturnTo(scrolling);var oldScroll=scrolling.ScrollValue;owner.SelectedIndex=0;Pump();ReturnTo(scrolling);
Check(scrolling.ScrollValue==oldScroll&&ResultsTab(scrolling).FindControl<HexContent>("HexContent").IsEffectivelyVisible,"switching tabs restores the results and scroll position");
scrolling.Close.Execute(null);Pump();Check(!Enumerable.Range(0,owner.Count).Any(i=>ReferenceEquals(owner[i],scrolling)),"closing results leaves source tabs open");
owner.SelectedIndex=0;Pump();Key(PhysicalKey.F3);var a=Address(first.View);Key(PhysicalKey.F3);var b=Address(first.View);
Check(a!=b&&a>=0x7400&&b>=0x7400,"F3 still cycles through the matching locations");
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
