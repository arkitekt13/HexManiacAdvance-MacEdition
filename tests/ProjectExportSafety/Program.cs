using System.Text.Json;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
var singletons = new Singletons();
ViewPort Fixture(bool oldTable) {
   var model = new PokemonModel(new byte[512], singletons: singletons);
   var view = new ViewPort("test.gba", model, InstantDispatch.Instance, singletons) { Width = 16, Height = 16 };
   view.Goto.Execute("20"); view.Edit("^main[value.]1 ");
   if (oldTable) { view.Goto.Execute("60"); view.Edit("^old[value.]1 "); }
   view.ChangeHistory.ChangeCompleted(); view.ChangeHistory.TagAsSaved();
   return view;
}
var root = Path.Combine(Path.GetTempPath(), "hma-export-" + Guid.NewGuid().ToString("N"));
var before = Fixture(true); var current = Fixture(false);
try {
   ProjectExport.Export(before.Model, root);
   var manifest = Path.Combine(root, "manifest.txt");
   var original = File.ReadAllBytes(manifest);
   var originalTable = File.ReadAllBytes(Path.Combine(root, "tables", "main.tsv"));
   Thread.Sleep(1100); // Cross a clock boundary: timestamps must not change identical exports.
   ProjectExport.Export(before.Model, root);
   Check(File.ReadAllBytes(manifest).SequenceEqual(original), "Identical exports must have byte-stable manifests.");
   Check(File.ReadAllBytes(Path.Combine(root, "tables", "main.tsv")).SequenceEqual(originalTable), "Identical exports must have byte-stable tables.");
   File.WriteAllText(Path.Combine(root, "tables", "notes.tsv"), "not an exported table");
   ProjectExport.Export(current.Model, root);
   Check(!File.Exists(Path.Combine(root, "tables", "old.tsv")), "Obsolete owned tables must be removed.");
   Check(File.ReadAllText(Path.Combine(root, "tables", "notes.tsv")) == "not an exported table", "Unrelated files must be preserved.");
   Check(ProjectExport.Import(current, root).Fields == 0, "Import must read only the manifest's owned files, ignoring unrelated TSVs.");
   var stableManifest = File.ReadAllBytes(manifest);
   // Simulate interruption after old files were backed up and a new tables folder appeared.
   var stageName = ".hma-export-" + Guid.NewGuid().ToString("N");
   var stage = Path.Combine(root, stageName); Directory.CreateDirectory(stage);
   Directory.Move(Path.Combine(root, "tables"), Path.Combine(stage, "previous-tables"));
   File.Move(manifest, Path.Combine(stage, "previous-manifest.txt"));
   Directory.CreateDirectory(Path.Combine(root, "tables"));
   File.WriteAllText(Path.Combine(root, "tables", "partial.tsv"), "incomplete");
   var pending = Path.Combine(root, ".hma-export.pending");
   File.WriteAllText(pending, JsonSerializer.Serialize(new { Stage = stageName, HadTables = true, HadManifest = true, Committed = false }));
   try { ProjectExport.Import(current, root); throw new Exception("Interrupted export accepted."); }
   catch (InvalidOperationException) { Check(current.ChangeHistory.IsSaved, "Interrupted export must not edit the model."); }
   ProjectExport.Export(current.Model, root);
   Check(!File.Exists(pending) && !Directory.Exists(stage), "Recovery must clean journal and staging files.");
   Check(File.ReadAllBytes(manifest).SequenceEqual(stableManifest) && File.Exists(Path.Combine(root, "tables", "notes.tsv")), "Recovery must retain a complete export and unrelated files.");
   File.WriteAllText(pending, JsonSerializer.Serialize(new { Stage = "../../outside", HadTables = true, HadManifest = true, Committed = false }));
   try { ProjectExport.Export(current.Model, root); throw new Exception("Unsafe recovery path accepted."); }
   catch (IOException) { Check(File.ReadAllBytes(manifest).SequenceEqual(stableManifest), "Unsafe recovery paths must not touch existing exports."); }
   File.Delete(pending);
   using (var held = new FileStream(Path.Combine(root, ".hma-export.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
      try { ProjectExport.Export(current.Model, root); throw new Exception("Concurrent export lock ignored."); }
      catch (IOException) { Check(File.ReadAllBytes(manifest).SequenceEqual(stableManifest), "Locked exports must remain unchanged."); }
   }
} finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
var collision = root + "-collision";
try {
   Directory.CreateDirectory(Path.Combine(collision, "tables"));
   File.WriteAllText(Path.Combine(collision, "tables", "main.tsv"), "user file");
   try { ProjectExport.Export(current.Model, collision); throw new Exception("Unowned file overwritten."); }
   catch (IOException) { Check(File.ReadAllText(Path.Combine(collision, "tables", "main.tsv")) == "user file", "Filename collisions with unowned files must fail without overwriting them."); }
} finally { if (Directory.Exists(collision)) Directory.Delete(collision, true); }
var failure = root + "-failure";
try {
   ProjectExport.Export(current.Model, failure);
   var path = Path.Combine(failure, "tables", "main.tsv");
   var old = File.ReadAllBytes(path);
   File.Delete(Path.Combine(failure, "manifest.txt"));
   Directory.CreateDirectory(Path.Combine(failure, "manifest.txt")); // Forces final publication to fail after table replacement.
   try { ProjectExport.Export(current.Model, failure); throw new Exception("Publish failure not reached."); }
   catch (IOException) {
      Check(File.ReadAllBytes(path).SequenceEqual(old), "Publication failure must roll back the old tables.");
      Check(!File.Exists(Path.Combine(failure, ".hma-export.pending")), "Successful rollback must clear its journal.");
   }
} finally { if (Directory.Exists(failure)) Directory.Delete(failure, true); }
Console.WriteLine("PASS: stable exports, owned-file cleanup, unrelated-file preservation, interrupted-export recovery, locks, unsafe paths, and publication rollback.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
