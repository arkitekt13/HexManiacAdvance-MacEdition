using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var fs = new StubFileSystem(); Application.Current!.Resources["FileSystem"] = fs;
App.UpdateThemeDictionary(Application.Current.Resources, new EditorViewModel(fs, InstantDispatch.Instance).Theme);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
void Pump(Window window) { Dispatcher.UIThread.RunJobs(); window.Measure(new Size(window.Width, window.Height)); window.Arrange(new Rect(0, 0, window.Width, window.Height)); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
void Tab(Window window, bool reverse = false) {
   window.KeyPressQwerty(PhysicalKey.Tab, reverse ? RawInputModifiers.Shift : RawInputModifiers.None);
   window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None); Pump(window);
}
void TabTo(Window window, TextBox target, bool reverse = false) {
   for (int i = 0; i < 30; i++) { Tab(window, reverse); if (target.IsFocused) return; }
   throw new InvalidOperationException("Keyboard traversal did not reach the requested field.");
}
bool All(TextBox box) => Math.Abs(box.SelectionStart - box.SelectionEnd) == (box.Text?.Length ?? 0);
void Click(Window window, Control control, double x = 25) {
   var point = control.TranslatePoint(new Avalonia.Point(x, control.Bounds.Height / 2), window)!.Value;
   window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Pump(window);
}

// Actual multi-choice table hierarchy, not a synthetic TextBox-only imitation.
var services = new Singletons(); var bytes = Enumerable.Repeat((byte)0xFF, 0x4000).ToArray();
BitConverter.GetBytes(0x08000200).CopyTo(bytes, 0x100); BitConverter.GetBytes(3).CopyTo(bytes, 0x104);
var names = new[] { "Charmander", "Squirtle", "Bulbasaur" };
for (int i = 0; i < names.Length; i++) {
   BitConverter.GetBytes(0x08000300 + i * 0x80).CopyTo(bytes, 0x200 + i * 8);
   BitConverter.GetBytes(0).CopyTo(bytes, 0x204 + i * 8);
   PCSString.Convert("\"" + names[i] + "\"").CopyTo(bytes, 0x300 + i * 0x80);
}
var model = new PokemonModel(bytes, new StoredMetadata(anchors: new[] {
   new StoredAnchor(0x100, "scripts.text.multichoice", "[options<[text<\"\"> unused::]/count> count::]1"),
}), services);
var view = new ViewPort("synthetic-choices.gba", model, InstantDispatch.Instance, services, fs: fs);
view.InitializationWorkload.GetAwaiter().GetResult(); view.Goto.Execute(0x100);
var tab = new TabView { DataContext = view }; var window = new Window { Width = 1440, Height = 1000, Content = tab }; window.Show(); Pump(window);
TextBox Box(string text) => tab.GetVisualDescendants().OfType<TextBox>().Single(b => b.Name == "StreamTextBox" && b.Text == text);
var first = Box(names[0]); var second = Box(names[1]); var third = Box(names[2]);
view.ChangeHistory.ClearHistory(false); var before = model.RawData.ToArray();
var metadata = model.ExportMetadata(null, services.MetadataInfo).Serialize();
first.Focus(); TabTo(window, second);
Check(All(second), "Tab selects the entire next multiple-choice entry.");
TabTo(window, first, reverse: true);
Check(All(first), "Shift+Tab selects the entire previous choice.");
Check(before.SequenceEqual(model.RawData) && !view.Undo.CanExecute(null), "Keyboard traversal alone cannot edit ROM data or history.");
TabTo(window, second); window.KeyTextInput("Eevee"); Pump(window);
Check(second.Text == "Eevee" && second.IsFocused, "Typing replaces the selected choice rather than prepending.");
Check(model.ReadPointer(0x208) == 0x380 && model.ReadPointer(0x200) == 0x300 && model.ReadPointer(0x210) == 0x400, "A short replacement preserves all choice pointers.");
Check(PCSString.Convert("\"Eevee\"").SequenceEqual(model.RawData.Skip(0x380).Take(6)), "The replacement has exact encoded bytes.");
Check(before.Skip(0x300).Take(0x80).SequenceEqual(model.RawData.Skip(0x300).Take(0x80)) &&
   before.Skip(0x400).Take(0x80).SequenceEqual(model.RawData.Skip(0x400).Take(0x80)), "Other choices remain byte-for-byte unchanged.");
