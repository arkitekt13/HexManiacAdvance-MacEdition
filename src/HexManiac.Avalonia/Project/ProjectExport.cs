using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

/// <summary>
/// Writes the ROM's tables out as text, and reads them back.
///
/// The point is version control. A .gba is one opaque 16MB blob: two people cannot work on a hack
/// at once, a change cannot be reviewed, and "what did I alter last week" has no answer. One
/// tab-separated file per table turns all of that into an ordinary diff.
///
/// Deliberately *not* a complete serialisation of the ROM - it is a data export. Pointers are
/// written for context but never imported, because rewriting one means relocating what it points
/// at, which is the editor's job and not a text file's. Import reports exactly what it skipped
/// rather than quietly doing half a job.
/// </summary>
public static class ProjectExport {
   private const string ManifestName = "manifest.txt";
   private const string TableFolder = "tables";

   public record Result(int Tables, int Rows, int Fields, IReadOnlyList<string> Notes);

   #region Export

   public static Result Export(IDataModel model, string directory) {
      Directory.CreateDirectory(directory);
      var tableDirectory = Path.Combine(directory, TableFolder);
      Directory.CreateDirectory(tableDirectory);

      var notes = new List<string>();
      int tables = 0, rows = 0, fields = 0;

      foreach (var (name, run) in NamedTables(model)) {
         var table = model.GetTableModel(name);
         if (table == null || table.Count == 0) continue;

         var segments = run.ElementContent;
         var text = new StringBuilder();
         text.AppendLine("# " + name);
         text.AppendLine("# exported by HexManiacAdvance - edit values, keep the columns");
         text.AppendLine(string.Join("\t", new[] { "index" }.Concat(segments.Select(s => s.Name))));

         for (int i = 0; i < table.Count; i++) {
            var element = table[i];
            var cells = new List<string> { i.ToString(CultureInfo.InvariantCulture) };
            foreach (var segment in segments) cells.Add(Format(element, segment));
            text.AppendLine(string.Join("\t", cells));
            rows++;
            fields += segments.Count;
         }

         File.WriteAllText(Path.Combine(tableDirectory, Sanitize(name) + ".tsv"), text.ToString());
         tables++;
      }

      var manifest = new StringBuilder();
      manifest.AppendLine("# HexManiacAdvance project export");
      manifest.AppendLine("formatVersion\t1");
      manifest.AppendLine($"schemaHash\t{SchemaHash(model)}");
      manifest.AppendLine($"gameCode\t{model.GetGameCode()}");
      manifest.AppendLine($"romSize\t0x{model.Count:X}");
      manifest.AppendLine($"tables\t{tables}");
      File.WriteAllText(Path.Combine(directory, ManifestName), manifest.ToString());

      return new Result(tables, rows, fields, notes);
   }

   private static string Format(ModelArrayElement element, ArrayRunElementSegment segment) {
      try {
         return segment.Type switch {
            ElementContentType.PCS => Escape(element.GetStringValue(segment.Name)),
            // Written for context and for a readable diff; never imported - see the class comment.
            // Escaped like any other cell: a pointer to text returns the text, which routinely
            // contains newlines, and an unescaped one splits the row and loses the columns after it.
            ElementContentType.Pointer => "<" + Escape(element.GetStringValue(segment.Name)) + ">",
            _ => element.GetValue(segment.Name).ToString(CultureInfo.InvariantCulture),
         };
      } catch (Exception) {
         return "?";
      }
   }

   private static string Escape(string value) =>
      (value ?? string.Empty).Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");

   private static string Unescape(string value) {
      var text = new StringBuilder();
      for (int i = 0; i < value.Length; i++) {
         if (value[i] != '\\') { text.Append(value[i]); continue; }
         if (i + 1 == value.Length) throw new FormatException("Trailing escape character");
         i++;
         text.Append(value[i] switch { 't' => '\t', 'n' => '\n', 'r' => '\r', '\\' => '\\', _ => throw new FormatException("Unknown escape sequence") });
      }
      return text.ToString();
   }

   #endregion

   #region Import

   public record ImportDiagnostic(string File, int Row, string Field, string Value, string Message);

