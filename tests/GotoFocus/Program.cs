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

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var fs = new StubFileSystem { MetadataFor = _ => Array.Empty<string>() };
Application.Current.Resources["FileSystem"] = fs;
var editor = new EditorViewModel(fs, InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
var view = new ViewPort(new LoadedFile("synthetic.gba", new byte[512]));
editor.Add(view);
editor.GotoViewModel.ControlVisible = false;
var window = new MainWindow { Width = 1200, Height = 800, FileSystem = fs, Editor = editor };
window.Resources["FileSystem"] = fs;
window.Show();
void Pump() {
   Dispatcher.UIThread.RunJobs();
   window.Measure(new Size(1200, 800)); window.Arrange(new Rect(0, 0, 1200, 800));
   AvaloniaHeadlessPlatform.ForceRenderTimerTick();
   Dispatcher.UIThread.RunJobs();
}
int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
var modifier = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;
void GoTo() { window.KeyPressQwerty(PhysicalKey.G, modifier); window.KeyReleaseQwerty(PhysicalKey.G, modifier); Pump(); }
Pump(); editor.GotoViewModel.ControlVisible = false; Pump();
var original = view.Model.RawData.ToArray();
// Delay presentation explicitly: the native dispatcher need not attach a newly
// created AngleTextBox editor before the queued focus work runs.
var angle = window.FindControl<AngleTextBox>("GotoBox");
var host = (Grid)angle.Parent;
var position = host.Children.IndexOf(angle);
host.Children.Remove(angle);
GoTo();
Check(editor.GotoViewModel.ControlVisible, "Command-G opens Go To.");
host.Children.Insert(position, angle); Pump();
var field = angle.GetTextBox();
Check(field.IsFocused, "Command-G must focus the actual text box on its first creation, without mouse hover.");
window.KeyTextInput("100"); Pump();
Check(editor.GotoViewModel.Text == "100", "Immediate typing reaches Go To rather than the hex editor.");
Check(view.Model.RawData.SequenceEqual(original), "Opening/typing in Go To leaves ROM bytes unchanged.");
var alternative = new TextBox { Text = "another field" };
host.Children.Add(alternative); Pump();
Check(alternative.Focus(), "Another text field can own keyboard focus.");
editor.GotoViewModel.Text = "previous"; Pump();
alternative.Focus(); GoTo();
Check(field.IsFocused && field.SelectedText == "previous", "Command-G refocuses Go To and selects its existing query for replacement.");
window.KeyTextInput("200"); Pump();
Check(editor.GotoViewModel.Text == "200", "Typing replaces the previous query.");
window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None); Pump();
Check(!editor.GotoViewModel.ControlVisible, "Escape closes Go To.");
// Queue the next input at input priority rather than waiting for background work.
editor.GotoViewModel.Text = "";
window.KeyPressQwerty(PhysicalKey.G, modifier); window.KeyReleaseQwerty(PhysicalKey.G, modifier);
Dispatcher.UIThread.Post(() => window.KeyTextInput("300"), DispatcherPriority.Input); Pump();
Check(field.IsFocused && editor.GotoViewModel.Text == "300", "Go To takes focus before the next queued text input.");
editor.GotoViewModel.ControlVisible = false; Pump();
// Closing before deferred attachment must cancel the request rather than steal focus later.
host.Children.Remove(angle);
GoTo(); editor.GotoViewModel.ControlVisible = false; Pump();
host.Children.Insert(position, angle); Pump();
Check(!field.IsFocused && !editor.GotoViewModel.ControlVisible, "Attaching after cancellation cannot steal focus into a closed Go To panel.");
GoTo();
Check(field.IsFocused, "Go To remains focusable after a cancelled delayed request.");
Check(view.Model.RawData.SequenceEqual(original) && !view.Undo.CanExecute(null), "All focus, query and cancellation checks preserve bytes and history.");
window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
