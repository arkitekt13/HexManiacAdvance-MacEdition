using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Review;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
var movementLists = BaseModel.GetDefaultMetadatas("BPRE").SelectMany(md => md.Lists).Where(list => list.Name == "movementtypes").ToArray();
WorkflowFixture Fixture(bool external = false) {
   var f = new WorkflowFixture();
   f.Model.LoadMetadata(new StoredMetadata(lists: movementLists));
   if (external) {
      var token = new ModelDelta();
      token.ChangeData(f.Model, 0x4000, new byte[] { 0x11, 0x13, 0xFE });
      TableStreamRun.TryParseTableStream(f.Model, 0x4000, SortedSpan<int>.None, "", "[move.movementtypes]!FE", null, out var stream);
      f.Model.ObserveAnchorWritten(token, "validation.movement", stream);
      token.ChangeData(f.Model, 0x4003, WorkflowFixture.Sentinel);
      f.Model.ObserveAnchorWritten(token, "validation.blocker", new NoInfoRun(0x4003));
   }
   return f;
}
string[] Metadata(WorkflowFixture f) {
   var m = f.Model.ExportMetadata(null, f.Services.MetadataInfo);
   // Compare whole named-anchor records canonically, preserving every association.
   return new StoredMetadata(anchors: m.NamedAnchors.OrderBy(a => a.Name).ToArray(), unmappedPointers: m.UnmappedPointers,
      matchedWords: m.MatchedWords, offsetPointers: m.OffsetPointers, lists: m.Lists, unmappedConstants: m.UnmappedConstants,
      gotoShortcuts: m.GotoShortcuts, tableGroups: m.TableGroups, generalInfo: f.Services.MetadataInfo,
      fields: new StoredMetadataFields { FreeSpaceBuffer = m.FreeSpaceBuffer, NextExportID = m.NextExportID,
         ShowRawIVByteForTrainer = m.ShowRawIVByteForTrainer }).Serialize();
}
WorkflowState State(WorkflowFixture f) => new(f.Model.RawData.ToArray(), Metadata(f));
bool Matches(WorkflowState state, WorkflowFixture f) => state.Bytes.SequenceEqual(f.Model.RawData) && state.Metadata.SequenceEqual(Metadata(f));
string Source(string command, string target, string row, string newline = "\n", bool dialogue = false) {
   var prefix = dialogue ? "loadpointer 0 <003000>\n{\n" + new string('A', 160) + "\n}\n" : "";
   return (prefix + command + " <" + target + ">\n{\nwalk_up\n  " + row + "\nwalk_right\n}\nend").Replace("\n", newline);
}

