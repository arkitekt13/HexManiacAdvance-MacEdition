using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
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
using Point=Avalonia.Point;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
var fs=new StubFileSystem();Application.Current.Resources["FileSystem"]=fs;var editor=new EditorViewModel(fs,InstantDispatch.Instance);App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
int checks=0;
void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
void Pump(Window host){Dispatcher.UIThread.RunJobs();host.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();}
void Drain(Window host){for(int i=0;i<4;i++)Pump(host);}
void Click(Window host,Point at){host.MouseMove(at);host.MouseDown(at,MouseButton.Left);host.MouseUp(at,MouseButton.Left);Drain(host);}
Rect Area(Control control,Visual relative)=>new(control.TranslatePoint(default,relative).Value,control.Bounds.Size);
Point ClickPoint(TextEditor field,ScrollViewer outer,Window host,int x=100){
 var viewport=outer.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
 var visible=Area(field.EditBox,host).Intersect(Area(viewport,host));
 Check(visible.Height>45,"The clicked editor must actually be visible.");
 return new Point(visible.Left+x,visible.Top+Math.Min(80,visible.Height/2));
}
void StableClick(TextEditor field,ScrollViewer outer,Window host,string context,int x=100){
 var at=ClickPoint(field,outer,host,x);var before=outer.Offset;
 var presenter=field.EditBox.GetVisualDescendants().OfType<TextPresenter>().Single();
 var local=host.TranslatePoint(at,presenter).Value;int expected=presenter.TextLayout.HitTestPoint(local).TextPosition;
 Click(host,at);
 Check(outer.Offset==before,$"{context}: clicking visible source must not jump the outer scroll offset: {before} -> {outer.Offset}.");
 Check(field.EditBox.IsFocused,$"{context}: clicked source retains keyboard focus.");
 Check(field.EditBox.CaretIndex==expected,$"{context}: caret must match the clicked character: {expected} / {field.EditBox.CaretIndex}.");
}
var reported=File.ReadAllText(args.Length==1?args[0]:"SyntheticTourScript.txt");var parts=reported.Split("\n\nsection1:");
Check(parts.Length==2,"Use both sections of the sanitized long-script reproduction as plain text, never a ROM fixture.");parts[1]="section1:"+parts[1];
foreach(var crlf in new[]{false,true})foreach(double size in new[]{14d,20d}){
 var tab=new TabView();var outer=tab.FindControl<ScrollViewer>("CodeToolMultiTextBox");((DockPanel)outer.Parent).Children.Remove(outer);outer.IsVisible=true;
 var models=parts.Select(text=>new TextEditorViewModel{Content=crlf?text.Replace("\n","\r\n"):text}).ToArray();
 var fields=models.Select(m=>new TextEditor{DataContext=m,FontSize=size,Margin=new Thickness(5)}).ToArray();
 var stack=new StackPanel();foreach(var field in fields)stack.Children.Add(field);stack.Children.Add(new Border{Height=200});outer.Content=stack;
 var blur=new TextBox{Text="Other field"};var grid=new Grid{RowDefinitions=new RowDefinitions("Auto,*")};grid.Children.Add(blur);grid.Children.Add(outer);Grid.SetRow(outer,1);
 var window=new Window{Width=800,Height=size==14?500:680,Content=grid};window.Show();Drain(window);
 var originals=models.Select(m=>m.Content).ToArray();
 foreach(var field in fields)foreach(double into in new[]{50d,180d}){
  blur.Focus();Drain(window);outer.Offset=new Vector(0,field.Bounds.Y+into);Drain(window);
  StableClick(field,outer,window,$"LF/CRLF={crlf}, font={size}, editor={Array.IndexOf(fields,field)}, offset={outer.Offset.Y}");
  StableClick(field,outer,window,"Already-focused click",180);
  Check(originals.SequenceEqual(models.Select(m=>m.Content)),"Clicks cannot change source text.");
 }
 // Manual scrolling while focused must survive a later click, too.
 outer.Offset=new Vector(0,fields[1].Bounds.Y+600);Drain(window);var settled=outer.Offset;
 StableClick(fields[1],outer,window,"After manual scrolling",240);
 Check(outer.Offset==settled,"Clicking after manual scrolling cannot snap back to the previous caret.");
 // Selection inside the viewport must not be mistaken for a request to reveal the whole editor.
 var from=ClickPoint(fields[1],outer,window,300);var to=from+new Vector(25,100);var offset=outer.Offset;
 window.MouseMove(from);window.MouseDown(from,MouseButton.Left);window.MouseMove(to,RawInputModifiers.LeftMouseButton);window.MouseUp(to,MouseButton.Left);Drain(window);
 Check(outer.Offset==offset&&fields[1].EditBox.SelectionStart!=fields[1].EditBox.SelectionEnd,"Dragging a visible text selection retains scroll position.");
 // Preserve legitimate keyboard-driven caret scrolling beyond the visible viewport.
 var input=fields[1].EditBox;input.SelectionStart=input.SelectionEnd=input.CaretIndex;offset=outer.Offset;
 for(int i=0;i<50;i++){window.KeyPressQwerty(PhysicalKey.ArrowDown,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.ArrowDown,RawInputModifiers.None);Pump(window);}
 Drain(window);var presenter=input.GetVisualDescendants().OfType<TextPresenter>().Single();var caret=presenter.TextLayout.HitTestTextPosition(input.CaretIndex);
 var top=presenter.TranslatePoint(new Point(caret.X,caret.Y),window).Value;
 var visible=Area(outer.GetVisualDescendants().OfType<ScrollContentPresenter>().First(),window);
 Check(outer.Offset.Y>offset.Y&&top.Y>=visible.Top-1&&top.Y<visible.Bottom,"Arrow-key navigation still scrolls the caret into view.");
 window.KeyTextInput("Z");Drain(window);
 Check(models[1].Content.Contains('Z'),"Typing continues in the clicked script field.");
 window.Content=null;window.Close();
}
// Actual selected script tab: focus/caret changes must not flush pending source or touch a ROM.
using(var fixture=new WorkflowFixture()){
 var source=fixture.View;source.Tools.SelectedIndex=3;fixture.Body.Editor.Content=reported;
 var body=fixture.Body;var bytes=fixture.Model.RawData.ToArray();var metadata=fixture.Metadata();var pending=fixture.Dispatch.Pending;
 var app=new EditorViewModel(fixture.FileSystem,InstantDispatch.Instance);app.Add(source);app.GotoViewModel.ControlVisible=false;
 var window=new MainWindow{Width=1400,Height=650,Editor=app,FileSystem=fixture.FileSystem};window.Resources["FileSystem"]=fixture.FileSystem;window.Show();Drain(window);
 var tab=window.GetVisualDescendants().OfType<TabView>().Single(t=>ReferenceEquals(t.DataContext,source));var outer=tab.FindControl<ScrollViewer>("CodeToolMultiTextBox");
 var field=tab.GetVisualDescendants().OfType<TextEditor>().Single(t=>ReferenceEquals(t.DataContext,body.Editor));
 // A real header field provides the blur without changing documents or source.
 var blur=tab.FindControl<TextBox>("AnchorTextBox");blur.Focus();Drain(window);outer.Offset=new Vector(0,500);Drain(window);
 StableClick(field,outer,window,"Actual script tab");
 Check(body.Content==reported&&ReferenceEquals(fixture.Body,body)&&fixture.Dispatch.Pending==pending,"Click/focus retains the exact pending script and debounce work.");
 Check(bytes.SequenceEqual(fixture.Model.RawData)&&metadata.SequenceEqual(fixture.Metadata())&&source.ChangeHistory.IsSaved&&!source.ChangeHistory.Undo.CanExecute(null),"Clicking and scrolling cannot change ROM bytes, metadata or history.");
 window.Editor=null;window.Close();
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
