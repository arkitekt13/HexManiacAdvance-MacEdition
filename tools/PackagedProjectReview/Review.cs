using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

static class Review {
   [MethodImpl(MethodImplOptions.NoInlining)]
   internal static void Run(string binaries, string root, string baseRom) {
      Directory.CreateDirectory(root);
      if (Directory.EnumerateFileSystemEntries(root).Any()) throw new ArgumentException("Use an empty disposable output directory for each run.");
      var report = new List<object>(); int checks = 0, cycles = 0, burstSaves = 0;
      void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
      string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
      var input = File.ReadAllBytes(baseRom); var inputHash = Hash(input);
      Check(inputHash.Equals("729041b940afe031302d630fdbe57c0c145f3f7b6d9b8eca5e98678d0ca4d059", StringComparison.OrdinalIgnoreCase), "Require the exact unmodified English FireRed 1.1 input; never run against a working hack.");
      Check(input.Length == 0x1000000 && input[0xBC] == 1 && System.Text.Encoding.ASCII.GetString(input, 0xAC, 4) == "BPRE", "Use a disposable clean English FireRed 1.1 base.");
      Check(typeof(App).Assembly.Location == Path.Combine(binaries, "HexManiacAdvance.dll"), "Load the actual packaged app assembly.");
      Check(typeof(Singletons).Assembly.Location == Path.Combine(binaries, "HexManiac.Core.dll"), "Load the actual packaged Core assembly.");
      Check(Path.GetFullPath(RuntimeEnvironment.GetRuntimeDirectory()).TrimEnd('/') == binaries.TrimEnd('/'), "Run on the copied bundle's self-contained runtime, not the installed SDK runtime.");
      var startup = typeof(App).GetMethod("GetViewModel", BindingFlags.NonPublic | BindingFlags.Static)!;
      EditorViewModel Fresh(MacFileSystem fs) => (EditorViewModel)startup.Invoke(null, new object[] { "", -1, fs, new[] { true, false } })!;
      var profile = Path.Combine(root, "isolated profile");
      using (var fs = new MacFileSystem(() => null, appSupportDirectory: profile)) {
         var editor = Fresh(fs);
         Check(editor.Count == 0 && string.IsNullOrEmpty(editor.ErrorMessage), "A new profile opens an empty editor without errors.");
         Check(editor.InsertAutoActive && Directory.GetCurrentDirectory() == binaries, "Fresh defaults and packaged resource-directory resolution are correct.");
         Check(editor.Singletons.ScriptLines.Count > 100, "Packaged script reference resources load from an unrelated launch directory.");
         editor.InsertAutoActive = false; editor.PreferScriptMapView = true; editor.WriteAppLevelMetadata();
         Check(File.Exists(Path.Combine(profile, "HexManiacAdvance.toml")), "Settings are saved into the isolated profile.");
      }
      using (var fs = new MacFileSystem(() => null, appSupportDirectory: profile)) {
         var editor = Fresh(fs);
         Check(!editor.InsertAutoActive && editor.PreferScriptMapView && editor.Count == 0, "Preferences survive a fresh editor without automatically opening a ROM.");
      }
      // Model scheduling is controlled; all reads, saves, metadata writes and watchers use the real Mac filesystem.
      Directory.SetCurrentDirectory(binaries);
      var services = new Singletons(InstantDispatch.Instance);
      foreach (var location in new[] { Path.Combine(Path.GetTempPath(), "hma packaged save " + Guid.NewGuid().ToString("N")), Path.Combine(root, "Documents location — spaces é") }) {
         Directory.CreateDirectory(location);
         var path = Path.Combine(location, "Release Save Trial.gba");
         var sidecar = Path.ChangeExtension(path, ".toml");
         var seed = new HardcodeTablesModel(services, input.ToArray(), new StoredMetadata(Array.Empty<string>())); seed.InitializationWorkload.GetAwaiter().GetResult();
         const int scriptAddress = 0xF00000, marker = 0xF00200;
         Check(seed.RawData.Skip(scriptAddress).Take(0x204).All(b => b == 0xFF), "Seed only unused bytes in the disposable copy.");
         var parser = new ScriptParser(seed.GetShortGameCode(), services.ScriptLines, 2);
         var token = new ModelDelta(); var source = Source(0);
         var code = parser.Compile(token, seed, scriptAddress, ref source, out _, out _);
         Check(code != null, "The disposable dialogue seed compiles.");
         token.ChangeData(seed, scriptAddress, code); parser.FormatScript<XSERun>(token, seed, scriptAddress, code.Length);
         seed.ObserveAnchorWritten(token, "release.review.script", new XSERun(scriptAddress));
         token.ChangeData(seed, marker, new byte[] { 1, 2, 3, 4 });
         var parsed = ArrayRun.TryParse(seed, "[value.]4", marker, SortedSpan<int>.None, out var table);
         Check(!parsed.HasError, "The disposable short-field table parses.");
         seed.ObserveAnchorWritten(token, "release.review.values", table);
         seed.SetList(token, "release.review.choices", "One", "Two", "Three");
         AtomicFileWriter.Write(path, stream => stream.Write(seed.RawData));
         var definitions = seed.ExportMetadata(null, services.MetadataInfo);
         Check(definitions.NamedAnchors.Count > 500 && definitions.Lists.Count > 50, "Use a full-size game metadata sidecar, not just the fabricated additions.");
         AtomicFileWriter.WriteAllLines(sidecar, definitions.Serialize());
         using var realFs = new MacFileSystem(() => null, appSupportDirectory: profile);
         var validMetadataBytes = File.ReadAllBytes(sidecar);
         var invalidMetadataBytes = System.Text.Encoding.UTF8.GetBytes("[[NamedAnchors]]\nAddress = 0x100\n");
         var validRomBytes = File.ReadAllBytes(path);
         File.WriteAllBytes(sidecar, invalidMetadataBytes);
         try {
            var rejected = new EditorViewModel(realFs, InstantDispatch.Instance);
            var rejectedOpen = rejected.OpenFileAsync(new LoadedFile(path, validRomBytes.ToArray())).GetAwaiter().GetResult();
            Check(rejectedOpen == null && rejected.Count == 0 && rejected.ErrorMessage.Contains("metadata"), "An invalid sidecar is rejected visibly without opening a partial project.");
            Check(invalidMetadataBytes.SequenceEqual(File.ReadAllBytes(sidecar)) && validRomBytes.SequenceEqual(File.ReadAllBytes(path)), "Rejecting metadata does not rewrite either project file.");
         } finally { File.WriteAllBytes(sidecar, validMetadataBytes); }
         for (int cycle = 1; cycle <= 30; cycle++) {
            var beforeOpenRom = File.ReadAllBytes(path); var beforeOpenToml = File.ReadAllBytes(sidecar);
            var owner = new EditorViewModel(realFs, InstantDispatch.Instance);
            var view = owner.OpenFileAsync(new LoadedFile(path, beforeOpenRom.ToArray())).GetAwaiter().GetResult()!;
            Check(view != null, "The saved disposable project reopens.");
            view.InitializationWorkload.GetAwaiter().GetResult();
            Check(view.ChangeHistory.IsSaved && !view.Undo.CanExecute(null), "Opening creates no edit history.");
            Check(beforeOpenRom.SequenceEqual(File.ReadAllBytes(path)) && beforeOpenToml.SequenceEqual(File.ReadAllBytes(sidecar)), "Opening does not rewrite either file.");
            view.Goto.Execute("release.review.script"); view.Tools.CodeTool.Mode = CodeMode.Script; view.Tools.CodeTool.IsSelected = true;
            var body = view.Tools.CodeTool.Contents[0]; body.Editor.Content = Source(cycle);
            view.ChangeHistory.ChangeCompleted();
            Check(!body.HasError && !view.Tools.CodeTool.HasUncompiledScriptChanges, "Dialogue edits compile successfully.");
            var afterScript = view.Model.RawData.ToArray();
            view.ChangeHistory.CurrentChange.ChangeData(view.Model, marker, (byte)(cycle + 10)); view.ChangeHistory.ChangeCompleted();
            var expected = view.Model.RawData.ToArray();
            view.Undo.Execute(null); Check(afterScript.SequenceEqual(view.Model.RawData), "Undo restores the short-field edit without discarding the script edit.");
            view.Redo.Execute(null); Check(expected.SequenceEqual(view.Model.RawData), "Redo restores the short-field edit exactly.");
            view.Save.Execute(realFs);
            Check(view.ChangeHistory.IsSaved && string.IsNullOrEmpty(owner.ErrorMessage), "The real Mac Save completes and tags the project saved.");
            Check(expected.SequenceEqual(File.ReadAllBytes(path)), "The saved ROM matches the in-memory edit exactly.");
            var diskMetadata = new StoredMetadata(File.ReadAllLines(sidecar));
            Check(diskMetadata.Version == "0.6.1" && diskMetadata.NamedAnchors.Any(a => a.Name == "release.review.script") && diskMetadata.Lists.Any(l => l.Name == "release.review.choices" && l.Contents.SequenceEqual(new[] { "One", "Two", "Three" })), "The actual TOML retains the custom definitions/list and original metadata format.");
            var stableAnchors = definitions.NamedAnchors.Where(a => a.Name != "release.review.script");
            Check(stableAnchors.All(a => diskMetadata.NamedAnchors.Any(b => a.Name == b.Name && a.Address == b.Address && a.Format == b.Format)), "Existing definitions are retained through every overwrite.");
            view.Close.Execute(realFs);
            Check(owner.Count == 0 && expected.SequenceEqual(File.ReadAllBytes(path)), "Closing a saved project succeeds without altering its ROM.");
            Check(!Directory.EnumerateFiles(location, ".*.tmp").Any(), "Successful saves and closes leave no staging files.");
            cycles++;
         }
         var burstOwner = new EditorViewModel(realFs, InstantDispatch.Instance);
         var burst = burstOwner.OpenFileAsync(new LoadedFile(path, File.ReadAllBytes(path))).GetAwaiter().GetResult()!;
         burst.InitializationWorkload.GetAwaiter().GetResult();
         burst.Goto.Execute("release.review.script"); burst.Tools.CodeTool.Mode = CodeMode.Script; burst.Tools.CodeTool.IsSelected = true;
         for (int edit = 31; edit <= 50; edit++) {
            var body = burst.Tools.CodeTool.Contents[0]; body.Editor.Content = Source(edit);
            burst.ChangeHistory.ChangeCompleted();
            Check(!body.HasError && !burst.Tools.CodeTool.HasUncompiledScriptChanges, "Continuous-session dialogue edits compile.");
            burst.Save.Execute(realFs);
            Check(burst.ChangeHistory.IsSaved && burst.Model.RawData.SequenceEqual(File.ReadAllBytes(path)), "Continuous-session overwrites retain exact ROM bytes.");
            var saved = new StoredMetadata(File.ReadAllLines(sidecar));
            Check(saved.NamedAnchors.Count >= definitions.NamedAnchors.Count && saved.Lists.Count >= definitions.Lists.Count, "Continuous-session overwrites retain the complete metadata sidecar.");
            burstSaves++;
         }
         burst.Close.Execute(realFs);
         var finalOwner = new EditorViewModel(realFs, InstantDispatch.Instance);
         var final = finalOwner.OpenFileAsync(new LoadedFile(path, File.ReadAllBytes(path))).GetAwaiter().GetResult()!;
         final.InitializationWorkload.GetAwaiter().GetResult(); final.Goto.Execute("release.review.script"); final.Tools.CodeTool.Mode = CodeMode.Script; final.Tools.CodeTool.IsSelected = true;
         Check(final.Tools.CodeTool.Contents.Any(b => b.Content.Contains("Trial 50:") && !b.HasError), "Final reopen decodes the last continuous-session dialogue edit.");
         // Exercise preference persistence without invoking the native chooser or another app.
         var preference = Path.Combine(profile, "emulator-review-" + report.Count + ".txt");
         var emulator = "/Applications/mGBA.app"; int picks = 0, starts = 0; var errors = new List<string>();
         Task<string> Pick() { picks++; return Task.FromResult(emulator); }
         Task<string> Start(System.Diagnostics.ProcessStartInfo info) {
            starts++;
            Check(info.FileName == "/usr/bin/open" && info.WorkingDirectory == "/" && info.ArgumentList.SequenceEqual(new[] { "-a", emulator, path }), "The packaged emulator launcher preserves native app/ROM arguments.");
            return Task.FromResult<string>(null);
         }
         if (Directory.Exists(emulator)) {
            var launcher = new ExternalEmulatorLauncher(preference, Pick, Start);
            launcher.LaunchAsync(final, realFs, () => true, errors.Add).GetAwaiter().GetResult();
            launcher = new ExternalEmulatorLauncher(preference, Pick, Start);
            launcher.LaunchAsync(final, realFs, () => true, errors.Add).GetAwaiter().GetResult();
            Check(picks == 1 && starts == 2 && errors.Count == 0 && File.ReadAllText(preference).TrimEnd('\r', '\n') == emulator, "Emulator choice is remembered only in the isolated test profile.");
         }
         final.Close.Execute(realFs);
         report.Add(new { location = location.StartsWith(root) ? "repository Documents path" : "temporary path", closeReopenCycles = 30, continuousSaves = 20, romBytes = new FileInfo(path).Length, metadataBytes = new FileInfo(sidecar).Length, anchors = new StoredMetadata(File.ReadAllLines(sidecar)).NamedAnchors.Count });
      }
      Check(inputHash == Hash(File.ReadAllBytes(baseRom)), "The original clean ROM was only read.");
      var result = new { checks, cycles, burstSaves, appVersion = typeof(App).Assembly.GetName().Version!.ToString(), coreVersion = typeof(Singletons).Assembly.GetName().Version!.ToString(), runtime = RuntimeEnvironment.GetRuntimeDirectory(), isolatedProfile = true, nativeUI = false, controlledModelDispatcher = true, scenarios = report };
      File.WriteAllText(Path.Combine(root, "result.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
      Console.WriteLine(JsonSerializer.Serialize(result));
   }
   static string Source(int cycle) {
      int length = new[] { 24, 200, 40, 800 }[cycle % 4];
      return "lock\n" + string.Concat(Enumerable.Repeat("setvar 0x4001 1\n", cycle % 5 + 1)) + "msgbox.npc <auto>\n{\nTrial " + cycle + ": " + new string('A', length) + "\n}\nrelease\nend";
   }
}
