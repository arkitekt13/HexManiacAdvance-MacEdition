using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace HavenSoft.HexManiac.AvaloniaUI.MacPlatform;

/// <summary>Choose an application bundle through Standard Additions, avoiding file-picker package filters.</summary>
public static class ExternalEmulatorPicker {
   public const string Script = "try\n" +
      "return POSIX path of (choose application with title \"Choose External Emulator\" " +
      "with prompt \"Select mGBA or another Game Boy Advance emulator.\" as alias)\n" +
      "on error number -128\nreturn \"\"\nend try";

   public static ProcessStartInfo CreateStartInfo() {
      var info = new ProcessStartInfo("/usr/bin/osascript") {
         UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
      };
      info.ArgumentList.Add("-e");
      info.ArgumentList.Add(Script);
      return info;
   }

   public static string ReadSelection(int exitCode, string output, string error) {
      if (exitCode != 0) throw new IOException("The application chooser failed: " + error.Trim());
      var path = output.TrimEnd('\r', '\n', Path.DirectorySeparatorChar);
      return string.IsNullOrWhiteSpace(path) ? null : path;
   }

   public static async Task<string> ChooseAsync() {
      using var process = Process.Start(CreateStartInfo());
      if (process == null) throw new IOException("macOS could not open the application chooser.");
      var output = process.StandardOutput.ReadToEndAsync();
      var error = process.StandardError.ReadToEndAsync();
      await process.WaitForExitAsync();
      return ReadSelection(process.ExitCode, await output, await error);
   }
}
