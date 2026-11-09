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
var window = new MainWindow { Width = 1200, Height = 800, FileSystem = fs, Editor = editor };
window.Resources["FileSystem"] = fs;
window.Show();
// Match normal startup: the window exists before the first ROM is opened.
editor.Add(view);
editor.GotoViewModel.ControlVisible=false;
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
// Attached controls can still be hidden when the native overlay begins opening.
editor.GotoViewModel.ControlVisible=false;Pump();
var panel=window.FindControl<ScrollViewer>("GotoPanel");
editor.GotoViewModel.ShowGoto.Execute(true);
panel.IsVisible=false;window.FocusManager.ClearFocus();Pump();
Check(!field.IsFocused&&!field.IsEffectivelyVisible,"Hidden overlay test starts without input focus.");
panel.IsVisible=true;Pump();
Check(field.IsFocused,"Go To finishes focusing after an already attached panel becomes visible.");
window.KeyTextInput("400");Pump();
Check(editor.GotoViewModel.Text=="400","Typing after delayed visibility reaches the Go To query.");
editor.GotoViewModel.ControlVisible=false;Pump();
editor.GotoViewModel.ShowGoto.Execute(true);panel.IsVisible=false;window.FocusManager.ClearFocus();Pump();
editor.GotoViewModel.ControlVisible=false;panel.IsVisible=true;Pump();
Check(!field.IsFocused,"A closed Go To panel cancels pending visibility focus.");
Check(view.Model.RawData.SequenceEqual(original)&&!view.Undo.CanExecute(null),"Delayed visibility and cancellation preserve ROM and history.");
// Switching tabs replaces GotoViewModel; native menu and shortcut must follow it.
editor.GotoViewModel.ControlVisible=false;Pump();
var retired=editor.GotoViewModel;
var second=new ViewPort(new LoadedFile("second-synthetic.gba",new byte[512]));
var secondBytes=second.Model.RawData.ToArray();editor.Add(second);Pump();
Check(!ReferenceEquals(retired,editor.GotoViewModel),"Opening a second tab really replaces the Go To model.");
GoTo();field=angle.GetTextBox();
Check(field.IsFocused,"Command-G focuses Go To after switching to a newly opened tab.");
window.KeyTextInput("120");Pump();
Check(editor.GotoViewModel.Text=="120","The new tab's Go To query receives immediate replacement typing.");
editor.GotoViewModel.ControlVisible=false;Pump();window.FocusManager.ClearFocus();
retired.ControlVisible=true;Pump();
Check(!field.IsFocused&&!editor.GotoViewModel.ControlVisible,"A retired model cannot reopen or refocus current Go To.");
retired.ControlVisible=false;editor.SelectedIndex=0;Pump();
editor.GotoViewModel.ShowGoto.Execute(true);Pump();field=angle.GetTextBox();
Check(field.IsFocused,"Native menu's bound command focuses Go To after returning to an existing tab.");
window.KeyTextInput("140");Pump();
Check(editor.GotoViewModel.Text=="140","Typing after native-command entry replaces the previous tab query.");
// Replace a model while its input-attachment focus request is still queued.
editor.GotoViewModel.ControlVisible=false;Pump();host.Children.Remove(angle);
var obsolete=editor.GotoViewModel;obsolete.ShowGoto.Execute(true);Pump();
editor.SelectedIndex=1;Pump();window.FocusManager.ClearFocus();host.Children.Insert(position,angle);Pump();
Check(!angle.GetTextBox().IsFocused,"Model replacement cancels a previous tab's deferred attachment focus.");
GoTo();field=angle.GetTextBox();Check(field.IsFocused,"The replacement model remains focusable after cancellation.");
Check(view.Model.RawData.SequenceEqual(original)&&second.Model.RawData.SequenceEqual(secondBytes)&&!view.Undo.CanExecute(null)&&!second.Undo.CanExecute(null),"Startup, tab switching and retired-model checks leave both ROMs and histories unchanged.");
window.Close();
window.FocusManager?.ClearFocus();editor.GotoViewModel.ControlVisible=true;Pump();
Check(!field.IsFocused,"Closing the window detaches its Go To focus subscription.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
