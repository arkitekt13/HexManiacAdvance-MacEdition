using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HavenSoft.HexManiac.Core.Models;

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

/// <summary>Publish an owned file set with rollback and a recoverable interrupted-export journal.</summary>
internal static class ProjectExportFiles {
   private const string Pending = ".hma-export.pending";
   private record Journal(string Stage, bool HadTables, bool HadManifest, bool Committed);

   public static IReadOnlyList<string> ReadFiles(string directory, Dictionary<string, string> manifest) {
      if (!manifest.TryGetValue("files", out var json)) return Directory.GetFiles(Path.Combine(directory, "tables"), "*.tsv").OrderBy(f => f, StringComparer.Ordinal).ToArray();
      var names = JsonSerializer.Deserialize<string[]>(json) ?? throw new InvalidOperationException("Invalid exported file list.");
      if (names.Any(n => !SafeName(n)) || names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
         throw new InvalidOperationException("Unsafe or duplicate exported file name.");
      return names.Select(n => Path.Combine(directory, "tables", n)).ToArray();
   }

   private static bool SafeName(string name) => !string.IsNullOrEmpty(name) &&
      name == Path.GetFileName(name) && !name.Contains('\\') && !name.Any(char.IsControl) && name.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase);

   public static IDisposable LockForRead(string directory) {
      var path = Path.Combine(directory, ".hma-export.lock");
      if (new FileInfo(path).LinkTarget != null) throw new IOException("Export lock must not be a symbolic link.");
      return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
   }

   public static void EnsureComplete(string directory) {
      if (File.Exists(Path.Combine(directory, Pending))) throw new InvalidOperationException("An interrupted export needs recovery; export to this folder again before importing it.");
   }

