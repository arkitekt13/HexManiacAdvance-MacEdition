using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Review;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
foreach (var newline in new[] { "\n", "\r\n", "\r" })
foreach (var command in new[] { "msgbox.npc", "msgbox.sign", "msgbox.default", "msgbox.yesno", "msgbox.autoclose", "preparemsg", "loadpointer 0" }) {
   using var f = new WorkflowFixture();
   var body = new CodeBody(f.Model, f.Tool.ScriptParser, null);
   var source = command + " " + newline + "end";
   body.Editor.Content = source; body.CaretPosition = command.Length;
   var baseline = f.State();
   Check(body.TryInsertAuto(out int caret), "Supported dialogue operands offer an automatic source scaffold.");
   var expected = command + " <auto>" + newline + "{" + newline + newline + "}" + newline + "end";
   Check(body.Content == expected, "The scaffold preserves newline style and the following end command.");
   Check(caret == expected.IndexOf('{') + 1 + newline.Length, "The returned caret targets the empty dialogue row.");
   Check(baseline.EqualsModel(f) && !f.View.Undo.CanExecute(null), "Source scaffolding performs no direct ROM allocation or history writes.");
   body.Editor.CaretIndex = caret; Check(!body.TryInsertAuto(), "Repeating assistance inside the body cannot duplicate the block.");
   var filled = expected.Insert(caret, "Hello, Michael!");
   f.Edit(filled);
   Check(!f.Body.HasError, "The completed scaffold compiles through the normal compiler.");
   int pointerOffset = command == "preparemsg" ? 1 : 2;
   int text = f.Model.ReadPointer(f.Body.Address + pointerOffset);
   var encoded = PCSString.Convert("\"Hello, Michael!\"");
   Check(text >= 0 && encoded.SequenceEqual(f.Model.RawData.Skip(text).Take(encoded.Count)), "Compilation allocates exact encoded dialogue and resolves its pointer.");
   Check(f.Model[f.Body.Address + (command.StartsWith("msgbox.") ? 8 : command == "preparemsg" ? 5 : 6)] == 2,
      "The following end command remains script code.");
   Check(WorkflowFixture.Callers.All(caller => f.Model.ReadPointer(caller + 1) == f.Body.Address), "Relocation preserves every known caller pointer.");
   var compiled = f.State(); f.View.Undo.Execute(null); f.Dispatch.Flush();
   Check(baseline.EqualsModel(f), "One Undo restores script, resources, pointers and metadata.");
   f.View.Redo.Execute(null); f.Dispatch.Flush(); Check(compiled.EqualsModel(f), "Redo restores the compiled scaffold.");
}
using (var f = new WorkflowFixture()) {
   var body = new CodeBody(f.Model, f.Tool.ScriptParser, null);
   body.Editor.Content = "msgbox.npc \n{\nKeep this text.\n}\nend"; body.CaretPosition = "msgbox.npc".Length;
   Check(body.TryInsertAuto(out _) && body.Content.Count(c => c == '{') == 1 && body.Content.Contains("Keep this text."), "An existing block is preserved instead of duplicated.");
   body.Editor.Content = "applymovement 0 \nend"; body.CaretPosition = "applymovement 0".Length;
   Check(body.TryInsertAuto(out _) && body.Content.Contains("<auto>") && !body.Content.Contains('{'), "Movement keeps pointer assistance without receiving a dialogue block.");
   f.Model.SpartanMode = true; body.Editor.Content = "msgbox.npc "; body.CaretPosition = "msgbox.npc".Length;
   Check(!body.TryInsertAuto(), "Spartan mode retains its existing assistance opt-out.");
   f.Model.SpartanMode = false; f.Tool.InsertAutoActive = false;
   f.Edit("msgbox.npc \nend", settle: false);
   Check(!f.Body.Content.Contains("<auto>") && !f.Body.Content.Contains('{'), "Disabling automatic insertion also disables scaffolding.");
}
foreach (var newline in new[] { "\n", "\r\n", "\r" }) using (var f = new WorkflowFixture()) {
   var baseline = f.State();
   var unfinished = "msgbox.npc <auto>" + newline + "{" + newline + "end";
   f.Edit(unfinished);
   Check(f.Body.HasError && f.Body.Content == unfinished && f.Body.Editor.ErrorLocations.Count > 0, "An unfinished manual block remains a rejected draft with diagnostics, rather than being auto-closed.");
   Check(baseline.EqualsModel(f) && !f.View.ChangeHistory.Undo.CanExecute(null), "Rejected blocks preserve all bytes, resources, pointers, metadata and model history.");
   Check(f.View.Undo.CanExecute(null), "The existing draft-aware Undo remains available to discard rejected source.");
   f.View.Save.Execute(f.FileSystem); f.View.SaveAs.Execute(f.FileSystem);
   Check(f.Saves == 0, "Rejected unfinished blocks cannot be saved as successful source.");
   bool closed = false; f.View.Closed += (_, _) => closed = true; f.View.Close.Execute(f.FileSystem);
   Check(!closed && f.Tool.HasUncompiledScriptChanges, "Close retains the rejected draft for correction.");
   f.Edit("msgbox.npc <auto>" + newline + "{" + newline + "Corrected." + newline + "}" + newline + "end");
   Check(!f.Body.HasError, "Explicit correction retries successfully.");
   f.View.Undo.Execute(null); f.Dispatch.Flush(); Check(baseline.EqualsModel(f), "Undo of a corrected draft restores the original state.");
}
foreach (bool navigate in new[] { false, true }) using (var f = new WorkflowFixture()) {
   var callbacks = new List<Action>();
   f.Dispatch.DispatchOverride = action => { callbacks.Add(action); return Task.CompletedTask; };
   var body = f.Body; body.CaretPosition = "msgbox.npc".Length;
   body.Editor.Content = "msgbox.npc \nend";
   if (navigate) f.View.Goto.Execute(WorkflowFixture.Other);
   else f.View.Undo.Execute(null);
   var state = f.State(); var caret = body.CaretPosition; var activeCaret = f.Body.CaretPosition;
   foreach (var callback in callbacks) callback();
   Check(state.EqualsModel(f) && body.CaretPosition == caret && f.Body.CaretPosition == activeCaret,
      "A queued scaffold caret cannot reapply edits or move a different editor after Undo/navigation.");
}
foreach (bool caretAfterSpace in new[] { false, true }) using (var f = new WorkflowFixture()) {
   var body = new CodeBody(f.Model, f.Tool.ScriptParser, null) { Content = "msgbox.npc " };
   body.CaretPosition = "msgbox.npc".Length + (caretAfterSpace ? 1 : 0);
   Check(body.TryInsertAuto(out var caret) && body.Content == "msgbox.npc <auto>\n{\n\n}" && caret == body.Content.IndexOf('{') + 2,
      "End-of-editor assistance supports caret notification on either side of the newly typed space.");
}
using (var f = new WorkflowFixture()) {
   var before = f.State();
   f.Edit("loadpointer 0 <003000>\n{\n" + new string('A', 180) + "\n}\npreparemsg <auto>\n{\nend");
   Check(f.Body.HasError && before.EqualsModel(f) && !f.View.ChangeHistory.Undo.CanExecute(null),
      "A later unclosed block rolls back prior dialogue/resource relocation, bytes, pointers and metadata.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
