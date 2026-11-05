using System.Security.Cryptography;
using System.Text.Json;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;

namespace HavenSoft.HexManiac.Review;

// Diagnostic expectations come from engine operand roles, not GetUsedVariables' opcode list.
// Decoder and map-root discovery are shared with HMA: this is not an exhaustive oracle.
public static class UsageDetectionAudit {
   public static void Run(string[] args) {
      if (args.Length is < 1 or > 2) throw new ArgumentException("--usage output.json [read-only-rom.gba]");
      var output = Path.GetFullPath(args[0]);
      var rom = args.Length == 2 ? Path.GetFullPath(args[1]) : null;
      if (output == rom || Path.GetExtension(output) != ".json") throw new ArgumentException("Use a separate .json report path.");
      Directory.SetCurrentDirectory(AppContext.BaseDirectory);
      var synthetic = new List<object>();
      foreach (var game in new[] { "BPRE", "BPGE", "BPEE", "AXVE", "AXPE" }) {
         var f = new UsageDetectionFixture(game);
         var reserves = Flags.GetUsedVariables(f.Model, f.Parser).Where(id => id != 0).Order().ToArray();
         var commands = new List<object>();
         foreach (var c in UsageContracts.Cases) {
            f.SetScript(c.Bytes);
            var before = f.Model.RawData.ToArray();
            var used = Flags.GetUsedVariables(f.Model, f.Parser);
            var expected = UsageContracts.ReadReferences(f.Model, UsageDetectionFixture.Code);
            if (!expected.Order().SequenceEqual(c.ExpectedVariables.Order())) throw new InvalidOperationException(c.Name + ": contract mismatch");
            if (!f.Model.RawData.SequenceEqual(before)) throw new InvalidOperationException("Scanner modified synthetic model.");
            commands.Add(new { c.Name, expected, detected = expected.Where(used.Contains), missing = expected.Where(id => !used.Contains(id)) });
         }
         f.SetScript(new byte[] { 2 });
         var headers = new List<object>();
         foreach (var type in new[] { 2, 4 }) {
            f.Header(type, 0x40FD, 0x40FE);
            var used = Flags.GetUsedVariables(f.Model, f.Parser);
            if (!Flags.GetAllTopLevelScripts(f.Model).Contains(UsageDetectionFixture.Code)) throw new InvalidOperationException("Missing isolated header root.");
            headers.Add(new { type, leftDetected = used.Contains(0x40FD), rightDetected = used.Contains(0x40FE) });
         }
         synthetic.Add(new { game, existingNativeReserves = reserves, commands, headers });
      }
      object real = null;
      if (rom != null) {
         var data = File.ReadAllBytes(rom);
         var hash = Convert.ToHexString(SHA256.HashData(data));
         var model = new HardcodeTablesModel(new Singletons(), data, new StoredMetadata(Array.Empty<string>()));
         model.InitializationWorkload.GetAwaiter().GetResult();
         if (model.GetGameCode() != "BPRE1") throw new InvalidOperationException("Real-ROM phase is scoped to the supplied BPRE1 fixture.");
         var parser = new ScriptParser(model.GetShortGameCode(), new Singletons().ScriptLines, 2);
         var before = model.RawData.ToArray();
         var used = Flags.GetUsedVariables(model, parser);
         var flags = Flags.GetUsedItemFlags(model, parser);
         var roots = Flags.GetAllTopLevelScripts(model);
         if (roots.Count < 100) throw new InvalidOperationException("Expected a populated real map/script fixture.");
         var references = new List<Reference>();
         var incomplete = new List<string>();
         var visited = new HashSet<int>();
         var queue = new Queue<int>(roots.Where(a => a >= 0 && a < model.Count));
         while (queue.TryDequeue(out var start)) {
            if (!visited.Add(start)) continue;
            if (visited.Count > 100000) { incomplete.Add("Review script-node budget reached."); break; }
            int address = start, bytes = 0;
            while (bytes < 0x1000) {
               if (address < 0 || address >= model.Count) { incomplete.Add($"Out of range: {address:X6}"); break; }
               var line = parser.GetLine(model, address); // raw commands, deliberately no macros
               if (line == null) { incomplete.Add($"Unknown opcode at {address:X6}"); break; }
               int length = line.CompiledByteLength(model, address, null);
               if (length <= 0 || address + length > model.Count) { incomplete.Add($"Invalid length at {address:X6}"); break; }
               foreach (var id in UsageContracts.ReadReferences(model, address)) references.Add(new(address, model[address], id, "command"));
               int offset = line.LineCode.Count;
               foreach (var arg in line.Args) {
                  if (arg.PointerType == ExpectedPointerType.Script) {
                     var target = model.ReadPointer(address + offset);
                     if (target >= 0 && target < model.Count) queue.Enqueue(target);
                  }
                  offset += arg.Length(model, -1);
               }
               if (line.IsEndingCommand) break;
               address += length; bytes += length;
               if (bytes >= 0x1000) incomplete.Add($"Review byte budget reached: {start:X6}");
            }
         }
         foreach (var bank in AllMapsModel.Create(model)) {
            if (bank == null) continue;
            foreach (var map in bank) {
               if (map?.MapScripts == null) continue;
               foreach (var header in map.MapScripts) {
                  int type = header.GetValue("type");
                  if (type is not (2 or 4)) continue;
                  int address = header.GetAddress("pointer");
                  for (int row = 0; row < 100; row++, address += 8) {
                     if (address < 0 || address + 8 > model.Count) { incomplete.Add("Invalid header table."); break; }
                     int left = model.ReadMultiByteValue(address, 2);
                     if (left is 0 or 0xFFFF) break;
                     int right = model.ReadMultiByteValue(address + 2, 2);
                     if (left >= 0x4000) references.Add(new(address, type, left, "header-left"));
                     if (right >= 0x4000) references.Add(new(address + 2, type, right, "header-right"));
                     if (row == 99) incomplete.Add("Review header-row budget reached.");
                  }
               }
            }
         }
         var missing = references.Where(r => !used.Contains(r.ID)).Distinct().OrderBy(r => r.Address).ToArray();
         bool modelUnchanged = model.RawData.SequenceEqual(before);
         bool diskUnchanged = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(rom))) == hash;
         if (!modelUnchanged || !diskUnchanged) throw new InvalidOperationException("Read-only validation failed.");
         real = new { game = model.GetGameCode(), sha256 = hash, byteLength = data.Length, roots = roots.Count,
            decodedSegments = visited.Count, usedVariables = used.Order(), usedFlags = flags.Order(),
            reviewedReferences = references.Count, missingIDs = missing.Select(r => r.ID).Distinct().Order(),
            missingReferences = missing, incomplete, modelUnchanged, diskUnchanged,
            limits = "12 opcode roles and conditional-header fields only; shared HMA decoder/roots; no native-code or complete command coverage; absence from this report does not prove unused." };
      }
      Directory.CreateDirectory(Path.GetDirectoryName(output)!);
      File.WriteAllText(output, JsonSerializer.Serialize(new { scope = "Read-only review of current production helpers; this tool does not modify application code or ROM data.", synthetic, real }, new JsonSerializerOptions { WriteIndented = true }));
      Console.WriteLine($"Usage-detection review completed: {synthetic.Count} game identities, {UsageContracts.Cases.Length} command cases each; real-ROM phase: {rom != null}.");
      Console.WriteLine(output);
   }
   public record Reference(int Address, int Opcode, int ID, string Kind);
}
