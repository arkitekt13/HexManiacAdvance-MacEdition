using System;
using System.Diagnostics;
using System.IO;

namespace HavenSoft.HexManiac.AvaloniaUI.MacPlatform;

internal static class MacOpen {
   internal static ProcessStartInfo CreateStartInfo(string target, bool reveal = false) {
      if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("No file or address was supplied.");
      if (!Path.IsPathRooted(target) && !Uri.TryCreate(target, UriKind.Absolute, out _)) target = Path.GetFullPath(target, AppContext.BaseDirectory);
      var info = new ProcessStartInfo("/usr/bin/open") {
         WorkingDirectory = "/", UseShellExecute = false, RedirectStandardError = true,
      };
      if (reveal) info.ArgumentList.Add("-R");
      info.ArgumentList.Add(target);
      return info;
   }
}
