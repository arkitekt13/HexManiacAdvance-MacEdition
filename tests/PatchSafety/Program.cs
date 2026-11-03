using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
var random = new Random(1433);
for (int sample = 0; sample < 80; sample++) {
   var source = new byte[random.Next(0, 2048)]; random.NextBytes(source);
   var destination = new byte[random.Next(0, 2048)]; random.NextBytes(destination);
   var ips = Patcher.BuildIpsPatch(source, destination);
   var model = new BasicModel(source.ToArray());
   Check(Patcher.ApplyIPSPatch(model, ips, new NoTrackChange()) >= 0 && model.RawData.SequenceEqual(destination), "IPS round trip must preserve exact output bytes and size.");
   var ups = Patcher.BuildUpsPatch(source, destination);
   model = new BasicModel(source.ToArray());
   Check(Patcher.ApplyUPSPatch(model, ups, () => new NoTrackChange(), false, out _) >= 0 && model.RawData.SequenceEqual(destination), "UPS forward round trip must match destination.");
   Check(Patcher.ApplyUPSPatch(model, ups, () => new NoTrackChange(), false, out var direction) >= 0 && direction == Patcher.UpsPatchDirection.DestinationToSource && model.RawData.SequenceEqual(source), "UPS reverse round trip must restore source.");
}
foreach (var (source, target) in new[] { (Array.Empty<byte>(), Array.Empty<byte>()), (new byte[12], new byte[100]), (new byte[100], Array.Empty<byte>()) }) {
   var model = new BasicModel(source.ToArray());
   Check(Patcher.ApplyIPSPatch(model, Patcher.BuildIpsPatch(source, target), new NoTrackChange()) >= 0 && model.RawData.SequenceEqual(target), "IPS empty/zero-filled expansion and truncation must preserve length.");
   model = new BasicModel(source.ToArray());
   Check(Patcher.ApplyUPSPatch(model, Patcher.BuildUpsPatch(source, target), () => new NoTrackChange(), false, out _) >= 0 && model.RawData.SequenceEqual(target), "UPS empty/zero-filled cases must preserve length.");
}
{
   var source = new byte[0x454F48]; var target = source.ToArray(); target[0x454F46] = 7;
   var model = new BasicModel(source);
   Check(Patcher.ApplyIPSPatch(model, Patcher.BuildIpsPatch(source, target), new NoTrackChange()) >= 0 && model.RawData.SequenceEqual(target),
      "A change at the reserved IPS EOF offset must round-trip without being mistaken for the footer.");
}
var original = new byte[512]; var edited = original.ToArray(); edited[2] = 42;
var valid = Patcher.BuildUpsPatch(original, edited);
var corrupt = valid.ToArray(); corrupt[^8] ^= 1;
BitConverter.GetBytes(Patcher.CalcCRC32(corrupt[..^4])).CopyTo(corrupt, corrupt.Length - 4);
{
   var model = new BasicModel(original.ToArray()); var tokens = 0;
   Check(Patcher.ApplyUPSPatch(model, corrupt, () => { tokens++; return new NoTrackChange(); }, false, out _) == -5,
      "An incorrect output checksum must be rejected.");
   Check(tokens == 0 && model.RawData.SequenceEqual(original), "Rejected patches must not request a change token or mutate bytes.");
}
foreach (var bytes in new[] { new byte[] { 0x50, 0x41, 0x54, 0x43, 0x48, 0, 0, 2, 0, 3, 42 }, new byte[] { 1, 2, 3 }, Array.Empty<byte>() }) {
   var model = new BasicModel(original.ToArray());
   Check(Patcher.ApplyIPSPatch(model, bytes, new NoTrackChange()) < 0 && model.RawData.SequenceEqual(original), "Malformed IPS must not throw or modify bytes.");
}
for (int sample = 0; sample < 100; sample++) {
   var bytes = new byte[random.Next(0, 150)]; random.NextBytes(bytes);
   var model = new BasicModel(original.ToArray());
   var status = Patcher.ApplyUPSPatch(model, bytes, () => new NoTrackChange(), false, out _);
   Check(status < 0 && model.RawData.SequenceEqual(original), "Random invalid UPS must be rejected without mutation.");
}
{
   var payload = new List<byte>(System.Text.Encoding.ASCII.GetBytes("UPS1"));
   payload.AddRange(Patcher.WriteVariableWidthInteger(original.Length));
   payload.AddRange(Patcher.WriteVariableWidthInteger(Patcher.MaximumPatchedLength + 1));
   payload.AddRange(BitConverter.GetBytes(Patcher.CalcCRC32(original)));
   payload.AddRange(BitConverter.GetBytes(0));
   payload.AddRange(BitConverter.GetBytes(Patcher.CalcCRC32(payload.ToArray())));
   var model = new BasicModel(original.ToArray());
   Check(Patcher.ApplyUPSPatch(model, payload.ToArray(), () => new NoTrackChange(), false, out _) == -7 && model.RawData.SequenceEqual(original),
      "Oversized patch output must be rejected before allocation or mutation.");
}
var singletons = new Singletons();
foreach (var patch in new[] { new LoadedFile("bad.ups", corrupt), new LoadedFile("bad.ips", new byte[] { 1, 2, 3 }) }) {
   var model = new PokemonModel(original.ToArray(), singletons: singletons);
   var view = new ViewPort("test.gba", model, InstantDispatch.Instance, singletons);
   model.WriteValue(view.ChangeHistory.CurrentChange, 8, 7); view.ChangeHistory.ChangeCompleted();
   // Construct the UPS failure against the current source, so it reaches output validation.
   if (patch.Name.EndsWith(".ups")) {
      var target = model.RawData.ToArray(); target[2] = 42;
      var bad = Patcher.BuildUpsPatch(model.RawData, target); bad[^8] ^= 1;
      BitConverter.GetBytes(Patcher.CalcCRC32(bad[..^4])).CopyTo(bad, bad.Length - 4);
      view.TryImport(new LoadedFile("bad.ups", bad), new StubFileSystem());
   } else view.TryImport(patch, new StubFileSystem());
   Check(model[2] == 0 && model[8] == 7 && view.Undo.CanExecute(null), "Rejected import must retain existing edits and undo history.");
   view.Undo.Execute(null);
   Check(model.RawData.SequenceEqual(original), "Undo after a rejected import must still restore earlier edits.");
}
Console.WriteLine("PASS: randomized IPS/UPS forward and reverse round trips, invalid patches, checksum rejection, and existing undo preservation.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
