using System.Text;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.DataFormats;
using HavenSoft.HexManiac.Performance;

namespace HavenSoft.HexManiac.Review;

// Test/review infrastructure only. Every address and event is fabricated.
public sealed class UsageDetectionFixture {
   public const int Code = 0x12000, Child = 0x12400;
   public PokemonModel Model { get; }
   public ScriptParser Parser { get; }
   public UsageDetectionFixture(string game) {
      Model = new SyntheticMapFixture(8, 8).Model;
      var token = new ModelDelta();
      token.ChangeData(Model, 0xAC, Encoding.ASCII.GetBytes(game));
      Model.WriteValue(token, 0xBC, 0);
      Model.LoadMetadata(BaseModel.GetDefaultMetadatas().First());
      Model.WritePointer(token, 0x128, 0x200);
      Model.WritePointer(token, 0x200, 0x300);
      Model.WritePointer(token, 0x204, 0x400);
      Model.WritePointer(token, 0x208, 0x500);
      // Signpost's byte layout matters to the real hidden-item reader.
      const string events = "[objects<[script<> flag:]1> scripts<[trigger: script<>]1> signposts<[x: y: elevation. kind. unused: arg::]1>]1";
      var format = Model.GetNextRun(0x100).FormatString.Replace("events<>", "events<" + events + ">")
         .Replace("mapscripts<>", "mapscripts<[type. pointer<>]!00>");
      var error = ArrayRun.TryParse(Model, format, 0x100, SortedSpan<int>.None, out var root);
      if (error.HasError) throw new InvalidOperationException(error.ErrorMessage);
      Model.ObserveAnchorWritten(new NoDataChangeDeltaModel(), "data.maps.banks", root);
      Parser = new ScriptParser(Model.GetShortGameCode(), new Singletons().ScriptLines, 2);
      SetScript(new byte[] { 2 });
   }
   public void SetScript(byte[] bytes) {
      var token = new ModelDelta();
      Model.WritePointer(token, 0x300, Code);
      Model.WritePointer(token, 0x402, Pointer.NULL);
      Model.WritePointer(token, 0x508, Pointer.NULL);
      token.ChangeData(Model, Code, bytes);
   }
   public void Header(int type, int left, int right) {
      var token = new ModelDelta();
      Model.WritePointer(token, 0x300, Pointer.NULL);
      Model.WritePointer(token, 0x12C, 0x600);
      Model.WriteValue(token, 0x600, type);
      Model.WritePointer(token, 0x601, 0x650);
      Model.WriteMultiByteValue(0x650, 2, token, left);
      Model.WriteMultiByteValue(0x652, 2, token, right);
      Model.WritePointer(token, 0x654, Code);
   }
}

public record OperandRule(int Offset, bool ValueOrVariable);
public record UsageCase(string Name, byte[] Bytes, int[] ExpectedVariables);

// Hand-reviewed, deliberately bounded role table, not a replacement scanner.
// Sources: pret pokefirered/pokeemerald/pokeruby src/scrcmd.c, named handlers.
// Direct operands are storage IDs; VarGet operands are IDs only at >= 0x4000.
// An invalid high ID remains a reference needing review, never a safe allocation.
public static class UsageContracts {
   public static readonly Dictionary<byte, OperandRule[]> Rules = new() {
      [0x16] = new[] { new OperandRule(1, false) },
      [0x17] = new[] { new OperandRule(1, false) },
      [0x18] = new[] { new OperandRule(1, false), new OperandRule(3, true) },
      [0x19] = new[] { new OperandRule(1, false), new OperandRule(3, false) },
      [0x1A] = new[] { new OperandRule(1, false), new OperandRule(3, true) },
      [0x21] = new[] { new OperandRule(1, false) },
      [0x22] = new[] { new OperandRule(1, false), new OperandRule(3, false) },
      [0x26] = new[] { new OperandRule(1, false) },
      [0x44] = new[] { new OperandRule(1, true), new OperandRule(3, true) },
      [0x47] = new[] { new OperandRule(1, true), new OperandRule(3, true) },
      [0x8F] = new[] { new OperandRule(1, true) },
      [0xB3] = new[] { new OperandRule(1, false) },
   };
   public static int[] ReadReferences(IDataModel model, int address) => !Rules.TryGetValue(model[address], out var rules)
      ? Array.Empty<int>() : rules.Select(r => (Rule: r, ID: model.ReadMultiByteValue(address + r.Offset, 2)))
      .Where(v => !v.Rule.ValueOrVariable || v.ID >= 0x4000).Select(v => v.ID).Distinct().ToArray();
   static byte[] Command(int opcode, params int[] operands) => new[] { (byte)opcode }
      .Concat(operands.SelectMany(v => new[] { (byte)v, (byte)(v >> 8) })).Append((byte)2).ToArray();
   public static readonly UsageCase[] Cases = {
      new("setvar literal", Command(0x16, 0x40FD, 0x40FE), new[] { 0x40FD }),
      new("addvar literal", Command(0x17, 0x40FD, 0x40FE), new[] { 0x40FD }),
      new("subvar source", Command(0x18, 0x40FD, 0x40FE), new[] { 0x40FD, 0x40FE }),
      new("subvar literal", Command(0x18, 0x40FD, 7), new[] { 0x40FD }),
      new("copyvar source", Command(0x19, 0x40FD, 0x40FE), new[] { 0x40FD, 0x40FE }),
      new("setorcopyvar source", Command(0x1A, 0x40FD, 0x40FE), new[] { 0x40FD, 0x40FE }),
      new("setorcopyvar literal", Command(0x1A, 0x40FD, 7), new[] { 0x40FD }),
      new("compare literal", Command(0x21, 0x40FD, 0x40FE), new[] { 0x40FD }),
      new("comparevars source", Command(0x22, 0x40FD, 0x40FE), new[] { 0x40FD, 0x40FE }),
      new("specialvar function literal", Command(0x26, 0x40FD, 0x40FE), new[] { 0x40FD }),
      new("additem variable arguments", Command(0x44, 0x40FD, 0x40FE), new[] { 0x40FD, 0x40FE }),
      new("additem literals", Command(0x44, 13, 2), Array.Empty<int>()),
      new("checkitem variable arguments", Command(0x47, 0x40FD, 0x40FE), new[] { 0x40FD, 0x40FE }),
      new("random variable", Command(0x8F, 0x40FE), new[] { 0x40FE }),
      new("random literal", Command(0x8F, 7), Array.Empty<int>()),
      new("VarGet below boundary literal", Command(0x8F, 0x3FFF), Array.Empty<int>()),
      new("VarGet temporary boundary", Command(0x8F, 0x4000), new[] { 0x4000 }),
      new("VarGet special boundary", Command(0x8F, 0x8000), new[] { 0x8000 }),
      new("checkcoins output", Command(0xB3, 0x40FE), new[] { 0x40FE }),
      new("copyvar special source", Command(0x19, 0x40FD, 0x800D), new[] { 0x40FD, 0x800D }),
   };
}
