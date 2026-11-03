using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Review;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
foreach (var separator in new[] { "\n", "\r\n" }) {
   using var f = new WorkflowFixture();
   string Source(int count, int value, string text = "Hello!", int address = WorkflowFixture.Text) =>
      WorkflowFixture.Source(count, value, text, address).Replace("\n", separator);
   int Anchor(string name) => f.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, name);
   void Integrity(int count, int value, string dialogue) {
      int root = Anchor("workflow.root"), text = Anchor("workflow.dialogue");
      Check(!f.Body.HasError && !f.Tool.ShowErrorText, "Settled valid source must have no compile error.");
      Check(f.Body.Address == root && f.Model.GetNextRun(root) is XSERun, $"Root anchor, editor address and script format must agree: body={f.Body.Address:X6}, root={root:X6}, run={f.Model.GetNextRun(root).GetType().Name}.");
      Check(f.Model[root] == 0x6A, "Retain lock opcode.");
      foreach (var caller in WorkflowFixture.Callers) {
         Check(f.Model.ReadPointer(caller + 1) == root, "Both callers must follow script relocation.");
         Check(f.Model.GetNextRun(root).PointerSources.Contains(caller + 1), "Retain caller source metadata.");
      }
      for (int i = 0; i < count; i++) Check(f.Model.RawData.Skip(root + 1 + i * 5).Take(5)
         .SequenceEqual(new byte[] { 0x16, 1, 0x40, (byte)value, 0 }), "Exact setvar opcode, ID and value bytes.");
      Check(f.Model.ReadPointer(root + 3 + count * 5) == text, "Retain or update the dialogue pointer.");
      Check(f.Model.GetNextRun(text) is PCSRun, "Dialogue retains its text format.");
      var expected = PCSString.Convert("\"" + dialogue + "\"");
      Check(f.Model.RawData.Skip(text).Take(expected.Count).SequenceEqual(expected), "Exact dialogue bytes.");
      foreach (var address in new[] { f.Neighbor, WorkflowFixture.Root - 8, f.TextNeighbor })
         Check(f.Model.RawData.Skip(address).Take(WorkflowFixture.Sentinel.Length).SequenceEqual(WorkflowFixture.Sentinel), "Preserve blocked neighboring bytes.");
      Check(f.Messages.Count == 0, "The successful workflow must not raise viewport errors.");
   }
   var baseline = f.State();
   Check(f.View.ChangeHistory.IsSaved && !f.View.Undo.CanExecute(null), "Fixture starts saved without undo history.");
   foreach (var invalid in new[] { "setvar 0x4001", "setvar 0x4001 0x", "not_a_command", "setvar MissingAlias 1" }) {
      f.Edit(invalid + separator + "end");
      Check(f.Body.HasError || f.Tool.ShowErrorText, "An invalid command/operand must report a compile error.");
      Check(baseline.EqualsModel(f), "Rejected ordinary commands must retain all bytes and metadata.");
      f.Edit(Source(1, 1));
      Check(baseline.EqualsModel(f), "Correcting rejected source back to baseline must retain bytes and metadata.");
      Integrity(1, 1, "Hello!");
   }
   var beforeRapid = f.State(); var replaced = f.Dispatch.Replaced;
   for (int value = 2; value <= 9; value++) f.Edit(Source(1, value), settle: false);
   Check(beforeRapid.EqualsModel(f), "Pending edits must not compile before the debounce callback.");
   Check(f.Dispatch.Pending > 0 && f.Dispatch.Replaced > replaced, "Rapid edits coalesce scheduled callbacks.");
   f.Dispatch.Flush(); f.View.ChangeHistory.ChangeCompleted(); Integrity(1, 9, "Hello!");
   f.Edit(Source(24, 2));
   Check(Anchor("workflow.root") != WorkflowFixture.Root, "Blocked growth must actually relocate the script.");
   Integrity(24, 2, "Hello!");
   string conversation = "Welcome to the village! Bring a Potion when you return.";
   f.Edit(Source(24, 3, conversation));
   Check(Anchor("workflow.dialogue") != WorkflowFixture.Text, "Blocked dialogue growth must actually relocate text.");
   Integrity(24, 3, conversation);
   f.Edit(Source(2, 4, conversation, Anchor("workflow.dialogue")));
   Integrity(2, 4, conversation);
   var final = f.State();
   var states = new List<WorkflowState> { final };
   while (f.View.Undo.CanExecute(null)) {
      Check(states.Count < 64, "Undo history must terminate.");
      f.View.Undo.Execute(null); f.Dispatch.Flush(); states.Add(f.State());
   }
   Check(baseline.EqualsModel(f), "Complete undo restores all bytes and metadata, apart from the existing free-space search preference.");
   // This group checks binary/history integrity. Undo need not navigate the selection
   // back to a relocated anchor; the separate review records that presentation outcome.
   f.View.Goto.Execute(Anchor("workflow.root")); f.Dispatch.Flush();
   Integrity(1, 1, "Hello!");
   for (int i = states.Count - 2; i >= 0; i--) {
      Check(f.View.Redo.CanExecute(null), "Every undone record remains redoable.");
      f.View.Redo.Execute(null); f.Dispatch.Flush();
      Check(states[i].EqualsModel(f), "Redo exactly restores each recorded byte/metadata state.");
   }
   Check(final.EqualsModel(f) && !f.View.Redo.CanExecute(null), "Complete redo restores the final state without extra records.");
   f.View.Goto.Execute(Anchor("workflow.root")); f.Dispatch.Flush();
   Integrity(2, 4, conversation);
   f.View.Save.Execute(f.FileSystem);
   Check(f.Saves == 1 && f.View.ChangeHistory.IsSaved, "Settled edits save successfully and mark the document saved.");
   Check(File.ReadAllBytes(f.FileName).SequenceEqual(f.Model.RawData), "Saved ROM bytes equal settled model bytes.");
   var reopened = f.Reopen();
   Check(reopened.RawData.SequenceEqual(f.Model.RawData), "Reopened ROM retains every byte.");
   foreach (var name in new[] { "workflow.root", "workflow.dialogue", "workflow.neighbor", "workflow.other" })
      Check(reopened.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, name) == Anchor(name), "Reopened metadata preserves anchor addresses.");
   foreach (var caller in WorkflowFixture.Callers)
      Check(reopened.ReadPointer(caller + 1) == Anchor("workflow.root") && reopened.GetNextRun(Anchor("workflow.root")).PointerSources.Contains(caller + 1), "Reopened callers and pointer metadata remain consistent.");
   var saved = f.State();
   f.View.Undo.Execute(null); f.Dispatch.Flush();
   Check(!f.View.ChangeHistory.IsSaved, "Undo across Save marks the document dirty.");
   f.View.Redo.Execute(null); f.Dispatch.Flush();
   Check(saved.EqualsModel(f) && f.View.ChangeHistory.IsSaved, "Redo across Save restores the saved state and saved tag.");
   bool closed = false; f.View.Closed += (_, _) => closed = true;
   f.View.Close.Execute(f.FileSystem);
   Check(closed && f.Prompts == 0 && f.Dispatch.Pending == 0, "A saved, settled document closes without prompts or pending work.");
   Console.WriteLine($"PASS: {(separator.Length == 1 ? "LF" : "CRLF")} errors/correction, coalescing, script/text relocation, exact history, save/reopen and settled close.");
}
using (var f = new WorkflowFixture()) {
   var other = f.Model.RawData.Skip(WorkflowFixture.Other).Take(14).ToArray();
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   f.View.Goto.Execute(WorkflowFixture.Other); f.Dispatch.Flush();
   Check(f.Model[WorkflowFixture.Root + 4] == 2, "Navigation must settle the edit at its original script.");
   Check(f.Model.RawData.Skip(WorkflowFixture.Other).Take(14).SequenceEqual(other), "Navigation must not write pending source into the destination script.");
}
using (var f = new WorkflowFixture()) {
   f.Edit(WorkflowFixture.Source(1, 2));
   f.Edit(WorkflowFixture.Source(1, 3), settle: false);
   f.View.Undo.Execute(null); f.Dispatch.Flush();
   Check(f.Model[WorkflowFixture.Root + 4] == 2, "Undo must include the pending edit rather than allowing it to reapply later.");
   Check(f.View.Redo.CanExecute(null), "Settling pending work before Undo must preserve Redo.");
   f.View.Redo.Execute(null); f.Dispatch.Flush();
   Check(f.Model[WorkflowFixture.Root + 4] == 3, "Redo must restore the settled edit.");
}
using (var f = new WorkflowFixture()) {
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   Check(f.View.Undo.CanExecute(null), "A pending source edit must enable Undo even before bytes change.");
   f.View.Undo.Execute(null); f.Dispatch.Flush();
   Check(f.Model[WorkflowFixture.Root + 4] == 1 && f.View.Redo.CanExecute(null), "Undo of the first pending edit restores the original and remains redoable.");
}
using (var f = new WorkflowFixture()) {
   const string invalid = "setvar MissingAlias 1\nend";
   f.Edit(invalid, settle: false);
   f.View.Goto.Execute(WorkflowFixture.Other); f.Dispatch.Flush();
   Check(f.Model[WorkflowFixture.Other + 4] == 7, "Rejected source must not affect another script.");
   Check(!f.View.Undo.CanExecute(null), "A draft in another script must not enable an unrelated Undo action.");
   f.View.Goto.Execute(WorkflowFixture.Root); f.Dispatch.Flush();
   Check(f.Body.Content == invalid && f.Body.HasError, "Navigation must retain a rejected draft and its diagnostics.");
   f.Edit(WorkflowFixture.Source(1, 1));
   Check(!f.Body.HasError, "A retained draft must recover when corrected.");
}
using (var f = new WorkflowFixture()) {
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   var stale = f.Dispatch.CapturePendingCallbacks();
   f.Edit(WorkflowFixture.Source(1, 3), settle: false);
   foreach (var callback in stale) callback();
   Check(f.Model[WorkflowFixture.Root + 4] == 1, "An already queued stale callback must not execute newer or older source.");
   f.Dispatch.Flush();
   Check(f.Model[WorkflowFixture.Root + 4] == 3, "Only the latest scheduled edit should compile.");
   foreach (var callback in stale) callback();
   Check(f.Model[WorkflowFixture.Root + 4] == 3, "A callback remains invalid after its context has completed.");
}
using (var f = new WorkflowFixture()) {
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   f.Tool.Mode = HavenSoft.HexManiac.Core.ViewModels.Tools.CodeMode.Raw;
   f.Dispatch.Flush();
   Check(f.Model[WorkflowFixture.Root + 4] == 2, "A mode switch must finish the source with its original parser before changing modes.");
}
foreach (bool saveAs in new[] { false, true }) using (var f = new WorkflowFixture()) {
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   Check(f.View.Save.CanExecute(f.FileSystem) && f.View.Name.EndsWith("*"), "Pending source must enable Save and display an unsaved marker.");
   (saveAs ? f.View.SaveAs : f.View.Save).Execute(f.FileSystem);
   Check(File.ReadAllBytes(f.FileName)[WorkflowFixture.Root + 4] == 2, "Save/Save As must include source pending at command time.");
   Check(f.View.ChangeHistory.IsSaved && !f.Tool.HasUncompiledScriptChanges, "A successful source-inclusive save must leave no unsaved draft.");
   f.Dispatch.Flush();
   Check(f.View.ChangeHistory.IsSaved && File.ReadAllBytes(f.FileName).SequenceEqual(f.Model.RawData), "Canceled compile callbacks must not dirty the saved document later.");
}
using (var f = new WorkflowFixture()) {
   var baseline = f.State(); bool closed = false; f.View.Closed += (_, _) => closed = true;
   f.Edit("setvar MissingAlias 1\nend", settle: false);
   f.View.Save.Execute(f.FileSystem);
   Check(f.Saves == 0 && File.ReadAllBytes(f.FileName).SequenceEqual(baseline.Bytes), "Invalid pending source must not be saved as a successful edit.");
   f.View.Close.Execute(f.FileSystem);
   Check(!closed && f.Tool.HasUncompiledScriptChanges && f.Model.ReferenceCount == 1, "Closing rejected source must retain the tab, draft and model.");
   Check(f.Messages.Count > 0, "A rejected persistence action must explain how to resolve the draft.");
   f.View.Undo.Execute(null); f.Dispatch.Flush();
   Check(!f.Tool.HasUncompiledScriptChanges && baseline.EqualsModel(f), "Undo can explicitly discard the rejected draft without altering the last compiled model.");
}
using (var f = new WorkflowFixture()) {
   bool closed = false; f.View.Closed += (_, _) => closed = true;
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   var stale = f.Dispatch.CapturePendingCallbacks();
   f.View.Close.Execute(f.FileSystem);
   Check(!closed && f.Prompts == 1 && f.Model.ReferenceCount == 1, "Close must settle pending source and honor cancellation of the normal save prompt.");
   Check(f.Model[WorkflowFixture.Root + 4] == 2 && File.ReadAllBytes(f.FileName)[WorkflowFixture.Root + 4] == 1, "Canceled close retains the compiled edit in memory without changing disk.");
   f.FileSystem.TrySavePrompt = _ => false;
   f.View.Close.Execute(f.FileSystem);
   foreach (var callback in stale) callback(); f.Dispatch.Flush();
   Check(closed && f.Model.ReferenceCount == 0 && f.Model[WorkflowFixture.Root + 4] == 2, "Explicit discard closes safely with no later callback writes.");
}
using (var f = new WorkflowFixture()) {
   int reads = 0;
   f.Edit(WorkflowFixture.Source(1, 2), settle: false);
   f.View.ConsiderReload(new StubFileSystem { LoadFile = _ => { reads++; return null; } });
   Check(reads == 0 && f.Tool.HasPendingScriptChanges, "External reload must not overwrite a pending source draft on an otherwise saved model.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
