using System;
using System.Diagnostics;
using Avalonia.Threading;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

/// <summary>
/// Ported from the CustomTraceListener at the bottom of HexManiac.WPF/Windows/MainWindow.xaml.cs.
///
/// HexManiac.Core calls Debug.Assert in a lot of places. The default listener pops a Windows
/// assert dialog, so WPF replaced it with one that goes through IFileSystem.ShowOptions and lets
/// the user silence further assertions for the session. The only changes here are
/// Dispatcher.UIThread.Invoke in place of Application.Current.Dispatcher.Invoke, and taking the
/// EditorViewModel directly instead of reaching for Application.Current.MainWindow.
/// </summary>
public class CustomTraceListener : TraceListener {
   private readonly IFileSystem fileSystem;
   private readonly EditorViewModel editor;
   private readonly TraceListener core = new DefaultTraceListener();

   private bool ignoreAssertions;
   private readonly string versionNumber;

   public CustomTraceListener(IFileSystem fs, EditorViewModel editor) {
      fileSystem = fs;
      this.editor = editor;
      versionNumber = $" (Mac {AppIdentity.VersionNumber}, {AppIdentity.BuildConfiguration})";
   }

   public override void Fail(string message, string detailMessage) {
      if (ignoreAssertions) return;
      if (Debugger.IsAttached) {
         core.Fail(message, detailMessage);
         return;
      }

      int result = 0;

      Dispatcher.UIThread.Invoke(() => {
         result = fileSystem.ShowOptions(
            "Debug Assert!" + versionNumber,
            message + Environment.NewLine + Environment.NewLine + detailMessage,
            null,
            new[] {
               new VisualOption {
                  Index = 0,
                  Option = "Ignore",
                  ShortDescription = "Don't Show Assertions",
                  Description = "Ignore assertions until the next time the application is opened."
               },
               new VisualOption {
                  Index = 1,
                  Option = "Debug",
                  ShortDescription = "View Details",
                  Description = "Bring up the full dialog with debugging options."
               },
               new VisualOption {
                  Index = 2,
                  Option = "Continue",
                  ShortDescription = "Ignore This One",
                  Description = "Ignore this assertion, but show this dialog again if there's another."
               },
            });
      });

      // user hit "Ignore Additional Assertions"
      ignoreAssertions = result == 0;
      while (result == 1) {
         if (Debugger.IsAttached) {
            Debugger.Break();
            break;
         } else {
            string logs = string.Empty;
            if (editor != null) {
               if (editor.SelectedTab is IViewPort viewPort && viewPort.Tools is ToolTray tray && tray.LogTool is LogTool logTool) {
                  logs = Environment.NewLine.Join(logTool.LogMessages);
               } else if (editor.SelectedTab is MapEditorViewModel mapEditor && mapEditor.ViewPort.Tools is ToolTray mapTray && mapTray.LogTool is LogTool mapLogTool) {
                  logs = Environment.NewLine.Join(mapLogTool.LogMessages);
               }
            }
            Dispatcher.UIThread.Invoke(() => {
               result = fileSystem.ShowOptions(
                  "Attach a Debugger",
                  "Attach a debugger and click 'Debug' to get more information about the following assertion:" + Environment.NewLine +
                  message + Environment.NewLine +
                  detailMessage + Environment.NewLine +
                  "To report this assertion, copy the stack trace into an issue on the Mac fork." +
                  Environment.NewLine + Environment.NewLine +
                  logs + Environment.NewLine +
                  "Stack Trace:" + Environment.NewLine +
                  Environment.StackTrace,
                  null,
                  new[] {
                     new VisualOption {
                        Index = 1,
                        Option = "Debug",
                        ShortDescription = "Show in Debugger",
                        Description = "Break in a connected debugger"
                     },
                     new VisualOption {
                        Index = 2,
                        Option = "Report",
                        ShortDescription = "Open Mac GitHub Issues",
                        Description = "Review and paste the stack trace into an issue on the Mac fork."
                     },
                  });
            });
            if (result == 2) {
               fileSystem.LaunchProcess("open", AppIdentity.IssuesUrl);
               result = 1;
            }
         }
      }
   }

   public override void Write(string message) => core.Write(message);

   public override void WriteLine(string message) => core.WriteLine(message);
}