   public static void Publish(string directory, IReadOnlyDictionary<string, string> documents, string manifest) {
      directory = Path.GetFullPath(directory);
      Directory.CreateDirectory(directory);
      var tables = Path.Combine(directory, "tables");
      var manifestPath = Path.Combine(directory, "manifest.txt");
      foreach (var path in new[] { tables, manifestPath, Path.Combine(directory, Pending), Path.Combine(directory, ".hma-export.lock") })
         if (new FileInfo(path).LinkTarget != null) throw new IOException("Managed export paths must not be symbolic links.");
      // Keep the lock file in place: deleting a lock name after closing can race a new writer.
      using var fileLock = new FileStream(Path.Combine(directory, ".hma-export.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
      Recover(directory);
      var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      var legacy = !File.Exists(manifestPath);
      if (File.Exists(manifestPath)) {
         var entries = File.ReadAllLines(manifestPath).Where(l => l.Contains('\t')).Select(l => l.Split('\t')).ToArray();
         var list = entries.FirstOrDefault(p => p[0] == "files");
         if (list != null) {
            var names = JsonSerializer.Deserialize<string[]>(list[1]) ?? throw new IOException("Invalid previous file list.");
            if (names.Any(n => !SafeName(n))) throw new IOException("Unsafe previous export file list.");
            foreach (var name in names) owned.Add(name);
         } else legacy = true;
      }
      // Migrate legacy exports only when their generated-file marker proves ownership.
      if (legacy && Directory.Exists(tables)) foreach (var file in Directory.GetFiles(tables, "*.tsv"))
         if (File.ReadLines(file).Take(2).Contains("# exported by HexManiacAdvance - edit values, keep the columns")) owned.Add(Path.GetFileName(file));
      if (Directory.Exists(tables) && Directory.EnumerateDirectories(tables).Any()) throw new IOException("The tables folder contains unmanaged subfolders; choose a clean export folder.");
      foreach (var name in documents.Keys) {
         if (!SafeName(name)) throw new IOException("Unsafe generated table filename.");
         var path = Path.Combine(tables, name);
         if (File.Exists(path) && !owned.Contains(name)) throw new IOException($"Refusing to overwrite unowned table file {name}.");
      }
      foreach (var path in owned.Select(n => Path.Combine(tables, n)).Append(manifestPath))
         if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0) throw new UnauthorizedAccessException("An export file is read-only.");
      var stageName = ".hma-export-" + Guid.NewGuid().ToString("N");
      var stage = Path.Combine(directory, stageName);
      var stagedTables = Path.Combine(stage, "tables");
      Directory.CreateDirectory(stagedTables);
      var journal = new Journal(stageName, Directory.Exists(tables), File.Exists(manifestPath), false);
      try {
         if (Directory.Exists(tables)) foreach (var path in Directory.GetFiles(tables)) {
            if (owned.Contains(Path.GetFileName(path))) continue;
            var info = new FileInfo(path);
            if (info.LinkTarget != null) throw new IOException("Unmanaged symbolic links in tables require a clean export folder.");
            File.Copy(path, Path.Combine(stagedTables, info.Name));
         }
         foreach (var (name, text) in documents) AtomicFileWriter.Write(Path.Combine(stagedTables, name), stream => {
            using var writer = new StreamWriter(stream, leaveOpen: true); writer.Write(text);
         });
         AtomicFileWriter.WriteAllLines(Path.Combine(stage, "manifest.txt"), manifest.Split('\n'));
         WriteJournal(directory, journal);
         if (journal.HadTables) Directory.Move(tables, Path.Combine(stage, "previous-tables"));
         if (journal.HadManifest) File.Move(manifestPath, Path.Combine(stage, "previous-manifest.txt"));
         Directory.Move(stagedTables, tables);
         File.Move(Path.Combine(stage, "manifest.txt"), manifestPath);
         WriteJournal(directory, journal with { Committed = true });
         Recover(directory);
      } catch {
         if (File.Exists(Path.Combine(directory, Pending))) Recover(directory);
         else if (Directory.Exists(stage)) Directory.Delete(stage, true);
         throw;
      }
   }

   private static void WriteJournal(string directory, Journal journal) =>
      AtomicFileWriter.WriteAllLines(Path.Combine(directory, Pending), new[] { JsonSerializer.Serialize(journal) });

   private static void Recover(string directory) {
      var path = Path.Combine(directory, Pending);
      if (!File.Exists(path)) return;
      var journal = JsonSerializer.Deserialize<Journal>(File.ReadAllText(path)) ?? throw new IOException("Invalid export recovery journal.");
      if (!journal.Stage.StartsWith(".hma-export-", StringComparison.Ordinal) || journal.Stage.Length != 44 ||
          !journal.Stage[12..].All(Uri.IsHexDigit)) throw new IOException("Unsafe export recovery directory.");
      var stage = Path.Combine(directory, journal.Stage);
      if (new DirectoryInfo(stage).LinkTarget != null) throw new IOException("Export recovery directory must not be a symbolic link.");
      foreach (var entry in new[] { "previous-tables", "previous-manifest.txt" })
         if (new FileInfo(Path.Combine(stage, entry)).LinkTarget != null) throw new IOException("Recovery backups must not be symbolic links.");
      if (!journal.Committed) {
         var tables = Path.Combine(directory, "tables");
         var previousTables = Path.Combine(stage, "previous-tables");
         if (Directory.Exists(previousTables)) {
            if (Directory.Exists(tables)) Directory.Delete(tables, true);
            Directory.Move(previousTables, tables);
         } else if (!journal.HadTables && Directory.Exists(tables)) Directory.Delete(tables, true);
         var manifest = Path.Combine(directory, "manifest.txt");
         var previousManifest = Path.Combine(stage, "previous-manifest.txt");
         if (File.Exists(previousManifest)) {
            if (File.Exists(manifest)) File.Delete(manifest);
            File.Move(previousManifest, manifest);
         } else if (!journal.HadManifest && File.Exists(manifest)) File.Delete(manifest);
      }
      if (Directory.Exists(stage)) Directory.Delete(stage, true);
      File.Delete(path);
   }
}
