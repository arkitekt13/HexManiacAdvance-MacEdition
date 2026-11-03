using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
var singletons = new Singletons();
var model = new PokemonModel(new byte[512], singletons: singletons);
var view = new ViewPort("test.gba", model, InstantDispatch.Instance, singletons) { Width = 16, Height = 16 };
view.Goto.Execute("20"); view.Edit("^review[value. other:]2 ");
view.ChangeHistory.ChangeCompleted(); view.ChangeHistory.TagAsSaved();
var folder = Path.Combine(Path.GetTempPath(), "hma-import-" + Guid.NewGuid().ToString("N"));
try {
   ProjectExport.Export(model, folder);
   var file = Directory.GetFiles(Path.Combine(folder, "tables"), "*.tsv").Single();
   var manifest = Path.Combine(folder, "manifest.txt");
   var originalManifest = File.ReadAllText(manifest);
   var originalBytes = model.RawData.ToArray();
   Check(ProjectExport.Import(view, folder).Fields == 0 && view.ChangeHistory.IsSaved, "Unchanged export/import must not dirty the document.");
   foreach (var row in new[] { "0\t999\t3", "0\t-1\t3", "0\t3\t65536", "0\tbad\t3", "0\t2", "7\t2\t3", "0\t2\t3\n0\t3\t4" }) {
      File.WriteAllText(file, "# review\nindex\tvalue\tother\n" + row + "\n");
      try { ProjectExport.Import(view, folder); throw new Exception("Invalid import was accepted."); }
      catch (ProjectExport.ImportValidationException ex) { Check(ex.Diagnostics.Count > 0 && ex.Diagnostics[0].Row >= 3, "Validation must report a source row and diagnostic."); }
      Check(model.RawData.SequenceEqual(originalBytes) && view.ChangeHistory.IsSaved, "Rejected files must leave data and saved state unchanged.");
   }
   File.WriteAllText(file, "# review\nindex\tvalue\tother\n0\t255\t65535\n1\t7\t8\n");
   var result = ProjectExport.Import(view, folder);
   Check(result.Fields == 4 && model.GetTableModel("review")[0].GetValue("value") == 255, "Valid boundary values must apply without wrapping.");
   view.Undo.Execute(null);
   Check(model.RawData.SequenceEqual(originalBytes), "One undo must revert every field in the import.");
   Check(view.Redo.CanExecute(null), "Undo must make the successful import redoable.");
   File.WriteAllText(file, "# review\nindex\tvalue\tother\n0\t999\t2\n");
   try { ProjectExport.Import(view, folder); } catch (ProjectExport.ImportValidationException) { }
   Check(view.Redo.CanExecute(null), "Rejected import must preserve existing redo history.");
   foreach (var text in new[] { originalManifest.Replace("gameCode\t", "missingCode\t"), originalManifest.Replace("schemaHash\t", "missingSchema\t"), originalManifest + "romSize\t0x200\n" }) {
      File.WriteAllText(manifest, text);
      try { ProjectExport.Import(view, folder); throw new Exception("Bad manifest accepted."); }
      catch (InvalidOperationException) { Check(model.RawData.SequenceEqual(originalBytes), "Invalid manifest must be rejected before mutation."); }
   }
   File.WriteAllText(manifest, originalManifest);
} finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
var textFolder = folder + "-text";
try {
   var textModel = new PokemonModel(new byte[512], singletons: singletons);
   var textView = new ViewPort("test.gba", textModel, InstantDispatch.Instance, singletons) { Width = 16, Height = 16 };
   textView.Goto.Execute("20"); textView.Edit("^text[name\"\"8]1 ");
   textModel.GetTableModel("text", () => textView.ChangeHistory.CurrentChange)[0].SetStringValue("name", "TEST");
   textView.Goto.Execute("60"); textView.Edit("^signed[value.|z]1 ");
   textModel.GetTableModel("signed", () => textView.ChangeHistory.CurrentChange)[0].SetValue("value", -1);
   textView.ChangeHistory.ChangeCompleted(); textView.ChangeHistory.TagAsSaved();
   var original = textModel.RawData.ToArray();
   ProjectExport.Export(textModel, textFolder);
   Check(ProjectExport.Import(textView, textFolder).Fields == 0, "Signed numbers and text must round-trip unchanged.");
   var textFile = Path.Combine(textFolder, "tables", "text.tsv");
   foreach (var cell in new[] { "TOO LONG FOR THIS FIELD", "bad\\q", "bad\\" }) {
      File.WriteAllText(textFile, "# text\nindex\tname\n0\t" + cell + "\n");
      try { ProjectExport.Import(textView, textFolder); throw new Exception("Lossy text import accepted."); }
      catch (ProjectExport.ImportValidationException) { Check(textModel.RawData.SequenceEqual(original), "Invalid text must not be truncated, unescaped silently, or written."); }
   }
   File.WriteAllText(textFile, "# text\nindex\tname\n0\tNEW\n");
   File.WriteAllText(Path.Combine(textFolder, "tables", "signed.tsv"), "# signed\nindex\tvalue\n0\t-129\n");
   try { ProjectExport.Import(textView, textFolder); throw new Exception("Signed overflow accepted."); }
   catch (ProjectExport.ImportValidationException) { Check(textModel.RawData.SequenceEqual(original), "A later invalid file must prevent earlier valid changes from being applied."); }
} finally { if (Directory.Exists(textFolder)) Directory.Delete(textFolder, true); }
var failureFolder = folder + "-failure";
try {
   var failingModel = new FailingModel(singletons);
   var failingView = new ViewPort("test.gba", failingModel, InstantDispatch.Instance, singletons) { Width = 16, Height = 16 };
   failingView.Goto.Execute("20"); failingView.Edit("^review[value. other:]1 ");
   failingView.ChangeHistory.ChangeCompleted();
   failingModel.WriteValue(failingView.ChangeHistory.CurrentChange, 8, 7); failingView.ChangeHistory.ChangeCompleted();
   failingView.Undo.Execute(null);
   var original = failingModel.RawData.ToArray();
   var metadata = failingModel.ExportMetadata(null, singletons.MetadataInfo).Serialize();
   ProjectExport.Export(failingModel, failureFolder);
   File.WriteAllText(Path.Combine(failureFolder, "tables", "review.tsv"), "# review\nindex\tvalue\tother\n0\t1\t2\n");
   failingModel.Armed = true;
   try { ProjectExport.Import(failingView, failureFolder); throw new Exception("Injected write failure was not reached."); }
   catch (IOException) { }
   Check(failingModel.RawData.SequenceEqual(original), "Unexpected failure after an earlier write must roll back all bytes.");
   Check(failingModel.ExportMetadata(null, singletons.MetadataInfo).Serialize().SequenceEqual(metadata), "Rollback must preserve model metadata.");
   Check(failingView.Redo.CanExecute(null), "Rollback must preserve pre-existing redo history.");
} finally { if (Directory.Exists(failureFolder)) Directory.Delete(failureFolder, true); }
Console.WriteLine("PASS: width validation, source diagnostics, full-input validation, unchanged round trip, one-step undo, redo preservation, and manifest guards.");
Console.WriteLine($"CHECKS_PASSED:{checks}");

public class FailingModel : PokemonModel {
   public bool Armed;
   public FailingModel(Singletons singletons) : base(new byte[512], singletons: singletons) { }
   public override byte this[int index] {
      get => base[index];
      set {
         if (Armed && index == 0x21) { Armed = false; throw new IOException("Injected second-field failure."); }
         base[index] = value;
      }
   }
}
