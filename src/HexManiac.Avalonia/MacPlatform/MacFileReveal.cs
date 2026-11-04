using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace HavenSoft.HexManiac.AvaloniaUI.MacPlatform;

public interface IFileRevealer {
   Task<string> RevealFileAsync(string path);
}

public static class MacFileReveal {
   public static ProcessStartInfo CreateStartInfo(string path) {
      var info = new ProcessStartInfo("/usr/bin/open") { UseShellExecute = false, RedirectStandardError = true };
      info.ArgumentList.Add("-R");
      info.ArgumentList.Add(Path.GetFullPath(path));
      return info;
   }

   public static async Task<string> RevealAsync(string path) {
      using var process = Process.Start(CreateStartInfo(path));
      if (process == null) return "macOS did not start Finder.";
      var stderr = process.StandardError.ReadToEndAsync();
      await process.WaitForExitAsync();
      var message = await stderr;
      return process.ExitCode == 0 ? null : string.IsNullOrWhiteSpace(message)
         ? $"macOS returned error {process.ExitCode}." : message.Trim();
   }
}