Click(window, third);
Check(third.IsFocused && third.SelectionStart == third.SelectionEnd, "Mouse clicks place a caret without selecting the whole entry.");
first.Focus(); second.SelectionStart = second.SelectionEnd = 2; second.Focus(); Pump(window);
Check(second.SelectionStart == second.SelectionEnd, "Programmatic focus does not force select-all.");
second.SelectionStart = second.SelectionEnd = 1; window.KeyTextInput("X"); Pump(window);
Check(second.IsFocused && second.SelectionStart == second.SelectionEnd && second.CaretIndex == 2, "Model refresh during typing does not reselect the field.");
view.ChangeHistory.ChangeCompleted(); var edited = model.RawData.ToArray();
view.Undo.Execute(null); Pump(window);
Check(before.SequenceEqual(model.RawData) && metadata.SequenceEqual(model.ExportMetadata(null, services.MetadataInfo).Serialize()), "Undo restores the original choices and metadata.");
view.Redo.Execute(null); Pump(window); Check(edited.SequenceEqual(model.RawData), "Redo restores the replacements exactly.");
first = Box(names[0]); first.Text = "First line\nSecond line"; Pump(window);
first.SelectionStart = first.SelectionEnd = 0; second.Focus(); TabTo(window, first, reverse: true);
Check(!All(first) && first.Text.Contains('\n'), "Multiline table dialogue is excluded from select-all on Tab.");
var tableBox = tab.GetVisualDescendants().OfType<TextBox>().First(b => b.Name == "StreamTextBox" &&
   b.DataContext is TextStreamElementViewModel stream && stream.Model.GetNextRun(stream.Model.ReadPointer(stream.Start)) is HavenSoft.HexManiac.Core.Models.Runs.ITableRun);
tableBox.SelectionStart = tableBox.SelectionEnd = 0; second.Focus(); TabTo(window, tableBox, reverse: true);
Check(!All(tableBox), "A whole table/list stream is excluded from short-field behavior.");
window.Close();

// Proxy-backed scalar fields must forward Tab focus to their newly created native TextBox.
var preceding = new TextBox { Text = "before" };
var scalar = new AngleTextBox { DataContext = new ValueModel(), TextBinding = new Binding("Value") { Mode = BindingMode.TwoWay } };
var following = new TextBox { Text = "after" };
var panel = new StackPanel(); panel.Children.Add(preceding); panel.Children.Add(scalar); panel.Children.Add(following);
window = new Window { Width = 500, Height = 300, Content = panel }; window.Show(); Pump(window);
preceding.Focus(); Tab(window);
var actual = scalar.Content as TextBox;
Check(actual != null && actual.IsFocused && All(actual), "Tab into an inactive AngleTextBox selects the complete scalar value.");
following.Focus(); Tab(window, reverse: true); actual = scalar.Content as TextBox;
Check(actual != null && actual.IsFocused && All(actual), "Shift+Tab also selects proxy-backed scalar values.");
window.KeyTextInput("42"); Pump(window);
Check(((ValueModel)scalar.DataContext!).Value == "42", "Typing replaces a scalar value.");
preceding.Focus(); Pump(window); Click(window, scalar); actual = scalar.Content as TextBox;
Check(actual != null && actual.IsFocused && actual.SelectionStart == actual.SelectionEnd, "Clicking a proxy-backed field still places a caret.");
var scriptVm = new TextEditorViewModel { Content = "lock\nend" };
var script = new TextEditor { DataContext = scriptVm, Height = 80 }; panel.Children.Add(script); Pump(window);
script.EditBox.Focus(NavigationMethod.Tab); Pump(window);
Check(!All(script.EditBox), "The script editor does not select its entire source on Tab focus.");
script.EditBox.CaretIndex = 0; script.EditBox.SelectionStart = script.EditBox.SelectionEnd = 0; Tab(window);
Check(script.EditBox.IsFocused && scriptVm.Content == "\tlock\nend", "Tab in the script editor keeps its normal indentation behavior.");
following.Focus(); script.DataContext = new TextEditorViewModel(false) { Content = "A whole dialogue paragraph should keep its caret." }; Pump(window);
script.EditBox.Focus(NavigationMethod.Tab); Pump(window);
Check(!All(script.EditBox), "A dedicated dialogue editor remains excluded even with one source line.");
window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class ValueModel : ViewModelCore {
   private string value = "123456";
   public string Value { get => value; set => TryUpdate(ref this.value, value); }
}