   public sealed class ImportValidationException : InvalidOperationException {
      public IReadOnlyList<ImportDiagnostic> Diagnostics { get; }
      public ImportValidationException(IReadOnlyList<ImportDiagnostic> diagnostics)
         : base("Project import rejected; no changes applied." + Environment.NewLine +
            string.Join(Environment.NewLine, diagnostics.Take(40).Select(d => $"{d.File}:{d.Row} {d.Field}: {d.Message} (value: {d.Value})"))) => Diagnostics = diagnostics;
   }

   private record PreparedField(string Table, int Index, string Field, string Text, int Number, bool IsText);

   public static Result Import(IEditableViewPort viewPort, string directory) {
      var model = viewPort.Model;
      var notes = new List<string>();
      var manifestPath = Path.Combine(directory, ManifestName);
      if (!File.Exists(manifestPath)) throw new InvalidOperationException($"No {ManifestName} in {directory} - is this a project export?");
      var manifest = new Dictionary<string, string>(StringComparer.Ordinal);
      foreach (var line in File.ReadAllLines(manifestPath).Where(l => l.Length > 0 && !l.StartsWith("#"))) {
         var split = line.Split('\t');
         if (split.Length != 2 || !manifest.TryAdd(split[0], split[1])) throw new InvalidOperationException("Malformed or duplicate project manifest entry; no changes applied.");
      }
      if (!manifest.TryGetValue("gameCode", out var gameCode) || gameCode != model.GetGameCode())
         throw new InvalidOperationException("Missing or mismatched project game code; no changes applied.");
      if (!manifest.TryGetValue("romSize", out var size) || !size.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
          !int.TryParse(size[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var romSize) || romSize != model.Count)
         throw new InvalidOperationException("Missing or mismatched ROM size; no changes applied.");
      if (manifest.TryGetValue("formatVersion", out var version)) {
         if (version != "1" || !manifest.TryGetValue("schemaHash", out var schema) || schema != SchemaHash(model))
            throw new InvalidOperationException("Unsupported project format or mismatched table layout; no changes applied.");
      } else notes.Add("Legacy export: table fields are validated, but no full layout fingerprint is available.");

      var tableDirectory = Path.Combine(directory, TableFolder);
      if (!Directory.Exists(tableDirectory)) throw new InvalidOperationException($"No {TableFolder} folder in {directory}.");
      var prepared = new List<PreparedField>();
      var errors = new List<ImportDiagnostic>();
      var seenTables = new HashSet<string>(StringComparer.Ordinal);
      int rows = 0, contextOnly = 0;
      // Read and validate all inputs before asking history for a token or writing a byte.
      foreach (var file in Directory.EnumerateFiles(tableDirectory, "*.tsv").OrderBy(f => f, StringComparer.Ordinal)) {
         var lines = File.ReadAllLines(file);
         var name = lines.FirstOrDefault(l => l.StartsWith("# "))?[2..].Trim();
         void Error(int row, string field, string value, string message) => errors.Add(new ImportDiagnostic(Path.GetFileName(file), row, field, value, message));
         if (name == null || !seenTables.Add(name)) { Error(1, "table", name ?? "", "Missing or duplicate table name"); continue; }
         var table = model.GetTableModel(name);
         if (table == null) { Error(1, "table", name, "Table does not exist in this ROM"); continue; }
         var segments = model.GetTable(name).ElementContent;
         var header = Array.FindIndex(lines, l => l.StartsWith("index\t"));
         if (header < 0) { Error(1, "header", "", "Missing column header"); continue; }
         var columns = lines[header].Split('\t');
         if (columns.Distinct(StringComparer.Ordinal).Count() != columns.Length || columns.Skip(1).Any(c => !segments.Any(s => s.Name == c))) {
            Error(header + 1, "header", lines[header], "Unknown or duplicate column"); continue;
         }
         var indices = new HashSet<int>();
         for (int row = header + 1; row < lines.Length; row++) {
            var line = lines[row];
            if (line.Length == 0 || line.StartsWith("#")) continue;
            var cells = line.Split('\t');
            if (cells.Length != columns.Length) { Error(row + 1, "row", line, "Wrong number of columns"); continue; }
            if (!int.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) || index < 0 || index >= table.Count || !indices.Add(index)) {
               Error(row + 1, "index", cells[0], "Invalid, out-of-range, or duplicate row index"); continue;
            }
            rows++;
            var element = table[index];
            for (int c = 1; c < columns.Length; c++) {
               var segment = segments.Single(s => s.Name == columns[c]);
               var cell = cells[c];
               if (segment.Type == ElementContentType.Pointer || segment.Length == 0 ||
                   segment.Type is not (ElementContentType.PCS or ElementContentType.Integer or ElementContentType.BitArray) ||
                   (segment.Type == ElementContentType.BitArray && segment.Length > 3)) { contextOnly++; continue; }
               if (segment.Type == ElementContentType.PCS) {
                  string text;
                  try { text = Unescape(cell); }
                  catch (FormatException ex) { Error(row + 1, segment.Name, cell, ex.Message); continue; }
                  var encoded = model.TextConverter.Convert(text, out var badCharacters);
                  if (badCharacters || encoded.Count == 0 || encoded.Count > segment.Length) { Error(row + 1, segment.Name, cell, "Text cannot be encoded losslessly within the field width"); continue; }
                  if (element.GetStringValue(segment.Name) != text) prepared.Add(new PreparedField(name, index, segment.Name, text, 0, true));
               } else {
                  var signed = segment is ArrayRunSignedSegment;
                  var bits = segment.Length * 8;
                  var minimum = signed ? -(1L << (bits - 1)) : (bits == 32 ? int.MinValue : 0L);
                  var maximum = signed ? (1L << (bits - 1)) - 1 : (1L << bits) - 1;
                  if (bits < 1 || bits > 32 || !long.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < minimum || value > maximum) {
                     Error(row + 1, segment.Name, cell, $"Number does not fit the {bits}-bit field"); continue;
                  }
                  var number = unchecked((int)value);
                  if (element.GetValue(segment.Name) != number) prepared.Add(new PreparedField(name, index, segment.Name, null, number, false));
               }
            }
         }
      }
      if (errors.Count > 0) throw new ImportValidationException(errors);
      if (contextOnly > 0) notes.Add($"{contextOnly} pointer, calculated, or unsupported field value(s) are context only and were not imported.");
      if (prepared.Count == 0) return new Result(0, rows, 0, notes);

      viewPort.ChangeHistory.ChangeCompleted();
      var token = new ModelDelta();
      try {
         foreach (var change in prepared) {
            var table = model.GetTableModel(change.Table, () => token);
            var element = table[change.Index];
            if (change.IsText) element.SetStringValue(change.Field, change.Text);
            else element.SetValue(change.Field, change.Number);
         }
         // Attach only a successfully applied token: validation/failure cannot destroy redo history.
         viewPort.ChangeHistory.InsertCustomChange(token);
         viewPort.ChangeHistory.ChangeCompleted();
      } catch {
         token.Revert(model);
         viewPort.Refresh();
         throw;
      }
      viewPort.Refresh();
      return new Result(prepared.Select(p => p.Table).Distinct().Count(), rows, prepared.Count, notes);
   }

   private static string SchemaHash(IDataModel model) {
      var lines = NamedTables(model).OrderBy(t => t.Name, StringComparer.Ordinal).Select(t =>
         t.Name + "|" + t.Run.FormatString + "|" + t.Run.Start.ToString(CultureInfo.InvariantCulture) + "|" + t.Run.ElementCount.ToString(CultureInfo.InvariantCulture) + "|" +
         string.Join(";", t.Run.ElementContent.Select(s => s.Name + ":" + s.GetType().Name + ":" + s.Length.ToString(CultureInfo.InvariantCulture))));
      return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", lines))));
   }

   #endregion

   private static IEnumerable<(string Name, ITableRun Run)> NamedTables(IDataModel model) {
      foreach (var run in model.Arrays) {
         var name = model.GetAnchorFromAddress(-1, run.Start);
         if (!string.IsNullOrEmpty(name)) yield return (name, run);
      }
   }

   private static string Sanitize(string name) {
      var invalid = Path.GetInvalidFileNameChars();
      return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
   }
}
