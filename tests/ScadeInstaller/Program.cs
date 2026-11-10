using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Review;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
void Rejected(Action action, string message) {
   try { action(); } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException) { Check(true, message); return; }
   throw new Exception("Expected rejection: " + message);
}
uint Crc(byte[] bytes) => (uint)Patcher.CalcCRC32(bytes);
string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
if ((args.Length == 4 || args.Length == 5 && args[4] == "--skip-maps") && (args[0] == "--ds-graphics-review" || args[0] == "--graphics-review")) {
   DsGraphicsCompatibility.Run(args[1], args[2], args[3], renderMaps: args.Length == 4);
   return;
}
if (args.Length == 3 && args[0] == "--real-rom") {
   var realChecks = RealRomVerification.Run(args[1], args[2]);
   Console.WriteLine($"CHECKS_PASSED:{realChecks}");
   return;
}
// Test-only encoder supplies independent command streams; expected output is always explicit.
byte[] Number(ulong value) {
   var result = new List<byte>();
   while (true) { byte low = (byte)(value & 127); value >>= 7; if (value == 0) { result.Add((byte)(low | 128)); return result.ToArray(); } result.Add(low); value--; }
}
byte[] Action(int kind, int count) => Number(((ulong)(count - 1) << 2) | (uint)kind);
byte[] Join(params byte[][] parts) => parts.SelectMany(x => x).ToArray();
byte[] Footer(byte[] prefix, byte[] source, byte[] target) {
   var bytes = Join(prefix, BitConverter.GetBytes(Crc(source)), BitConverter.GetBytes(Crc(target)));
   return Join(bytes, BitConverter.GetBytes(Crc(bytes)));
}
byte[] Patch(byte[] source, byte[] target, byte[] commands, byte[] metadata = null) => Footer(Join("BPS1"u8.ToArray(), Number((ulong)source.Length), Number((ulong)target.Length), Number((ulong)(metadata?.Length ?? 0)), metadata ?? Array.Empty<byte>(), commands), source, target);
byte[] Literal(byte[] source, byte[] target) => Patch(source, target, Join(Action(1, target.Length), target));
byte[] Rechecksum(byte[] patch) { BinaryPrimitives.WriteUInt32LittleEndian(patch.AsSpan(patch.Length - 4), Crc(patch[..^4])); return patch; }
void Vector(byte[] source, byte[] target, byte[] commands, string name, byte[] metadata = null) {
   var before = source.ToArray(); var patch = Patch(source, target, commands, metadata); var patchBefore = patch.ToArray();
   Check(BpsPatch.Apply(source, patch).SequenceEqual(target), name);
   Check(source.SequenceEqual(before) && patch.SequenceEqual(patchBefore), name + " preserves inputs");
}
// Optional independent comparison of the supplied command stream on fabricated bytes.
if (args.Length == 2 && args[0] == "--command-stream-comparison") {
   var independentFolder = args[1];
   var sourceBytes = File.ReadAllBytes(Path.Combine(independentFolder, "source.bin"));
   var targetBytes = File.ReadAllBytes(Path.Combine(independentFolder, "target.bin"));
   var patchBytes = File.ReadAllBytes(Path.Combine(independentFolder, "test-only.bps"));
   var beforeSource = Hash(sourceBytes); var beforePatch = Hash(patchBytes);
   Check(BpsPatch.Apply(sourceBytes, patchBytes).SequenceEqual(targetBytes), "complete supplied command stream agrees with independent patcher on synthetic input");
   Check(Hash(sourceBytes) == beforeSource && Hash(patchBytes) == beforePatch, "complete command stream preserves inputs");
   Console.WriteLine($"CHECKS_PASSED:{checks}");
   return;
}
var alphabet = Encoding.ASCII.GetBytes("abcdefghij");
Vector(alphabet, alphabet, Action(0, 10), "SourceRead");
Vector(alphabet, "HELLO"u8.ToArray(), Join(Action(1, 5), "HELLO"u8.ToArray()), "TargetRead with metadata", "ignored metadata"u8.ToArray());
Vector(alphabet, "fgab"u8.ToArray(), Join(Action(2, 2), Number(10), Action(2, 2), Number(15)), "SourceCopy forward and backward");
Vector(Array.Empty<byte>(), "ababababab"u8.ToArray(), Join(Action(1, 2), "ab"u8.ToArray(), Action(3, 8), Number(0)), "Overlapping TargetCopy");
Vector(Array.Empty<byte>(), "abcdeabde"u8.ToArray(), Join(Action(1, 5), "abcde"u8.ToArray(), Action(3, 2), Number(0), Action(3, 2), Number(2)), "TargetCopy relative forward");
Vector(Array.Empty<byte>(), "abcdeababc"u8.ToArray(), Join(Action(1, 5), "abcde"u8.ToArray(), Action(3, 2), Number(0), Action(3, 3), Number(5)), "TargetCopy relative backward");
Vector(Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>(), "Empty output");
var multi = Enumerable.Range(0, 20000).Select(i => (byte)i).ToArray();
Vector(multi, multi, Action(0, multi.Length), "Multibyte command length");
var good = Literal(alphabet, "abc"u8.ToArray());
Rejected(() => BpsPatch.Apply(alphabet, null), "null patch");
Rejected(() => BpsPatch.Apply(null, good), "null source");
Rejected(() => BpsPatch.Apply(alphabet, "BPS1"u8.ToArray()), "truncated header");
var bad = good.ToArray(); bad[0] = 0;
Rejected(() => BpsPatch.Apply(alphabet, bad), "header signature");
bad = good.ToArray(); bad[8] ^= 1;
Rejected(() => BpsPatch.Apply(alphabet, bad), "patch checksum");
Rejected(() => BpsPatch.Apply(new byte[10], good), "source checksum");
Rejected(() => BpsPatch.Apply(new byte[9], good), "source length");
bad = good.ToArray(); bad[^8] ^= 1; Rechecksum(bad);
Rejected(() => BpsPatch.Apply(alphabet, bad), "target checksum");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[11], Action(0, 11))), "source read bound");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[2], Join(Action(2, 2), Number(3)))), "negative source offset");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[2], Join(Action(2, 2), Number(20)))), "source copy past end");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[2], Join(Action(3, 2), Number(0)))), "unwritten target offset");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[2], Join(Action(3, 2), Number(3)))), "negative target offset");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[1], Join(Action(1, 2), new byte[2]))), "output overrun");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[2], Join(Action(1, 2), new byte[1]))), "literal hits footer");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[2], Join(Action(1, 1), new byte[1]))), "incomplete output");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[1], Join(Action(1, 1), new byte[1], Action(0, 1)))), "extra commands");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[1], new byte[] { 0 })), "truncated action integer");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[1], Enumerable.Repeat((byte)0, 11).ToArray())), "overflowing action integer");
Rejected(() => BpsPatch.Apply(alphabet, Footer(Join("BPS1"u8.ToArray(), Number(10), Number((ulong)Patcher.MaximumPatchedLength + 1), Number(0)), alphabet, Array.Empty<byte>())), "bounded allocation");
Rejected(() => BpsPatch.Apply(alphabet, Footer(Join("BPS1"u8.ToArray(), Number(10), Number(1), Number(100)), alphabet, new byte[1])), "metadata hits footer");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[2], Join(Action(2, 1), Number(0), Action(2, 1), Number(ulong.MaxValue - 1)))), "relative source addition overflow");
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[3], Join(Action(1, 1), new byte[1], Action(3, 1), Number(0), Action(3, 1), Number(ulong.MaxValue - 1)))), "relative target addition overflow");
var largeOffset = Join(Action(2, 1), Number(ulong.MaxValue));
Rejected(() => BpsPatch.Apply(alphabet, Patch(alphabet, new byte[1], largeOffset)), "oversized signed offset");
// All truncation points of a valid vector must reject cleanly, without unchecked indexing.
for (int i = 0; i < good.Length; i++) Rejected(() => BpsPatch.Apply(alphabet, good[..i]), "patch truncation " + i);

