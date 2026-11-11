using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

// A fabricated one-byte lesson patch on the exact real base; never the unfinished tutorial project.
internal static class RealBaseVerification {
   public static int Run(string input) {
      int checks = 0;
      void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
      string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
      var source = File.ReadAllBytes(input);
      Check(Hash(source) == TutorialPatch.CleanBaseSha256, "Exact clean English FireRed 1.1 fixture.");
      var folder = Path.Combine(Path.GetTempPath(), "hma-tutorial-real-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(folder);
      try {
         int romWrites = 0, metadataWrites = 0;
         var fs = new StubFileSystem {
            MetadataFor = name => File.Exists(Path.ChangeExtension(name, ".toml")) ? File.ReadAllLines(Path.ChangeExtension(name, ".toml")) : Array.Empty<string>(),
            Save = file => { File.WriteAllBytes(file.Name, file.Contents); romWrites++; return true; },
            SaveMetadata = (name, lines) => { File.WriteAllLines(Path.ChangeExtension(name, ".toml"), lines); metadataWrites++; return true; },
         };
         var editor = new EditorViewModel(fs, InstantDispatch.Instance);
         var baseline = editor.OpenFileAsync(new LoadedFile(Path.Combine(folder, "base.gba"), source.ToArray())).GetAwaiter().GetResult();
         baseline.InitializationWorkload.GetAwaiter().GetResult();
         Check(baseline.Model.RawData.SequenceEqual(source), "Initial real base load leaves bytes unchanged.");
         var definitions = baseline.Model.ExportMetadata(null, editor.Singletons.MetadataInfo);
         var toml = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, definitions.Serialize()) + Environment.NewLine);
         var target = source.ToArray(); target[0x700000] ^= 1;
         var patch = Patcher.BuildUpsPatch(source, target);
         var manifest = new TutorialPatch.Manifest {
            Title = "Temporary verification lesson", BaseRom = TutorialPatch.BaseGame,
            BaseSha256 = Hash(source), PatchSha256 = Hash(patch), OutputSha256 = Hash(target), TomlSha256 = Hash(toml),
         };
         File.WriteAllBytes(Path.Combine(folder, "tutorial.ups"), patch);
         File.WriteAllBytes(Path.Combine(folder, "tutorial.toml"), toml);
         File.WriteAllText(Path.Combine(folder, "tutorial.json"), JsonSerializer.Serialize(manifest));
         var tutorial = TutorialPatch.Load(folder, out var reason);
         Check(tutorial != null && reason == null && tutorial.MatchesBase(source), "Public production loader accepts the pinned real base.");
         var edited = source.ToArray(); edited[0x700001] ^= 1;
         Check(!tutorial.MatchesBase(edited), "One changed byte rejects an edited 1.1 base.");
         var wrongRevision = source.ToArray(); wrongRevision[0xBC] = 0;
         Check(!tutorial.MatchesBase(wrongRevision), "Revision zero is rejected.");
         var bytes = tutorial.Create(source);
         Check(bytes.SequenceEqual(target), "Real-base output matches the fabricated lesson exactly.");
         var output = Path.Combine(folder, "My Tutorial.gba");
         tutorial.SaveNewCopy(output, bytes);
         Check(File.ReadAllBytes(output).SequenceEqual(target), "Output ROM published exactly.");
         Check(File.ReadAllBytes(Path.ChangeExtension(output, ".toml")).SequenceEqual(toml), "Original supplied TOML published exactly.");
         romWrites = metadataWrites = 0;
         var view = editor.OpenFileAsync(new LoadedFile(output, File.ReadAllBytes(output))).GetAwaiter().GetResult();
         view.InitializationWorkload.GetAwaiter().GetResult();
         Check(view.Model.RawData.SequenceEqual(target), "Initial normal opening retains lesson bytes.");
         Check(metadataWrites == 0 && File.ReadAllBytes(Path.ChangeExtension(output, ".toml")).SequenceEqual(toml), "Initial normal opening does not regenerate metadata.");
         bool DefinitionsMatch(ViewPort v) {
            var actual = v.Model.ExportMetadata(null, editor.Singletons.MetadataInfo).NamedAnchors;
            return definitions.NamedAnchors.All(a => actual.Any(b => a.Name == b.Name && a.Address == b.Address && a.Format == b.Format));
         }
         Check(DefinitionsMatch(view), "All real named definitions retained on opening.");
         var snapshot = ScriptMapSnapshot.Create(view, 3000);
         Check(snapshot.Image.PixelData.Length > 0, "Real Pallet Town reference renders with supplied metadata.");
         view.ChangeHistory.CurrentChange.ChangeData(view.Model, 0x700001, (byte)(view.Model[0x700001] ^ 1));
         view.ChangeHistory.ChangeCompleted(); view.Save.Execute(fs);
         Check(romWrites == 1 && metadataWrites == 1 && view.ChangeHistory.IsSaved, "Actual Save writes the ROM/TOML pair.");
         view.Undo.Execute(null); Check(view.Model.RawData.SequenceEqual(target), "Undo restores lesson bytes.");
         view.Redo.Execute(null); var saved = view.Model.RawData.ToArray();
         Check(!saved.SequenceEqual(target), "Redo restores the explicit test edit.");
         var reopened = editor.OpenFileAsync(new LoadedFile(output, File.ReadAllBytes(output))).GetAwaiter().GetResult();
         reopened.InitializationWorkload.GetAwaiter().GetResult();
         Check(reopened.Model.RawData.SequenceEqual(saved) && DefinitionsMatch(reopened), "Actual save/reopen retains bytes and every named definition.");
         Check(baseline.Model.RawData.SequenceEqual(source) && baseline.ChangeHistory.IsSaved, "Original open base and its history remain untouched.");
         Check(Hash(File.ReadAllBytes(input)) == TutorialPatch.CleanBaseSha256, "Original uploaded ROM remains unchanged.");
         Console.WriteLine($"TUTORIAL_REAL_BASE_PASS:anchors={definitions.NamedAnchors.Count}:checks={checks}");
      } finally { Directory.Delete(folder, true); }
      return checks;
   }
}
