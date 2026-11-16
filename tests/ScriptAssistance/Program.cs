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
foreach (var command in new[] { "msgbox.npc", "msgbox", "preparemsg", "move.player", "move.camera", "single.battle", "move.npc", "applymovement", "setflag", "checkflag", "setvar", "addvar" }) {
   using var f = new WorkflowFixture();
   f.Model.LoadMetadata(new StoredMetadata(lists: BaseModel.GetDefaultMetadatas("BPRE").SelectMany(md => md.Lists)
      .Where(list => list.Name == "movementtypes").ToArray()));
   f.Dispatch.DispatchOverride = action => {
      var completion = new TaskCompletionSource();
      Dispatcher.UIThread.Post(() => { action(); completion.SetResult(); }); return completion.Task;
   };
   if (command == "single.battle") f.Model.SetList(new NoDataChangeDeltaModel(), "data.trainers.stats", "Trainer", "Grunt", "Boss");
   var investigator = new Investigator(); f.Tool.Investigator = investigator;
   f.Tool.Contents.Clear(); f.Tool.UpdateContent(); f.Edit("section0:\nend");
   Application.Current!.Resources["FileSystem"] = f.FileSystem;
   var editor = new EditorViewModel(f.FileSystem, InstantDispatch.Instance); App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
   var window = new MainWindow { Width = 1400, Height = 900, FileSystem = f.FileSystem, Editor = editor };
   window.Resources["FileSystem"] = f.FileSystem; editor.Add(f.View); window.Show(); editor.GotoViewModel.ControlVisible = false;
   f.View.Tools.SelectedIndex = Enumerable.Range(0, f.View.Tools.Count).First(i => ReferenceEquals(f.View.Tools[i], f.Tool));
   void Pump() { Dispatcher.UIThread.RunJobs(); window.Measure(new Size(1400, 900)); window.Arrange(new Rect(0, 0, 1400, 900)); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
   Pump(); var body = f.Body;
   var control = window.GetVisualDescendants().OfType<TextEditor>().Single(t => ReferenceEquals(t.DataContext, body.Editor)); var box = control.EditBox;
   Check(ReferenceEquals(control.Tag, body), "The script control must bind its owning CodeBody, not its nested editor model.");
   box.Focus(); int insertion = box.Text!.IndexOf('\n') + 1;
   box.CaretIndex = insertion; box.SelectionStart = box.SelectionEnd = insertion;
   window.KeyTextInput("\n"); Pump(); box.CaretIndex = insertion; box.SelectionStart = box.SelectionEnd = insertion; Pump();
   var baseline = f.State();
   var prefix = acceptance == "typed" ? command : command[..^1];
   foreach (char c in prefix) { window.KeyTextInput(c.ToString()); Pump(); }
   if (acceptance == "typed") {
      window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None); window.KeyTextInput(" "); window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None); Pump();
   } else {
      var overlay = window.GetVisualDescendants().OfType<AutocompleteOverlay>().Single(o => ReferenceEquals(o.Target, control));
      var itemsControl = overlay.FindControl<ItemsControl>("AutocompleteItems")!;
      var options = ((IEnumerable<AutoCompleteSelectionItem>)itemsControl.ItemsSource!).ToArray();
      var wanted = Array.FindIndex(options, item => item.CompletionText.Trim() == (command == "msgbox" ? "msgbox.npc" : command));
      Check(wanted >= 0, $"The command list must offer {command} ({acceptance}): {string.Join(";", options.Select(o => o.CompletionText))}.");
      for (int i = 0; i < wanted; i++) { window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None); window.KeyReleaseQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None); Pump(); }
      if (acceptance == "enter") {
         window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None); window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None); Pump();
      } else {
         var button = itemsControl.GetVisualDescendants().OfType<Button>().Single(b => b.DataContext is AutoCompleteSelectionItem item && item.IsSelected);
         var root = TopLevel.GetTopLevel(button)!;
         var point = button.TranslatePoint(new Avalonia.Point(button.Bounds.Width / 2, button.Bounds.Height / 2), root)!.Value;
         root.MouseDown(point, MouseButton.Left); root.MouseUp(point, MouseButton.Left); Pump();
      }
   }
   if (command is "move.npc" or "applymovement") {
      window.KeyTextInput("8"); Pump();
      window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None); window.KeyTextInput(" "); window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None); Pump();
   }
   if (command == "single.battle") {
      window.KeyTextInput(acceptance == "typed" ? "Grunt" : "Gru"); Pump();
      if (acceptance == "typed") {
         window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None); window.KeyTextInput(" "); window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None); Pump();
      } else {
         var overlay = window.GetVisualDescendants().OfType<AutocompleteOverlay>().Single(o => ReferenceEquals(o.Target, control));
         var items = overlay.FindControl<ItemsControl>("AutocompleteItems");
         var options = ((IEnumerable<AutoCompleteSelectionItem>)items.ItemsSource).ToArray();
         int wanted = Array.FindIndex(options, item => item.DisplayText == "Grunt");
         Check(wanted >= 0, "The trainer list offers the Grunt operand before paired assistance.");
         for (int i=0;i<wanted;i++) { window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None); window.KeyReleaseQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None); Pump(); }
         if (acceptance == "enter") { window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None); window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None); Pump(); }
         else {
            var button = items.GetVisualDescendants().OfType<Button>().Single(b => b.DataContext is AutoCompleteSelectionItem item && item.IsSelected);
            var top = TopLevel.GetTopLevel(button); var at = button.TranslatePoint(new Avalonia.Point(button.Bounds.Width/2,button.Bounds.Height/2), top).Value;
            ((Window)top).MouseMove(at); ((Window)top).MouseDown(at,MouseButton.Left); ((Window)top).MouseUp(at,MouseButton.Left); Pump();
         }
      }
   }
   Check(body.CaretPosition == box.CaretIndex && box.CaretIndex > insertion, "Actual typing must synchronize script and UI caret positions.");
   Check(box.IsFocused && baseline.EqualsModel(f), "Typing assistance preserves focus and does not bypass delayed compilation.");
   if (command == "single.battle") {
      Check(body.Content.Contains("single.battle Grunt <auto> <auto>") && body.Content.Count(c=>c=='{')==2 && body.Content.Count(c=>c=='}')==2,
         "Typed, Enter and clicked trainer completions immediately create both text blocks.");
      Check(box.CaretIndex == body.Content.IndexOf('{')+2, "The native text box caret enters the first trainer text block.");
      int operand = body.Content.IndexOf("<auto>", StringComparison.Ordinal);
      box.SelectionStart = operand; box.SelectionEnd = operand+6; window.KeyTextInput("<003000>"); Pump();
      Check(body.Content.Contains("Grunt <003000> <auto>") && body.Content.Count(c=>c=='{')==2, "Replacing a generated trainer pointer preserves the explicit address and both blocks.");
      box.SelectionStart = operand; box.SelectionEnd = operand+8; window.KeyTextInput("<auto>"); Pump();
      box.CaretIndex = body.Content.IndexOf('{')+2; box.SelectionStart = box.SelectionEnd = box.CaretIndex;
      window.KeyTextInput("Before battle."); Pump();
      int second = body.Content.IndexOf('{',body.Content.IndexOf('}')+1)+2;
      box.CaretIndex = second; box.SelectionStart = box.SelectionEnd = second; window.KeyTextInput("After battle."); Pump();
      f.Dispatch.Flush(); f.View.ChangeHistory.ChangeCompleted(); Pump();
      Check(!body.HasError && f.Model.ReadMultiByteValue(body.Address+2,2)==1, "The completed trainer script compiles with the chosen Grunt ID.");
      var firstBytes = PCSString.Convert("\"Before battle.\""); var secondBytes = PCSString.Convert("\"After battle.\"");
      Check(firstBytes.SequenceEqual(f.Model.RawData.Skip(f.Model.ReadPointer(body.Address+6)).Take(firstBytes.Count)) &&
         secondBytes.SequenceEqual(f.Model.RawData.Skip(f.Model.ReadPointer(body.Address+10)).Take(secondBytes.Count)),
         "Actual text input reaches the correct prebattle and postbattle ROM resources.");
      var compiled = f.State(); f.View.Undo.Execute(null); f.Dispatch.Flush(); Pump(); Check(baseline.EqualsModel(f), "Undo restores the pre-trainer state after actual UI entry.");
      f.View.Redo.Execute(null); f.Dispatch.Flush(); Pump(); Check(compiled.EqualsModel(f), "Redo restores both trainer resources after actual UI entry.");
   } else if (command.StartsWith("msgbox") || command.StartsWith("move.") || command is "preparemsg" or "applymovement") {
      Check(body.Content.Contains("<auto>"), $"{acceptance}/{command}: a supported pointer operand must receive auto. Source: {body.Content}");
      Check(box.Text![..box.CaretIndex].EndsWith("{\n"), $"The caret lands inside the empty dialogue body ({acceptance}/{command}, caret {box.CaretIndex}/{body.CaretPosition}, source {body.Content.Replace("\n", "|")}).");
      Check(body.Content.Count(c => c == '{') == 1 && body.Content.Count(c => c == '}') == 1,
         "The enhanced assistance creates exactly one complete dialogue scaffold.");
      var content = command.StartsWith("move.") || command == "applymovement" ? "walk_up" : "Hello!";
      window.KeyTextInput(content); Pump();
      Check(body.Content.Contains("{\n" + content + "\n}"), "Typing immediately after assistance enters the resource body.");
      f.Dispatch.Flush(); f.View.ChangeHistory.ChangeCompleted(); Pump(); Check(!body.HasError, "The existing auto-pointer form still compiles.");
      if (content == "walk_up") {
         int movement = f.Model.ReadPointer(body.Address + 3);
         Check(f.Model.RawData.Skip(movement).Take(2).SequenceEqual(new byte[] { 0x11, 0xFE }),
            "Assisted movement uses the actual FireRed walk_up byte and terminator, not an empty stream.");
      }
      var compiled = f.State(); f.View.Undo.Execute(null); f.Dispatch.Flush(); Pump();
      Check(baseline.EqualsModel(f), "Undo restores the complete pre-assistance resource state.");
      f.View.Redo.Execute(null); f.Dispatch.Flush(); Pump(); Check(compiled.EqualsModel(f), "Redo restores the complete assisted resource edit.");
   } else {
      bool flag = command.Contains("flag");
      Check(body.CanInsertFlag == flag && body.CanInsertVar != flag, "Eligibility follows the current flag/variable operand.");
      var button = window.GetVisualDescendants().OfType<Button>().Single(b => ReferenceEquals(b.DataContext, body) && b.Name == (flag ? "FloatingInsertFlagButton" : "FloatingInsertVarButton"));
      Check(button.IsVisible && button.Command!.CanExecute(null), "The unused-ID insertion button is offered.");
      if (command is "checkflag" or "addvar") button.Command!.Execute(null);
      else { window.KeyPressQwerty(PhysicalKey.U, RawInputModifiers.Meta); window.KeyReleaseQwerty(PhysicalKey.U, RawInputModifiers.None); }
      Pump();
      Check(body.Content.Contains(flag ? "0x0222" : "0x4040"), "Both button and Cmd+U insert the investigator's explicit operand.");
      Check(flag ? investigator.Flags == 1 && investigator.Variables == 0 : investigator.Variables == 1 && investigator.Flags == 0, "An insertion requests exactly one appropriate ID.");
      if (!flag) { window.KeyTextInput("1"); Pump(); }
      f.Dispatch.Flush(); f.View.ChangeHistory.ChangeCompleted(); Pump();
      Check(!body.HasError, "The completed assisted command compiles normally.");
      var compiled = f.State(); f.View.Undo.Execute(null); f.Dispatch.Flush(); Pump();
      Check(baseline.EqualsModel(f), "Undo restores all bytes/metadata before the assisted command.");
      f.View.Redo.Execute(null); f.Dispatch.Flush(); Pump(); Check(compiled.EqualsModel(f), "Redo restores the assisted edit.");
   }
   window.Editor = null; window.Close();
}
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class Investigator : IDataInvestigator {
   public int Flags, Variables;
   public int FindNextUnusedFlag() { Flags++; return 0x222; }
   public int FindNextUnusedVariable() { Variables++; return 0x4040; }
}