// These patches were generated and round-tripped by independent Rom Patcher JS.
using (var vectors = JsonDocument.Parse(File.ReadAllText("IndependentBpsVectors.json"))) {
   foreach (var vector in vectors.RootElement.EnumerateArray()) {
      var independentSource = Convert.FromBase64String(vector.GetProperty("source").GetString());
      var independentTarget = Convert.FromBase64String(vector.GetProperty("target").GetString());
      var independentPatch = Convert.FromBase64String(vector.GetProperty("patch").GetString());
      var sourceBefore = independentSource.ToArray();
      Check(BpsPatch.Apply(independentSource, independentPatch).SequenceEqual(independentTarget), "independent patcher vector agrees byte for byte");
      Check(sourceBefore.SequenceEqual(independentSource), "independent vector preserves source");
   }
}

var directory = Path.Combine(Path.GetTempPath(), "hma-scade-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try {
   Check(ScadePackage.Load(directory, out var reason) == null && reason.Contains("not included"), "missing assets disabled");
   using var fixture = new WorkflowFixture();
   var original = fixture.State(); var source = fixture.Model.RawData.ToArray();
   var target = source.Concat(new byte[] { 8, 7, 6, 5 }).ToArray();
   // Deliberately relocated vanilla-name anchor: defaults must never replace this definition.
   var toml = Encoding.UTF8.GetBytes("\uFEFF[General]\nApplicationVersion = '''99.0.0'''\nShowRawIVByteForTrainer = True\n\n[[NamedAnchors]]\nName = '''data.pokemon.names'''\nAddress = 0x009000\nFormat = '''`asc`4'''\n\n[[NamedAnchors]]\nName = '''scade.test.custom'''\nAddress = 0x009010\nFormat = '''[value.]4'''\n");
   var manifest = new ScadePackage.Manifest { Version = "synthetic", BaseRom = ScadePackage.BaseGame, SourceLength = source.Length,
      SourceCrc32 = Crc(source), PatchSha256 = Hash(Literal(source, target)), TomlSha256 = Hash(toml) };
   var manifestPath = Path.Combine(directory, "scade.json");
   var patchPath = Path.Combine(directory, "SCADE.bps"); var tomlPath = Path.Combine(directory, "SCADE.toml");
   void Write() { File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest)); File.WriteAllBytes(patchPath, Literal(source, target)); File.WriteAllBytes(tomlPath, toml); }
   Write(); var package = ScadePackage.Load(directory, out reason);
   Check(package != null && reason == null, "valid package loads through actual metadata parser");
   Check(package.MatchesBase(source) && package.DisabledReason(fixture.View) == null, "eligible exact base");
   Check(package.DisabledReason(null).Contains("1.0"), "no source disabled");
   Check(package.DisabledReason(new SearchResultsViewPort("query")).Contains("1.0"), "search results are not a patchable ROM");
   Check(package.Create(source).SequenceEqual(target) && original.EqualsModel(fixture), "detached patch preserves source and history");
   var wrong = source.ToArray(); wrong[0xBC] = 1;
   Rejected(() => package.Create(wrong), "revision 1.1 rejected");
   wrong = source.ToArray(); wrong[0x100] ^= 1;
   Rejected(() => package.Create(wrong), "modified ROM rejected");
   var pending = fixture.Body.Content + "\nsetvar 0x4001";
   fixture.Edit(pending, settle: false); var queued = fixture.Dispatch.CapturePendingCallbacks();
   Check(package.DisabledReason(fixture.View).Contains("pending/rejected"), "pending script blocks creation");
   Check(fixture.Dispatch.CapturePendingCallbacks().SequenceEqual(queued) && fixture.Body.Content == pending, "gate preserves queued source");
   fixture.Dispatch.Flush();
   Check(package.DisabledReason(fixture.View).Contains("pending/rejected") && fixture.Tool.HasUncompiledScriptChanges, "rejected draft blocks creation");
   Check(original.EqualsModel(fixture), "gates preserve original model");
   fixture.View.Undo.Execute(null);fixture.Dispatch.Flush();
   Check(!fixture.Tool.HasUncompiledScriptChanges, "draft discard resumes normal workflow");
   var path = Path.Combine(directory, "My Adventure.gba");
   Check(!File.Exists(path) && !File.Exists(Path.ChangeExtension(path, ".toml")), "cancel before publication leaves no outputs");
   package.SaveNewCopy(path, target);
   var sidecar = Path.ChangeExtension(path, ".toml");
   Check(File.ReadAllBytes(path).SequenceEqual(target) && File.ReadAllBytes(sidecar).SequenceEqual(toml), "paired matching-name files and exact supplied TOML");
   Rejected(() => package.SaveNewCopy(path, target), "existing ROM and sidecar preserved");
   Check(File.ReadAllBytes(sidecar).SequenceEqual(toml), "collision never replaces supplied metadata");
   Rejected(() => package.SaveNewCopy(Path.Combine(directory, "bad.gba"), source), "unverified output cannot be saved");
   Rejected(() => package.SaveNewCopy(Path.Combine(directory, "bad.txt"), target), "wrong extension rejected");
   var blocked = Path.Combine(directory, "blocked.gba");File.WriteAllText(Path.ChangeExtension(blocked, ".toml"), "keep");
   Rejected(() => package.SaveNewCopy(blocked, target), "existing TOML without ROM preserved");
   Check(!File.Exists(blocked) && File.ReadAllText(Path.ChangeExtension(blocked, ".toml")) == "keep", "sidecar collision preserves unrelated content");
   var race = Path.Combine(directory, "race.gba");
   Rejected(() => package.SaveNewCopy(race, target, () => {
      Check(File.ReadAllBytes(Path.ChangeExtension(race, ".toml")).SequenceEqual(toml) && !File.Exists(race), "TOML published before ROM");
      File.WriteAllText(race, "other writer");
   }), "ROM publication race");
   Check(File.ReadAllText(race) == "other writer" && !File.Exists(Path.ChangeExtension(race, ".toml")), "failed ROM publish removes only owned TOML");
   var fail = Path.Combine(directory, "failure.gba");
   Rejected(() => package.SaveNewCopy(fail, target, () => throw new IOException("injected publication failure")), "publication failure");
   Check(!File.Exists(fail) && !File.Exists(Path.ChangeExtension(fail, ".toml")), "failure leaves no partial pair");
   package.SaveNewCopy(fail, target);
   Check(File.Exists(fail) && File.ReadAllBytes(Path.ChangeExtension(fail, ".toml")).SequenceEqual(toml), "retry succeeds");
   Rejected(() => package.SaveNewCopy(Path.Combine(directory, "missing", "bad.gba"), target), "unwritable/missing destination");
   var dirCollision = Path.Combine(directory, "folder.gba");Directory.CreateDirectory(dirCollision);
   Rejected(() => package.SaveNewCopy(dirCollision, target), "ROM directory collision");
   dirCollision = Path.Combine(directory, "folder2.gba");Directory.CreateDirectory(Path.ChangeExtension(dirCollision, ".toml"));
   Rejected(() => package.SaveNewCopy(dirCollision, target), "TOML directory collision");
   Check(!Directory.GetFiles(directory, ".hma-scade-*").Any(), "staging cleaned after success and failure");

   // Use the normal editor/file-system path, not direct LoadMetadata, for both opens.
   int projectRomWrites = 0, projectMetadataWrites = 0;
   var fs = new StubFileSystem {
      MetadataFor = name => File.Exists(Path.ChangeExtension(name, ".toml")) ? File.ReadAllLines(Path.ChangeExtension(name, ".toml")) : Array.Empty<string>(),
      Save = file => { File.WriteAllBytes(file.Name, file.Contents); projectRomWrites++; return true; },
      SaveMetadata = (name, lines) => { File.WriteAllLines(Path.ChangeExtension(name, ".toml"), lines); projectMetadataWrites++; return true; },
   };
   var editor = new EditorViewModel(fs, InstantDispatch.Instance);
   var opened = editor.OpenFileAsync(new LoadedFile(path, File.ReadAllBytes(path))).GetAwaiter().GetResult();
   opened.Model.InitializationWorkload.GetAwaiter().GetResult();opened.InitializationWorkload.GetAwaiter().GetResult();
   int Address(ViewPort v, string name) => v.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, name);
   Check(Address(opened, "data.pokemon.names") == 0x9000 && Address(opened, "scade.test.custom") == 0x9010, "normal initial open uses relocated custom definitions, not vanilla defaults");
   Check(File.ReadAllBytes(sidecar).SequenceEqual(toml), "initial opening does not regenerate supplied TOML");
   Check(opened.Model.ExportMetadata(null, editor.Singletons.MetadataInfo).ShowRawIVByteForTrainer, "creator trainer setting loaded");
   byte oldValue = opened.Model[0x9010];
   opened.ChangeHistory.CurrentChange.ChangeData(opened.Model, 0x9010, (byte)(oldValue ^ 1));
   opened.ChangeHistory.ChangeCompleted();
   opened.Save.Execute(fs);
   Check(projectRomWrites == 1 && projectMetadataWrites == 1 && opened.ChangeHistory.IsSaved, "Save actually serializes a changed custom-metadata project");
   var saved = new StoredMetadata(File.ReadAllLines(sidecar));
   Check(saved.NamedAnchors.Any(a => a.Name == "data.pokemon.names" && a.Address == 0x9000 && a.Format == "`asc`4") &&
      saved.NamedAnchors.Any(a => a.Name == "scade.test.custom" && a.Address == 0x9010 && a.Format == "[value.]4"), "save retains custom addresses and formats");
   var reopened = editor.OpenFileAsync(new LoadedFile(path, File.ReadAllBytes(path))).GetAwaiter().GetResult();
   reopened.Model.InitializationWorkload.GetAwaiter().GetResult();reopened.InitializationWorkload.GetAwaiter().GetResult();
   Check(Address(reopened, "data.pokemon.names") == 0x9000 && Address(reopened, "scade.test.custom") == 0x9010, "save/reopen retains custom definitions");
   Check(reopened.Model[0x9010] == (byte)(oldValue ^ 1), "reopened project contains the actually saved edit");
   Check(original.EqualsModel(fixture), "loading new pair preserves original source model");
   editor.CloseAll.Execute(fs);

   // Bad packaged assets disable the action instead of silently falling back to vanilla metadata.
   File.WriteAllBytes(tomlPath, "bad TOML"u8.ToArray());
   Check(ScadePackage.Load(directory, out reason) == null && reason.Contains("does not match"), "tampered TOML disabled");
   Write();File.WriteAllBytes(patchPath, "bad patch"u8.ToArray());
   Check(ScadePackage.Load(directory, out reason) == null, "tampered patch disabled");
   Write();File.WriteAllText(manifestPath, "{bad");
   Check(ScadePackage.Load(directory, out reason) == null, "malformed manifest disabled");
   manifest.TomlSha256 = Hash(""u8.ToArray());Write();File.WriteAllBytes(tomlPath, Array.Empty<byte>());
   Check(ScadePackage.Load(directory, out reason) == null, "empty metadata disabled even with matching hash");
   var oldToml = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(toml).Replace("99.0.0", "0.0.0"));
   manifest.TomlSha256 = Hash(oldToml);Write();File.WriteAllBytes(tomlPath, oldToml);
   Check(ScadePackage.Load(directory, out reason) == null, "metadata requiring vanilla upgrade is refused");
   manifest.TomlSha256 = Hash(toml);manifest.BaseRom = "English FireRed 1.1";Write();
   Check(ScadePackage.Load(directory, out reason) == null, "wrong package base disabled");

   // Actual shipped assets: identity and HMA parser, never a personal game.
   var bundled = ScadePackage.Load(ScadePackage.DefaultDirectory, out reason);
   Check(bundled != null && bundled.Version == "2026-10-21", "actual SCADE package included and valid");
   var suppliedTomlPath = Path.Combine(ScadePackage.DefaultDirectory, "SCADE.toml");
   Check(Hash(File.ReadAllBytes(suppliedTomlPath)).Equals("BDFE821E879CCF4727070F276580D9BC35AAB9E65038946495FFF706605A84DE"), "supplied TOML unchanged");
   var supplied = new StoredMetadata(File.ReadAllLines(suppliedTomlPath));
   Check(supplied.NamedAnchors.Count == 541 && supplied.Lists.Count == 79 && supplied.Version == "99.0.0", "actual TOML parsed with creator definitions");
   Check(!StoredMetadata.NeedVersionUpdate(supplied.Version, editor.Singletons.MetadataInfo.VersionNumber), "actual TOML will not be upgraded from defaults");
   Application.Current.Resources["FileSystem"] = fs;
   App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
   var window = new MainWindow { Width = 1000, Height = 700, FileSystem = fs, Editor = editor };
   window.Resources["FileSystem"] = fs;window.Show();Dispatcher.UIThread.RunJobs();
   IEnumerable<NativeMenuItem> Items(NativeMenu menu) => menu.Items.OfType<NativeMenuItem>().SelectMany(i => new[] { i }.Concat(i.Menu == null ? Enumerable.Empty<NativeMenuItem>() : Items(i.Menu)));
   var menu = Items(NativeMenu.GetMenu(window)).Single(i => Equals(i.Header, "Create SCADE ROM…"));
   Check(menu.IsVisible && !menu.IsEnabled && menu.ToolTip.Contains("1.0"), "native SCADE menu is present and explanatory without a matching base");
   Check(Items(NativeMenu.GetMenu(window)).Any(i => Equals(i.Header, "Create Tutorial ROM…")), "tutorial menu remains separate");
   window.Editor = null;window.Close();
} finally { Directory.Delete(directory, true); }
Console.WriteLine($"CHECKS_PASSED:{checks}");
