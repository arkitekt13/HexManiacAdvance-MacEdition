using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Text;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

/// <summary>
/// Ported from the crash-reporting half of HexManiac.WPF/Windows/MainWindow.xaml.cs
/// (HandleException / AppendGeneralAppInfo / AppendException / ExtractExceptionInfo).
///
/// Split into its own file because MainWindow.axaml.cs is already the largest file in the port,
/// and because Avalonia raises unhandled exceptions from two places rather than WPF's single
/// DispatcherUnhandledException, so both entry points call in here.
///
/// The WPF version also offered to turn off hardware acceleration when the render thread failed
/// ("SyncFlush" in the stack). That is an HwndTarget.RenderMode change with no Avalonia analog -
/// the render mode is fixed when the platform is initialized in Program.cs - so a render failure
/// falls through to the normal report instead.
/// </summary>
public static class CrashReporter {
   /// <summary>A stable writable location outside the app bundle and working directory.</summary>
   public static string DefaultLogDirectory => OperatingSystem.IsMacOS()
      ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", "HexManiacAdvance")
      : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HexManiacAdvance", "Logs");

   /// <param name="logDirectory">Optional destination for isolated diagnostic tests.</param>
   public static void Report(EditorViewModel editor, IFileSystem fileSystem, string windowTitle, Exception exception, string logDirectory = null) {
      if (exception == null) return;
      var report = BuildReport(editor, exception);
      string savedPath = null;
      string logFailure = null;
      try {
         var directory = Path.GetFullPath(logDirectory ?? DefaultLogDirectory);
         Directory.CreateDirectory(directory);
         var path = Path.Combine(directory, "crash.log");
         File.AppendAllText(path, report);
         savedPath = path;
      } catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException) {
         // Keep the full report available to copy even when disk logging fails.
         logFailure = error.Message;
      }

