using System.Text;
using System.Reflection;
using Avalonia.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

// Real-control lifetime regressions; fabricated source only.
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var fs = new StubFileSystem(); var editor = new EditorViewModel(fs, InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current!.Resources, editor.Theme);
Application.Current.Resources["FileSystem"] = fs;
var singletons = new Singletons(); var bytes = Enumerable.Repeat((byte)0xFF, 0x10000).ToArray();
Encoding.ASCII.GetBytes("BPRE").CopyTo(bytes, 0xAC); bytes[0xBC] = 0;
var model = new PokemonModel(bytes, singletons: singletons); model.LoadMetadata(BaseModel.GetDefaultMetadatas().First());
var parser = new ScriptParser(model.GetShortGameCode(), singletons.ScriptLines, 2);
CodeBody Body() { var body = new CodeBody(model, parser, null); parser.AddKeywords(model, body); return body; }
var body = Body(); var control = new TextEditor { DataContext = body.Editor, FontSize = 14 };
var overlay = new AutocompleteOverlay { DataContext = body, Target = control };
var other = new Button { Content = "Other control", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom };
var holder = new Grid { Children = { control, overlay } };
var root = new Grid { Children = { holder, other } };
var outside = new Button { Content = "Non-focusable action", Focusable = false, ZIndex = 1, Width = 180, Height = 40,
   HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom };
root.Children.Add(outside); int outsideClicks = 0; outside.Click += (_, _) => outsideClicks++;
var window = new Window { Width = 1000, Height = 750, Content = root }; window.Show();
void Layout() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
var original = model.RawData.ToArray(); var metadata = model.ExportMetadata(null, singletons.MetadataInfo).Serialize();
void Dismissed(string scenario) {
   Layout();
   Check(!overlay.IsVisible, scenario + ": overlay must be dismissed.");
   Check(!overlay.FindControl<Popup>("Popup")!.IsOpen, scenario + ": native popup state must close.");
   Check(overlay.FindControl<ItemsControl>("AutocompleteItems")!.ItemsSource is not System.Collections.IEnumerable items || !items.Cast<object>().Any(), scenario + ": old options must be discarded.");
}
void Offer() { control.EditBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent)); Layout(); }
void Prepare() {
   window.Activate();
   holder.IsVisible = true; overlay.Target = null; control.DataContext = body.Editor; overlay.DataContext = body;
   body.Content = "msgb"; Layout(); control.EditBox.CaretIndex = 4; overlay.Target = control;
   control.EditBox.Focus(); Layout(); Offer();
   if (!overlay.IsVisible || !overlay.FindControl<Popup>("Popup")!.IsOpen) throw new InvalidOperationException($"Focused completion positive control failed: focus={control.EditBox.IsFocused}, active={window.IsActive}, visible={overlay.IsVisible}, open={overlay.FindControl<Popup>("Popup")!.IsOpen}.");
   Check(control.EditBox.IsFocused && overlay.IsVisible && overlay.FindControl<Popup>("Popup")!.IsOpen, "Focused typing still opens suggestions.");
}
Prepare(); other.Focus(); Dismissed("Ordinary focus loss");
body.Content = "msgbo"; control.EditBox.CaretIndex = 5; Dismissed("Unfocused bound source update");
Prepare(); overlay.DataContext = Body(); Dismissed("Overlay context replaced with empty body");
Prepare(); overlay.DataContext = new object(); Dismissed("Unsupported overlay context");
Prepare(); control.DataContext = Body().Editor; Dismissed("Target editor context replaced");
Prepare(); overlay.Target = new TextBox(); Dismissed("Target replaced");
Prepare(); overlay.Target = null; Dismissed("Target removed");
Prepare(); holder.IsVisible = false; Dismissed("Editor parent hidden");
body.Content = "msgbo"; control.EditBox.CaretIndex = 5; Dismissed("Hidden bound source update");
holder.IsVisible = true; Dismissed("Parent restored without typing");
Prepare(); control.IsVisible = false; Dismissed("Target hidden"); control.IsVisible = true;
Prepare(); holder.IsEnabled = false; Dismissed("Editor parent disabled"); holder.IsEnabled = true;
Prepare();
// Exercise Avalonia's native deactivation callback without claiming OS event delivery.
typeof(WindowBase).GetMethod("HandleDeactivated", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
Dismissed("Window deactivated");
body.Content = "msgbo"; control.EditBox.CaretIndex = 5; Dismissed("Inactive window source update");
window.Activate(); Dismissed("Window activated without typing");
Prepare(); root.Children.Remove(holder); Dismissed("Editor and overlay detached");
body.Content = "msgbo"; control.EditBox.CaretIndex = 5; Dismissed("Detached source updated");
root.Children.Add(holder); Layout(); Dismissed("Reattached without typing");
Prepare(); holder.Children.Remove(control); Dismissed("Target detached while overlay stays attached");
holder.Children.Insert(0,control); Layout(); Dismissed("Target reattached without typing");
Prepare(); other.Focus(); Layout();
var before = control.EditBox.Text;
control.EditBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter }); Layout();
Check(control.EditBox.Text!.TrimEnd() == before!.TrimEnd(), "Dismissed completion must not accept stale text (ordinary Enter may add a newline).");
Prepare(); overlay.ClearAutocompleteOptions(); Offer();
Check(overlay.IsVisible, "Focused fresh text notifications remain eligible after dismissal.");
Prepare();
var outsidePoint = outside.TranslatePoint(new Avalonia.Point(outside.Bounds.Width / 2, outside.Bounds.Height / 2), window)!.Value;
window.MouseMove(outsidePoint); window.MouseDown(outsidePoint, MouseButton.Left); window.MouseUp(outsidePoint, MouseButton.Left); Layout();
Check(outsideClicks == 1, "Outside-click fixture must hit its intended action exactly once.");
Dismissed("Click outside popup on non-focusable action");
Check(outsideClicks == 1, "Dismissing suggestions must pass the click through to the intended action.");
Check(control.EditBox.IsFocused, "Outside-click dismissal cannot depend on focus loss.");
overlay.ClearAutocompleteOptions(); window.Close(); Layout();
Check(original.SequenceEqual(model.RawData), "Lifecycle changes preserve every synthetic ROM byte.");
Check(metadata.SequenceEqual(model.ExportMetadata(null, singletons.MetadataInfo).Serialize()), "Lifecycle changes preserve metadata.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
