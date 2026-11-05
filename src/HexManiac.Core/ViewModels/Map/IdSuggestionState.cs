using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Runs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HavenSoft.HexManiac.Core.ViewModels.Map {
   // Optional lifecycle contract; existing standalone IDataInvestigator implementations keep
   // their original behavior. Scopes belong to synchronous tool actions, not ROM history.
   public interface IScopedDataInvestigator : IDataInvestigator {
      IDisposable BeginIdAllocation();
      void SetPendingScripts(object owner, IEnumerable<(ScriptParser Parser, string Source)> sources);
   }

   internal sealed class IdSuggestionState {
      private readonly IDataModel model;
      private readonly ScriptParser parser;
      private HashSet<int> usedFlags, usedVariables;
      private ModelCacheScope flagScope, variableScope;
      private IFormattedRun[] flagRuns, variableRuns;
      private readonly HashSet<int> unownedFlags = new(), unownedVariables = new();
      private readonly List<Allocation> allocations = new();
      private readonly Dictionary<object, (ScriptParser Parser, string Source)[]> pending = new();

      public IdSuggestionState(IDataModel model, ScriptParser parser) => (this.model, this.parser) = (model, parser);

      private T[] SnapshotRuns<T>() where T : IFormattedRun {
         T[] snapshot = null;
         if (model is PokemonModel pokemon) pokemon.ThreadlockRuns(() => snapshot = model.All<T>().ToArray());
         else snapshot = model.All<T>().ToArray();
         return snapshot;
      }

      public void Initialize() {
         var scope = model.CurrentCacheScope;
         flagRuns = variableRuns = SnapshotRuns<IFormattedRun>();
         usedFlags = ReadUsed(variable: false);
         usedVariables = ReadUsed(variable: true);
         flagScope = variableScope = scope;
      }

      private HashSet<int> ReadUsed(bool variable) {
         var used = variable ? Flags.GetUsedVariables(model, parser) : Flags.GetUsedItemFlags(model, parser);
         // Deleting an event need not delete its formatted script. Keep known orphaned
         // scripts protected as well, so reattaching them cannot silently collide with a reuse.
         var roots = SnapshotRuns<XSERun>().Select(run => run.Start).ToArray();
         var commands = new byte[] { 0x16, 0x17, 0x18, 0x19, 0x1A, 0x21, 0x22, 0x26, 0x29, 0x2A, 0x2B, 0x44, 0x47, 0x8F, 0xB3 };
         foreach (var spot in Flags.GetKnownScriptSpots(model, parser, roots, commands)) {
            var length = spot.Line.CompiledByteLength(model, spot.Address, null);
            var opcode = model[spot.Address];
            if (!variable) {
               if (opcode is 0x29 or 0x2A or 0x2B && length >= 3) used.Add(model.ReadMultiByteValue(spot.Address + 1, 2));
               continue;
            }
            foreach (var operand in Flags.VariableOperands(opcode)) {
               if (operand.Offset + 2 > length) continue;
               var id = model.ReadMultiByteValue(spot.Address + operand.Offset, 2);
               if (!operand.ValueOrVariable || id >= 0x4000) used.Add(id);
            }
         }
         return used;
      }

      public IDisposable BeginAllocation() {
         var allocation = new Allocation(this);
         allocations.Add(allocation);
         return allocation;
      }

      public void SetPendingScripts(object owner, IEnumerable<(ScriptParser Parser, string Source)> sources) {
         var snapshot = sources.ToArray();
         if (snapshot.Length == 0) pending.Remove(owner);
         else pending[owner] = snapshot;
      }

      public int Find(bool variable) {
         var scope = model.CurrentCacheScope;
         // Metadata-only formatting/imports can introduce a known script without a byte write.
         var runs = SnapshotRuns<IFormattedRun>();
         HashSet<int> used;
         if (variable) {
            if (!ReferenceEquals(variableScope, scope) || !runs.SequenceEqual(variableRuns, ReferenceEqualityComparer.Instance)) {
               variableRuns = runs;
               usedVariables = ReadUsed(variable: true);
               variableScope = scope;
            }
            used = usedVariables;
         } else {
            if (!ReferenceEquals(flagScope, scope) || !runs.SequenceEqual(flagRuns, ReferenceEqualityComparer.Instance)) {
               flagRuns = runs;
               usedFlags = ReadUsed(variable: false);
               flagScope = scope;
            }
            used = usedFlags;
         }
         var blocked = new HashSet<int>(variable ? unownedVariables : unownedFlags);
         foreach (var allocation in allocations) blocked.UnionWith(variable ? allocation.Variables : allocation.Flags);
         foreach (var source in pending.Values.SelectMany(sources => sources)) {
            var references = PendingScriptIds.Read(model, source.Parser, source.Source);
            blocked.UnionWith(variable ? references.Variables : references.Flags);
         }
         var candidate = variable ? 0x4034 : 0x21;
         // Preserve the existing candidate ranges/policy; legal-range exhaustion is separate work.
         while (used.Contains(candidate) || blocked.Contains(candidate)) candidate++;
         var reservations = allocations.Count == 0
            ? variable ? unownedVariables : unownedFlags
            : variable ? allocations[^1].Variables : allocations[^1].Flags;
         reservations.Add(candidate);
         return candidate;
      }

      private sealed class Allocation : IDisposable {
         private IdSuggestionState owner;
         public HashSet<int> Flags { get; } = new();
         public HashSet<int> Variables { get; } = new();
         public Allocation(IdSuggestionState owner) => this.owner = owner;
         public void Dispose() {
            owner?.allocations.Remove(this);
            owner = null;
         }
      }
   }

   internal static class PendingScriptIds {
      public static (HashSet<int> Flags, HashSet<int> Variables) Read(IDataModel model, ScriptParser parser, string source) {
         var flags = new HashSet<int>();
         var variables = new HashSet<int>();
         // Normalize a local copy exactly as compilation does, without completing a rejected block.
         ScriptParser.InsertMissingClosers(ref source, 0, completeMissing: false);
         var labels = new LabelLibrary(model, new Dictionary<string, int>(), cacheArgumentMatches: true);
         int depth = 0;
         foreach (var raw in source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)) {
            var text = raw.Split('#')[0].Trim();
            if (text == "{") { depth++; continue; }
            if (text == "}") { depth = Math.Max(0, depth - 1); continue; }
            if (depth != 0 || text.Length == 0 || text.EndsWith(":")) continue;
            var command = parser.FirstMatch(text);
            if (command == null) continue;
            // Line compilation only builds bytes/labels; it does not run script/resource writes.
            // This handles macros and the researched literal/variable operand distinctions.
            if (command.Compile(model, 0, text, labels, out var bytes) == null) {
               var data = new BasicModel(bytes);
               for (int at = 0; at < data.Count;) {
                  var line = parser.GetLine(data, at);
                  if (line == null) break;
                  var length = line.CompiledByteLength(data, at, null);
                  if (length <= 0 || length > data.Count - at) break;
                  var opcode = data[at];
                  if (opcode is 0x29 or 0x2A or 0x2B && length >= 3) flags.Add(data.ReadMultiByteValue(at + 1, 2));
                  foreach (var operand in Flags.VariableOperands(opcode)) {
                     if (operand.Offset + 2 > length) continue;
                     var id = data.ReadMultiByteValue(at + operand.Offset, 2);
                     if (!operand.ValueOrVariable || id >= 0x4000) variables.Add(id);
                  }
                  at += length;
               }
            } else {
               // An unfinished command can already contain a valid flag/variable operand.
               // Use the compiler's own enum/numeric conversion, never a second numeric grammar.
               var args = command is MacroScriptLine macro ? macro.ShortFormArgs : command.Args;
               var tokens = ScriptLine.Tokenize(text).Skip(command is MacroScriptLine ? 1 : command.LineCode.Count).ToArray();
               int offset = command.LineCode.Count;
               for (int i = 0; i < args.Count && i < tokens.Length; i++) {
                  if (args[i] is not ScriptArg arg) break;
                  int argumentOffset = offset;
                  offset += arg.Length(model, 0);
                  if (arg.Type != ArgType.Short) continue;
                  bool flag = arg.Name == "flag" && arg.EnumTableName == "|h";
                  var role = command.LineCode.Count == 1
                     ? Flags.VariableOperands(command.LineCode[0]).FirstOrDefault(operand => operand.Offset == argumentOffset)
                     : default;
                  bool variable = arg.EnumTableName == "scriptvariablealiases" || role.Offset != 0;
                  if (!flag && !variable) continue;
                  var value = new List<byte>();
                  if (arg.Build(model, 0, tokens[i], value, labels) != null || value.Count != 2) continue;
                  var id = value[0] | value[1] << 8;
                  if (flag) flags.Add(id);
                  else if (role.Offset == 0 || !role.ValueOrVariable || id >= 0x4000) variables.Add(id);
               }
            }
         }
         return (flags, variables);
      }
   }
}
