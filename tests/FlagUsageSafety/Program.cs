using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels.DataFormats;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Review;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool condition, string message) {
   if (!condition) throw new InvalidOperationException(message);
   checks++;
}
foreach (var game in new[] { "BPRE", "BPGE", "BPEE", "AXVE", "AXPE" }) {
   var f = new UsageDetectionFixture(game);
   Check(f.Model.GetGameCode() == game + "0", game + ": fixture identity");
   var native = game switch {
      "BPRE" or "BPGE" => new[] { 0x4035, 0x4037, 0x40AA, 0x40CF, 0x40F1 },
      "BPEE" => new[] { 0x4036, 0x403E, 0x40CD, 0x40DD, 0x40F6 },
      _ => new[] { 0x4040, 0x4042, 0x4054, 0x4097, 0x40C2 },
   };
   var baseline = Flags.GetUsedVariables(f.Model, f.Parser);
   foreach (var id in native) Check(baseline.Contains(id), $"{game}: preserve existing native reserve {id:X4}");
   foreach (var c in UsageContracts.Cases) {
      f.SetScript(c.Bytes);
      var before = f.Model.RawData.ToArray();
      Check(f.Parser.GetLine(f.Model, UsageDetectionFixture.Code)?.CompiledByteLength(f.Model, UsageDetectionFixture.Code, null) == c.Bytes.Length - 1,
         $"{game}/{c.Name}: real decoder must agree on handcrafted command width");
      Check(UsageContracts.ReadReferences(f.Model, UsageDetectionFixture.Code).Order().SequenceEqual(c.ExpectedVariables.Order()),
         $"{game}/{c.Name}: independent expected operand roles");
      var used = Flags.GetUsedVariables(f.Model, f.Parser);
      // Protect supported behavior; missing operands are findings in the review tool,
      // never assertions requiring a future scanner to keep overlooking them.
      if (new byte[] { 0x16, 0x17, 0x18, 0x19, 0x1A, 0x21, 0x22, 0x26 }.Contains(c.Bytes[0]))
         Check(used.Contains(0x40FD), $"{game}/{c.Name}: preserve first-operand detection");
      if (new byte[] { 0x16, 0x17, 0x21, 0x26 }.Contains(c.Bytes[0]))
         Check(!used.Contains(0x40FE), $"{game}/{c.Name}: do not mistake a literal for a variable");
      Check(f.Model.RawData.SequenceEqual(before), $"{game}/{c.Name}: scanning must not change model bytes");
   }
   foreach (byte op in new byte[] { 0x29, 0x2A, 0x2B }) {
      f.SetScript(new byte[] { op, 0xF0, 0, 2 });
      var before = f.Model.RawData.ToArray();
      Check(Flags.GetUsedItemFlags(f.Model, f.Parser).Contains(0xF0), $"{game}: flag opcode {op:X2}");
      Check(Flags.FindFlagUsages(f.Model, f.Parser, 0xF0).Count > 0, $"{game}: flag usage location {op:X2}");
      Check(f.Model.RawData.SequenceEqual(before), $"{game}: flag scans are read-only");
   }
   f.SetScript(new byte[] { 0x16, 0xFD, 0x40, 1, 0, 2 });
   foreach (int type in new[] { 2, 4 }) {
      f.Header(type, 0x40FE, 1);
      Check(Flags.GetAllTopLevelScripts(f.Model).Contains(UsageDetectionFixture.Code), $"{game}: detached header root {type}");
      Check(Flags.GetUsedVariables(f.Model, f.Parser).Contains(0x40FD), $"{game}: header target command is scanned");
      Check(!Flags.GetUsedVariables(f.Model, f.Parser).Contains(1), $"{game}: header comparison literal is not a variable");
   }
   var token = new ModelDelta();
   f.Model.WritePointer(token, 0x12C, Pointer.NULL);
   f.SetScript(new byte[] { 0x04, 0, 0x24, 1, 8, 2 }); // call child, then end
   token.ChangeData(f.Model, UsageDetectionFixture.Child, new byte[] { 0x29, 0xF1, 0, 0x16, 0xFD, 0x40, 1, 0, 3 });
   Check(Flags.GetUsedItemFlags(f.Model, f.Parser).Contains(0xF1), game + ": follow call flag");
   Check(Flags.GetUsedVariables(f.Model, f.Parser).Contains(0x40FD), game + ": follow call variable");
   token.ChangeData(f.Model, UsageDetectionFixture.Child, new byte[] { 0x29, 0xF1, 0, 0x16, 0xFD, 0x40, 1, 0, 0x04, 0, 0x20, 1, 8, 3 });
   Check(Flags.GetUsedItemFlags(f.Model, f.Parser).Contains(0xF1), game + ": terminate call cycle, retain flag");
   Check(Flags.GetUsedVariables(f.Model, f.Parser).Contains(0x40FD), game + ": terminate call cycle, retain variable");
   f.Model.WriteMultiByteValue(0x304, 2, token, 0xF2);
   f.Model.WriteMultiByteValue(0x400, 2, token, 0x40FE);
   f.Model.WriteValue(token, 0x505, 5);
   f.Model.WriteValue(token, 0x50A, 3);
   int hiddenBase = game is "BPRE" or "BPGE" ? 1000 : game == "BPEE" ? 500 : 600;
   Check(Flags.GetUsedItemFlags(f.Model, f.Parser).Contains(0xF2), game + ": object visibility flag");
   Check(Flags.GetUsedItemFlags(f.Model, f.Parser).Contains(hiddenBase + 3), game + ": hidden-item offset");
   Check(Flags.GetUsedVariables(f.Model, f.Parser).Contains(0x40FE), game + ": coordinate trigger");
   f.SetScript(new byte[] { 0x16, 0xFD, 0x40, 1, 0, 0x29, 0xF3, 0, 2 });
   f.Model.WritePointer(token, 0x300, Pointer.NULL);
   f.Model.WritePointer(token, 0x402, UsageDetectionFixture.Code);
   Check(Flags.GetUsedVariables(f.Model, f.Parser).Contains(0x40FD), game + ": isolated coordinate script variable");
   Check(Flags.GetUsedItemFlags(f.Model, f.Parser).Contains(0xF3), game + ": isolated coordinate script flag");
   f.Model.WritePointer(token, 0x402, Pointer.NULL);
   f.Model.WriteValue(token, 0x505, 0);
   f.Model.WritePointer(token, 0x508, UsageDetectionFixture.Code);
   Check(Flags.GetUsedVariables(f.Model, f.Parser).Contains(0x40FD), game + ": isolated signpost script variable");
   Check(Flags.GetUsedItemFlags(f.Model, f.Parser).Contains(0xF3), game + ": isolated signpost script flag");
}
Console.WriteLine("PASS: supported usage detection, literal distinctions, roots and read-only scans across five fabricated game identities.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
