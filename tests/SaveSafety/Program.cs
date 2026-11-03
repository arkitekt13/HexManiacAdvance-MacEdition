using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

// Reference data is copied beside this executable by the real editor's build targets.
Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(App).Assembly.Location)!);
var singletons = new Singletons();
var checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
ViewPort DirtyView() {
   var model = new PokemonModel(new byte[512], singletons: singletons);
   var view = new ViewPort("review.gba", model, InstantDispatch.Instance, singletons);
   model.WriteValue(view.ChangeHistory.CurrentChange, 0, 42);
   view.ChangeHistory.ChangeCompleted();
   return view;
}
foreach (var saveAs in new[] { false, true }) {
   var view = DirtyView();
   var metadataSucceeds = false;
   var fs = new StubFileSystem { Save = _ => true, SaveMetadata = (_, _) => metadataSucceeds, RequestNewName = (_, _, _) => "copy.gba" };
   (saveAs ? view.SaveAs : view.Save).Execute(fs);
   Check(!view.ChangeHistory.IsSaved, "A metadata failure must leave Save and Save As unsaved.");
   metadataSucceeds = true;
   view.Save.Execute(fs);
   Check(view.ChangeHistory.IsSaved, "A successful retry must mark the document saved.");
}
foreach (var outcome in new bool?[] { null, false, true }) {
   var view = DirtyView();
   var closed = false;
   view.Closed += (_, _) => closed = true;
   var fs = new StubFileSystem { TrySavePrompt = _ => outcome, SaveMetadata = (_, _) => true };
   view.Close.Execute(fs);
   Check(closed == (outcome != null), "Cancel/failure must retain the tab; discard and successful save may close it.");
}
{
   var view = DirtyView(); var closed = false;
   view.Closed += (_, _) => closed = true;
   view.Close.Execute(new StubFileSystem { TrySavePrompt = _ => true, SaveMetadata = (_, _) => false });
   Check(!closed && !view.ChangeHistory.IsSaved && view.Model.ReferenceCount == 1,
      "Metadata failure while closing must retain the unsaved tab and its model reference.");
}
var folder = Path.Combine(Path.GetTempPath(), "hma-save-safety-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
try {
   var path = Path.Combine(folder, "test.gba"); var original = new byte[] { 1, 2, 3, 4 };
   File.WriteAllBytes(path, original);
   try { AtomicFileWriter.Write(path, stream => { stream.WriteByte(99); throw new IOException("injected disk failure"); }); }
   catch (IOException) { }
   Check(File.ReadAllBytes(path).SequenceEqual(original), "A failed staged write must not modify the old ROM.");
   Check(Directory.GetFiles(folder).Length == 1, "Failed staged writes must clean up their temporary files.");
   AtomicFileWriter.Write(path, stream => stream.WriteByte(9));
   Check(File.ReadAllBytes(path).SequenceEqual(new byte[] { 9 }), "Replacement must not leave trailing bytes when shrinking.");
   var metadata = Path.Combine(folder, "test.toml");
   AtomicFileWriter.WriteAllLines(metadata, new[] { "name = \"Pokémon\"", "version = \"0.6.1\"" });
   Check(File.ReadAllLines(metadata)[0] == "name = \"Pokémon\"", "Metadata must retain UTF-8 content.");
   if (!OperatingSystem.IsWindows()) {
      File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
      AtomicFileWriter.Write(path, stream => stream.WriteByte(8));
      Check(File.GetUnixFileMode(path) == (UnixFileMode.UserRead | UnixFileMode.UserWrite), "Replacement must preserve Unix access permissions.");
      var link = Path.Combine(folder, "linked.gba");
      File.CreateSymbolicLink(link, path);
      AtomicFileWriter.Write(link, stream => stream.WriteByte(7));
      Check(new FileInfo(link).LinkTarget != null && File.ReadAllBytes(path)[0] == 7, "Saving through a symbolic link must update its target and keep the link.");
      File.Delete(link);
   }
   File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
   try { AtomicFileWriter.Write(path, stream => stream.WriteByte(6)); throw new Exception("Read-only save unexpectedly succeeded."); }
   catch (UnauthorizedAccessException) { }
   finally { File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly); }
   Check(File.ReadAllBytes(path)[0] == 7 || OperatingSystem.IsWindows(), "A read-only destination must remain unchanged.");
} finally { Directory.Delete(folder, true); }
foreach (var saveAs in new[] { false, true }) {
   var view = DirtyView(); var metadataWrites = 0;
   var fs = new StubFileSystem { Save = _ => false, SaveMetadata = (_, _) => { metadataWrites++; return true; }, RequestNewName = (_, _, _) => "copy.gba" };
   (saveAs ? view.SaveAs : view.Save).Execute(fs);
   Check(!view.ChangeHistory.IsSaved && metadataWrites == 0 && view.FileName == "review.gba",
      "A failed ROM save must retain dirty state and the original filename without writing metadata.");
}
Console.WriteLine("PASS: save/close failure handling, metadata retry, staged write failure, shrinking, UTF-8, permissions, links, and read-only protection.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
