using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Media;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException(message);}
var fs=new StubFileSystem();var themeEditor=new EditorViewModel(fs,InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current!.Resources,themeEditor.Theme);
Application.Current.Resources["FileSystem"]=fs;
var window=new Window{Width=1000,Height=750};window.Show();
void Layout(){Dispatcher.UIThread.RunJobs();window.Measure(new Size(1000,750));window.Arrange(new Rect(0,0,1000,750));AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
PokemonModel Model(Singletons singletons) {
   var bytes=Enumerable.Repeat((byte)0xFF,0x10000).ToArray();Encoding.ASCII.GetBytes("BPRE").CopyTo(bytes,0xAC);bytes[0xBC]=0;
   var model=new PokemonModel(bytes,singletons:singletons);model.LoadMetadata(BaseModel.GetDefaultMetadatas().First());return model;
}
var singletons=new Singletons();var model=Model(singletons);
var parser=new ScriptParser(model.GetShortGameCode(),singletons.ScriptLines,2);
var body=new CodeBody(model,parser,null);parser.AddKeywords(model,body);
var control=new TextEditor{DataContext=body.Editor,FontSize=14};
var overlay=new AutocompleteOverlay{DataContext=body};
window.Content=new Grid{Children={control,overlay}};Layout();
AutoCompleteSelectionItem[] Items()=>((overlay.FindControl<ItemsControl>("AutocompleteItems")!.ItemsSource as IEnumerable<AutoCompleteSelectionItem>)??Array.Empty<AutoCompleteSelectionItem>()).ToArray();
KeyEventArgs Press(Key key) {var e=new KeyEventArgs{RoutedEvent=InputElement.KeyDownEvent,Key=key};control.EditBox.RaiseEvent(e);Layout();return e;}
void Prepare(string text,int caret) {
   overlay.Target=null;body.Content=text;Layout();control.EditBox.SelectionStart=caret;control.EditBox.SelectionEnd=caret;control.EditBox.CaretIndex=caret;Layout();
   overlay.Target=control;control.EditBox.Focus();Layout();overlay.ClearAutocompleteOptions();
}
void Offer(){control.EditBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));Layout();}
string Join(string[] lines,string mode)=>mode=="mixed"?string.Concat(lines.Select((line,i)=>line+(i==lines.Length-1?"":i%2==0?"\r\n":"\n"))):string.Join(mode,lines);
int Start(string text,string line)=>text.IndexOf(line,StringComparison.Ordinal);
var initial=model.RawData.ToArray();
foreach(var ending in new[]{"\n","\r\n","mixed"})foreach(var query in new[]{"msgb","setvar tem"}) {
   foreach(var (lines,lineIndex) in new[]{(new[]{query,"end"},0),(new[]{"lock",query,"end"},1),
      (new[]{"# before","","lock",query,"","release","end"},3),(new[]{"# before","","lock","",query},4)}) {
      var text=Join(lines,ending);var start=Start(text,query);Prepare(text,start+query.Length);
      var expected=body.GetTokenComplete(query,lineIndex,query.Length)!;
      Check(expected?.Count>0,"Core must supply completions for the fixture prefix.");Offer();
      Check(Items().Select(item=>item.CompletionText).SequenceEqual(expected.Select(item=>item.LineText)),"Overlay must offer Core's exact completions on every source line.");
      Check(overlay.IsVisible,"A nonempty completion list must be visible.");
      Check(control.EditBox.Text==text && model.RawData.SequenceEqual(initial),"Showing completions must not edit text or ROM bytes.");
      var selected=Items().First(item=>item.IsSelected);var replacement=selected.CompletionText;
      var after=text[..start]+replacement+text[(start+query.Length)..];
      Check(Press(Key.Enter).Handled,"Enter must accept the selected completion.");
      Check(control.EditBox.Text==after && body.Content==after,"Accept must replace exactly the active line, preserving all other text and original line endings.");
      Check(control.EditBox.CaretIndex==start+replacement.Length,"Caret must follow the completed prefix.");
      Check(!overlay.IsVisible && Items().Length==0,"Accept must clear stale options.");
      Check(model.RawData.SequenceEqual(initial),"Standalone autocomplete must not mutate ROM bytes.");
   }
}
foreach(var ending in new[]{"\n","\r\n"}) {
   // Keep text after the caret, including repeated spaces, exactly as the core completion specifies.
   var text="lock"+ending+"setvar tem  1"+ending+"end";
   var lineStart=text.IndexOf("setvar",StringComparison.Ordinal);var caret=lineStart+"setvar tem".Length;
   Prepare(text,caret);Offer();var first=Items().First(item=>item.IsSelected);
   Check(first.CompletionText.EndsWith("  1"),"Completions must preserve the existing operand suffix.");
   var expected=text[..lineStart]+first.CompletionText+ending+"end";Press(Key.Enter);
   Check(control.EditBox.Text==expected,"Acceptance inside a line must preserve its suffix and neighboring lines.");
   Check(control.EditBox.CaretIndex==caret+first.CompletionText.Length-"setvar tem  1".Length,"Caret must retain its correct position before the suffix.");

   var selectionText="lock"+ending+"setvar tem";
   Prepare(selectionText,selectionText.Length);control.EditBox.SelectionStart=0;control.EditBox.SelectionEnd=selectionText.Length;Offer();
   Check(Items().Length>0,"An active caret on a later line must map independently of the selection start.");
   Check(Math.Abs(((TranslateTransform)overlay.RenderTransform!).Y-(control.EditBox.FontSize*1.4*2+2))<.001,"Popup must follow the active caret line rather than the selection's first line.");
   Press(Key.Up);
   Check(Items()[^1].IsSelected,"Up must wrap from the first completion to the last.");Press(Key.Down);
   Check(Items()[0].IsSelected,"Down must wrap back to the first completion.");Press(Key.Escape);
   Check(!overlay.IsVisible && Items().Length==0,"Escape must dismiss suggestions without accepting them.");
   Check(control.EditBox.Text==selectionText,"Menu navigation and dismissal must not change the document.");

   Prepare("lock"+ending+"msgb",5+ending.Length+3);Offer();
   var button=overlay.FindControl<ItemsControl>("AutocompleteItems")!.GetVisualDescendants().OfType<Button>()
      .First(element=>element.DataContext is AutoCompleteSelectionItem);
   var clicked=(AutoCompleteSelectionItem)button.DataContext!;
   control.EditBox.Focus();Layout();
   var popupRoot=TopLevel.GetTopLevel(button)!;
   var point=button.TranslatePoint(new Avalonia.Point(button.Bounds.Width/2,button.Bounds.Height/2),popupRoot)!.Value;
   popupRoot.MouseMove(point);popupRoot.MouseDown(point,MouseButton.Left);Layout();
   Check(control.EditBox.IsFocused && overlay.IsVisible,"Pointer press on a script suggestion must keep focus and the popup until release.");
   popupRoot.MouseUp(point,MouseButton.Left);Layout();
   Check(control.EditBox.Text=="lock"+ending+clicked.CompletionText,"Clicking an actual suggestion must replace the correct later line.");
   Check(!overlay.IsVisible && Items().Length==0,"Mouse acceptance must clear the popup.");

   Prepare("msgb",4);Offer();Check(Items().Length>0,"Boundary test must start with visible options.");
   control.EditBox.CaretIndex=0;Offer();Check(!overlay.IsVisible && Items().Length==0,"Document-start caret must clear old suggestions.");
   var boundary="lock"+ending+"msgb";
   foreach(var at in new[]{0,4,4+ending.Length,boundary.Length}) {
      Prepare(boundary,at);Offer();
      Check(at==boundary.Length?Items().Length>0:Items().Length==0,"Line-end/start boundaries must not offer another line's completions.");
   }
   if(ending=="\r\n"){Prepare(boundary,5);Offer();Check(Items().Length==0,"A caret within CRLF must be bounded to the previous line end.");}
   Prepare("lock"+ending,4+ending.Length);Offer();Check(Items().Length==0,"Empty final lines must not retain another line's options.");

   // Quote/space acceptance uses the same active-line mapping as Enter.
   var quoted="lock"+ending+"setvar \"Guard Name";
   Prepare(quoted,quoted.Length);
   overlay.FindControl<ItemsControl>("AutocompleteItems")!.ItemsSource=new[]{new AutoCompleteSelectionItem("Guard Name","setvar \"Guard Name\" ",true)};
   overlay.IsVisible=true;Layout();
   Check(!Press(Key.Space).Handled && control.EditBox.Text==quoted,"Space inside a quote must not prematurely accept a suggestion.");
   Check(Press(Key.OemQuotes).Handled,"Closing a quote must accept the current quoted suggestion.");
   Check(control.EditBox.Text=="lock"+ending+"setvar \"Guard Name\" ","Quote acceptance must edit the correct line without normalizing its preceding newline.");
}
// Exercise a real text-input notification rather than only prepared routed notifications.
Prepare("lock\nmsg",8);control.EditBox.Focus();window.KeyTextInput("b");Layout();
Check(control.EditBox.Text=="lock\nmsgb" && Items().Length>0,"Typing a character on a later line must offer completions.");

