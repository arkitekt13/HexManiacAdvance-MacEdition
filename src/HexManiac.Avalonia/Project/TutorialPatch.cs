using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

/// <summary>Optional, forward-only tutorial distribution; never patches an editor model.</summary>
public sealed class TutorialPatch {
   public const string BaseGame = "English FireRed 1.1";
   public sealed class Manifest {
      public string Title { get; set; }
      public string BaseRom { get; set; }
      public string BaseSha256 { get; set; }
      public string PatchSha256 { get; set; }
      public string OutputSha256 { get; set; }
   }

   private readonly Manifest manifest;
   private readonly byte[] patch;
   private TutorialPatch(Manifest manifest, byte[] patch) => (this.manifest, this.patch) = (manifest, patch);
   public string Title => manifest.Title;
   public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "resources", "Optional", "Tutorial");

   public static TutorialPatch Load(string directory, out string reason) {
      reason = "The tutorial patch has not been included in this build yet.";
      var manifestPath = Path.Combine(directory, "tutorial.json");
      var patchPath = Path.Combine(directory, "tutorial.ups");
      if (!File.Exists(manifestPath) || !File.Exists(patchPath)) return null;
      try {
         if (new FileInfo(manifestPath).Length > 16 * 1024 || new FileInfo(patchPath).Length > Patcher.MaximumPatchedLength)
            throw new InvalidDataException("Tutorial assets exceed the supported size.");
         var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
         if (manifest == null || string.IsNullOrWhiteSpace(manifest.Title) || manifest.BaseRom != BaseGame ||
             !IsHash(manifest.BaseSha256) || !IsHash(manifest.PatchSha256) || !IsHash(manifest.OutputSha256))
            throw new InvalidDataException("The tutorial manifest must identify English FireRed 1.1 and three SHA-256 hashes.");
         var patch = File.ReadAllBytes(patchPath);
         if (!Matches(patch, manifest.PatchSha256) || patch.Length < 18 || !patch.Take(4).SequenceEqual(new byte[] { 85, 80, 83, 49 }))
            throw new InvalidDataException("The tutorial UPS patch does not match its manifest.");
         reason = null;
         return new TutorialPatch(manifest, patch);
      } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException) {
         reason = "Tutorial unavailable: " + ex.Message;
         return null;
      }
   }

   private static bool IsHash(string value) => value?.Length == 64 && value.All(Uri.IsHexDigit);
   private static bool Matches(byte[] bytes, string hash) => string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), hash, StringComparison.OrdinalIgnoreCase);
   public bool MatchesBase(byte[] source) => source != null && source.Length > 0xBC &&
      source[0xAC] == 'B' && source[0xAD] == 'P' && source[0xAE] == 'R' && source[0xAF] == 'E' &&
      source[0xBC] == 1 && Matches(source, manifest.BaseSha256);

   public string DisabledReason(IViewPort source) {
      if (source == null) return "Open the unmodified English FireRed 1.1 ROM first.";
      if (source.UpdateInProgress) return "Wait for the ROM to finish loading.";
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
      return candidate.RawData;
   }

   public static void SaveNewCopy(string destination, byte[] bytes) {
      destination = Path.GetFullPath(destination);
      if (!string.Equals(Path.GetExtension(destination), ".gba", StringComparison.OrdinalIgnoreCase))
         throw new IOException("Choose a new .gba filename for the tutorial ROM.");
      if (File.Exists(destination) || Directory.Exists(destination) || File.Exists(Path.ChangeExtension(destination, ".toml")))
         throw new IOException("Choose a new filename. Existing ROMs and metadata are never replaced.");
      var temporary = Path.Combine(Path.GetDirectoryName(destination), ".hma-tutorial-" + Guid.NewGuid().ToString("N"));
      try {
         using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
            stream.Write(bytes);
            stream.Flush(true);
         }
         File.Move(temporary, destination, overwrite: false);
      } finally { if (File.Exists(temporary)) File.Delete(temporary); }
   }
}
