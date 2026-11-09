using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IkarusUpgrade")]

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

/// <summary>Exact reviewed visual upgrade with a mandatory current-project backup, independent of ROM cleanliness.</summary>
public sealed class IkarusUpgrade {
   public const string Title = "Ikarus Visual Upgrade";
   public const string Recommendation = "Recommended for an unedited or minimally edited project. Edited projects are allowed, but existing changes may be overwritten.";
   public const string Compatibility = "Appears compatible with vanilla FireRed, HUBOL, CFRU and SCADE. Automated checks covered English FireRed 1.0, the bundled HUBOL build and the supplied SCADE build. Gameplay, FireRed 1.1 and other CFRU builds require further testing.";
   public const string Changes = "This upgrade replaces graphics and also changes map layouts, movement/elevation permissions, block behaviors/layers and one Five Isle Meadow tile-change routine.";
   public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "resources", "Optional", "Ikarus");
   private const string PatchHash = "CBC79D5613C24D682119C6A43BA191BAAE39A0C7FAB8A0CBF53311891A3C8B7C";
   private const string CreditsHash = "DD3A799FF2B84888EBCBC87A64B1CCF19B6433EA8CB469B678A8586CC00C2513";
   private readonly byte[] patch;
   public string Credits { get; }
   private IkarusUpgrade(byte[] patch, string credits) => (this.patch, Credits) = (patch, credits);
   public static IkarusUpgrade Load(string directory, out string reason) {
      try {
         var patch = File.ReadAllBytes(Path.Combine(directory, "Ikarus.ips"));
         var credits = File.ReadAllBytes(Path.Combine(directory, "Creator-README.txt"));
         if (Hash(patch) != PatchHash || Hash(credits) != CreditsHash) throw new InvalidDataException("The bundled patch or creator credits do not match the reviewed version.");
         reason = null;
         return new IkarusUpgrade(patch, Encoding.UTF8.GetString(credits));
      } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException) {
         reason = "Ikarus Visual Upgrade unavailable: " + ex.Message;
         return null;
      }
   }
   private static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
   public static string DisabledReason(ViewPort source) {
      if (source == null) return "Open an English FireRed ROM first.";
      if (source.UpdateInProgress || !source.InitializationWorkload.IsCompleted) return "Wait for the ROM to finish loading.";
      var data = source.Model.RawData;
      if (data.Length < 0xBD || data[0xAC] != 'B' || data[0xAD] != 'P' || data[0xAE] != 'R' || data[0xAF] != 'E')
         return "Ikarus Visual Upgrade requires an English FireRed ROM. Other games and languages are not supported.";
      if (data.Length < 16 * 1024 * 1024 || data.Length > Patcher.MaximumPatchedLength || source.Model is not PokemonModel)
         return "Open a supported 16–64 MiB FireRed project with its metadata.";
      return null;
   }

   public sealed class Prepared {
      internal readonly ViewPort Source;
      internal readonly string FileName;
      internal readonly byte[] Before, After;
      internal readonly string[] Metadata;
      internal Prepared(ViewPort source, byte[] before, byte[] after, string[] metadata) =>
         (Source, FileName, Before, After, Metadata) = (source, source.FullFileName, before, after, metadata);
      public bool MatchesCurrent() => Source.FullFileName == FileName && Source.Model.RawData.SequenceEqual(Before) &&
         Source.Model.ExportMetadata(null, Source.Singletons.MetadataInfo).Serialize().SequenceEqual(Metadata);
      public string SuggestedBackupName => (string.IsNullOrEmpty(FileName) ? "FireRed" : Path.GetFileNameWithoutExtension(FileName)) + "_before_Ikarus.gba";
   }
   public Prepared Prepare(ViewPort source) {
      var reason = DisabledReason(source);
      if (reason != null) throw new InvalidOperationException(reason);
      if (source.Tools?.CodeTool.HasUncompiledScriptChanges == true) throw new InvalidOperationException("Finish or discard pending/rejected script edits before applying the upgrade.");
      var before = source.Model.RawData.ToArray();
      var metadata = source.Model.ExportMetadata(null, source.Singletons.MetadataInfo).Serialize();
      var candidate = new BasicModel(before.ToArray());
      if (Patcher.ApplyIPSPatch(candidate, patch, new ModelDelta()) < 0) throw new InvalidDataException("The visual upgrade could not be validated.");
      if (candidate.RawData.SequenceEqual(before)) throw new InvalidOperationException("This ROM already contains every byte of the Ikarus Visual Upgrade.");
      // Preflight formatting on an isolated model, before any backup or live write.
      var services = new Singletons(InstantDispatch.Instance);
      var check = new PokemonModel(candidate.RawData.ToArray(), new StoredMetadata(metadata), services);
      check.InitializationWorkload.GetAwaiter().GetResult();
      var expected = new StoredMetadata(metadata).NamedAnchors;
      var actual = check.ExportMetadata(null, services.MetadataInfo).NamedAnchors;
      if (!check.RawData.SequenceEqual(candidate.RawData) || expected.Any(a => !actual.Any(b => b.Name == a.Name && b.Address == a.Address && b.Format == a.Format)))
         throw new InvalidDataException("The upgrade conflicts with this project's data definitions. No patch was applied.");
      return new Prepared(source, before, candidate.RawData, metadata);
   }

   public sealed class Backup {
      private readonly string path;
      private readonly string romHash, metadataHash;
      internal readonly Prepared Snapshot;
      internal Backup(Prepared snapshot, string path, byte[] metadata) =>
         (Snapshot, this.path, romHash, metadataHash) = (snapshot, path, Hash(snapshot.Before), Hash(metadata));
      public string Path => path;
      public bool Verify() => File.Exists(path) && File.Exists(System.IO.Path.ChangeExtension(path, ".toml")) &&
         Hash(File.ReadAllBytes(path)) == romHash && Hash(File.ReadAllBytes(System.IO.Path.ChangeExtension(path, ".toml"))) == metadataHash;
   }
   public Backup WriteBackup(Prepared snapshot, string destination) => WriteBackup(snapshot, destination, null);
   internal Backup WriteBackup(Prepared snapshot, string destination, Action beforeRomPublish) {
      destination = Path.GetFullPath(destination);
      if (!string.Equals(Path.GetExtension(destination), ".gba", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(destination)))
         throw new IOException("Choose a new .gba name for the backup.");
      var sidecar = Path.ChangeExtension(destination, ".toml");
      if (File.Exists(destination) || File.Exists(sidecar) || Directory.Exists(destination) || Directory.Exists(sidecar))
         throw new IOException("Choose another backup name. Existing ROMs and TOML files are never replaced.");
      byte[] metadata = new UTF8Encoding(false).GetBytes(string.Join(Environment.NewLine, snapshot.Metadata) + Environment.NewLine);
      string staging = Path.Combine(Path.GetDirectoryName(destination), ".hma-ikarus-" + Guid.NewGuid().ToString("N"));
      bool ownsSidecar = false;
      try {
         Write(staging + ".gba", snapshot.Before);
         Write(staging + ".toml", metadata);
         File.Move(staging + ".toml", sidecar, overwrite: false);
         ownsSidecar = true;
         beforeRomPublish?.Invoke();
         File.Move(staging + ".gba", destination, overwrite: false);
      } catch {
         if (ownsSidecar) File.Delete(sidecar);
         throw;
      } finally {
         File.Delete(staging + ".gba");
         File.Delete(staging + ".toml");
      }
      var backup = new Backup(snapshot, destination, metadata);
      if (!backup.Verify()) throw new IOException("The backup did not verify. The upgrade was not applied.");
      return backup;
   }
   private static void Write(string path, byte[] data) {
      using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
      stream.Write(data);
      stream.Flush(true);
   }
   public void Apply(Prepared snapshot, Backup backup) {
      if (backup == null || !ReferenceEquals(backup.Snapshot, snapshot) || !backup.Verify())
         throw new IOException("A verified matching ROM/TOML backup is required before applying Ikarus.");
      if (!snapshot.MatchesCurrent() || snapshot.Source.Tools?.CodeTool.HasUncompiledScriptChanges == true)
         throw new InvalidOperationException("The project changed while preparing the upgrade. Your backup is retained; run the utility again.");
      var source = snapshot.Source;
      var change = ReloadingModelDelta.Apply(source.Model, snapshot.After, source.Singletons.MetadataInfo);
      source.ChangeHistory.InsertCustomChange(change);
      source.ChangeHistory.ChangeCompleted();
   }
}
