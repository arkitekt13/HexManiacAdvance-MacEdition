using System.Diagnostics;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Review;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
var picker = ExternalEmulatorPicker.CreateStartInfo();
Check(picker.FileName == "/usr/bin/osascript" && !picker.UseShellExecute && picker.ArgumentList.Count == 2,
   "The picker invokes the system application chooser without shell interpolation.");
Check(picker.ArgumentList[1].Contains("choose application") && picker.ArgumentList[1].Contains("as alias"),
   "Choose application bundles rather than ordinary files.");
Check(ExternalEmulatorPicker.ReadSelection(0, "/Applications/mGBA.app/\n", "") == "/Applications/mGBA.app",
   "Normalize the trailing slash returned for application aliases.");
Check(ExternalEmulatorPicker.ReadSelection(0, "\n", "") == null, "Cancel is an empty selection.");
Check(ExternalEmulatorPicker.ReadSelection(0, "/Applications/Emulator With Spaces.app/\n", "") == "/Applications/Emulator With Spaces.app",
   "Preserve spaces in selected application paths.");
bool pickerFailed = false;
try { ExternalEmulatorPicker.ReadSelection(1, "", "chooser unavailable"); }
catch (IOException e) { pickerFailed = e.Message.Contains("chooser unavailable"); }
Check(pickerFailed, "A chooser failure is actionable rather than a silent cancellation.");
using var f = new WorkflowFixture();
var preference = Path.Combine(f.Folder, "settings", "external-emulator.txt");
var app = Path.Combine(f.Folder, "Applications with spaces", "mGBA.app");
Directory.CreateDirectory(Path.Combine(app, "Contents"));
AtomicFileWriter.WriteAllLines(Path.Combine(app, "Contents", "Info.plist"), new[] { "synthetic application" });
var errors = new List<string>(); int picks = 0, starts = 0, prompts = 0;
string choice = app;
Task<string> Pick() { picks++; return Task.FromResult(choice); }
Task<string> Start(ProcessStartInfo info) {
   starts++;
   Check(info.FileName == "/usr/bin/open" && !info.UseShellExecute, "Launch explicitly through macOS without a shell.");
   Check(info.ArgumentList.SequenceEqual(new[] { "-a", app, f.View.FullFileName }), "Keep application and ROM paths as separate arguments.");
   return Task.FromResult<string>(null);
}
bool Save() { prompts++; return true; }
var launcher = new ExternalEmulatorLauncher(preference, Pick, Start);
choice = null;
await launcher.LaunchAsync(f.View, f.FileSystem, Save, errors.Add);
Check(!File.Exists(preference) && starts == 0 && prompts == 0, "Canceling first selection changes no preferences or ROMs.");
choice = app;
await launcher.LaunchAsync(f.View, f.FileSystem, Save, errors.Add);
Check(picks == 2 && starts == 1 && File.ReadAllText(preference).Trim() == app, "First selection is remembered and launched.");
launcher = new ExternalEmulatorLauncher(preference, Pick, Start);
await launcher.LaunchAsync(f.View, f.FileSystem, Save, errors.Add);
Check(picks == 2 && starts == 2 && prompts == 0, "A new launcher reuses the preference without prompting on a saved ROM.");
Directory.Move(app, app + ".moved"); choice = null;
await launcher.LaunchAsync(f.View, f.FileSystem, Save, errors.Add);
Check(picks == 3 && starts == 2 && File.ReadAllText(preference).Trim() == app, "A missing app prompts again and cancellation preserves the preference.");
Directory.Move(app + ".moved", app);
f.Edit(WorkflowFixture.Source(1, 2), settle: false);
await launcher.LaunchAsync(f.View, f.FileSystem, () => false, errors.Add);
Check(starts == 2 && f.Tool.HasPendingScriptChanges && f.Saves == 0, "Canceling Save and Launch leaves pending source and disk untouched.");
await launcher.LaunchAsync(f.View, f.FileSystem, Save, errors.Add);
Check(starts == 3 && f.Saves == 1 && File.ReadAllBytes(f.FileName)[WorkflowFixture.Root + 4] == 2, "Save and Launch settles source and writes it before launch.");
f.Dispatch.Flush();
Check(f.View.ChangeHistory.IsSaved && !f.Tool.HasUncompiledScriptChanges, "Stale callbacks do not dirty the launched snapshot.");
f.Edit("not_a_command", settle: false);
await launcher.LaunchAsync(f.View, f.FileSystem, Save, errors.Add);
Check(starts == 3 && f.Tool.HasUncompiledScriptChanges && f.Body.HasError, "Rejected source blocks launch and retains diagnostics.");
f.View.Undo.Execute(null); f.Dispatch.Flush();
f.Edit(WorkflowFixture.Source(1, 3));
var failSave = new StubFileSystem { Save = _ => false };
await launcher.LaunchAsync(f.View, failSave, Save, errors.Add);
Check(starts == 3 && !f.View.ChangeHistory.IsSaved, "A failed save blocks emulator launch.");
f.View.Save.Execute(f.FileSystem);
var failing = new ExternalEmulatorLauncher(preference, Pick, _ => Task.FromResult("application launch failed"));
await failing.LaunchAsync(f.View, f.FileSystem, Save, errors.Add);
Check(errors.Last().Contains("application launch failed"), "Nonzero launch results produce actionable errors.");
await launcher.LaunchAsync(null, f.FileSystem, Save, errors.Add);
Check(starts == 3 && errors.Last().Contains("Select a ROM"), "No ROM produces an explanation without launching.");
var paused = new TaskCompletionSource<string>();
var delayed = new ExternalEmulatorLauncher(Path.Combine(f.Folder, "other.txt"), () => paused.Task, Start);
var first = delayed.LaunchAsync(f.View, f.FileSystem, Save, errors.Add);
await delayed.LaunchAsync(f.View, f.FileSystem, Save, errors.Add);
paused.SetResult(null); await first;
Check(starts == 3, "Repeated clicks during selection do not create duplicate launches.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