overlay.Target=null;window.Content=null;Layout();
foreach(var ending in new[]{"\n","\r\n"}) {
   var dispatch=new ManualDispatch();var services=new Singletons(dispatch);var m=Model(services);
   var viewport=new ViewPort("autocomplete-fixture.bin",m,InstantDispatch.Instance,services);
   viewport.InitializationWorkload.GetAwaiter().GetResult();var tool=viewport.Tools.CodeTool;var token=new ModelDelta();
   var seed="lock\nsetvar temp0 1\nrelease\nend";var bytes=tool.ScriptParser.Compile(token,m,0x1000,ref seed,out _,out _);
   Check(bytes!=null,"History fixture must compile.");token.ChangeData(m,0x1000,bytes);m.ObserveAnchorWritten(token,"safety.script",new XSERun(0x1000));
   m.WritePointer(token,0x100,0x1000);m.ObserveRunWritten(token,new PointerRun(0x100));
   viewport.Goto.Execute(0x1000);tool.Mode=CodeMode.Script;tool.IsSelected=true;dispatch.Flush();
   viewport.ChangeHistory.ClearHistory(false);var before=m.RawData.ToArray();var metadata=m.ExportMetadata(null,services.MetadataInfo).Serialize();
   body=tool.Contents[0];
   viewport.Tools.SelectedIndex=Enumerable.Range(0,viewport.Tools.Count).First(i=>ReferenceEquals(viewport.Tools[i],tool));
   window.Content=new TabView{DataContext=viewport};Layout();
   control=window.GetVisualDescendants().OfType<TextEditor>().Single(c=>ReferenceEquals(c.DataContext,body.Editor));
   overlay=window.GetVisualDescendants().OfType<AutocompleteOverlay>().Single(o=>ReferenceEquals(o.Target,control));
   var partial=string.Join(ending,new[]{"lock","setvar tem 1","release","end"});
   var typing=partial.Replace("setvar tem 1","setvar  1");
   Prepare(typing,typing.IndexOf("setvar ",StringComparison.Ordinal)+7);
   control.EditBox.Focus();
   foreach(var character in "tem") {
      var caret=control.EditBox.CaretIndex;
      window.KeyTextInput(character.ToString());Layout();
      Check(control.EditBox.IsFocused && control.EditBox.CaretIndex==caret+1,
         "Live script typing must retain focus and advance its caret on every character.");
   }
   Check(control.EditBox.Text==partial,"Continuous typing must preserve all surrounding script source.");Offer();
   Check(Items().Length>0,"Live CodeTool must offer variable suggestions without compiling incomplete text.");
   var wanted=Array.FindIndex(Items(),item=>item.CompletionText=="setvar temp1 1");Check(wanted>=0,"Fixture must offer the intended variable alias.");
   for(int i=0;i<wanted;i++)Press(Key.Down);
   // The editor's Undo command can now settle/undo a pending draft; ROM history
   // must still remain unchanged before the actual compile callback.
   Check(m.RawData.SequenceEqual(before) && !viewport.ChangeHistory.Undo.CanExecute(null),"Incomplete input and menu navigation must not mutate data/history before compilation.");
   Check(Press(Key.Enter).Handled,"Live completion must be accepted through the actual key handler.");
   Check(control.EditBox.Text==partial.Replace("setvar tem 1","setvar temp1 1"),"Completion must preserve the entire surrounding live script.");
   dispatch.Flush();Layout();viewport.ChangeHistory.ChangeCompleted();
   Check(control.EditBox.IsFocused && window.GetVisualDescendants().OfType<TextEditor>().Any(c=>ReferenceEquals(c,control)),
      "Settled script compilation in the actual tab must retain the live focused editor.");
   var completed=before.ToArray();completed[0x1002]=1;
   Check(!body.HasError && !tool.ShowErrorText && m.RawData.SequenceEqual(completed),"Compiled completion must change only the intended variable byte.");
   Check(m.ReadPointer(0x100)==0x1000 && m.GetNextRun(0x1000).PointerSources.Contains(0x100),"Completion must preserve the script's caller pointer and source metadata.");
   int undo=0;while(viewport.Undo.CanExecute(null)){Check(++undo<10,"Undo must terminate.");viewport.Undo.Execute(null);Layout();}
   Check(m.RawData.SequenceEqual(before) && m.ExportMetadata(null,services.MetadataInfo).Serialize().SequenceEqual(metadata),"Undo must restore all script bytes and metadata.");
   while(viewport.Redo.CanExecute(null)){viewport.Redo.Execute(null);Layout();}
   Check(m.RawData.SequenceEqual(completed),"Redo must restore the exact compiled completion.");
   overlay.Target=null;window.Content=null;Layout();
}
window.Close();
Console.WriteLine("PASS: LF/CRLF/mixed line mapping, exact offerings/acceptance, suffixes, blank/boundary carets, quotes, real text input, caller pointers, and undo/redo.");
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class ManualDispatch : IWorkDispatcher {
   readonly List<ManualWorkTimer> timers=new();
   public IDelayWorkTimer CreateDelayTimer(){var timer=new ManualWorkTimer();timers.Add(timer);return timer;}
   public void Flush(){for(int pass=0;timers.Any(t=>t.HasScheduledWork);pass++){if(pass>10)throw new InvalidOperationException("Timers failed to settle.");foreach(var timer in timers.ToArray())timer.RunWork();}}
   public Task WaitForRenderingAsync()=>Task.CompletedTask;
   public void BlockOnUIWork(Action action)=>action();
   public Task DispatchWork(Action action){action();return Task.CompletedTask;}
   public Task RunBackgroundWork(Action action)=>DispatchWork(action);
}
