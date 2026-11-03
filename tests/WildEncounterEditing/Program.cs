using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
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
var formats = new[] {
   (Kind: "grass", Count: 12, Format: "[basic|comment=0|20% common|comment=2|10% uncommon|comment=6|5% rare|comment=8|4% mythic|comment=10|1% lowLevel. highLevel. species:species]12"),
   (Kind: "surf", Count: 5, Format: "[lowLevel. highLevel. species:species]5"),
   (Kind: "tree", Count: 5, Format: "[lowLevel. highLevel. species:species]5"),
   (Kind: "fish", Count: 10, Format: "[old|comment=0|old_rod: good|comment=2|good_rod: super|comment=5|super_rod: lowLevel. highLevel. species:species]10"),
};
foreach (var format in formats) foreach (var separator in new[] { "\n", "\r\n" }) {
   var bytes = Enumerable.Repeat((byte)0xFF, 0x4000).ToArray();
   BitConverter.GetBytes(20).CopyTo(bytes, 0x100); BitConverter.GetBytes(0x08000200).CopyTo(bytes, 0x104);
   for (int i = 0; i < format.Count; i++) new byte[] { 2, 3, 0, 0 }.CopyTo(bytes, 0x200 + i * 4);
   var names = new[] { "RATTATA", "RATICATE", "PIKACHU" };
   for (int i = 0; i < names.Length; i++) PCSString.Convert("\"" + names[i] + "\"").CopyTo(bytes, 0x400 + i * 12);
   var services = new Singletons();
   var model = new PokemonModel(bytes, new StoredMetadata(anchors: new[] {
      new StoredAnchor(0x400, "species", "[name\"\"12]3"),
      new StoredAnchor(0x100, "wild", $"[rate:: list<{format.Format}>]1"),
   }), services);
   var view = new ViewPort("synthetic.gba", model, InstantDispatch.Instance, services, fs: fs);
   view.InitializationWorkload.GetAwaiter().GetResult(); view.Goto.Execute(0x100);
   var tab = new TabView { DataContext = view };
   var window = new Window { Width = 1440, Height = 1000, Content = tab }; window.Show();
   void Pump() {
      Dispatcher.UIThread.RunJobs(); window.Measure(new Size(1440, 1000)); window.Arrange(new Rect(0, 0, 1440, 1000));
      AvaloniaHeadlessPlatform.ForceRenderTimerTick();
   }
   TextBox Box() => tab.GetVisualDescendants().OfType<TextBox>().Single(b => b.Name == "StreamTextBox" && b.DataContext is TextStreamElementViewModel);
   Pump(); var box = Box(); var stream = (TextStreamElementViewModel)box.DataContext!;
   view.ChangeHistory.ClearHistory(false); view.ChangeHistory.TagAsSaved(); model.ResetChanges();
   var baseline = model.RawData.ToArray();
   var metadata = model.ExportMetadata(null, services.MetadataInfo).Serialize();
   int redundantReplacements = 0;
   view.Tools.TableTool.Groups.CollectionChanged += (_, e) => {
      if (e.OldItems?.Count == 1 && e.NewItems?.Count == 1 && ReferenceEquals(e.OldItems[0], e.NewItems[0])) redundantReplacements++;
   };
   int firstSlot = box.Text!.IndexOf("2, 3,", StringComparison.Ordinal);
   box.Focus(); box.SelectionStart = firstSlot; box.SelectionEnd = firstSlot + 1;
   window.KeyTextInput("4"); Pump();
   Check(ReferenceEquals(box, Box()) && box.IsFocused, "Typing a level must preserve the live text box and keyboard focus.");
   Check(box.CaretIndex == firstSlot + 1 && model[0x200] == 4, "The level edit updates its byte and retains the caret.");
   Check(redundantReplacements == 0, "Refreshing a retained stream group must not emit a same-object Replace.");

   // Prepare a partial species field, then deliver real input one character at a time.
   var prepared = box.Text!.Replace("\r\n", "\n").Replace("\n", separator);
   int speciesStart = prepared.IndexOf("RATTATA", StringComparison.Ordinal);
   box.Text = prepared.Remove(speciesStart, "RATTATA".Length); box.CaretIndex = speciesStart; Pump();
   foreach (var character in "RAT") {
      var caret = box.CaretIndex; window.KeyTextInput(character.ToString()); Pump();
      Check(ReferenceEquals(box, Box()) && box.IsFocused && box.CaretIndex == caret + 1,
         "Every species-prefix keystroke preserves focus, editor identity and caret.");
   }
   var overlay = tab.GetVisualDescendants().OfType<AutocompleteOverlay>().Single(o => ReferenceEquals(o.Target, box));
   var items = overlay.FindControl<ItemsControl>("AutocompleteItems")!;
   var choices = ((IEnumerable<AutoCompleteSelectionItem>)items.ItemsSource!).ToArray();
   Check(overlay.IsVisible && choices.Any(c => c.DisplayText.Contains("RATICATE")), "Partial species names must display matching suggestions.");
   var choice = choices.Single(c => c.DisplayText.Contains("RATICATE"));
   if (separator == "\n") {
      var button = items.GetVisualDescendants().OfType<Button>().Single(b => ReferenceEquals(b.DataContext, choice));
      button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
   } else {
      for (int i = 0; i < Array.IndexOf(choices, choice); i++)
         box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
      var enter = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter };
      box.RaiseEvent(enter);
      Check(enter.Handled, "Enter accepts the keyboard-selected species suggestion.");
   }
   Pump();
   Check(box.IsFocused && ReferenceEquals(box, Box()) && box.Text!.Contains("RATICATE"), "Accepting a suggestion preserves the editor and applies its species name.");
   Check(model.ReadMultiByteValue(0x202, 2) == 1 && model.RawData.Skip(0x204).Take(4).SequenceEqual(baseline.Skip(0x204).Take(4)),
      "Acceptance writes the selected species ID without changing the other encounter slot.");
   Check(model.ReadPointer(0x104) == 0x200 && model.ReadMultiByteValue(0x100, 4) == 20, "Typing preserves encounter rate and list pointer.");
   Check(metadata.SequenceEqual(model.ExportMetadata(null, services.MetadataInfo).Serialize()), "In-place encounter edits preserve metadata.");
   Check(Enumerable.Range(0, model.Count).All(i => i >= 0x200 && i < 0x204 || model[i] == baseline[i]), "Only the edited encounter slot may change.");
   view.ChangeHistory.ChangeCompleted(); var edited = model.RawData.ToArray();
   view.Undo.Execute(null); Pump();
   Check(baseline.SequenceEqual(model.RawData), "Undo restores the original encounter bytes.");
   view.Redo.Execute(null); Pump();
   Check(edited.SequenceEqual(model.RawData), "Redo restores the edited encounter bytes.");
   view.Goto.Execute(0x400); Pump(); view.Goto.Execute(0x100); Pump();
   Check(Box().Text!.Contains("RATICATE"), "Navigation refreshes the stream from the edited model.");
   Console.WriteLine($"PASS: {format.Kind}, {format.Count} slots, {(separator.Length == 1 ? "LF" : "CRLF")}, focus/caret, mouse/Enter suggestions, bytes/metadata and history.");
   window.Close(); Pump();
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
