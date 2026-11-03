using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

namespace HavenSoft.HexManiac.AvaloniaUI.MacPlatform;

/// <summary>Launch saved ROMs in an explicitly selected application, independent of Finder associations.</summary>
public sealed class ExternalEmulatorLauncher {
   readonly string preferencePath;
   readonly Func<Task<string>> chooseApplication;
   readonly Func<ProcessStartInfo, Task<string>> start;
   bool launching;

   public ExternalEmulatorLauncher(string preferencePath, Func<Task<string>> chooseApplication,
      Func<ProcessStartInfo, Task<string>> start = null) {
      this.preferencePath = preferencePath;
      this.chooseApplication = chooseApplication;
      this.start = start ?? StartAsync;
   }

   public async Task LaunchAsync(ViewPort view, IFileSystem fileSystem, Func<bool> confirmSave, Action<string> error) {
      if (launching) return;
      launching = true;
      try {
         if (view == null || view.UpdateInProgress) { error("Select a ROM and wait for its current update to finish before launching an emulator."); return; }
         if (!string.Equals(Path.GetExtension(view.FileName), ".gba", StringComparison.OrdinalIgnoreCase)) {
            error("Select a Game Boy Advance ROM to launch in an emulator."); return;
         }
         var application = File.Exists(preferencePath) ? File.ReadAllText(preferencePath).Trim() : null;
         if (!IsApplication(application)) {
            application = await chooseApplication();
            if (application == null) return;
            if (!IsApplication(application)) { error("Select an emulator application such as mGBA.app."); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(preferencePath));
            AtomicFileWriter.WriteAllLines(preferencePath, new[] { application });
         }
         // The picker can yield to navigation or other UI work. Retain the selected ROM,
         // but recheck its lifetime and pending source before saving or launching it.
         if (view.Model.ReferenceCount == 0 || view.UpdateInProgress) return;
         if (!view.ChangeHistory.IsSaved || view.Tools.CodeTool.HasUncompiledScriptChanges) {
            if (!confirmSave()) return;
            view.Save.Execute(fileSystem);
            if (!view.ChangeHistory.IsSaved || view.Tools.CodeTool.HasUncompiledScriptChanges) return;
         }
         if (string.IsNullOrEmpty(view.FullFileName) || !File.Exists(view.FullFileName)) {
            error("Save the ROM to disk before launching an emulator."); return;
         }
         var failure = await start(CreateStartInfo(application, view.FullFileName));
         if (failure != null) error("Could not launch the external emulator: " + failure);
      } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) {
         error("Could not launch the external emulator: " + exception.Message);
      } finally { launching = false; }
   }

   static bool IsApplication(string path) => !string.IsNullOrWhiteSpace(path) &&
      path.EndsWith(".app", StringComparison.OrdinalIgnoreCase) && Directory.Exists(path) &&
      File.Exists(Path.Combine(path, "Contents", "Info.plist"));

   public static ProcessStartInfo CreateStartInfo(string application, string rom) {
      var info = new ProcessStartInfo("/usr/bin/open") { UseShellExecute = false, RedirectStandardError = true };
      info.ArgumentList.Add("-a");
      info.ArgumentList.Add(application);
      info.ArgumentList.Add(rom);
      return info;
   }

   static async Task<string> StartAsync(ProcessStartInfo info) {
      using var process = Process.Start(info);
      if (process == null) return "macOS did not start the launch request.";
      var stderr = process.StandardError.ReadToEndAsync();
      await process.WaitForExitAsync();
      var message = await stderr;
      return process.ExitCode == 0 ? null : string.IsNullOrWhiteSpace(message) ? $"macOS returned error {process.ExitCode}." : message.Trim();
   }
}