      var links = new List<ProcessModel>();
      if (savedPath != null) links.Add(new ProcessModel("Show crash.log in Finder", "/" + savedPath));
      links.Add(new ProcessModel("Report this via GitHub (Mac fork)", BuildIssueUrl(exception)));
      links.Add(new ProcessModel("Copy full crash report to the clipboard", "~" +
         "I got a crash! (" + AppIdentity.VersionNumber + ")" + Environment.NewLine +
         "Please describe what you were doing right before the crash:" + Environment.NewLine +
         "```text" + Environment.NewLine + report + "```"));
      fileSystem.ShowCustomMessageBox(
         "An unhandled error occurred. Please report it to the Mac fork on GitHub." + Environment.NewLine +
         windowTitle + " might be in a bad state. You should close as soon as possible." + Environment.NewLine +
         "Here's a summary of the issue:" + Environment.NewLine + Environment.NewLine +
         ExtractExceptionInfo(exception) + Environment.NewLine +
         (savedPath != null ? "The error has been logged to: " + savedPath
            : "Could not save crash.log: " + logFailure + Environment.NewLine + "You can still copy the full report below.") +
         Environment.NewLine + "Review the report for personal paths or filenames before sharing it.",
         showYesNoCancel: false, links.ToArray());
   }

   /// <summary>Keep browser URLs short; the complete diagnostic report is copied separately.</summary>
   public static string BuildIssueUrl(Exception exception) {
      var title = $"[Mac {AppIdentity.VersionNumber}] {exception.GetType().Name}";
      var body = "Please describe what you were doing and how to reproduce the crash.\n\n" +
         AppIdentity.VersionLabel + "\n" + exception.GetType().FullName + "\n\n" +
         "Use 'Copy full crash report to the clipboard' in the crash dialog and paste it below after reviewing personal paths and filenames.\n";
      return AppIdentity.IssuesUrl + "/new?title=" + Uri.EscapeDataString(title) + "&body=" + Uri.EscapeDataString(body);
   }

   public static string BuildReport(EditorViewModel editor, Exception exception) {
      var text = new StringBuilder();
      text.AppendLine("Version Number: " + AppIdentity.VersionNumber);
      text.AppendLine(AppIdentity.BuildConfiguration + " Version");
      text.AppendLine("Build identity: " + AppIdentity.InformationalVersion);
      text.AppendLine("Repository: " + AppIdentity.RepositoryUrl);
      text.AppendLine("OS: " + RuntimeInformation.OSDescription);
      text.AppendLine("Architecture: " + RuntimeInformation.ProcessArchitecture);
      text.AppendLine("Runtime: " + RuntimeInformation.FrameworkDescription);
      text.AppendLine(DateTimeOffset.Now.ToString("O"));
      text.AppendLine("General Information:");
      // Damaged editor state must not prevent recording the original exception.
      try { AppendGeneralAppInfo(text, editor); }
      catch (Exception error) { text.AppendLine("Editor diagnostics unavailable: " + error.GetType().Name + ": " + error.Message); }
      try {
         IEnumerable<string> logs = null;
         if (editor?.SelectedTab is IViewPort vp && vp.Tools is ToolTray tray) logs = tray.LogTool?.LogMessages;
         else if (editor?.SelectedTab is MapEditorViewModel map && map.ViewPort.Tools is ToolTray mapTray) logs = mapTray.LogTool?.LogMessages;
         if (logs != null) {
            var lines = logs.ToList();
            if (lines.Count > 15) lines.RemoveRange(1, lines.Count - 15);
            if (lines.Count > 0) {
               text.AppendLine("Recent Log Information:");
               foreach (var line in lines) text.AppendLine(line);
            }
         }
      } catch (Exception error) { text.AppendLine("Recent logs unavailable: " + error.GetType().Name); }
      text.AppendLine("Exception Information:");
      AppendException(text, exception);
      text.AppendLine("-------------------------------------------");
      text.AppendLine();
      return text.ToString();
   }

   private static string ExtractExceptionInfo(Exception ex) {
      var exceptionTypeShortName = ex.GetType().ToString().Split('.').Last().Split("Exception").First();
      var info = exceptionTypeShortName + ":" + Environment.NewLine + $"{ex.Message}" + Environment.NewLine;
      if (ex is AggregateException ag) {
         foreach (var e in ag.InnerExceptions) info += ExtractExceptionInfo(e);
      }
      return info;
   }

   private static void AppendException(StringBuilder text, Exception ex, int indent = 0) {
      var lineStart = new string(' ', indent);

      text.AppendLine(lineStart + ex.GetType().ToString());
      text.AppendLine(lineStart + ex.Message);
      if (ex is ArgumentOutOfRangeException aoore) text.AppendLine(lineStart + aoore.ActualValue?.ToString() ?? "<null>");
      if (ex is AggregateException ae) {
         foreach (var e in ae.InnerExceptions) AppendException(text, e, indent + 2);
      } else if (ex.InnerException != null) {
         AppendException(text, ex.InnerException, indent + 2);
      }

      text.AppendLine(ex.StackTrace + Environment.NewLine);
   }

   private static void AppendGeneralAppInfo(StringBuilder text, EditorViewModel editor) {
      if (editor == null) {
         text.AppendLine("No Editor ViewModel found.");
         return;
      }
      text.AppendLine("Current tab count: " + editor.Count);
      text.AppendLine("Current selected tab: " + editor.SelectedIndex);
      text.AppendLine("---");
      foreach (var tab in editor) {
         if (tab is IEditableViewPort viewPort) {
            text.AppendLine("Tab is ViewPort for " + Path.GetFileName(viewPort.FileName));
            text.AppendLine("Game Code: " + viewPort.Model.GetGameCode());
            text.AppendLine("Data Length: 0x" + viewPort.Model.Count.ToAddress());
            text.AppendLine("Pokemon Count: " + (viewPort.Model.GetTable(HardcodeTablesModel.PokemonNameTable)?.ElementCount ?? 0));
            text.AppendLine("---");
         } else if (tab is MapEditorViewModel mapEditor && mapEditor.PrimaryMap is not null) {
            var layout = new LayoutModel(mapEditor.PrimaryMap.GetLayout());
            var events = mapEditor.PrimaryMap.EventGroup;
            text.AppendLine("Tab is map for " + mapEditor.PrimaryMap.Name);
            text.AppendLine($"Size: {layout.Width}x{layout.Height}");
            if (events == null) text.AppendLine("(No Events)");
            else text.AppendLine($"Events: {events.Objects.Count}-{events.Warps.Count}-{events.Scripts.Count}-{events.Signposts.Count}");
            text.AppendLine("---");
         } else {
            text.AppendLine($"Tab is {tab.GetType()}");
            text.AppendLine(tab.Name);
            text.AppendLine("---");
         }
      }
   }
}
