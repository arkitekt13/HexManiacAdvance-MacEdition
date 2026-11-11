using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Review;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
void Pump(MainWindow window) {
   Dispatcher.UIThread.RunJobs(); window.Measure(new Size(1400, 1000)); window.Arrange(new Rect(0, 0, 1400, 1000));
   AvaloniaHeadlessPlatform.ForceRenderTimerTick();
}
MainWindow Open(ViewPort view, IFileSystem fs) {
   Application.Current!.Resources["FileSystem"] = fs;
   var editor = new EditorViewModel(fs, InstantDispatch.Instance);
   App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
   var window = new MainWindow { Width = 1400, Height = 1000, FileSystem = fs, Editor = editor };
   window.Resources["FileSystem"] = fs; editor.Add(view); window.Show();
   editor.GotoViewModel.ControlVisible = false; Pump(window); return window;
}


const string dialogue = "Have you heard about BILL?\n\nEveryone calls him a POKéMANIAC!\n\nI think people are just jealous of\nBILL, though.\n\nWho wouldn't want to boast about\ntheir POKéMON?";
foreach (var reverse in new[] { false, true })
foreach (var edges in new[] { false, true })
foreach (var action in new[] { "backspace", "delete", "replace" }) {
   using var f = new WorkflowFixture();
   f.Edit($"section0: # {WorkflowFixture.Root:X6}\n  msgbox.npc <{WorkflowFixture.Text:X6}>\n{{\n{dialogue}\n}}\n  end");
   f.View.ChangeHistory.ClearHistory(false); f.View.ChangeHistory.TagAsSaved();
   var original = f.State();
   var disk = File.ReadAllBytes(f.FileName);
   var window = Open(f.View, f.FileSystem);
   f.View.Tools.SelectedIndex = 3; Pump(window);
   var control = window.GetVisualDescendants().OfType<TextEditor>().Single(t => ReferenceEquals(t.DataContext, f.Body.Editor));
   var box = control.EditBox;
   var source = box.Text;
   var start = source.IndexOf('{') + (edges ? 1 : 2);
   var end = source.IndexOf('}') - (edges ? 0 : 1);
   box.Focus(); box.SelectionStart = reverse ? end : start; box.SelectionEnd = reverse ? start : end;
   Check(box.SelectedText.Contains("Have you heard"), "The actual input holds the intended dialogue selection.");
   if (action == "replace") window.KeyTextInput("Replacement dialogue.");
   else {
      var key = action == "backspace" ? PhysicalKey.Backspace : PhysicalKey.Delete;
      window.KeyPressQwerty(key, RawInputModifiers.None);
      window.KeyReleaseQwerty(key, RawInputModifiers.None);
   }
   Pump(window);
   var expected = source[..start] + (action == "replace" ? "Replacement dialogue." : "") + source[end..];
   Check(box.Text == expected, $"{action} changes only the selection, reverse={reverse}, edges={edges}.");
   Check(box.IsFocused, "Editing retains keyboard focus.");
   Check(original.EqualsModel(f), "Typing/deletion is still deferred rather than immediately changing ROM data.");
   f.Dispatch.Flush(); f.View.ChangeHistory.ChangeCompleted(); Pump(window);
   Check(!box.Text.Contains("Have you heard") && !f.Tool.HasUncompiledScriptChanges, "Compilation retains the deletion/replacement and completes successfully.");
   var after = f.State();
   Check(!original.EqualsModel(f), "Successful compilation publishes the dialogue change.");
   f.View.Undo.Execute(null); f.Dispatch.Flush(); Pump(window);
   Check(original.EqualsModel(f), "One Undo restores original ROM bytes and metadata.");
   f.View.Redo.Execute(null); f.Dispatch.Flush(); Pump(window);
   Check(after.EqualsModel(f), "Redo restores the compiled dialogue change.");
   Check(f.Saves == 0 && File.ReadAllBytes(f.FileName).SequenceEqual(disk), "Source editing does not save a project file.");
   // Explicit model-driven caret requests still work outside a selection notification.
   control.ViewModel.PushCaretUpdate(2); Pump(window);
   Check(box.CaretIndex == 2, "Programmatic assistance can still position the caret.");
   var readOnly = box.Text; box.IsReadOnly = true; box.SelectionStart = readOnly.Length; box.SelectionEnd = 0;
   window.KeyPressQwerty(PhysicalKey.Backspace, RawInputModifiers.None);
   window.KeyReleaseQwerty(PhysicalKey.Backspace, RawInputModifiers.None); Pump(window);
   Check(box.Text == readOnly && after.EqualsModel(f), "Read-only source cannot be deleted even with a backward selection.");
   window.Editor = null; window.Close(); Dispatcher.UIThread.RunJobs();
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
