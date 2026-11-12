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
   body.Editor.Content = "pokemart \nend"; body.CaretPosition = "pokemart".Length;
   Check(body.TryInsertAuto(out _) && body.Content.Contains("<auto>") && !body.Content.Contains('{'), "Mart resources retain pointer assistance without receiving a dialogue/movement block.");
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
foreach (var command in new[] { "msgbox.npc", "single.battle 0" })
foreach (bool navigate in new[] { false, true }) using (var f = new WorkflowFixture()) {
   var callbacks = new List<Action>();
   f.Dispatch.DispatchOverride = action => { callbacks.Add(action); return Task.CompletedTask; };
   var body = f.Body; body.CaretPosition = command.Length;
   body.Editor.Content = command + " \nend";
   if (navigate) f.View.Goto.Execute(WorkflowFixture.Other);
   else f.View.Undo.Execute(null);
   var state = f.State(); var caret = body.CaretPosition; var activeCaret = f.Body.CaretPosition;
   foreach (var callback in callbacks) callback();
   Check(state.EqualsModel(f) && body.CaretPosition == caret && f.Body.CaretPosition == activeCaret,
      "A queued scaffold caret cannot reapply edits or move a different editor after Undo/navigation.");
}
foreach (var command in new[] { "msgbox.npc", "single.battle 0" })
foreach (bool caretAfterSpace in new[] { false, true }) using (var f = new WorkflowFixture()) {
   var body = new CodeBody(f.Model, f.Tool.ScriptParser, null) { Content = command + " " };
   body.CaretPosition = command.Length + (caretAfterSpace ? 1 : 0);
   var expected = command + (command == "msgbox.npc" ? " <auto>\n{\n\n}" : " <auto> <auto>\n{\n\n}\n{\n\n}");
   Check(body.TryInsertAuto(out var caret) && body.Content == expected && caret == body.Content.IndexOf('{') + 2,
      "End-of-editor assistance supports caret notification on either side of the newly typed space.");
}
using (var f = new WorkflowFixture()) {
   var before = f.State();
   f.Edit("loadpointer 0 <003000>\n{\n" + new string('A', 180) + "\n}\npreparemsg <auto>\n{\nend");
   Check(f.Body.HasError && before.EqualsModel(f) && !f.View.ChangeHistory.Undo.CanExecute(null),
      "A later unclosed block rolls back prior dialogue/resource relocation, bytes, pointers and metadata.");
}
foreach (var newline in new[] { "\n", "\r\n", "\r" })
foreach (var command in new[] { "move.player", "move.camera", "move.npc 3", "applymovement 3" }) {
   using var f = new WorkflowFixture();
   f.Model.SetList(new NoDataChangeDeltaModel(), "movementtypes", "walk_down", "walk_up", "walk_left", "walk_right");
   var baseline = f.State();
   var body = new CodeBody(f.Model, f.Tool.ScriptParser, null) { Content = command + " " + newline + "end" };
   body.CaretPosition = command.Length;
   Check(body.TryInsertAuto(out int caret), "A final movement operand receives automatic scaffolding.");
   Check(body.Content == command + " <auto>" + newline + "{" + newline + newline + "}" + newline + "end",
      "Movement scaffolding preserves newline style and the following command.");
   Check(caret == body.Content.IndexOf('{') + 1 + newline.Length && baseline.EqualsModel(f),
      "The movement caret targets the empty body without direct ROM/history writes.");
   var movements = string.Join(newline, "walk_up", "walk_right", "walk_up", "walk_right");
   f.Edit(body.Content.Insert(caret, movements));
   Check(!f.Body.HasError, "The generated movement source compiles normally.");
   int pointer = f.Model.ReadPointer(f.Body.Address + 3);
   Check(new byte[] { 1, 3, 1, 3, 0xFE }.SequenceEqual(f.Model.RawData.Skip(pointer).Take(5)),
      "Movement resources encode enum instructions and the FE terminator, not dialogue text.");
   int actor = command == "move.player" ? 0xFF : command == "move.camera" ? 0x7F : 3;
   Check(f.Model.ReadMultiByteValue(f.Body.Address + 1, 2) == actor, "Player/camera/NPC actor operands remain unchanged.");
   Check(f.Model[f.Body.Address + (command.StartsWith("applymovement") ? 7 : 10)] == 2,
      "The following end command remains outside the movement stream.");
   Check(WorkflowFixture.Callers.All(c => f.Model.ReadPointer(c + 1) == f.Body.Address), "Movement script relocation preserves known callers.");
   var compiled = f.State(); f.View.Undo.Execute(null); f.Dispatch.Flush();
   Check(baseline.EqualsModel(f), "Undo restores all movement, script, pointer and metadata state.");
   f.View.Redo.Execute(null); f.Dispatch.Flush(); Check(compiled.EqualsModel(f), "Redo restores the complete compiled movement edit.");
}
using (var f = new WorkflowFixture()) {
   var body = new CodeBody(f.Model, f.Tool.ScriptParser, null) { Content = "move.player \n{\nwalk_up\n}\nend" };
   body.CaretPosition = "move.player".Length;
   Check(body.TryInsertAuto(out _) && body.Content.Count(c => c == '{') == 1 && body.Content.Contains("walk_up"),
      "An existing movement block is not duplicated or overwritten.");
   var baseline = f.State(); f.Edit("move.camera <auto>\n{\nwalk_up\nend");
   Check(f.Body.HasError && f.Body.Editor.ErrorLocations.Count > 0 && baseline.EqualsModel(f),
      "Unclosed movement drafts keep diagnostics and all original ROM/metadata state.");
   f.View.Save.Execute(f.FileSystem); f.View.SaveAs.Execute(f.FileSystem);
   Check(f.Saves == 0, "Unclosed movement drafts retain save protection.");
}
// Trainer battles with only trailing text operands can scaffold both in argument order.
foreach (var newline in new[] { "\n", "\r\n", "\r" })
foreach (var command in new[] { "single.battle 0", "single.battle.rematch 0", "single.battle.canlose 0" }) {
   using var f = new WorkflowFixture();
   var baseline = f.State();
   var body = new CodeBody(f.Model, f.Tool.ScriptParser, null) { Content = command + " " + newline + "end" };
   body.CaretPosition = command.Length;
   Check(body.TryInsertAuto(out int caret), "A trainer battle offers automatic text operands.");
   var block = newline + "{" + newline + newline + "}";
   var expected = command + " <auto> <auto>" + block + block + newline + "end";
   Check(body.Content == expected, "Both trainer text pointers and blocks must appear immediately, before compilation/reload.");
   Check(caret == expected.IndexOf('{') + 1 + newline.Length, "Trainer assistance targets the first prebattle text row.");
   Check(baseline.EqualsModel(f), "Paired trainer scaffolding must not allocate or write ROM/resources/history directly.");
   body.CaretPosition = caret;
   Check(!body.TryInsertAuto(), "Assistance inside the first trainer block cannot duplicate either block.");
   var secondCaret = expected.IndexOf('{', expected.IndexOf('}') + 1) + 1 + newline.Length;
   var filled = expected.Insert(secondCaret, "After battle.").Insert(caret, "Before battle.");
   f.Edit(filled);
   Check(!f.Body.HasError, "The two generated trainer text blocks compile normally.");
   var first = f.Model.ReadPointer(f.Body.Address + 6); var second = f.Model.ReadPointer(f.Body.Address + 10);
   var beforeText = PCSString.Convert("\"Before battle.\""); var afterText = PCSString.Convert("\"After battle.\"");
   Check(first != second && beforeText.SequenceEqual(f.Model.RawData.Skip(first).Take(beforeText.Count)) &&
      afterText.SequenceEqual(f.Model.RawData.Skip(second).Take(afterText.Count)), "Trainer pointers must target the respective prebattle and postbattle text bytes.");
   Check(f.Model[f.Body.Address + 14] == 2 && WorkflowFixture.Callers.All(c => f.Model.ReadPointer(c + 1) == f.Body.Address),
      "The following command and relocation callers remain intact.");
   var compiled = f.State(); f.View.Undo.Execute(null); f.Dispatch.Flush();
   Check(baseline.EqualsModel(f), "One Undo restores both trainer resources, pointers and metadata.");
   f.View.Redo.Execute(null); f.Dispatch.Flush(); Check(compiled.EqualsModel(f), "Redo restores both trainer text resources.");
   f.View.Undo.Execute(null); f.Dispatch.Flush();
   var rejected = filled + newline + "not_a_command"; f.Edit(rejected);
   Check(f.Body.HasError && f.Body.Content == rejected && baseline.EqualsModel(f), "A late trainer-script failure retains source and rolls back both resources atomically.");
   f.View.Save.Execute(f.FileSystem); Check(f.Saves == 0, "Rejected paired trainer source still blocks saving.");
   f.Edit(filled); Check(!f.Body.HasError, "Correcting rejected paired trainer source retries successfully.");
}
// Continuations still require a script pointer, so keep their existing operand entry.
foreach (var command in new[] { "single.battle.continue.silent 0", "single.battle.continue.music 0" }) {
   using var f = new WorkflowFixture();
   var body = new CodeBody(f.Model, f.Tool.ScriptParser, null) { Content = command + " " };
   body.CaretPosition = command.Length;
   Check(body.TryInsertAuto(out _) && body.Content == command + " <auto>", "Continuation entry must not skip the required winscript operand or enter a text block.");
   body.Content += " "; body.CaretPosition = body.Content.Length;
   Check(body.TryInsertAuto(out _) && body.Content == command + " <auto> <auto>", "The second continuation text pointer retains ordinary pointer assistance.");
   body.Content += " "; body.CaretPosition = body.Content.Length;
   Check(!body.TryInsertAuto(), "Continuation script destinations are never fabricated by text scaffolding.");
   f.Edit(body.Content + "<001800>\n{\nBefore.\n}\n{\nAfter.\n}\nend");
   Check(!f.Body.HasError && f.Model.ReadPointer(f.Body.Address + 14) == WorkflowFixture.Other, "A manually completed continuation retains its explicit winscript destination.");
}
using (var f = new WorkflowFixture()) {
   foreach (var prefix in new[] { "single.battle 0 ", "single.battle 0 <003000> " })
   foreach (var comment in new[] { "", "# Existing prebattle dialogue\n" }) {
      var body = new CodeBody(f.Model, f.Tool.ScriptParser, null) { Content = prefix + "\n" + comment + "{\nKeep first.\n}\n{\nKeep second.\n}\nend" };
      body.CaretPosition = prefix.TrimEnd().Length;
      Check(body.TryInsertAuto(out _) && body.Content.Count(c=>c=='{') == 2 && body.Content.Contains("Keep first.") && body.Content.Contains("Keep second."),
         "Existing trainer blocks and explicit addresses are retained without adding or reordering blocks.");
   }
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
