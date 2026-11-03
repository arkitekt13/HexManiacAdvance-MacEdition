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
Console.WriteLine($"CHECKS_PASSED:{checks}");
