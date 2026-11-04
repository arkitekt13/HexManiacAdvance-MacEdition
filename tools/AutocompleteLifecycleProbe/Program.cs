using System.Text;
using System.Text.Json;
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

// Characterization only: captures popup lifecycle behavior without modifying the application.
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
var window = new Window { Width = 1000, Height = 750, Content = root }; window.Show();
void Layout() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
var rows = new List<object>(); int controls = 0;
var original = model.RawData.ToArray(); var metadata = model.ExportMetadata(null, singletons.MetadataInfo).Serialize();
void Capture(string scenario) {
   Layout();
   rows.Add(new { scenario, focused = control.EditBox.IsFocused, overlayVisible = overlay.IsVisible,
      effectiveVisible = overlay.IsEffectivelyVisible, popupOpen = overlay.FindControl<Popup>("Popup")!.IsOpen,
      options = (overlay.FindControl<ItemsControl>("AutocompleteItems")!.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().Count() ?? 0 });
}
void Offer() { control.EditBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent)); Layout(); }
void Prepare() {
   holder.IsVisible = true; overlay.Target = null; control.DataContext = body.Editor; overlay.DataContext = body;
   body.Content = "msgb"; Layout(); control.EditBox.CaretIndex = 4; overlay.Target = control;
   control.EditBox.Focus(); Layout(); Offer();
   if (!overlay.IsVisible || !overlay.FindControl<Popup>("Popup")!.IsOpen) throw new InvalidOperationException("Focused completion positive control failed.");
   controls++;
}
Prepare(); Capture("focused-positive-control");
other.Focus(); Capture("ordinary-focus-loss");
// A real binding update queued by document refresh, after focus has moved elsewhere.
body.Content = "msgbo"; control.EditBox.CaretIndex = 5; Capture("unfocused-bound-source-update");
Prepare(); overlay.DataContext = Body(); Capture("overlay-context-replaced-with-empty-body");
Prepare(); control.DataContext = Body().Editor; Capture("target-editor-context-replaced");
Prepare(); overlay.Target = new TextBox { Text = string.Empty }; Capture("target-replaced");
Prepare(); holder.IsVisible = false; Capture("editor-parent-hidden");
holder.IsVisible = true; Capture("editor-parent-restored-without-typing");
holder.IsVisible = true; Prepare(); root.Children.Remove(holder); Capture("editor-and-overlay-detached");
root.Children.Add(holder); Layout(); Capture("reattached-without-typing");
overlay.ClearAutocompleteOptions(); window.Close(); Layout();
if (!original.SequenceEqual(model.RawData) || !metadata.SequenceEqual(model.ExportMetadata(null, singletons.MetadataInfo).Serialize()))
   throw new InvalidOperationException("Popup probe changed synthetic ROM bytes or metadata.");
Console.WriteLine(JsonSerializer.Serialize(new { positiveControls = controls, bytesAndMetadataPreserved = true, rows }, new JsonSerializerOptions { WriteIndented = true }));
