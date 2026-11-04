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
void Close(MainWindow window) { window.Editor = null; window.Close(); Dispatcher.UIThread.RunJobs(); }
void Key(MainWindow window, PhysicalKey key, bool shift = false) {
   var modifiers = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;
   if (shift) modifiers |= RawInputModifiers.Shift;
   window.KeyPressQwerty(key, modifiers); window.KeyReleaseQwerty(key, RawInputModifiers.None); Pump(window);
}
NativeMenuItem Menu(MainWindow window, string header) => NativeMenu.GetMenu(window).Items.OfType<NativeMenuItem>()
   .Single(i => i.Header == "Edit").Menu!.Items.OfType<NativeMenuItem>().Single(i => i.Header == header);
void EditMenu(MainWindow window, string header) { var command = Menu(window, header).Command!; command.Execute(null); Pump(window); }
void Clipboard(MainWindow window, string text) => UiSync.Run(() => window.Clipboard!.SetTextAsync(text));
string ReadClipboard(MainWindow window) => UiSync.Run(() => window.Clipboard!.GetTextAsync())!;
void Until(Func<bool> condition) => UiSync.Run(async () => {
   var timeout = System.Diagnostics.Stopwatch.StartNew();
   while (!condition()) {
      if (timeout.Elapsed > TimeSpan.FromSeconds(3)) throw new InvalidOperationException("Clipboard operation did not complete.");
      await Task.Delay(1);
   }
});

// Both actual routed shortcuts and the command installed on the native menu use the real
// TextBox clipboard, backed by Avalonia.Headless's clipboard rather than the user's clipboard.
foreach (var native in new[] { false, true }) foreach (var separator in new[] { "\n", "\r\n" }) using (var f = new WorkflowFixture()) {
   var window = Open(f.View, f.FileSystem);
   f.View.Tools.SelectedIndex = Enumerable.Range(0, f.View.Tools.Count).First(i => ReferenceEquals(f.View.Tools[i], f.Tool)); Pump(window);
   TextBox Box() => window.GetVisualDescendants().OfType<TextEditor>().Single(t => ReferenceEquals(t.DataContext, f.Body.Editor)).EditBox;
   var box = Box(); var original = f.State(); var source = box.Text!;
   void Do(PhysicalKey key, string header) { if (native) EditMenu(window, header); else Key(window, key); }
   ((IFileSystem)f.FileSystem).CopyText = "AA BB"; Clipboard(window, "AA BB");
   var start = source.IndexOf("lock", StringComparison.Ordinal);
   box.Focus(); box.SelectionStart = start; box.SelectionEnd = start + 4;
   Do(PhysicalKey.C, "Copy");
   Until(() => ReadClipboard(window) == "lock");
   Check(ReadClipboard(window) == "lock" && original.EqualsModel(f), "Copy uses the source selection and preserves ROM/metadata.");
   Check(((IFileSystem)f.FileSystem).CopyText == "AA BB", "Text copy cannot invoke the binary copy command.");
   box.SelectionStart = start + 4; box.SelectionEnd = start; Clipboard(window, "AA BB");
   Do(PhysicalKey.V, "Paste / Replace");
   Until(() => box.Text == source[..start] + "AA BB" + source[(start + 4)..]);
   Check(box.Text == source[..start] + "AA BB" + source[(start + 4)..] && box.IsFocused && ReferenceEquals(Box(), box), "Paste replaces a backward source selection and retains focus/editor identity.");
   Check(original.EqualsModel(f), "Before compilation, raw hex clipboard text cannot write ROM bytes or metadata.");
   f.Dispatch.Flush(); Pump(window);
   Check(original.EqualsModel(f) && f.Body.HasError && f.Body.Editor.ErrorLocations.Count > 0, "Rejected pasted source retains diagnostics without model writes.");
   f.View.Save.Execute(f.FileSystem); f.View.SaveAs.Execute(f.FileSystem);
   Check(f.Saves == 0, "Rejected pasted source blocks Save and Save As.");
   box = Box(); box.Focus(); Do(PhysicalKey.A, "Select All");
   Check(Math.Abs(box.SelectionEnd - box.SelectionStart) == box.Text!.Length && box.IsFocused, "Select All targets the source textbox.");
   var corrected = WorkflowFixture.Source(1, 2, "Pasted dialogue.").Replace("\n", separator);
   Clipboard(window, corrected); Do(PhysicalKey.V, "Paste / Replace");
   Until(() => box.Text!.Replace("\r\n", "\n") == corrected.Replace("\r\n", "\n"));
   Check(box.Text!.Replace("\r\n", "\n") == corrected.Replace("\r\n", "\n"), "Multiline paste preserves script content.");
   f.Dispatch.Flush(); f.View.ChangeHistory.ChangeCompleted(); Pump(window);
   Check(!f.Body.HasError && !f.Tool.HasUncompiledScriptChanges && !original.EqualsModel(f), "Corrected pasted source compiles through the normal pipeline.");
   var compiled = f.State();
   Key(window, PhysicalKey.Z); f.Dispatch.Flush(); Pump(window);
   Check(original.EqualsModel(f), "Undo restores all script/resource bytes and metadata.");
   Key(window, PhysicalKey.Z, shift: true); f.Dispatch.Flush(); Pump(window);
   Check(compiled.EqualsModel(f), "Redo restores the compiled paste.");
   box = Box(); box.Focus(); start = box.Text!.IndexOf("lock", StringComparison.Ordinal); box.SelectionStart = start + 4; box.SelectionEnd = start;
   Do(PhysicalKey.X, "Cut");
   Until(() => ReadClipboard(window) == "lock" && !box.Text!.Contains("lock"));
   Check(ReadClipboard(window) == "lock" && !box.Text!.Contains("lock") && box.IsFocused, "Cut removes only selected source and retains focus.");
   Check(compiled.EqualsModel(f), "Source cut cannot immediately invoke ROM cut.");
   f.Dispatch.Clear();
   box.IsReadOnly = true; var readOnly = box.Text; Clipboard(window, "AA BB");
   Do(PhysicalKey.V, "Paste / Replace"); Do(PhysicalKey.X, "Cut"); EditMenu(window, "Delete");
   Check(box.Text == readOnly && compiled.EqualsModel(f), "Read-only text operations never fall through to ROM edits.");
   box.IsReadOnly = false;
   // Legacy paste and Deep Copy are also guarded when editing source.
   box.SelectAll(); Clipboard(window, "AA BB"); Key(window, PhysicalKey.B);
   Until(() => box.Text == "AA BB");
   Check(box.Text == "AA BB" && compiled.EqualsModel(f), "Legacy Cmd+B pastes source rather than ROM bytes.");
   box.SelectAll(); Key(window, PhysicalKey.C, shift: true);
   Until(() => ReadClipboard(window) == "AA BB");
   Check(ReadClipboard(window) == "AA BB" && compiled.EqualsModel(f), "Deep Copy in text copies text without changing ROM metadata.");
   f.Dispatch.Clear(); Close(window);
}

