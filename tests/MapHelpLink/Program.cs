using System.ComponentModel;
using System.Diagnostics;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.Core;

int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
const string url = "https://github.com/haven1433/HexManiacAdvance/wiki/Legendary-Pok%C3%A9mon-Encounters";
var info = MacHelpLink.CreateStartInfo(url);
Check(info.FileName == "/usr/bin/open", "Use the absolute macOS launcher path.");
Check(info.WorkingDirectory == "/", "The browser launch has a stable working directory.");
Check(!info.UseShellExecute && info.RedirectStandardError, "Launch directly and capture errors.");
Check(info.Arguments == "" && info.ArgumentList.SequenceEqual(new[] { url }), "Pass the unchanged upstream tutorial URL as one argument.");
var spaced = MacHelpLink.CreateStartInfo("https://example.com/help?q=two%20words&topic=flags");
Check(spaced.ArgumentList.Count == 1, "URL punctuation is not parsed as command-line arguments.");
var fileOpen = MacOpen.CreateStartInfo("/tmp/folder with spaces/rom \"quoted\".gba");
Check(fileOpen.ArgumentList.Single() == "/tmp/folder with spaces/rom \"quoted\".gba", "Local file names remain a single unquoted argument.");
Check(MacOpen.CreateStartInfo("resources/scripts").ArgumentList.Single() == Path.Combine(AppContext.BaseDirectory,"resources/scripts"), "Application-relative resources resolve independently of the current directory.");
foreach (var invalid in new[] { "", "relative/path", "file:///tmp/example", "javascript:alert(1)" }) {
   bool started = false;
   var error = await MacHelpLink.OpenAsync(invalid, _ => { started = true; return null; });
   Check(error != null && !started, "Invalid/non-web links never start a process.");
}
Check(await MacHelpLink.OpenAsync(url, _ => null) != null, "A missing child process returns an error.");
foreach (var exception in new Exception[] { new FileNotFoundException("cwd missing"), new Win32Exception(2), new UnauthorizedAccessException("denied"), new InvalidOperationException("not started") }) {
   Check(await MacHelpLink.OpenAsync(url, _ => throw exception) != null, "Expected launch failures return errors instead of escaping.");
}
var savedDirectory = Directory.GetCurrentDirectory();
var deleted = Path.Combine(Path.GetTempPath(), "hma-help-cwd-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(deleted);
try {
   Directory.SetCurrentDirectory(deleted);
   Directory.Delete(deleted);
   bool oldFailed = false;
   try {
      // Reproduce the old relative executable lookup without opening a browser.
      using var old = Process.Start(new ProcessStartInfo("true") { UseShellExecute = false });
      old?.WaitForExit();
   } catch (FileNotFoundException) { oldFailed = true; }
   Check(oldFailed, "The old relative launcher reproduces GetCwd/FileNotFoundException after bundle replacement.");
   var result = await MacHelpLink.OpenAsync(url, start => {
      Check(start.WorkingDirectory == "/" && start.FileName == "/usr/bin/open", "Production launch settings survive a deleted current directory.");
      // Keep the real process-start path, but do not open the user's browser.
      start.FileName = "/usr/bin/true";
      start.ArgumentList.Clear();
      return Process.Start(start);
   });
   Check(result == null, "Absolute launch succeeds even when the parent's current directory was removed.");
   foreach (var native in new[] {
      MacOpen.CreateStartInfo("resources/scripts"),
      MacFileReveal.CreateStartInfo("/tmp/a file.gba"),
      ExternalEmulatorLauncher.CreateStartInfo("/Applications/mGBA.app", "/tmp/a file.gba"),
      ExternalEmulatorPicker.CreateStartInfo(),
      NativeProcess.CreateStartInfo(url),
   }) {
      Check(Path.IsPathRooted(native.FileName) && native.WorkingDirectory == "/", "Audited native launchers use absolute executable paths and stable working directories.");
      native.FileName = "/usr/bin/true"; native.ArgumentList.Clear();
      using var process = Process.Start(native); process.WaitForExit();
      Check(process.ExitCode == 0, "The audited launch configuration works with a removed parent cwd without opening apps.");
   }
} finally {
   Directory.SetCurrentDirectory(savedDirectory);
   if (Directory.Exists(deleted)) Directory.Delete(deleted);
}
var failed = await MacHelpLink.OpenAsync(url, start => {
   start.FileName = "/bin/sh";
   start.ArgumentList.Clear();
   start.ArgumentList.Add("-c");
   start.ArgumentList.Add("printf 'browser unavailable' >&2; exit 7");
   return Process.Start(start);
});
Check(failed == "browser unavailable", "Nonzero exits report the child's diagnostic.");
Check(await MacHelpLink.OpenAsync(url, start => {
   start.FileName = "/usr/bin/false";
   start.ArgumentList.Clear();
   return Process.Start(start);
}) == "macOS returned error 1.", "A nonzero exit without stderr still reports an error.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