// Failure before correction must be atomic even after an earlier dialogue resource grew.
if (!args.Contains("--compatibility-only"))
foreach (string newline in new[] { "\n", "\r\n", "\r" })
foreach (string command in new[] { "move.player", "move.camera", "move.npc 8", "applymovement 8" })
foreach (bool external in new[] { false, true })
foreach (string badRow in new[] { "walk_upp", "walk_dwon", "walk_rihgt", "NoSuchMovementInstruction", "..." }) {
   using var f = Fixture(external);
   var before = State(f); var disk = File.ReadAllBytes(f.FileName);
   var changed = Enumerable.Range(0, f.Model.Count).Where(f.Model.HasChanged).ToArray();
   int allocation = f.Model.FreeSpaceStart;
   string target = external ? "004000" : "auto";
   string source = Source(command, target, badRow, newline, dialogue: true);
   f.Edit(source);
   Check(f.Body.HasError && f.Tool.HasUncompiledScriptChanges, "Unknown movement rows must reject the draft, not silently omit/substitute a movement.");
   Check(Matches(before, f) && f.Model.FreeSpaceStart == allocation, "A bad later movement restores all ROM bytes, resource/pointer metadata and allocation state.");
   Check(changed.SequenceEqual(Enumerable.Range(0, f.Model.Count).Where(f.Model.HasChanged)), "Failure restores byte highlighting.");
   Check(f.Body.Content == source && f.Body.Editor.ErrorLocations.Any(e => e.Type == SegmentType.Error && e.Line == 7 && e.Start == 2 && e.Length == badRow.Length),
      "Rejected source retains a diagnostic on the exact indented movement row across newline styles.");
   Check(f.CompileErrors.Any(error => error.Contains(badRow)), "Diagnostics identify the unrecognized movement name.");
   Check(!f.View.ChangeHistory.Undo.CanExecute(null) && f.View.ChangeHistory.IsSaved, "A failed attempt adds no model history or dirty tag.");
   f.View.Save.Execute(f.FileSystem); f.View.SaveAs.Execute(f.FileSystem);
   Check(f.Saves == 0 && disk.SequenceEqual(File.ReadAllBytes(f.FileName)), "Save and Save As protect the rejected source without touching disk.");
   bool closed = false; f.View.Closed += (_, _) => closed = true; f.View.Close.Execute(f.FileSystem);
   Check(!closed && f.Tool.HasUncompiledScriptChanges, "Close retains the rejected movement draft.");
   f.View.Goto.Execute(WorkflowFixture.Other); f.View.Goto.Execute(WorkflowFixture.Root); f.Dispatch.Flush();
   Check(f.Body.Content == source && f.Body.HasError && Matches(before, f), "Navigation retains rejected source/diagnostics and cannot apply it to another script.");
   f.Edit(source.Replace(badRow, "walk_down"));
   Check(!f.Body.HasError && !f.Tool.HasUncompiledScriptChanges, "Correction retries successfully from the untouched model.");
   int pointer = f.Model.ReadPointer(f.Body.Address + 9); // loadpointer is six bytes, then movement pointer offset three.
   Check(f.Model.RawData.Skip(pointer).Take(4).SequenceEqual(new byte[] { 0x11, 0x10, 0x13, 0xFE }), "Corrected movement retains exact directions and FE termination.");
   Check(WorkflowFixture.Callers.All(c => f.Model.ReadPointer(c + 1) == f.Body.Address), "Correction/relocation retains every script caller.");
   if (external) Check(pointer != 0x4000 && f.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, "validation.movement") == pointer,
      "Blocked existing movement growth relocates its named anchor and pointer.");
   var compiled = State(f);
   f.View.Undo.Execute(null); f.Dispatch.Flush(); Check(Matches(before, f), "One Undo restores all dialogue, movement, script, pointers and metadata.");
   f.View.Redo.Execute(null); f.Dispatch.Flush(); Check(Matches(compiled, f), "One Redo restores the complete correction.");
}

// Validate the actual catalog, including existing duplicate-name priority and numeric aliases.
using (var f = Fixture()) {
   var names = f.Model.GetOptions("movementtypes").Select((name, id) => (name, id))
      .Where(x => !string.IsNullOrWhiteSpace(x.name) && x.id < 0xFE).GroupBy(x => x.name).Select(g => g.First()).ToArray();
   Check(names.Length == 166, "Use the real FireRed catalog's 166 distinct names, not replacement movement IDs.");
   foreach (var entry in names) Check(ArrayRunEnumSegment.TryParse("movementtypes", f.Model, entry.name, out int value) && value == entry.id,
      "Existing exact/duplicate alias priority is unchanged.");
   foreach (string command in new[] { "move.player", "move.camera", "move.npc 8", "applymovement 8" })
   foreach (bool external in new[] { false, true }) {
      using var data = Fixture(external);
      string source = command + " <" + (external ? "004000" : "auto") + ">\n{\n" + string.Join("\n", names.Select(x => x.name)) + "\n}\nend";
      data.Edit(source);
      Check(!data.Body.HasError && !data.Tool.HasUncompiledScriptChanges, "Every real named movement compiles through each command and resource path.");
      int pointer = data.Model.ReadPointer(data.Body.Address + 3);
      Check(data.Model.RawData.Skip(pointer).Take(names.Length + 1).SequenceEqual(names.Select(x => (byte)x.id).Append((byte)0xFE)),
         "Every named movement retains its exact byte and terminator.");
   }
}

