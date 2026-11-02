using System.Diagnostics;
if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("This runner checks the Mac shell script.");
var checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
var source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../scripts/verify-mac.sh"));
var root = Path.Combine(Path.GetTempPath(), "hma-runner-" + Guid.NewGuid().ToString("N"));
try {
   Directory.CreateDirectory(Path.Combine(root, "scripts"));
   Directory.CreateDirectory(Path.Combine(root, "mock-bin"));
   File.Copy(source, Path.Combine(root, "scripts", "verify-mac.sh"));
   var dotnet = Path.Combine(root, "mock-bin", "dotnet");
   File.WriteAllText(dotnet, "#!/bin/bash\ncase \"$1\" in\nbuild) exit \"${MOCK_BUILD_EXIT:-0}\";;\nrestore) exit \"${MOCK_RESTORE_EXIT:-0}\";;\nesac\nprintf '%s\\n' \"$MOCK_REPORT\"\nexit \"${MOCK_RUN_EXIT:-0}\"\n");
   File.SetUnixFileMode(dotnet, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
   (int Exit, string Output) Run(string report, int build = 0, int run = 0, int restore = 0) {
      var info = new ProcessStartInfo("/bin/bash") { RedirectStandardOutput = true, RedirectStandardError = true };
      info.ArgumentList.Add(Path.Combine(root, "scripts", "verify-mac.sh"));
      info.Environment["PATH"] = Path.Combine(root, "mock-bin") + ":" + Environment.GetEnvironmentVariable("PATH");
      info.Environment["MOCK_REPORT"] = report;
      info.Environment["MOCK_BUILD_EXIT"] = build.ToString();
      info.Environment["MOCK_RUN_EXIT"] = run.ToString();
      info.Environment["MOCK_RESTORE_EXIT"] = restore.ToString();
      using var process = Process.Start(info)!;
      var output = process.StandardOutput.ReadToEndAsync();
      var error = process.StandardError.ReadToEndAsync();
      if (!process.WaitForExit(15000)) { process.Kill(true); throw new Exception("Verification runner hung."); }
      return (process.ExitCode, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
   }
   Check(Run("CHECKS_PASSED:1").Exit != 0, "An empty project set must fail.");
   foreach (var name in new[] { "First", "Second" }) {
      Directory.CreateDirectory(Path.Combine(root, "tests", name));
      File.WriteAllText(Path.Combine(root, "tests", name, name + ".csproj"), "mock project");
   }
   Check(Run("").Exit != 0, "Silent success must fail.");
   Check(Run("PASS: no assertions counted").Exit != 0, "A generic PASS line must not count as executed checks.");
   Check(Run("CHECKS_PASSED:0").Exit != 0, "Zero checks must fail.");
   Check(Run("CHECKS_PASSED:2\nCHECKS_PASSED:2").Exit != 0, "Ambiguous duplicate reports must fail.");
   Check(Run("CHECKS_PASSED:2", build: 3).Exit != 0, "Build failures must fail verification.");
   Check(Run("CHECKS_PASSED:2", run: 4).Exit != 0, "A failing program must fail even after reporting checks.");
   Check(Run("CHECKS_PASSED:2", restore: 5).Exit != 0, "Restore failures must fail verification.");
   var success = Run("CHECKS_PASSED:3");
   Check(success.Exit == 0 && success.Output.Contains("2 regression groups, 6 assertions; no groups skipped."),
      "Successful verification must count every group and its assertions.");
} finally { Directory.Delete(root, true); }
Console.WriteLine("PASS: verification runner rejects empty, silent, ambiguous, and failed runs and reports successful group counts.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
