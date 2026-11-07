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
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Review;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext()); Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
foreach (var acceptance in new[] { "typed", "enter", "click" })
foreach (var command in new[] { "msgbox.npc", "preparemsg", "move.player" })
foreach (var newline in new[] { "\n", "\r\n" })
foreach (var mode in new[] { "normal", "trailing-spaces", "trailing-tabs", "auto-deleted" }) {
   using var f = new WorkflowFixture();
   f.Model.LoadMetadata(new StoredMetadata(lists: BaseModel.GetDefaultMetadatas("BPRE").SelectMany(md => md.Lists)
      .Where(list => list.Name == "movementtypes").ToArray()));
   f.Dispatch.DispatchOverride = action => {
      var completion = new TaskCompletionSource();
      Dispatcher.UIThread.Post(() => { action(); completion.SetResult(); }); return completion.Task;
   };
   f.Tool.Contents.Clear(); f.Tool.UpdateContent(); f.Edit("section0:\n  lockall\n  setvar 0x403F 1\n  move.player <auto>\n{\nemote_exclamation_mark\n}\n  waitmovement 0\n  move.npc 9 <auto>\n{\nwalk_left\nwalk_left\nwalk_left\nwalk_left\nwalk_left\nwalk_left\nwalk_left\n}\n  goto <section1>\n  end\n\nsection1:\n  end");
   Application.Current!.Resources["FileSystem"] = f.FileSystem;
   var editor = new EditorViewModel(f.FileSystem, InstantDispatch.Instance); App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
   var window = new MainWindow { Width = 1400, Height = 900, FileSystem = f.FileSystem, Editor = editor };
   window.Resources["FileSystem"] = f.FileSystem; editor.Add(f.View); window.Show(); editor.GotoViewModel.ControlVisible = false;
   f.View.Tools.SelectedIndex = Enumerable.Range(0, f.View.Tools.Count).First(i => ReferenceEquals(f.View.Tools[i], f.Tool));
   void Pump() { Dispatcher.UIThread.RunJobs(); window.Measure(new Size(1400, 900)); window.Arrange(new Rect(0, 0, 1400, 900)); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
   Pump(); var body = f.Body;
   var control = window.GetVisualDescendants().OfType<TextEditor>().Single(t => ReferenceEquals(t.DataContext, body.Editor)); var box = control.EditBox;
   Check(ReferenceEquals(control.Tag, body), "The script control must bind its owning CodeBody, not its nested editor model.");
   body.Editor.Content=body.Content.Replace("\r\n","\n").Replace("\n",newline);Pump();
   f.View.ChangeHistory.ClearHistory(false);f.View.ChangeHistory.TagAsSaved();
   if(mode=="auto-deleted"){
      box.Focus();var autoStart=box.Text.IndexOf("<auto>",StringComparison.Ordinal);box.CaretIndex=autoStart+6;box.SelectionStart=box.SelectionEnd=box.CaretIndex;Pump();
      window.KeyPressQwerty(PhysicalKey.Backspace,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.Backspace,RawInputModifiers.None);Pump();
      Check(f.Tool.InsertAutoActive,"Editing an earlier auto pointer must not switch off assistance.");
      box.SelectionStart=autoStart;box.SelectionEnd=autoStart+5;window.KeyTextInput("<003800>");Pump();
      Check(f.Tool.InsertAutoActive,"Replacing an earlier auto pointer must leave future assistance enabled.");
   }
   box.Focus(); int insertion = box.Text!.LastIndexOf("  end",StringComparison.Ordinal);
   box.CaretIndex = insertion; box.SelectionStart = box.SelectionEnd = insertion;
   window.KeyTextInput(newline); Pump(); box.CaretIndex = insertion; box.SelectionStart = box.SelectionEnd = insertion; Pump();

   if(mode=="trailing-spaces"||mode=="trailing-tabs"){window.KeyTextInput(mode=="trailing-tabs"?"  \t":"   ");Pump();box.CaretIndex=insertion;box.SelectionStart=box.SelectionEnd=insertion;Pump();}
   var unchangedPrefix=body.Content[..insertion];var baseline=f.State();
   var typed=acceptance=="typed"?command:command[..^1];
   foreach(char c in typed){window.KeyTextInput(c.ToString());Pump();}
   if(acceptance=="typed")Check(!body.Content[insertion..].Contains("<auto>"),"Typing the command name alone must wait for the following space, even when old whitespace follows it.");
   if(acceptance=="typed"){window.KeyPressQwerty(PhysicalKey.Space,RawInputModifiers.None);window.KeyTextInput(" ");window.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None);Pump();}
   else{
    var overlay=window.GetVisualDescendants().OfType<AutocompleteOverlay>().Single(x=>ReferenceEquals(x.Target,control));
    var items=overlay.FindControl<ItemsControl>("AutocompleteItems");
    var options=((IEnumerable<AutoCompleteSelectionItem>)items.ItemsSource).ToArray();
    int wanted=Array.FindIndex(options,x=>x.CompletionText.Trim()==command);
    if(wanted<0)throw new Exception("No NPC suggestion");
    for(int i=0;i<wanted;i++){window.KeyPressQwerty(PhysicalKey.ArrowDown,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.ArrowDown,RawInputModifiers.None);Pump();}
    if(acceptance=="enter"){window.KeyPressQwerty(PhysicalKey.Enter,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.Enter,RawInputModifiers.None);Pump();}
    else{var button=items.GetVisualDescendants().OfType<Button>().Single(b=>b.DataContext is AutoCompleteSelectionItem item&&item.IsSelected);var top=TopLevel.GetTopLevel(button);var at=button.TranslatePoint(new Avalonia.Point(button.Bounds.Width/2,button.Bounds.Height/2),top).Value;((Window)top).MouseMove(at);((Window)top).MouseDown(at,MouseButton.Left);((Window)top).MouseUp(at,MouseButton.Left);Pump();}
   }
   var commandStart=body.Content.LastIndexOf(command+" ",StringComparison.Ordinal);
   var operand=body.Content.IndexOf("<auto>",commandStart,StringComparison.Ordinal);
   Check(operand>commandStart,$"{acceptance}/{command}/{newline.Length}/{mode}: initial entry must offer an auto operand.");
   var open=body.Content.IndexOf('{',operand);var close=body.Content.IndexOf('}',open);
   Check(open>operand&&box.CaretIndex>open&&box.CaretIndex<=close,"The caret must land inside exactly the new resource block.");
   Check(body.Content.Count(c=>c=='{')==3&&body.Content.StartsWith(unchangedPrefix,StringComparison.Ordinal),"Earlier sections, labels and movement blocks must remain unchanged.");
   Check(baseline.EqualsModel(f)&&!f.View.ChangeHistory.Undo.CanExecute(null),"Source assistance must not directly write ROM/resources/metadata/history.");
   string Replace(int start,int count,string replacement){
      var expected=body.Content[..start]+replacement+body.Content[(start+count)..];
      box.Focus();box.SelectionStart=start;box.SelectionEnd=start+count;
      if(replacement.Length>0)window.KeyTextInput(replacement);
      else{window.KeyPressQwerty(PhysicalKey.Backspace,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.Backspace,RawInputModifiers.None);}
      Pump();return expected;
   }
   var expected=Replace(operand,6,"<003000>");
   Check(body.Content==expected&&f.Tool.InsertAutoActive,"Replacing auto with a specific address must preserve the exact user edit and future assistance.");
   expected=Replace(operand,8,"");
   Check(body.Content==expected,"Deleting a pointer must not immediately put auto back.");
   expected=Replace(operand,0,"  ");
   Check(body.Content==expected,"Further whitespace in an emptied operand must not reinsert auto.");
   insertion=body.Content.LastIndexOf("  end",StringComparison.Ordinal);
   box.CaretIndex=insertion;box.SelectionStart=box.SelectionEnd=insertion;window.KeyTextInput(newline);Pump();box.CaretIndex=insertion;box.SelectionStart=box.SelectionEnd=insertion;Pump();
   foreach(char c in "preparemsg"){window.KeyTextInput(c.ToString());Pump();}
   window.KeyTextInput(" ");Pump();
   var fresh=body.Content.LastIndexOf("preparemsg",StringComparison.Ordinal);
   Check(body.Content.IndexOf("<auto>",fresh,StringComparison.Ordinal)>fresh,"A later command must still receive initial help after pointer replacement/deletion.");
   Check(baseline.EqualsModel(f),"All assistance and pointer-source edits remain source-only until ordinary compilation.");
   window.Editor=null;window.Close();
}

// Deferred help must lose to newer source, explicit Undo, and navigation.
foreach(var action in new[]{"edit","undo","navigate"})using(var f=new WorkflowFixture()){
 var callbacks=new List<Action>();f.Dispatch.DispatchOverride=work=>{callbacks.Add(work);return Task.CompletedTask;};
 var body=f.Body;body.CaretPosition="msgbox.npc".Length;body.Editor.Content="msgbox.npc \nend";
 Check(callbacks.Count>0&&!body.Content.Contains("<auto>"),"Initial assistance is planned without mutating input before its callback.");
 if(action=="edit")body.ApplyCompletion("msgbox.npc <003000>\nend",18);
 else if(action=="undo")f.View.Undo.Execute(null);
 else f.View.Goto.Execute(WorkflowFixture.Other);
 var state=f.State();var source=body.Content;var active=f.Body.Content;var caret=body.CaretPosition;var activeCaret=f.Body.CaretPosition;
 foreach(var callback in callbacks.ToArray())callback();
 Check(state.EqualsModel(f)&&body.Content==source&&f.Body.Content==active&&body.CaretPosition==caret&&f.Body.CaretPosition==activeCaret,"A stale assistance callback cannot change newer source, another script, model state or either caret.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