// Actual dialogue/name tool and wild encounter stream templates hosted in the full MainWindow.
foreach (var kind in new[] { "dialogue", "map-name", "grass", "surf", "tree", "fish" }) {
   var services = new Singletons(); var fs = new StubFileSystem();
   var bytes = Enumerable.Repeat((byte)0xFF, 0x4000).ToArray();
   bool names = kind is "dialogue" or "map-name";
   var count = kind == "grass" ? 12 : kind == "fish" ? 10 : 5;
   if (!names) {
      BitConverter.GetBytes(20).CopyTo(bytes, 0x100); BitConverter.GetBytes(0x08000200).CopyTo(bytes, 0x104);
      for (int i = 0; i < count; i++) new byte[] { 2, 3, 0, 0 }.CopyTo(bytes, 0x200 + i * 4);
      PCSString.Convert("\"RATTATA\"").CopyTo(bytes, 0x400);
   }
   var metadata = names ? null : new StoredMetadata(anchors: new[] {
      new StoredAnchor(0x400, "species", "[name\"\"12]1"),
      new StoredAnchor(0x100, "wild", $"[rate:: list<[lowLevel. highLevel. species:species]{count}>]1"),
   });
   var view = new ViewPort("synthetic.gba", new PokemonModel(bytes, metadata, services), InstantDispatch.Instance, services, fs: fs);
   view.InitializationWorkload.GetAwaiter().GetResult();
   if (names) view.Edit(kind == "dialogue" ? "@000100 ^name\"\" \"PALLET TOWN\" " : "@000100 ^names[name\"\"16]2 \"PALLET TOWN\" @000110 \"ROUTE ONE\" ");
   view.ChangeHistory.ClearHistory(false); view.Goto.Execute(0x100);
   if (names) { view.Tools.StringTool.Address = 0x100; view.Tools.SelectedIndex = Enumerable.Range(0, view.Tools.Count).First(i => ReferenceEquals(view.Tools[i], view.Tools.StringTool)); }
   var window = Open(view, fs);
   if (names) { view.Tools.StringTool.Address = 0x100; view.Tools.SelectedIndex = Enumerable.Range(0, view.Tools.Count).First(i => ReferenceEquals(view.Tools[i], view.Tools.StringTool)); Pump(window); }
   var box = window.GetVisualDescendants().OfType<TextBox>().First(b => names ? b.Name == "StringToolTextBox" : b.Name == "StreamTextBox" && b.DataContext is TextStreamElementViewModel);
   var before = view.Model.RawData.ToArray(); var text = box.Text!;
   box.Focus(); box.SelectionStart = 0; box.SelectionEnd = Math.Min(1, text.Length); Key(window, PhysicalKey.C);
   Until(() => ReadClipboard(window) == text[..1]);
   Check(ReadClipboard(window) == text[..1] && before.SequenceEqual(view.Model.RawData), kind + " copies selected text without binary commands.");
   int pastes = 0; box.PastingFromClipboard += (_, _) => pastes++;
   Clipboard(window, text[..1]); Key(window, PhysicalKey.V); Until(() => pastes == 1);
   Check(box.IsFocused && box.Text == text && before.SequenceEqual(view.Model.RawData), kind + " paste remains in the field and preserves equivalent data.");
   Close(window);
}

// Genuine hex focus still uses the binary clipboard and existing history behavior.
using (var f = new WorkflowFixture()) {
   var window = Open(f.View, f.FileSystem); var before = f.State();
   var hex = window.GetVisualDescendants().OfType<HexContent>().First(); hex.Focus();
   ((IFileSystem)f.FileSystem).CopyText = "AA BB"; EditMenu(window, "Paste / Replace");
   Check(f.Model[WorkflowFixture.Root] == 0xAA && f.Model[WorkflowFixture.Root + 1] == 0xBB, "Hex focus retains binary paste.");
   f.View.ChangeHistory.ChangeCompleted(); f.View.Undo.Execute(null); f.Dispatch.Flush();
   Check(before.EqualsModel(f), "Binary paste remains undoable."); Close(window);
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
