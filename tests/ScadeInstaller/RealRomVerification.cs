using System.Security.Cryptography;
using System.Text.Json;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;

// Opt-in only. Never discover, copy into the repository or modify a user's original ROM.
internal static class RealRomVerification {
   public static int Run(string sourcePath, string independentOutputPath) {
      int checks = 0;
      void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
      string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
      var source = File.ReadAllBytes(sourcePath);
      var sourceHash = Hash(source);
      var directory = Path.Combine(Path.GetTempPath(), "hma-scade-real-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(directory);
      try {
         var package = ScadePackage.Load(ScadePackage.DefaultDirectory, out var reason);
         Check(package != null && package.MatchesBase(source), "creator patch matches supplied clean FireRed 1.0");
         var result = package.Create(source);
         Check(Hash(source) == sourceHash, "patching preserves input bytes");
         Check(result.SequenceEqual(File.ReadAllBytes(independentOutputPath)), "current SCADE patch output agrees byte for byte with independent patcher");
         Check(result.Length == 33554432 && Hash(result) == "CB1D9C40063C8721374F2F217D27762463E2256CF744B5B4C3DEB3490CCD6350", "verified real output identity");
         var path = Path.Combine(directory, "SCADE verification.gba");
         var sidecar = Path.ChangeExtension(path, ".toml");
         package.SaveNewCopy(path, result);
         var suppliedBytes = File.ReadAllBytes(Path.Combine(ScadePackage.DefaultDirectory, "SCADE.toml"));
         Check(File.ReadAllBytes(sidecar).SequenceEqual(suppliedBytes), "real output TOML is byte-identical before opening");
         var supplied = new StoredMetadata(File.ReadAllLines(sidecar));
         int romWrites = 0, metadataWrites = 0;
         var fs = new StubFileSystem {
            MetadataFor = name => File.Exists(Path.ChangeExtension(name, ".toml")) ? File.ReadAllLines(Path.ChangeExtension(name, ".toml")) : Array.Empty<string>(),
            Save = file => { File.WriteAllBytes(file.Name, file.Contents); romWrites++; return true; },
            SaveMetadata = (name, lines) => { File.WriteAllLines(Path.ChangeExtension(name, ".toml"), lines); metadataWrites++; return true; },
         };
         var editor = new EditorViewModel(fs, InstantDispatch.Instance);
         var baseCopy = Path.Combine(directory, "Clean base copy.gba");
         File.WriteAllBytes(baseCopy, source);
         var baseView = editor.OpenFileAsync(new LoadedFile(baseCopy, source.ToArray())).GetAwaiter().GetResult();
         Check(baseView != null, "normal HMA loader opens the clean base copy");
         baseView.Model.InitializationWorkload.GetAwaiter().GetResult();baseView.InitializationWorkload.GetAwaiter().GetResult();
         Check(baseView.Model.RawData.SequenceEqual(source) && package.DisabledReason(baseView) == null, "normal loaded clean base is eligible for SCADE utility");
         romWrites = metadataWrites = 0;
         var opened = editor.OpenFileAsync(new LoadedFile(path, File.ReadAllBytes(path))).GetAwaiter().GetResult();
         Check(opened != null, "normal HMA file-opening path accepts real patched ROM");
         opened.Model.InitializationWorkload.GetAwaiter().GetResult();
         opened.InitializationWorkload.GetAwaiter().GetResult();
         Check(opened.Model.RawData.SequenceEqual(result), "initial HMA loading does not modify ROM bytes");
         Check(File.ReadAllBytes(sidecar).SequenceEqual(suppliedBytes) && metadataWrites == 0, "initial HMA opening does not regenerate the creator TOML");
         int Address(ViewPort view, string name) => view.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, name);
         var missing = supplied.NamedAnchors.Where(a => Address(opened, a.Name) != a.Address).Select(a => a.Name).ToArray();
         Console.WriteLine("REAL_INITIAL_ANCHOR_MISMATCHES:" + JsonSerializer.Serialize(missing));
         Check(missing.Length == 0, "all creator named anchor addresses load unchanged");
         foreach (var (name, address, count) in new[] {
            ("data.pokemon.names", 0x161F428, 1489), ("data.pokemon.stats", 0x1A92720, 1489),
            ("data.pokemon.moves.names", 0x10F8EDC, 992), ("data.trainers.stats", 0x1273158, 256),
            ("data.maps.banks", 0x3526A8, 43),
            ("data.abilities.descriptions", 0x100B050, 317), ("graphics.moves.backgrounds.all", 0x100B544, 27),
         }) {
            var run = opened.Model.GetNextRun(address) as ITableRun;
            Check(Address(opened, name) == address && run?.ElementCount == count, "real custom table address/length: " + name);
         }
         var snapshots = new List<string>();
         foreach (int map in new[] { 3000, 3001, 4000 }) {
            var snapshot = ScriptMapSnapshot.Create(opened, map);
            Check(snapshot.Image.PixelWidth > 0 && snapshot.Image.PixelHeight > 0 && snapshot.Image.PixelData.Length > 0, "real patched map snapshot renders: " + map);
            snapshots.Add(snapshot.Caption);
         }
         Check(opened.Model.RawData.SequenceEqual(result), "map references leave real patched ROM unchanged");
         // Force a real normal Save: an unchanged document's Save is deliberately a no-op.
         int statsAddress = Address(opened, "data.pokemon.stats");
         byte before = opened.Model[statsAddress + 28];
         opened.ChangeHistory.CurrentChange.ChangeData(opened.Model, statsAddress + 28, (byte)(before ^ 1));
         opened.ChangeHistory.ChangeCompleted();
         Check(!opened.ChangeHistory.IsSaved, "disposable stat edit requires saving");
         opened.Save.Execute(fs);
         Check(romWrites == 1 && metadataWrites == 1 && opened.ChangeHistory.IsSaved, "normal Save actually writes both disposable ROM and project metadata");
         var saved = new StoredMetadata(File.ReadAllLines(sidecar));
         var missingSaved = supplied.NamedAnchors.Where(a => !saved.NamedAnchors.Any(x => x.Name == a.Name && x.Address == a.Address)).Select(a => a.Name).ToArray();
         Console.WriteLine("REAL_SAVED_ANCHOR_MISMATCHES:" + JsonSerializer.Serialize(missingSaved));
         Check(missingSaved.Length == 0, "all supplied anchor addresses survive actual serialization");
         var changedFormats = supplied.NamedAnchors.Where(a => saved.NamedAnchors.Single(x => x.Name == a.Name).Format != a.Format).Select(a => a.Name).ToArray();
         Console.WriteLine("REAL_SAVED_FORMAT_MISMATCHES:" + JsonSerializer.Serialize(changedFormats));
         foreach (var name in changedFormats) Console.WriteLine("REAL_FORMAT_DETAIL:" + JsonSerializer.Serialize(new {name, supplied = supplied.NamedAnchors.Single(a=>a.Name==name).Format, saved = saved.NamedAnchors.Single(a=>a.Name==name).Format}));
         Check(changedFormats.Length == 0, "all creator anchor formats survive actual serialization");
         foreach (string name in new[] { "data.pokemon.names", "data.pokemon.stats", "data.pokemon.moves.names", "data.trainers.stats", "data.maps.banks" }) {
            Check(saved.NamedAnchors.Single(a => a.Name == name).Format == supplied.NamedAnchors.Single(a => a.Name == name).Format, "supplied core table format retained after actual Save: " + name);
         }
         opened.Undo.Execute(null);
         Check(opened.Model.RawData.SequenceEqual(result), "Undo after real Save restores original patched bytes");
         opened.Redo.Execute(null);
         Check(opened.Model[statsAddress + 28] == (byte)(before ^ 1), "Redo restores disposable edit");
         var reopened = editor.OpenFileAsync(new LoadedFile(path, File.ReadAllBytes(path))).GetAwaiter().GetResult();
         Check(reopened != null, "saved real project reopens");
         reopened.Model.InitializationWorkload.GetAwaiter().GetResult();reopened.InitializationWorkload.GetAwaiter().GetResult();
         Check(supplied.NamedAnchors.All(a => Address(reopened, a.Name) == a.Address), "all supplied anchor addresses survive save/reopen");
         Check(reopened.Model[statsAddress + 28] == (byte)(before ^ 1), "reopened ROM contains saved disposable edit");
         var reopenedSnapshot = ScriptMapSnapshot.Create(reopened, 3000);
         Check(reopenedSnapshot.Image.PixelData.Length > 0, "map reference renders after real save/reopen");
         Check(baseView.Model.RawData.SequenceEqual(source) && File.ReadAllBytes(baseCopy).SequenceEqual(source), "editing and saving SCADE leaves the loaded base copy untouched");
         Check(File.ReadAllBytes(sourcePath).SequenceEqual(source), "uploaded original stays unchanged on disk");
         Console.WriteLine("REAL_MAP_SNAPSHOTS:" + JsonSerializer.Serialize(snapshots));
         Console.WriteLine("REAL_OUTPUT_SHA256:" + Hash(result));
         // Discard only the temporary windows/models, not an application session or user settings.
      } finally { Directory.Delete(directory, true); }
      return checks;
   }
}
