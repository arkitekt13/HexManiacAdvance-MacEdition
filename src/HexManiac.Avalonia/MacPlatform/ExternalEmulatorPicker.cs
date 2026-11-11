using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace HavenSoft.HexManiac.AvaloniaUI.MacPlatform;

/// <summary>Select a bundle directly without relying on application registration or file-type filters.</summary>
public static class ExternalEmulatorPicker {
   // No "of type" filter: some installed apps are disabled by native application/type
   // filtering. Keep bundles opaque so the selection is the .app, not an inner executable.
   // ExternalEmulatorLauncher validates the bundle before remembering or launching it.
   public const string Script = "try\n" +
      "return POSIX path of (choose file with prompt \"Choose an emulator application, such as mGBA.app.\" " +
      "default location (path to applications folder) without showing package contents)\n" +
      "on error number -128\nreturn \"\"\nend try";

   public static ProcessStartInfo CreateStartInfo() {
      var info = new ProcessStartInfo("/usr/bin/osascript") {
         WorkingDirectory = "/", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
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
