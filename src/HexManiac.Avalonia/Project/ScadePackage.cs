using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ScadeInstaller")]

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

/// <summary>A versioned SCADE patch and its matching creator metadata; never edits the source model.</summary>
public sealed class ScadePackage {
   public const string BaseGame = "English FireRed 1.0";
   public sealed class Manifest {
      public string Version { get; set; }
      public string BaseRom { get; set; }
      public int SourceLength { get; set; }
      public uint SourceCrc32 { get; set; }
      public string PatchSha256 { get; set; }
      public string TomlSha256 { get; set; }
   }
   private readonly Manifest manifest;
   private readonly byte[] patch, toml;
   private ScadePackage(Manifest manifest, byte[] patch, byte[] toml) => (this.manifest, this.patch, this.toml) = (manifest, patch, toml);
   public string Version => manifest.Version;
   public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "resources", "Optional", "SCADE");

   public static ScadePackage Load(string directory, out string reason) {
      reason = "The SCADE patch and matching definitions are not included in this build.";
      var manifestPath = Path.Combine(directory, "scade.json");
      var patchPath = Path.Combine(directory, "SCADE.bps");
      var tomlPath = Path.Combine(directory, "SCADE.toml");
      if (!File.Exists(manifestPath) || !File.Exists(patchPath) || !File.Exists(tomlPath)) return null;
      try {
         if (new FileInfo(manifestPath).Length > 16 * 1024 || new FileInfo(patchPath).Length > Patcher.MaximumPatchedLength ||
             new FileInfo(tomlPath).Length > 4 * 1024 * 1024) throw new InvalidDataException("SCADE assets exceed the supported size.");
         var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
         if (manifest == null || manifest.BaseRom != BaseGame || string.IsNullOrWhiteSpace(manifest.Version) ||
             manifest.SourceLength <= 0xBC || manifest.SourceLength > Patcher.MaximumPatchedLength ||
             !IsHash(manifest.PatchSha256) || !IsHash(manifest.TomlSha256)) throw new InvalidDataException("Invalid SCADE manifest.");
         var patch = File.ReadAllBytes(patchPath);
         var toml = File.ReadAllBytes(tomlPath);
         if (!Matches(patch, manifest.PatchSha256) || !Matches(toml, manifest.TomlSha256))
            throw new InvalidDataException("The SCADE patch or TOML does not match this release.");
         // Parse through HMA's actual reader; preserve the original bytes for the first open.
         using var text = new StringReader(new UTF8Encoding(false, true).GetString(toml).TrimStart('\uFEFF'));
         var lines = new System.Collections.Generic.List<string>();
         for (var line = text.ReadLine(); line != null; line = text.ReadLine()) lines.Add(line);
         var metadata = new StoredMetadata(lines.ToArray());
         if (metadata.IsEmpty || metadata.NamedAnchors.Count == 0 ||
             StoredMetadata.NeedVersionUpdate(metadata.Version, typeof(Singletons).Assembly.GetName().Version.ToString()))
            throw new InvalidDataException("The supplied SCADE definitions are empty or would be upgraded from application defaults.");
         reason = null;
         return new ScadePackage(manifest, patch, toml);
      } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or FormatException or OverflowException) {
         reason = "SCADE unavailable: " + ex.Message;
         return null;
      }
   }
   private static bool IsHash(string value) => value?.Length == 64 && value.All(Uri.IsHexDigit);
   private static bool Matches(byte[] data, string hash) => string.Equals(Convert.ToHexString(SHA256.HashData(data)), hash, StringComparison.OrdinalIgnoreCase);
   public bool MatchesBase(byte[] source) => source != null && source.Length == manifest.SourceLength &&
      source[0xAC] == 'B' && source[0xAD] == 'P' && source[0xAE] == 'R' && source[0xAF] == 'E' && source[0xBC] == 0 &&
      (uint)Patcher.CalcCRC32(source) == manifest.SourceCrc32;
   public string DisabledReason(IViewPort source) {
      if (source == null) return "Open the unmodified English FireRed 1.0 base ROM first.";
      if (source.UpdateInProgress) return "Wait for the ROM to finish loading.";
      if (source.Tools?.CodeTool?.HasUncompiledScriptChanges == true)
         return "Finish or discard pending/rejected script edits before creating a SCADE ROM.";
      if (!MatchesBase(source.Model.RawData)) return "SCADE requires its exact unmodified English FireRed 1.0 base ROM.";
      return null;
   }
   public byte[] Create(byte[] source) {
      if (!MatchesBase(source)) throw new InvalidDataException("SCADE requires its exact unmodified English FireRed 1.0 base ROM.");
      return BpsPatch.Apply(source, patch);
   }

   /// <summary>Publish TOML before ROM, without replacement. Remove owned files on ordinary failure.</summary>
   public void SaveNewCopy(string destination, byte[] bytes) => SaveNewCopy(destination, bytes, null);

   // Fault-injection seam for publication errors; never used by the UI.
   internal void SaveNewCopy(string destination, byte[] bytes, Action beforePublishRom) {
      if (bytes == null || bytes.Length > Patcher.MaximumPatchedLength || patch.Length < 12 ||
          (uint)Patcher.CalcCRC32(bytes) != System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(patch.AsSpan(patch.Length - 8, 4)))
         throw new InvalidDataException("Only verified SCADE output can be saved.");
      destination = Path.GetFullPath(destination);
      if (!string.Equals(Path.GetExtension(destination), ".gba", StringComparison.OrdinalIgnoreCase) ||
          string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(destination)))
         throw new IOException("Choose a new .gba filename for the SCADE ROM.");
      var sidecar = Path.ChangeExtension(destination, ".toml");
      if (File.Exists(destination) || Directory.Exists(destination) || File.Exists(sidecar) || Directory.Exists(sidecar))
         throw new IOException("Choose another name. Existing ROMs and TOML files are never replaced.");
      var folder = Path.GetDirectoryName(destination);
      var staging = Path.Combine(folder, ".hma-scade-" + Guid.NewGuid().ToString("N"));
      var stagedRom = staging + ".gba";
      var stagedToml = staging + ".toml";
      bool publishedToml = false;
      try {
         Write(stagedRom, bytes);
         Write(stagedToml, toml);
         File.Move(stagedToml, sidecar, overwrite: false);
         publishedToml = true;
         beforePublishRom?.Invoke();
         File.Move(stagedRom, destination, overwrite: false);
      } catch {
         if (publishedToml) File.Delete(sidecar);
         throw;
      } finally {
         File.Delete(stagedRom);
         File.Delete(stagedToml);
      }
   }
   private static void Write(string path, byte[] bytes) {
      using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
      stream.Write(bytes);
      stream.Flush(true);
   }
}
