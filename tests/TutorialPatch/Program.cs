using System.Security.Cryptography;
using System.Text.Json;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Performance;
using HavenSoft.HexManiac.Review;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
void Rejected(Action action, string message) {
   try { action(); } catch (Exception ex) when (ex is IOException or InvalidDataException) { Check(true, message); return; }
   throw new Exception(message);
}
string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
var directory = Path.Combine(Path.GetTempPath(), "hma-tutorial-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try {
   Check(TutorialPatch.Load(directory, out var reason) == null && reason.Contains("not been included"), "Missing assets stay unavailable.");
   // A fabricated ROM with header identity, never a commercial ROM.
   var fixture = new SyntheticMapFixture(4, 3);
   var source = fixture.Model.RawData;
   source[0xAC] = (byte)'B'; source[0xAD] = (byte)'P'; source[0xAE] = (byte)'R'; source[0xAF] = (byte)'E'; source[0xBC] = 1;
   var before = source.ToArray();
   var metadata = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize();
   var undoBefore = fixture.ViewPort.Undo.CanExecute(null);
   var destination = source.Concat(new byte[] { 1, 2, 3, 4 }).ToArray();
   destination[0x8000] = 42;
   var patch = Patcher.BuildUpsPatch(source, destination);
   var manifest = new TutorialPatch.Manifest {
      Title = "Synthetic tutorial", BaseRom = TutorialPatch.BaseGame,
      BaseSha256 = Hash(source), PatchSha256 = Hash(patch), OutputSha256 = Hash(destination),
   };
   var manifestPath = Path.Combine(directory, "tutorial.json");
   var patchPath = Path.Combine(directory, "tutorial.ups");
   void Write() { File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest)); File.WriteAllBytes(patchPath, patch); }
   Write();
   var tutorial = TutorialPatch.Load(directory, out reason);
   Check(tutorial != null && reason == null && tutorial.Title == "Synthetic tutorial", "Valid manifest/UPS loads.");
   Check(tutorial.MatchesBase(source), "Exact synthetic base matches.");
   Check(!tutorial.MatchesBase(destination), "Already patched ROM cannot be applied backwards.");
   var result = tutorial.Create(source);
   Check(result.SequenceEqual(destination), "Forward UPS handles changed bytes and expansion exactly.");
   Check(source.SequenceEqual(before), "Successful patching does not change the original bytes.");
   Check(metadata.SequenceEqual(fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize()), "Original pointers/run metadata are preserved.");
   Check(fixture.ViewPort.Undo.CanExecute(null) == undoBefore, "Original Undo history is preserved.");
   var mismatch = source.ToArray(); mismatch[0x9000] ^= 1;
   Rejected(() => tutorial.Create(mismatch), "Changed base is rejected.");
   Rejected(() => tutorial.Create(destination), "Reverse application is rejected.");
   mismatch = source.ToArray(); mismatch[0xBC] = 0;
   manifest.BaseSha256 = Hash(mismatch); Write();
   Check(!TutorialPatch.Load(directory, out reason).MatchesBase(mismatch), "FireRed 1.0 is rejected even with a matching manifest hash.");
   mismatch[0xBC] = 1; mismatch[0xAC] = (byte)'X'; manifest.BaseSha256 = Hash(mismatch); Write();
   Check(!TutorialPatch.Load(directory, out reason).MatchesBase(mismatch), "Wrong game is rejected even with a matching hash.");
   manifest.BaseSha256 = Hash(source); manifest.OutputSha256 = new string('0', 64); Write();
   Rejected(() => TutorialPatch.Load(directory, out reason).Create(source), "Incorrect expected output hash is rejected before writing.");
   manifest.OutputSha256 = Hash(destination);
   patch[^1] ^= 1; manifest.PatchSha256 = Hash(patch); Write();
   Rejected(() => TutorialPatch.Load(directory, out reason).Create(source), "Invalid UPS checksum is rejected even if manifest hash matches.");
   Check(source.SequenceEqual(before) && fixture.ViewPort.Undo.CanExecute(null) == undoBefore, "Failures also preserve source and history.");
   manifest.PatchSha256 = new string('0', 64); Write();
   Check(TutorialPatch.Load(directory, out reason) == null && reason.Contains("manifest"), "Mismatched patch hash disables the tutorial.");
   manifest.BaseRom = "English FireRed 1.0"; Write();
   Check(TutorialPatch.Load(directory, out reason) == null, "Wrong manifest base disables the tutorial.");
   File.WriteAllText(manifestPath, "{ broken");
   Check(TutorialPatch.Load(directory, out reason) == null && reason.StartsWith("Tutorial unavailable"), "Malformed manifest fails safely.");
   var output = Path.Combine(directory, "Tutorial.gba");
   TutorialPatch.SaveNewCopy(output, result);
   Check(File.ReadAllBytes(output).SequenceEqual(destination), "Verified output can be saved as a separate ROM.");
   Rejected(() => TutorialPatch.SaveNewCopy(output, source), "Existing ROM cannot be overwritten.");
   Check(File.ReadAllBytes(output).SequenceEqual(destination), "Rejected overwrite preserves existing output.");
   var sidecarOutput = Path.Combine(directory, "WithMetadata.gba");
   File.WriteAllText(Path.ChangeExtension(sidecarOutput, ".toml"), "keep me");
   Rejected(() => TutorialPatch.SaveNewCopy(sidecarOutput, source), "Existing metadata prevents reusing an output name.");
   Check(!File.Exists(sidecarOutput) && File.ReadAllText(Path.ChangeExtension(sidecarOutput, ".toml")) == "keep me", "Existing metadata is preserved.");
   Rejected(() => TutorialPatch.SaveNewCopy(Path.Combine(directory, "wrong.txt"), source), "Non-ROM extension is rejected.");
   Rejected(() => TutorialPatch.SaveNewCopy(Path.Combine(directory, "missing", "output.gba"), source), "Failed output leaves no completed ROM.");
   Check(!Directory.EnumerateFiles(directory, ".hma-tutorial-*").Any(), "No staging files remain after writes or failures.");
   Check(source.SequenceEqual(before), "All output operations leave original bytes untouched.");
   using var workflow = new WorkflowFixture();
   workflow.Model.RawData[0xBC] = 1;
   var workflowBase = workflow.Model.RawData.ToArray();
   var workflowOutput = workflowBase.ToArray(); workflowOutput[0x6000] ^= 1;
   patch = Patcher.BuildUpsPatch(workflowBase, workflowOutput);
   manifest.BaseRom = TutorialPatch.BaseGame; manifest.BaseSha256 = Hash(workflowBase);
   manifest.OutputSha256 = Hash(workflowOutput); manifest.PatchSha256 = Hash(patch); Write();
   tutorial = TutorialPatch.Load(directory, out reason);
   Check(tutorial.DisabledReason(null) != null, "No selected ROM disables tutorial creation.");
   Check(tutorial.DisabledReason(workflow.View) == null, "An exact base without drafts enables creation.");
   workflow.Edit(WorkflowFixture.Source(1, 2), settle: false);
   var pendingState = workflow.State(); var pendingSource = workflow.Body.Content;
   Check(workflow.Tool.HasPendingScriptChanges && tutorial.DisabledReason(workflow.View).Contains("pending/rejected"), "Pending source blocks the tutorial before hash inspection.");
   Check(pendingState.EqualsModel(workflow) && workflow.Body.Content == pendingSource && workflow.Tool.HasPendingScriptChanges,
      "Menu eligibility does not compile, discard or change queued source.");
   workflow.Dispatch.Flush();
   Check(tutorial.DisabledReason(workflow.View) != null, "Compiled edits no longer match the required base.");
   workflow.View.Undo.Execute(null); workflow.Dispatch.Flush();
   Check(tutorial.DisabledReason(workflow.View) == null, "Undo back to the exact base enables the tutorial again.");
   workflow.Edit(WorkflowFixture.Source(1, 1) + "\nnot_a_command");
   var rejectedState = workflow.State(); var rejectedSource = workflow.Body.Content;
   Check(workflow.Body.HasError && tutorial.DisabledReason(workflow.View).Contains("pending/rejected"), "Rejected drafts block creation even though ROM bytes match.");
   Check(rejectedState.EqualsModel(workflow) && workflow.Body.Content == rejectedSource && workflow.Body.Editor.ErrorLocations.Count > 0,
      "Eligibility retains rejected source, diagnostics and model state.");
   Check(workflow.View.Redo.CanExecute(null), "Rejected draft eligibility preserves the existing Redo entry.");
} finally { Directory.Delete(directory, true); }
Console.WriteLine($"CHECKS_PASSED:{checks}");
