using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TutorialPatch")]

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

/// <summary>Optional, forward-only tutorial distribution; never patches an editor model.</summary>
public sealed class TutorialPatch {
   public const string BaseGame = "English FireRed 1.1";
   public const string CleanBaseSha256 = "729041B940AFE031302D630FDBE57C0C145F3F7B6D9B8ECA5E98678D0CA4D059";
   public const int CleanBaseLength = 16 * 1024 * 1024;
   public sealed class Manifest {
      public string Title { get; set; }
      public string BaseRom { get; set; }
      public string BaseSha256 { get; set; }
      public string PatchSha256 { get; set; }
      public string OutputSha256 { get; set; }
      public string TomlSha256 { get; set; }
   }

   private readonly Manifest manifest;
   private readonly byte[] patch, toml;
   private readonly string[] metadata;
   private readonly int baseLength;
   private TutorialPatch(Manifest manifest, byte[] patch, byte[] toml, string[] metadata, int baseLength) =>
      (this.manifest, this.patch, this.toml, this.metadata, this.baseLength) = (manifest, patch, toml, metadata, baseLength);
   public string Title => manifest.Title;
   public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "resources", "Optional", "Tutorial");

   public static TutorialPatch Load(string directory, out string reason) => Load(directory, out reason, CleanBaseSha256, CleanBaseLength);

   // Synthetic tests supply fabricated identities; the shipped loader always pins the clean retail base above.
   internal static TutorialPatch LoadTestPackage(string directory, out string reason, byte[] testBase) =>
      Load(directory, out reason, Convert.ToHexString(SHA256.HashData(testBase)), testBase.Length);

   private static TutorialPatch Load(string directory, out string reason, string requiredBaseHash, int requiredBaseLength) {
      reason = "The tutorial patch has not been included in this build yet.";
      var manifestPath = Path.Combine(directory, "tutorial.json");
      var patchPath = Path.Combine(directory, "tutorial.ups");
      var tomlPath = Path.Combine(directory, "tutorial.toml");
      if (!File.Exists(manifestPath) || !File.Exists(patchPath) || !File.Exists(tomlPath)) return null;
      try {
         if (new FileInfo(manifestPath).Length > 16 * 1024 || new FileInfo(patchPath).Length > Patcher.MaximumPatchedLength ||
             new FileInfo(tomlPath).Length > 4 * 1024 * 1024)
            throw new InvalidDataException("Tutorial assets exceed the supported size.");
         var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
         if (manifest == null || string.IsNullOrWhiteSpace(manifest.Title) || manifest.BaseRom != BaseGame ||
             !string.Equals(manifest.BaseSha256, requiredBaseHash, StringComparison.OrdinalIgnoreCase) ||
             !IsHash(manifest.PatchSha256) || !IsHash(manifest.OutputSha256) || !IsHash(manifest.TomlSha256))
            throw new InvalidDataException("The tutorial manifest must identify the exact clean English FireRed 1.1 base and verified patch, output and TOML hashes.");
         var patch = File.ReadAllBytes(patchPath);
         if (!Matches(patch, manifest.PatchSha256) || patch.Length < 18 || !patch.Take(4).SequenceEqual(new byte[] { 85, 80, 83, 49 }))
            throw new InvalidDataException("The tutorial UPS patch does not match its manifest.");
         var toml = File.ReadAllBytes(tomlPath);
         if (!Matches(toml, manifest.TomlSha256)) throw new InvalidDataException("The tutorial TOML does not match its manifest.");
         using var text = new StringReader(new UTF8Encoding(false, true).GetString(toml).TrimStart('\uFEFF'));
         var lines = new System.Collections.Generic.List<string>();
         for (var line = text.ReadLine(); line != null; line = text.ReadLine()) lines.Add(line);
         var metadata = new StoredMetadata(lines.ToArray());
         if (metadata.IsEmpty || metadata.NamedAnchors.Count == 0 ||
             StoredMetadata.NeedVersionUpdate(metadata.Version, typeof(Singletons).Assembly.GetName().Version.ToString()))
            throw new InvalidDataException("Save the tutorial's TOML with this HMA version before packaging; definitions must be present and must not require regeneration from defaults.");
         reason = null;
         return new TutorialPatch(manifest, patch, toml, lines.ToArray(), requiredBaseLength);
      } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or FormatException or OverflowException) {
         reason = "Tutorial unavailable: " + ex.Message;
         return null;
      }
   }

   private static bool IsHash(string value) => value?.Length == 64 && value.All(Uri.IsHexDigit);
   private static bool Matches(byte[] bytes, string hash) => string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), hash, StringComparison.OrdinalIgnoreCase);
   public bool MatchesBase(byte[] source) => source != null && source.Length == baseLength && source.Length > 0xBC &&
      source[0xAC] == 'B' && source[0xAD] == 'P' && source[0xAE] == 'R' && source[0xAF] == 'E' &&
      source[0xBC] == 1 && Matches(source, manifest.BaseSha256);

   public string DisabledReason(IViewPort source) {
      if (source?.Model == null) return "Open the unmodified English FireRed 1.1 ROM first.";
      if (source.UpdateInProgress || !source.Model.InitializationWorkload.IsCompleted ||
          source is ViewPort view && !view.InitializationWorkload.IsCompleted) return "Wait for the ROM to finish loading.";
      if (source.Tools?.CodeTool?.HasUncompiledScriptChanges == true)
         return "Finish or discard pending/rejected script edits before creating the tutorial ROM.";
      if (!MatchesBase(source.Model.RawData)) return "This tutorial requires its exact unmodified English FireRed 1.1 base ROM.";
      return null;
   }

   public byte[] Create(byte[] source) {
      if (!MatchesBase(source)) throw new InvalidDataException("Open the unmodified English FireRed 1.1 ROM required by this tutorial.");
      // A detached model keeps all source bytes, metadata, relocation and history untouched.
      var candidate = new BasicModel(source.ToArray());
      if (Patcher.ApplyUPSPatch(candidate, patch, () => new NoTrackChange(), false, out var direction) < 0 ||
          direction != Patcher.UpsPatchDirection.SourceToDestination || !Matches(candidate.RawData, manifest.OutputSha256))
         throw new InvalidDataException("The tutorial patch failed verification. No ROM was written.");
      var supplied = new StoredMetadata(metadata).NamedAnchors;
      if (supplied.Any(anchor => anchor.Address < 0 || anchor.Address >= candidate.Count))
         throw new InvalidDataException("The supplied tutorial TOML contains an address outside the patched ROM.");
      var services = new Singletons(InstantDispatch.Instance);
      var check = new PokemonModel(candidate.RawData.ToArray(), new StoredMetadata(metadata), services);
      check.InitializationWorkload.GetAwaiter().GetResult();
      var actual = check.ExportMetadata(null, services.MetadataInfo).NamedAnchors;
      if (!check.RawData.SequenceEqual(candidate.RawData) || supplied.Any(a =>
          !actual.Any(b => b.Name == a.Name && b.Address == a.Address && b.Format == a.Format)))
         throw new InvalidDataException("The supplied tutorial TOML does not describe the patched ROM. No output was written.");
      return candidate.RawData;
   }

   /// <summary>Publish the original supplied TOML before the ROM, without replacing either existing file.</summary>
   public void SaveNewCopy(string destination, byte[] bytes) => SaveNewCopy(destination, bytes, null);

   internal void SaveNewCopy(string destination, byte[] bytes, Action beforePublishRom) {
      if (bytes == null || bytes.Length > Patcher.MaximumPatchedLength || !Matches(bytes, manifest.OutputSha256))
         throw new InvalidDataException("Only verified tutorial output can be saved.");
      destination = Path.GetFullPath(destination);
      if (!string.Equals(Path.GetExtension(destination), ".gba", StringComparison.OrdinalIgnoreCase) ||
          string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(destination)))
         throw new IOException("Choose a new .gba filename for the tutorial ROM.");
      var sidecar = Path.ChangeExtension(destination, ".toml");
      if (File.Exists(destination) || Directory.Exists(destination) || File.Exists(sidecar) || Directory.Exists(sidecar))
         throw new IOException("Choose a new filename. Existing ROMs and metadata are never replaced.");
      var temporary = Path.Combine(Path.GetDirectoryName(destination), ".hma-tutorial-" + Guid.NewGuid().ToString("N"));
      bool publishedToml = false;
      try {
         Write(temporary + ".gba", bytes);
         Write(temporary + ".toml", toml);
         File.Move(temporary + ".toml", sidecar, overwrite: false);
         publishedToml = true;
         beforePublishRom?.Invoke();
         File.Move(temporary + ".gba", destination, overwrite: false);
      } catch {
         if (publishedToml) File.Delete(sidecar);
         throw;
      } finally {
         File.Delete(temporary + ".gba");
         File.Delete(temporary + ".toml");
      }
   }
   private static void Write(string path, byte[] bytes) {
      using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
      stream.Write(bytes);
      stream.Flush(true);
   }
}
