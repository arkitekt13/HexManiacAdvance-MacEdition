using Avalonia;
using Point = Avalonia.Point;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
 .UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
var fs=new StubFileSystem();Application.Current.Resources["FileSystem"]=fs;
var editor=new EditorViewModel(fs,InstantDispatch.Instance);App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
int checks=0;
void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
var host=new Window{Width=340,Height=220};host.Show();
void Pump(){Dispatcher.UIThread.RunJobs();host.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();}
Rect Bounds(Control control,Visual relative)=>new(control.TranslatePoint(default,relative).Value,control.Bounds.Size);
void NoOverlap(ScrollViewer scroll,string context){
 var presenter=scroll.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
 var area=Bounds(presenter,scroll);
 foreach(var bar in scroll.GetVisualDescendants().OfType<ScrollBar>().Where(b=>b.IsVisible)){
  var bounds=Bounds(bar,scroll);
  Check(bar.Orientation==Avalonia.Layout.Orientation.Vertical?area.Right<=bounds.Left+.1:area.Bottom<=bounds.Top+.1,context+": content viewport must not extend beneath a scrollbar.");
  var size=bar.Bounds.Size;host.MouseMove(bar.TranslatePoint(new Point(bar.Bounds.Width/2,bar.Bounds.Height/2),host).Value);Pump();
  Check(size==bar.Bounds.Size&&area==Bounds(presenter,scroll),context+": hovering must not change scrollbar or content geometry.");
 }
}
var source=string.Join("\n",Enumerable.Repeat(new string('W',160),60));
var model=new TextEditorViewModel{Content=source};
var code=new TextEditor{DataContext=model};host.Content=code;Pump();
var codeScroll=code.EditBox.GetVisualDescendants().OfType<ScrollViewer>().First();
NoOverlap(codeScroll,"Script editor");
code.EditBox.SelectionStart=3;code.EditBox.SelectionEnd=8;Pump();
var selection=(code.EditBox.SelectionStart,code.EditBox.SelectionEnd);
codeScroll.Offset=new Vector(80,60);Pump();
Check(codeScroll.Offset.X>0&&codeScroll.Offset.Y>0,"Script editor retains horizontal and vertical scrolling.");
Check(selection==(code.EditBox.SelectionStart,code.EditBox.SelectionEnd)&&model.Content==source,"Scrolling preserves script source and selection.");
var syntax=code.GetVisualDescendants().OfType<Canvas>().First();
var clip=syntax.Clip as RectangleGeometry;
var viewport=codeScroll.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
Check(clip!=null&&clip.Rect==Bounds(viewport,syntax),"Syntax paint must be clipped to the text viewport, outside scrollbar gutters.");
foreach(double width in new[]{220d,420d}){host.Width=width;Pump();NoOverlap(codeScroll,"Resized script editor");}

// Exercise the actual XAML viewer instances, including panels normally hidden by selection.
var map=new MapTab();var tab=new TabView();
var viewers=new[]{map.FindControl<ScrollViewer>("BlockViewer"),map.FindControl<ScrollViewer>("EventScrollViewer"),map.FindControl<ScrollViewer>("HeaderPanel"),map.FindControl<ScrollViewer>("TileViewer"),tab.FindControl<ScrollViewer>("TableScrollViewer"),tab.FindControl<ScrollViewer>("ImageToolPanel"),tab.FindControl<ScrollViewer>("CodeToolMultiTextBox")};
foreach(var scroll in viewers){
 Check(scroll!=null,"Every tested production panel viewer must exist.");
 if(scroll.Parent is Panel panel)panel.Children.Remove(scroll);
 else if(scroll.Parent is Decorator decorator)decorator.Child=null;
 else if(scroll.Parent is ContentControl content)content.Content=null;
 else throw new Exception("Unexpected viewer parent: "+scroll.Parent);
 scroll.IsVisible=true;scroll.Width=double.NaN;scroll.Height=double.NaN;
 var arrow=new Button{Content=">",Width=28,Height=28,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Right,VerticalAlignment=Avalonia.Layout.VerticalAlignment.Top};
 var contentGrid=new Grid{Height=900};contentGrid.Children.Add(arrow);scroll.Content=contentGrid;
 int clicks=0;arrow.Click+=(_,_)=>clicks++;
 foreach(double width in new[]{220d,420d}){
  host.Width=width;host.Content=scroll;Pump();NoOverlap(scroll,scroll.Name);
  var at=arrow.TranslatePoint(new Point(arrow.Bounds.Width/2,arrow.Bounds.Height/2),host).Value;
  host.MouseMove(at);host.MouseDown(at,MouseButton.Left);host.MouseUp(at,MouseButton.Left);Pump();
  Check(clicks>0,scroll.Name+": a right-edge navigation button stays clickable.");clicks=0;
  scroll.Offset=new Vector(0,100);Pump();Check(scroll.Offset.Y>0,scroll.Name+": vertical scrolling still works.");scroll.Offset=default;Pump();
 }
 host.Content=null;Pump();
}
host.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
