using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MapHelpLink")]

namespace HavenSoft.HexManiac.AvaloniaUI.MacPlatform;

internal static class MacHelpLink {
   internal static ProcessStartInfo CreateStartInfo(string url) {
      if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
          (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
         throw new ArgumentException("The help link is not a valid web address.");
      // A running app's bundle may have been replaced by a rebuild. Neither
      // executable lookup nor the child process may depend on its old cwd.
      return MacOpen.CreateStartInfo(url);
   }

   internal static async Task<string> OpenAsync(string url, Func<ProcessStartInfo, Process> start = null) {
      try {
         using var process = (start ?? Process.Start)(CreateStartInfo(url));
         if (process == null) return "macOS did not start the browser.";
         var stderr = process.StandardError.ReadToEndAsync();
         await process.WaitForExitAsync();
         var message = await stderr;
         return process.ExitCode == 0 ? null : string.IsNullOrWhiteSpace(message)
            ? $"macOS returned error {process.ExitCode}." : message.Trim();
      } catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException) {
         return ex.Message;
      }
   }
}
