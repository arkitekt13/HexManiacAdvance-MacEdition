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
foreach (var command in new[] { "msgbox.npc", "msgbox", "preparemsg", "move.player", "move.camera", "setflag", "checkflag", "setvar", "addvar" }) {
   using var f = new WorkflowFixture();
   f.Dispatch.DispatchOverride = action => {
      var completion = new TaskCompletionSource();
      Dispatcher.UIThread.Post(() => { action(); completion.SetResult(); }); return completion.Task;
   };
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
   foreach (char c in command) { window.KeyTextInput(c.ToString()); Pump(); }
   window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None); window.KeyTextInput(" "); window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None); Pump();
   Check(body.CaretPosition == box.CaretIndex && box.CaretIndex > insertion, "Actual typing must synchronize script and UI caret positions.");
   Check(box.IsFocused && baseline.EqualsModel(f), "Typing assistance preserves focus and does not bypass delayed compilation.");
   if (command.StartsWith("msgbox") || command.StartsWith("move.") || command == "preparemsg") {
      Check(body.Content.Contains("<auto>"), "A space at a supported text-pointer operand inserts <auto>.");
      Check(box.Text![..box.CaretIndex].EndsWith("{\n"), "The caret lands inside the empty dialogue body.");
      Check(body.Content.Count(c => c == '{') == 1 && body.Content.Count(c => c == '}') == 1,
         "The enhanced assistance creates exactly one complete dialogue scaffold.");
      var content = command.StartsWith("move.") ? "walk_up" : "Hello!";
      window.KeyTextInput(content); Pump();
      Check(body.Content.Contains("{\n" + content + "\n}"), "Typing immediately after assistance enters the resource body.");
      f.Dispatch.Flush(); Pump(); Check(!body.HasError, "The existing auto-pointer form still compiles.");
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
