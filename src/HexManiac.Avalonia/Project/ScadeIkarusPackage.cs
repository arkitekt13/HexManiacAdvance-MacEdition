using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ScadeIkarusInstaller")]

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

/// <summary>The reviewed exact-SCADE compatibility pair; never edits the source document.</summary>
public sealed class ScadeIkarusPackage {
   public const string Title = "Ikarus Visual Upgrade — SCADE (Experimental)";
   public const string SourceVersion = "2026-11-15";
   public const string SourceHash = "67c78da450f8631b7c57d92fb9345acd822ac2b3a37efa585656a6f55b15f99c";
   public const string OutputHash = "6e7841faf2a9dff16385f4160ff02c582405098e531b763238de5e06499cc728";
   public const string PatchHash = "32831d272be7f87f28d4b39db90c1cb220387883821b485d73ee7afffaa730ff";
   public const string TomlHash = "9ae24bede428941f7fe77ed7a252ebc0159795475639c6ed62d88f95aac89e50";
   public const string ExperimentalNotice = "Experimental: automated map, asset and save/reopen checks and a basic emulator test passed. Broader battles, regional animations and progression remain unverified.";
   private const int SourceLength = 32 * 1024 * 1024;
   private sealed class Manifest {
      public Manifest() { }
      public string Version { get; set; }
      public string SourceScadeVersion { get; set; }
      public int SourceLength { get; set; }
      public string SourceSha256 { get; set; }
      public string OutputSha256 { get; set; }
      public string PatchSha256 { get; set; }
      public string TomlSha256 { get; set; }
      public string CreditsSha256 { get; set; }
   }
   private readonly byte[] patch, toml;
   private readonly string sourceHash, outputHash;
   private readonly int sourceLength;
   public string Credits { get; }
   public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "resources", "Optional", "IkarusScade");
   private ScadeIkarusPackage(Manifest manifest, byte[] patch, byte[] toml, string credits) =>
      (this.patch, this.toml, sourceHash, outputHash, sourceLength, Credits) =
      (patch, toml, manifest.SourceSha256, manifest.OutputSha256, manifest.SourceLength, credits);

   public static ScadeIkarusPackage Load(string directory, out string reason) =>
      Load(directory, out reason, SourceLength, SourceHash, OutputHash, PatchHash, TomlHash);

   // Fabricated identities exercise publication failures without distributing a game ROM.
   internal static ScadeIkarusPackage LoadTestPackage(string directory, byte[] source, byte[] target, out string reason) =>
      Load(directory, out reason, source.Length, Hash(source), Hash(target),
         Hash(File.ReadAllBytes(Path.Combine(directory, "Ikarus-SCADE.bps"))),
         Hash(File.ReadAllBytes(Path.Combine(directory, "Ikarus-SCADE.toml"))));

   private static ScadeIkarusPackage Load(string directory, out string reason, int length, string sourceHash,
      string outputHash, string patchHash, string tomlHash) {
      reason = "The experimental SCADE/Ikarus package is not included in this build.";
      try {
         var manifestPath = Path.Combine(directory, "ikarus-scade.json");
         if (new FileInfo(manifestPath).Length > 16 * 1024) throw new InvalidDataException("Package manifest exceeds the supported size.");
         var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
         if (manifest == null || string.IsNullOrWhiteSpace(manifest.Version) || manifest.SourceScadeVersion != SourceVersion ||
             manifest.SourceLength != length || !Equal(manifest.SourceSha256, sourceHash) || !Equal(manifest.OutputSha256, outputHash) ||
             !Equal(manifest.PatchSha256, patchHash) || !Equal(manifest.TomlSha256, tomlHash) ||
             manifest.CreditsSha256?.Length != 64 || !manifest.CreditsSha256.All(Uri.IsHexDigit))
            throw new InvalidDataException("The package must identify the reviewed SCADE base, patch, output and matching TOML.");
         byte[] Read(string name, int limit) {
            var path = Path.Combine(directory, name);
            if (new FileInfo(path).Length > limit) throw new InvalidDataException("Package asset exceeds the supported size.");
            return File.ReadAllBytes(path);
         }
         var patch = Read("Ikarus-SCADE.bps", Patcher.MaximumPatchedLength);
         var toml = Read("Ikarus-SCADE.toml", 4 * 1024 * 1024);
         var credits = Read("Creator-Credits.txt", 256 * 1024);
         if (!Equal(Hash(patch), patchHash) || !Equal(Hash(toml), tomlHash) || !Equal(Hash(credits), manifest.CreditsSha256))
            throw new InvalidDataException("The patch, TOML or credits do not match this package.");
         using var reader = new StringReader(new UTF8Encoding(false, true).GetString(toml).TrimStart('\uFEFF'));
         var lines = new List<string>();
         for (var line = reader.ReadLine(); line != null; line = reader.ReadLine()) lines.Add(line);
         var metadata = new StoredMetadata(lines.ToArray());
         if (metadata.IsEmpty || metadata.NamedAnchors.Count == 0 ||
             StoredMetadata.NeedVersionUpdate(metadata.Version, typeof(Singletons).Assembly.GetName().Version.ToString()))
            throw new InvalidDataException("Matching definitions are missing or would be regenerated from defaults.");
         reason = null;
         return new(manifest, patch, toml, Encoding.UTF8.GetString(credits));
      } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or FormatException or OverflowException) {
         reason = "SCADE/Ikarus unavailable: " + ex.Message;
         return null;
      }
   }
   private static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
   private static bool Equal(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
   public bool MatchesBase(byte[] source) => source != null && source.Length == sourceLength && source.Length > 0xBC &&
      source[0xAC] == 'B' && source[0xAD] == 'P' && source[0xAE] == 'R' && source[0xAF] == 'E' && source[0xBC] == 0 && Equal(Hash(source), sourceHash);
   public string DisabledReason(IViewPort source) {
      if (source?.Model == null) return "Open fresh SCADE " + SourceVersion + " output first.";
      if (source.UpdateInProgress || !source.Model.InitializationWorkload.IsCompleted ||
          source is ViewPort view && !view.InitializationWorkload.IsCompleted) return "Wait for the ROM to finish loading.";
      if (source.Tools?.CodeTool?.HasUncompiledScriptChanges == true) return "Finish or discard pending script edits first.";
      return MatchesBase(source.Model.RawData) ? null : "Requires exact fresh SCADE " + SourceVersion + " output. Edited projects, other versions and already-patched games are not supported.";
   }
   public byte[] Create(byte[] source) {
      if (!MatchesBase(source)) throw new InvalidDataException("Requires the exact fresh SCADE " + SourceVersion + " base.");
      var output = BpsPatch.Apply(source, patch);
      if (!Equal(Hash(output), outputHash)) throw new InvalidDataException("The compatibility output did not verify.");
      return output;
   }
   public void SaveNewCopy(string destination, byte[] output) => SaveNewCopy(destination, output, null);
   internal void SaveNewCopy(string destination, byte[] output, Action beforeRomPublish) {
      if (output == null || !Equal(Hash(output), outputHash)) throw new InvalidDataException("Only verified compatibility output can be saved.");
      destination = Path.GetFullPath(destination);
      if (!string.Equals(Path.GetExtension(destination), ".gba", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(destination)))
         throw new IOException("Choose a new .gba filename.");
      var sidecar = Path.ChangeExtension(destination, ".toml");
      if (File.Exists(destination) || Directory.Exists(destination) || File.Exists(sidecar) || Directory.Exists(sidecar))
         throw new IOException("Choose another name. Existing ROMs and TOML files are never replaced.");
      var stage = Path.Combine(Path.GetDirectoryName(destination), ".hma-scade-ikarus-" + Guid.NewGuid().ToString("N"));
      bool ownsToml = false;
      try {
         Write(stage + ".gba", output); Write(stage + ".toml", toml);
         File.Move(stage + ".toml", sidecar, overwrite: false); ownsToml = true;
         beforeRomPublish?.Invoke();
         File.Move(stage + ".gba", destination, overwrite: false);
      } catch {
         if (ownsToml) File.Delete(sidecar);
         throw;
      } finally { File.Delete(stage + ".gba"); File.Delete(stage + ".toml"); }
   }
   private static void Write(string path, byte[] bytes) {
      using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
      stream.Write(bytes); stream.Flush(true);
   }
}
