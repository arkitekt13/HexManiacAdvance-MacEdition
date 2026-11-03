using System.Text.Json;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Review;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.ViewModels;

if (args.Length != 1) throw new ArgumentException("Expected a separate JSON report path; no ROM input.");
var output = Path.GetFullPath(args[0]);
if (!Path.GetExtension(output).Equals(".json", StringComparison.OrdinalIgnoreCase))
   throw new ArgumentException("The output must be a separate .json report, never a ROM input.");
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var rows = new List<object>();
void Review(string name, Action<WorkflowFixture, Dictionary<string, object>> action) {
   using var f = new WorkflowFixture();
   var baseline = f.State(); var row = new Dictionary<string, object> { ["case"] = name };
   try { action(f, row); }
   catch (Exception error) { row["exception"] = error.ToString(); }
   row["modelChanged"] = !baseline.Bytes.SequenceEqual(f.Model.RawData);
   row["metadataChanged"] = !baseline.Metadata.SequenceEqual(f.Metadata());
   row["changedAddresses"] = Enumerable.Range(0, baseline.Bytes.Length).Where(i => baseline.Bytes[i] != f.Model[i]).Take(24).Select(i => i.ToString("X6")).ToArray();
   row["bodyError"] = f.Tool.Contents.Any(b => b.HasError);
   row["compileEvents"] = f.CompileErrors.ToArray();
   row["errors"] = f.Tool.Contents.Where(b => b.HasError).Select(b => b.ErrorText).Concat(f.Messages).ToArray();
   row["isSaved"] = f.View.ChangeHistory.IsSaved;
   row["saves"] = f.Saves; row["prompts"] = f.Prompts;
   row["pendingCallbacks"] = f.Dispatch.Pending;
   row["source"] = f.Tool.Contents.FirstOrDefault()?.Content;
   rows.Add(row);
}
foreach (var (name, source) in new[] {
   ("missing operand", "setvar 0x4001\nend"),
   ("incomplete hexadecimal", "setvar 0x4001 0x\nend"),
   ("unknown command before dialogue", "not_a_command\n" + WorkflowFixture.Source(1, 1, "Changed dialogue.")),
   ("unknown command after dialogue", WorkflowFixture.Source(1, 1, "Changed dialogue.") + "\nnot_a_command"),
   ("same-size dialogue before error", WorkflowFixture.Source(1, 1, "Howdy!") + "\nnot_a_command"),
   ("dialogue error before terminator", WorkflowFixture.Source(1, 1, "Changed dialogue.").Replace("\nrelease\nend", "\nnot_a_command\nrelease\nend")),
   ("invalid argument after dialogue", WorkflowFixture.Source(1, 1, "Changed dialogue.") + "\nsetvar MissingAlias 1"),
   ("grown dialogue before error", WorkflowFixture.Source(1, 1, new string('A', 160)) + "\nnot_a_command"),
   ("auto stream before error", "loadpointer 0 <auto>\n{\nNew dialogue.\n}\nnot_a_command"),
   ("placeholder allocation before error", "loadpointer 0 <??????>\n{\nNew dialogue.\n}\nnot_a_command"),
   ("missing local label", "call <missing_section>\nend"),
   ("missing dialogue closer with zero caret", "loadpointer 0 <003000>\n{\nHello!"),
}) Review(name, (f, row) => {
   var baseline = f.State();
   f.Edit(source);
   row["unchangedAfterEdit"] = baseline.EqualsModel(f);
   row["dialogueAddress"] = f.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, "workflow.dialogue");
   row["sourceHasCompileError"] = f.Body.HasError || f.Tool.ShowErrorText;
   f.View.Save.Execute(f.FileSystem);
   row["savedBytesDifferFromBaseline"] = !File.ReadAllBytes(f.FileName).SequenceEqual(baseline.Bytes);
});
Review("save-as before debounce", (f, row) => {
   var baseline = f.State();
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   row["pendingBeforeSave"] = f.Dispatch.Pending;
   row["saveEnabledBeforeFlush"] = f.View.Save.CanExecute(f.FileSystem);
   f.View.SaveAs.Execute(f.FileSystem);
   row["saveAsWroteOldBytes"] = File.ReadAllBytes(f.FileName).SequenceEqual(baseline.Bytes);
   f.Dispatch.Flush();
   row["diskEqualsSettledModel"] = File.ReadAllBytes(f.FileName).SequenceEqual(f.Model.RawData);
});
Review("save before debounce with unrelated dirty edit", (f, row) => {
   f.Model.WriteValue(f.View.ChangeHistory.CurrentChange, 0x7000, 42);
   f.View.ChangeHistory.ChangeCompleted();
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   row["saveEnabledBeforeFlush"] = f.View.Save.CanExecute(f.FileSystem);
   f.View.Save.Execute(f.FileSystem);
   row["savedImmediateValue"] = File.ReadAllBytes(f.FileName)[WorkflowFixture.Root + 4];
   f.Dispatch.Flush();
   row["settledImmediateValue"] = f.Model[WorkflowFixture.Root + 4];
   row["diskEqualsSettledModel"] = File.ReadAllBytes(f.FileName).SequenceEqual(f.Model.RawData);
});
Review("close before debounce", (f, row) => {
   bool closed = false; f.View.Closed += (_, _) => closed = true;
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   f.View.Close.Execute(f.FileSystem);
   row["closed"] = closed;
   row["referencesAtClose"] = f.Model.ReferenceCount;
   row["pendingAtClose"] = f.Dispatch.Pending;
   f.Dispatch.Flush();
});
Review("navigate before debounce", (f, row) => {
   var other = f.Model.RawData.Skip(WorkflowFixture.Other).Take(14).ToArray();
   var originalBody = f.Body;
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   f.View.Goto.Execute(WorkflowFixture.Other);
   row["bodyReused"] = ReferenceEquals(originalBody, f.Body);
   row["bodyAddressBeforeFlush"] = f.Body.Address;
   f.Dispatch.Flush();
   row["otherScriptChanged"] = !f.Model.RawData.Skip(WorkflowFixture.Other).Take(other.Length).SequenceEqual(other);
   row["rootValue"] = f.Model[WorkflowFixture.Root + 4];
   row["otherValue"] = f.Model[WorkflowFixture.Other + 4];
});
Review("undo before debounce", (f, row) => {
   f.Edit(WorkflowFixture.Source(1, 2));
   f.Edit(WorkflowFixture.Source(1, 3), settle: false);
   f.View.Undo.Execute(null);
   row["valueImmediatelyAfterUndo"] = f.Model[WorkflowFixture.Root + 4];
   row["redoAvailableBeforeFlush"] = f.View.Redo.CanExecute(null);
   f.Dispatch.Flush();
   row["valueAfterPendingCallback"] = f.Model[WorkflowFixture.Root + 4];
   row["redoAvailableAfterFlush"] = f.View.Redo.CanExecute(null);
});
Review("correct after rejected dialogue edit", (f, row) => {
   var baseline = f.State();
   f.Edit(WorkflowFixture.Source(1, 1, "Changed dialogue.") + "\nnot_a_command");
   row["changedDespiteError"] = !baseline.EqualsModel(f);
   f.Edit(WorkflowFixture.Source(1, 1));
   row["restoredAfterCorrection"] = baseline.EqualsModel(f);
   row["validAfterCorrection"] = !f.Body.HasError && !f.Tool.ShowErrorText;
});
Review("raw parser late error", (f, row) => {
   var baseline = f.State();
   var source = WorkflowFixture.Source(1, 1, "Changed dialogue.") + "\nnot_a_command";
   var result = f.Tool.ScriptParser.Compile(new ModelDelta(), f.Model, WorkflowFixture.Root, ref source, out _, out _);
   row["compileReturnedNull"] = result == null;
   row["modelChangedDespiteFailure"] = !baseline.EqualsModel(f);
});
Review("missing dialogue closer with end caret", (f, row) => {
   var source = "loadpointer 0 <003000>\n{\nHello!";
   f.Body.Editor.CaretIndex = source.Length;
   f.Edit(source);
   row["closerInserted"] = f.Body.Content.Contains("}");
   row["compileFailed"] = f.CompileErrors.Count > 0;
});
Review("undo relocation selection context", (f, row) => {
   var baseline = f.State();
   f.Edit(WorkflowFixture.Source(24, 2));
   row["relocatedAddress"] = f.Body.Address;
   int count = 0;
   while (f.View.Undo.CanExecute(null)) {
      if (++count > 32) throw new InvalidOperationException("Undo did not terminate.");
      f.View.Undo.Execute(null); f.Dispatch.Flush();
   }
   row["restoredBytesAndMetadata"] = baseline.EqualsModel(f);
   row["bodyAddressAfterUndo"] = f.Body.Address;
   row["rootAnchorAfterUndo"] = f.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, "workflow.root");
   f.View.Goto.Execute(WorkflowFixture.Root); f.Dispatch.Flush();
   row["bodyAddressAfterExplicitNavigation"] = f.Body.Address;
   while (f.View.Redo.CanExecute(null)) { f.View.Redo.Execute(null); f.Dispatch.Flush(); }
   row["bodyAddressAfterRedo"] = f.Body.Address;
   row["rootAnchorAfterRedo"] = f.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, "workflow.root");
});