// These are accepted legacy forms; validation must preserve their compiled bytes.
foreach (bool external in new[] { false, true })
foreach (var (row, value) in new[] { ("walk_up", 17), ("WALK_UP", 17), ("walk_u", 17), ("\"walk_up\"", 17),
   ("\twalk_up", 17), ("17", 17), ("\t17", 17), ("256", 0), ("0x98", 152) }
   .Concat(external ? new[] { ("0x11", 17), ("(17)", 17), ("((17))", 17), ("walk_up,", 17) } : Array.Empty<(string, int)>())) {
   using var f = Fixture(external);
   f.Edit("move.player <" + (external ? "004000" : "auto") + ">\n{\n" + row + " # keep comment\n}\nend");
   Check(!f.Body.HasError, "Supported legacy alias/partial/numeric/comment forms still compile: " + row);
   int pointer = f.Model.ReadPointer(f.Body.Address + 3);
   Check(f.Model[pointer] == value, "Preserve the existing numeric truncation and terminator handling: " + row);
}
// Capture raw numeric behavior before the separate script-formatting pass; this is
// deliberately not validation of whether out-of-range values make sense in-game.
foreach (bool external in new[] { false, true })
foreach (var (row, value) in new[] { ("-1", 255), ("256", 0), ("254", external ? 255 : 254) }) {
   using var f = Fixture(external);
   var token = new ModelDelta();
   string source = "move.player <" + (external ? "004000" : "auto") + ">\n{\n" + row + "\n}\nend";
   var result = f.Tool.ScriptParser.Compile(token, f.Model, WorkflowFixture.Root, ref source, out _, out _);
   Check(result != null && (external ? f.Model[0x4000] : result[^2]) == value,
      "Raw numeric wrapping and existing-stream terminator protection retain their previous behavior.");
}
if (!args.Contains("--compatibility-only")) {
using (var f = Fixture()) {
   f.Edit(WorkflowFixture.Source(1, 2)); f.View.Undo.Execute(null); f.Dispatch.Flush();
   var before = State(f);
   f.Edit(Source("move.player", "auto", "walk_upp", dialogue: true));
   Check(f.Body.HasError && Matches(before, f) && f.View.Redo.CanExecute(null), "Invalid movement preserves existing Redo.");
   f.View.Undo.Execute(null); f.Dispatch.Flush();
   Check(Matches(before, f) && f.View.Redo.CanExecute(null), "Explicit draft discard preserves Redo without model edits.");
   f.View.Redo.Execute(null); f.Dispatch.Flush(); Check(f.Model[WorkflowFixture.Root + 4] == 2, "Prior successful work remains redoable.");
}
using (var f = Fixture()) {
   var before = State(f); var token = new ModelDelta();
   string source = Source("move.player", "auto", "walk_upp", dialogue: true), original = source;
   var result = f.Tool.ScriptParser.Compile(token, f.Model, WorkflowFixture.Root, ref source, out var moved, out _);
   Check(result == null && Matches(before, f) && !token.HasAnyChange && moved.Count == 0 && source == original,
      "Raw parser callers receive atomic failure without speculative source, moves or token changes.");
}
foreach (string newline in new[] { "\n", "\r\n", "\r" }) using (var f = Fixture()) {
   var before = State(f); var token = new ModelDelta();
   string source = Source("move.npc 8", "??????", "walk_upp", newline, dialogue: true), original = source;
   var result = f.Tool.ScriptParser.Compile(token, f.Model, WorkflowFixture.Root, ref source, out var moved, out _);
   Check(result == null && Matches(before, f) && !token.HasAnyChange && moved.Count == 0 && source == original,
      "Failed placeholder allocation restores earlier dialogue writes, resource allocation and source addresses.");
}
using (var f = Fixture()) {
   f.Edit("msgbox.default <auto>\n{\nwalk_upp\n...\n}\nend");
   Check(!f.Body.HasError && !f.Tool.HasUncompiledScriptChanges, "Movement validation does not reject ordinary dialogue containing those words or punctuation.");
}
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
