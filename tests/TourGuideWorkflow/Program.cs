using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Review;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
const string InvalidMovement = "NoSuchMovementInstruction";
if (args.Contains("--probe-format")) {
   foreach (string source in new[] {
      "msgbox.default <auto>\n{\nWelcome!\n}\nend",
      "lockall\nmsgbox.default <auto>\n{\nWelcome!\n}\nmove.player <auto>\n{\nwalk_up\n}\nmsgbox.default <auto>\n{\nNext stop!\n}\nreleaseall\nend",
   }) using (var f = new WorkflowFixture()) {
      f.Model.SetList(new NoDataChangeDeltaModel(), "movementtypes", "walk_down", "walk_up", "walk_left", "walk_right");
      f.Edit(source);
      int root = f.Body.Address;
      int pointer = f.Model.ReadPointer(root + (source.StartsWith("lockall") ? 3 : 2));
      Console.WriteLine($"FORMAT_PROBE before: root={root:X6}; text={pointer:X6}; run={f.Model.GetNextRun(pointer)}; source={f.Body.Content.Replace("\n", "|")}");
      f.Tool.ScriptParser.FormatScript<XSERun>(new ModelDelta(), f.Model, root);
      Console.WriteLine($"FORMAT_PROBE reformat: run={f.Model.GetNextRun(pointer)}");
   }
   return;
}
if (args.Contains("--probe-invalid-movement")) {
   foreach (bool allocated in new[] { false, true }) using (var f = new WorkflowFixture()) {
      f.Model.SetList(new NoDataChangeDeltaModel(), "movementtypes", "walk_down", "walk_up", "walk_left", "walk_right");
      f.Edit("move.player <auto>\n{\nwalk_up\nwalk_right\n}\nend");
      int movement = f.Model.ReadPointer(f.Body.Address + 3);
      string target = allocated ? movement.ToString("X6") : "auto";
      f.Edit($"move.player <{target}>\n{{\nwalk_up\n{InvalidMovement}\nwalk_right\n}}\nend");
      movement = f.Model.ReadPointer(f.Body.Address + 3);
      Console.WriteLine($"INVALID_MOVEMENT_PROBE: existing={allocated}; error={f.Body.HasError}; bytes={Convert.ToHexString(f.Model.RawData.Skip(movement).Take(4).ToArray())}; draft={f.Tool.HasUncompiledScriptChanges}");
   }
   return;
}
foreach (var newline in new[] { "\n", "\r\n", "\r" }) {
   using var f = new WorkflowFixture();
   f.Model.SetList(new NoDataChangeDeltaModel(), "movementtypes", "walk_down", "walk_up", "walk_left", "walk_right");
   var original = f.State();
   string Join(params string[] lines) => string.Join(newline, lines);
   string Source(IReadOnlyList<int> targets = null, int repeats = 1, bool grow = false, bool lateError = false) {
      string At(int i) => targets == null ? "auto" : targets[i].ToString("X6");
      string Move(string name) => string.Join(newline, Enumerable.Repeat(name, repeats));
      return Join("lockall", $"msgbox.default <{At(0)}>", "{", grow ? new string('A', 120) : "Welcome!", "}",
         $"move.npc 8 <{At(1)}>", "{", Move("walk_right"), "}",
         $"move.player <{At(2)}>", "{", Move("walk_down"), "}",
         $"move.camera <{At(3)}>", "{", Move("walk_left"), "}",
         $"applymovement 9 <{At(4)}>", "{", Move("walk_up"), "}", "waitmovement 9",
         $"msgbox.default <{At(5)}>", "{", grow ? "The tour continues after all four movement blocks." : "Next stop!", "}",
         lateError ? "not_a_command" : "releaseall", "end");
   }
   int Root() => f.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, "workflow.root");
   int[] Targets() => new[] { 3, 12, 22, 32, 42, 51 }.Select(offset => f.Model.ReadPointer(Root() + offset)).ToArray();
   void Integrity(int repeats, bool grown) {
      Check(!f.Body.HasError && !f.Tool.HasUncompiledScriptChanges, "A valid mixed tour settles without a rejected draft.");
      int root = Root(); var targets = Targets();
      Check(f.Model[root] == 0x69 && f.Model[root + 57] == 0x6B && f.Model[root + 58] == 2,
         "Lock/release/end remain executable code outside every resource block.");
      Check(targets.Distinct().Count() == 6, "Six resources retain distinct pointers.");
      foreach (int caller in WorkflowFixture.Callers)
         Check(f.Model.ReadPointer(caller + 1) == root && f.Model.GetNextRun(root).PointerSources.Contains(caller + 1),
            "Both original callers and their metadata follow script relocation.");
      foreach (var (offset, actor) in new[] { (9, 8), (19, 255), (29, 127), (39, 9) }) {
         Check(f.Model[root + offset] == 0x4F && f.Model.ReadMultiByteValue(root + offset + 1, 2) == actor,
            "NPC, player, camera and asynchronous movement retain their own actor IDs.");
         if (offset != 39) Check(f.Model.RawData.Skip(root + offset + 7).Take(3).SequenceEqual(new byte[] { 0x51, 0, 0 }),
            "The movement macros retain their existing implicit waitmovement 0.");
      }
      Check(f.Model.RawData.Skip(root + 46).Take(3).SequenceEqual(new byte[] { 0x51, 9, 0 }),
         "An explicit wait for the asynchronous NPC remains intact.");
      foreach (var (i, value) in new[] { (1, 3), (2, 0), (3, 2), (4, 1) }) {
         var expected = Enumerable.Repeat((byte)value, repeats).Append((byte)0xFE);
         Check(expected.SequenceEqual(f.Model.RawData.Skip(targets[i]).Take(repeats + 1)),
            "Each movement stream retains exact direction bytes and an FE terminator.");
         Check(f.Model.GetNextRun(targets[i]) is TableStreamRun run && run.Start == targets[i] &&
            run.PointerSources.Contains(root + new[] { 3, 12, 22, 32, 42, 51 }[i]),
            "Each movement format retains its current script pointer-source metadata.");
      }
      foreach (var (i, text) in new[] { (0, grown ? new string('A', 120) : "Welcome!"),
         (5, grown ? "The tour continues after all four movement blocks." : "Next stop!") }) {
         var expected = PCSString.Convert("\"" + text + "\"");
         Check(expected.SequenceEqual(f.Model.RawData.Skip(targets[i]).Take(expected.Count)) && f.Model.GetNextRun(targets[i]) is PCSRun,
            $"Dialogue {i} before/after movement retains exact bytes/format: root={root:X6}, target={targets[i]:X6}, expected={Convert.ToHexString(expected.ToArray())}, actual={Convert.ToHexString(f.Model.RawData.Skip(targets[i]).Take(expected.Count).ToArray())}, run={f.Model.GetNextRun(targets[i]).GetType().Name}/{f.Model.GetNextRun(targets[i]).Start:X6}, length={PCSString.ReadString(f.Model, targets[i], true)}.");
      }
      foreach (int at in new[] { f.Neighbor, WorkflowFixture.Root - 8, f.TextNeighbor })
         Check(f.Model.RawData.Skip(at).Take(WorkflowFixture.Sentinel.Length).SequenceEqual(WorkflowFixture.Sentinel),
            "Tour compilation preserves the original neighboring sentinels.");
   }
   f.Edit(Source()); Integrity(1, false);
   Check(Root() != WorkflowFixture.Root, "The initial tour actually exercises script relocation.");
   var first = f.State();
   f.View.Undo.Execute(null); f.Dispatch.Flush(); Check(original.EqualsModel(f), "One Undo removes all six new resources and script relocation.");
   f.View.Redo.Execute(null); f.Dispatch.Flush(); Check(first.EqualsModel(f), "One Redo restores the complete initial tour.");
   f.View.Goto.Execute(Root()); f.Dispatch.Flush();
   var oldTargets = Targets();
   // Named resources must survive relocation, including those which later blocks still reference.
   var token = new ModelDelta();
   for (int i = 0; i < oldTargets.Length; i++)
      f.Model.ObserveAnchorWritten(token, "tour.resource" + i, f.Model.GetNextRun(oldTargets[i]));
   int afterResources = oldTargets.Max(at => at + f.Model.GetNextRun(at).Length);
   token.ChangeData(f.Model, afterResources, WorkflowFixture.Sentinel);
   f.Model.ObserveAnchorWritten(token, "tour.blocker", new NoInfoRun(afterResources));
   f.View.ChangeHistory.ClearHistory(false); f.View.ChangeHistory.TagAsSaved();
   var beforeGrowth = f.State();
   f.Edit(Source(oldTargets, 32, grow: true, lateError: true));
   Check(beforeGrowth.EqualsModel(f) && f.Body.HasError, "A late error after six resource edits restores every byte and metadata entry.");
   Check(f.Body.Editor.ErrorLocations.Count > 0 && f.Body.Content.Contains("not_a_command"), "Rejected mixed source retains its diagnostics and text.");
   Check(!f.View.ChangeHistory.Undo.CanExecute(null), "Failed mixed compilation adds no model history.");
   f.View.Save.Execute(f.FileSystem); f.View.SaveAs.Execute(f.FileSystem);
   Check(f.Saves == 0, "Both save routes protect the rejected mixed draft.");
   var rejected = f.Body.Content;
   f.View.Goto.Execute(WorkflowFixture.Other); f.View.Goto.Execute(Root()); f.Dispatch.Flush();
   Check(f.Body.Content == rejected && f.Body.HasError && beforeGrowth.EqualsModel(f), "Navigation retains the mixed rejected draft without applying it elsewhere.");
   f.Edit(Source(oldTargets, 32, grow: true)); Integrity(32, true);
   var grownTargets = Targets();
   Check(oldTargets.Zip(grownTargets).All(pair => pair.First != pair.Second), "All six blocked resources really relocate during growth.");
   for (int i = 0; i < grownTargets.Length; i++)
      Check(f.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, "tour.resource" + i) == grownTargets[i],
         "Each named resource anchor follows its own relocation.");
   Check(f.Model.RawData.Skip(afterResources).Take(WorkflowFixture.Sentinel.Length).SequenceEqual(WorkflowFixture.Sentinel), "Growth preserves the final resource blocker.");
   var grown = f.State();
   f.View.Undo.Execute(null); f.Dispatch.Flush(); Check(beforeGrowth.EqualsModel(f), "One Undo restores all six resources, pointers, anchors and bytes before correction.");
   f.View.Redo.Execute(null); f.Dispatch.Flush(); Check(grown.EqualsModel(f), "One Redo restores all mixed growth changes.");
   f.View.Goto.Execute(Root()); f.Dispatch.Flush();
   f.Edit(Source(grownTargets), settle: false);
   var callbacks = f.Dispatch.CapturePendingCallbacks();
   f.View.Goto.Execute(WorkflowFixture.Other); f.Dispatch.Flush();
   var shrunk = f.State();
   foreach (var callback in callbacks) callback();
   Check(shrunk.EqualsModel(f), "Callbacks captured before mixed-script navigation cannot reapply edits.");
   f.View.Goto.Execute(Root()); f.Dispatch.Flush(); Integrity(1, false);
   f.View.Undo.Execute(null); f.Dispatch.Flush();
   foreach (var callback in callbacks) callback();
   Check(grown.EqualsModel(f) && f.View.Redo.CanExecute(null), "Undo after settling navigation restores growth and stale callbacks preserve Redo.");
   f.View.Redo.Execute(null); f.Dispatch.Flush(); Check(shrunk.EqualsModel(f), "Redo restores the settled shortened tour.");
   f.View.Goto.Execute(Root()); f.Dispatch.Flush();
   f.View.Save.Execute(f.FileSystem);
   Check(f.Saves == 1 && f.View.ChangeHistory.IsSaved, "A corrected mixed tour saves normally.");
   var reopened = f.Reopen();
   Check(reopened.RawData.SequenceEqual(f.Model.RawData), "Save/reopen retains every mixed-script and resource byte.");
   Check(reopened.ExportMetadata(null, f.Services.MetadataInfo).Serialize().Where(line => !line.StartsWith("FreeSpaceSearch = ")).SequenceEqual(f.Metadata()),
      "Save/reopen retains all mixed-script metadata apart from the existing free-space search preference.");
   Console.WriteLine($"PASS: {newline.Length switch { 2 => "CRLF", _ => newline == "\r" ? "CR" : "LF" }} mixed tour allocation, growth, failure, navigation, history and save.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