// Corroborate key paths through the actual Avalonia text control and focus loss.
// Full TabView/native event ordering is still outside this headless review.
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
var shellFs = new StubFileSystem();
App.UpdateThemeDictionary(Application.Current!.Resources, new EditorViewModel(shellFs, InstantDispatch.Instance).Theme);
Application.Current.Resources["FileSystem"] = shellFs;
var window = new Window { Width = 900, Height = 650 }; window.Show();
void Layout() {
   Dispatcher.UIThread.RunJobs(); window.Measure(new Size(900, 650)); window.Arrange(new Rect(0, 0, 900, 650));
   AvaloniaHeadlessPlatform.ForceRenderTimerTick();
}
Review("text control navigate before debounce", (f, row) => {
   var control = new TextEditor { DataContext = f.Body.Editor, FontSize = 14 };
   var navigation = new Button { Content = "Navigate" };
   window.Content = new StackPanel { Children = { control, navigation } }; Layout();
   control.EditBox.Focus();
   control.EditBox.Text = WorkflowFixture.Source(1, 2); Layout();
   row["editReachedBody"] = f.Body.Content == WorkflowFixture.Source(1, 2);
   row["navigationFocusSucceeded"] = navigation.Focus(); Layout();
   row["textBoxFocusedAfterFocusMove"] = control.EditBox.IsFocused;
   row["pendingAfterFocusLoss"] = f.Dispatch.Pending;
   f.View.Goto.Execute(WorkflowFixture.Other); Layout();
   f.Dispatch.Flush(); Layout();
   row["rootValue"] = f.Model[WorkflowFixture.Root + 4];
   row["otherValue"] = f.Model[WorkflowFixture.Other + 4];
   window.Content = null; Layout();
});
Review("text control late error after dialogue", (f, row) => {
   var control = new TextEditor { DataContext = f.Body.Editor, FontSize = 14 };
   window.Content = control; Layout(); control.EditBox.Focus();
   control.EditBox.Text = WorkflowFixture.Source(1, 1, "Changed dialogue.") + "\nnot_a_command"; Layout();
   f.Dispatch.Flush(); Layout();
   row["visibleErrorAfterSettling"] = f.Body.HasError;
   row["errorLocationCount"] = f.Body.Editor.ErrorLocations.Count;
   row["invalidLineStillVisible"] = control.EditBox.Text?.Contains("not_a_command") == true;
   window.Content = null; Layout();
});
window.Close();
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new {
   scope = "Characterization only; fabricated models and temporary files; no supplied ROM, native UI, production changes, wall-clock race simulation or assertions requiring bugs to persist.",
   cases = rows,
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Completed {rows.Count} workflow probes; inspect observations and exceptions in {output}.");
