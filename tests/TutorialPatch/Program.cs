using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.Core.ViewModels;
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
   byte[] toml = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join("\n", metadata) + "\n# tutorial definitions\n")).ToArray();
   manifest.TomlSha256 = Hash(toml);
   byte[] trustedSource = source;
   var manifestPath = Path.Combine(directory, "tutorial.json");
   var patchPath = Path.Combine(directory, "tutorial.ups");
   var tomlPath = Path.Combine(directory, "tutorial.toml");
   void Write() { File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest)); File.WriteAllBytes(patchPath, patch); File.WriteAllBytes(tomlPath, toml); }
   TutorialPatch Load() => TutorialPatch.LoadTestPackage(directory, out reason, trustedSource);
   Write();
   var tutorial = Load();
   Check(tutorial != null && reason == null && tutorial.Title == "Synthetic tutorial", "Valid manifest/UPS loads.");
   Check(TutorialPatch.Load(directory, out _) == null, "Production loader rejects a manifest that calls fabricated bytes an unmodified retail base.");
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
   Check(!TutorialPatch.LoadTestPackage(directory, out reason, mismatch).MatchesBase(mismatch), "FireRed 1.0 is rejected even with a matching manifest hash.");
   mismatch[0xBC] = 1; mismatch[0xAC] = (byte)'X'; manifest.BaseSha256 = Hash(mismatch); Write();
   Check(!TutorialPatch.LoadTestPackage(directory, out reason, mismatch).MatchesBase(mismatch), "Wrong game is rejected even with a matching hash.");
   manifest.BaseSha256 = Hash(source); manifest.OutputSha256 = new string('0', 64); Write();
   Rejected(() => Load().Create(source), "Incorrect expected output hash is rejected before writing.");
   manifest.OutputSha256 = Hash(destination);
   patch[^1] ^= 1; manifest.PatchSha256 = Hash(patch); Write();
   Rejected(() => Load().Create(source), "Invalid UPS checksum is rejected even if manifest hash matches.");
   Check(source.SequenceEqual(before) && fixture.ViewPort.Undo.CanExecute(null) == undoBefore, "Failures also preserve source and history.");
   manifest.PatchSha256 = new string('0', 64); Write();
   Check(Load() == null && reason.Contains("manifest"), "Mismatched patch hash disables the tutorial.");
   manifest.BaseRom = "English FireRed 1.0"; Write();
   Check(Load() == null, "Wrong manifest base disables the tutorial.");
   File.WriteAllText(manifestPath, "{ broken");
   Check(Load() == null && reason.StartsWith("Tutorial unavailable"), "Malformed manifest fails safely.");
   var output = Path.Combine(directory, "MyLesson.gba");
   tutorial.SaveNewCopy(output, result);
   Check(File.ReadAllBytes(Path.ChangeExtension(output, ".toml")).SequenceEqual(toml), "Original supplied TOML is copied byte for byte before opening.");
   Check(File.ReadAllBytes(output).SequenceEqual(destination), "Verified output can be saved as a separate ROM.");
   Rejected(() => tutorial.SaveNewCopy(output, result), "Existing ROM cannot be overwritten.");
   Check(File.ReadAllBytes(output).SequenceEqual(destination), "Rejected overwrite preserves existing output.");
   var sidecarOutput = Path.Combine(directory, "WithMetadata.gba");
   File.WriteAllText(Path.ChangeExtension(sidecarOutput, ".toml"), "keep me");
   Rejected(() => tutorial.SaveNewCopy(sidecarOutput, result), "Existing metadata prevents reusing an output name.");
   Check(!File.Exists(sidecarOutput) && File.ReadAllText(Path.ChangeExtension(sidecarOutput, ".toml")) == "keep me", "Existing metadata is preserved.");
   Rejected(() => tutorial.SaveNewCopy(Path.Combine(directory, "wrong.txt"), result), "Non-ROM extension is rejected.");
   Rejected(() => tutorial.SaveNewCopy(Path.Combine(directory, "missing", "output.gba"), result), "Failed output leaves no completed ROM.");
   Check(!Directory.EnumerateFiles(directory, ".hma-tutorial-*").Any(), "No staging files remain after writes or failures.");
   Check(source.SequenceEqual(before), "All output operations leave original bytes untouched.");
   var race = Path.Combine(directory, "race.gba");
   Rejected(() => tutorial.SaveNewCopy(race, result, () => {
      Check(File.ReadAllBytes(Path.ChangeExtension(race, ".toml")).SequenceEqual(toml), "TOML exists before the ROM is published.");
      File.WriteAllText(race, "foreign file");
   }), "Publication collision aborts.");
   Check(File.ReadAllText(race) == "foreign file" && !File.Exists(Path.ChangeExtension(race, ".toml")), "Failed publication preserves foreign ROM and removes owned TOML.");
   foreach (bool sidecar in new[] { false, true }) {
      var path = Path.Combine(directory, sidecar ? "toml-directory.gba" : "rom-directory.gba");
      Directory.CreateDirectory(sidecar ? Path.ChangeExtension(path, ".toml") : path);
      Rejected(() => tutorial.SaveNewCopy(path, result), "Directory collision is rejected.");
   }
   Rejected(() => tutorial.SaveNewCopy(Path.Combine(directory, "unverified.gba"), source), "Arbitrary unverified output cannot be published.");
   Check(!Directory.EnumerateFiles(directory, ".hma-tutorial-*").Any(), "Paired publication cleans all staging files.");
   int romWrites = 0, metadataWrites = 0;
   var fs = new StubFileSystem {
      MetadataFor = name => File.Exists(Path.ChangeExtension(name, ".toml")) ? File.ReadAllLines(Path.ChangeExtension(name, ".toml")) : Array.Empty<string>(),
      Save = file => { File.WriteAllBytes(file.Name, file.Contents); romWrites++; return true; },
      SaveMetadata = (name, lines) => { File.WriteAllLines(Path.ChangeExtension(name, ".toml"), lines); metadataWrites++; return true; },
   };
   var editor = new EditorViewModel(fs, InstantDispatch.Instance);
   var opened = editor.OpenFileAsync(new LoadedFile(output, File.ReadAllBytes(output))).GetAwaiter().GetResult();
   opened.Model.InitializationWorkload.GetAwaiter().GetResult(); opened.InitializationWorkload.GetAwaiter().GetResult();
   Check(opened.Model.RawData.SequenceEqual(result), "Normal opening retains exact patched bytes.");
   Check(metadataWrites == 0 && File.ReadAllBytes(Path.ChangeExtension(output, ".toml")).SequenceEqual(toml), "Normal opening uses supplied TOML without regenerating it.");
   var expected = new StoredMetadata(metadata).NamedAnchors;
   bool DefinitionsMatch(IDataModel model) {
      var actual = model.ExportMetadata(null, editor.Singletons.MetadataInfo).NamedAnchors;
      return expected.All(a => actual.Any(b => a.Name == b.Name && a.Address == b.Address && a.Format == b.Format));
   }
   Check(DefinitionsMatch(opened.Model), "Every bundled named definition loads correctly.");
   opened.ChangeHistory.CurrentChange.ChangeData(opened.Model, 0xA000, (byte)(opened.Model[0xA000] ^ 1));
   opened.ChangeHistory.ChangeCompleted(); opened.Save.Execute(fs);
   Check(romWrites == 1 && metadataWrites == 1, "A real normal Save writes both project files.");
   var reopened = editor.OpenFileAsync(new LoadedFile(output, File.ReadAllBytes(output))).GetAwaiter().GetResult();
   reopened.InitializationWorkload.GetAwaiter().GetResult();
   Check(DefinitionsMatch(reopened.Model), "All bundled definitions survive actual Save/reopen.");
   var savedToml = toml.ToArray();
   manifest.BaseRom = TutorialPatch.BaseGame; manifest.BaseSha256 = Hash(source); manifest.OutputSha256 = Hash(destination);
   patch = Patcher.BuildUpsPatch(source, destination); manifest.PatchSha256 = Hash(patch); Write();
   File.Delete(tomlPath); Check(Load() == null, "Missing TOML disables package."); Write();
   File.AppendAllText(tomlPath, "tampered"); Check(Load() == null, "Tampered TOML disables package."); Write();
   toml = Encoding.UTF8.GetBytes("not valid definitions"); manifest.TomlSha256 = Hash(toml); Write();
   Check(Load() == null, "Empty/malformed metadata is rejected even with matching hash.");
   toml = savedToml; manifest.TomlSha256 = Hash(toml); Write();
   manifest.TomlSha256 = null; Write(); Check(Load() == null, "Missing TOML verification hash disables package.");
   manifest.TomlSha256 = Hash(toml);
   toml = Encoding.UTF8.GetBytes(string.Join("\n", metadata.Select(line => line.StartsWith("ApplicationVersion = ") ? "ApplicationVersion = '''0.0.0'''" : line)) + "\n");
   manifest.TomlSha256 = Hash(toml); Write(); Check(Load() == null, "Metadata requiring regeneration from defaults is rejected.");
   toml = savedToml.Concat(Encoding.UTF8.GetBytes("\n[[NamedAnchors]]\nName = '''tutorial.invalid'''\nAddress = 0x700000\nFormat = '''[value.]4'''\n")).ToArray();
   manifest.TomlSha256 = Hash(toml); Write();
   var invalidDefinitions = Load(); Check(invalidDefinitions != null, "Syntactically valid metadata loads before candidate validation.");
   Rejected(() => invalidDefinitions.Create(source), "Out-of-ROM tutorial definitions block creation before publication.");
   toml = savedToml; manifest.TomlSha256 = Hash(toml); Write();
   manifest.BaseSha256 = new string('0',64); Write(); Check(Load() == null, "Manifest cannot silently select a different base.");
   using var workflow = new WorkflowFixture();
   workflow.Model.RawData[0xBC] = 1;
   var workflowBase = workflow.Model.RawData.ToArray();
   var workflowOutput = workflowBase.ToArray(); workflowOutput[0x6000] ^= 1;
   patch = Patcher.BuildUpsPatch(workflowBase, workflowOutput);
   manifest.BaseRom = TutorialPatch.BaseGame; manifest.BaseSha256 = Hash(workflowBase);
   trustedSource = workflowBase;
   toml = Encoding.UTF8.GetBytes(string.Join("\n",workflow.Model.ExportMetadata(null, workflow.Services.MetadataInfo).Serialize()) + "\n");
   manifest.TomlSha256 = Hash(toml);
   manifest.OutputSha256 = Hash(workflowOutput); manifest.PatchSha256 = Hash(patch); Write();
   tutorial = Load();
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
if (args.Length == 1) checks += RealBaseVerification.Run(args[0]);
Console.WriteLine($"CHECKS_PASSED:{checks}");
