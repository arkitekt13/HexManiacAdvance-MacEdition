using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;

var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
var root = Path.Combine(Path.GetTempPath(), "hma-crash-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try {
   var version = typeof(AppIdentity).Assembly.GetName().Version!;
   Check(AppIdentity.VersionNumber == version.ToString(version.Revision > 0 ? 4 : 3), "Use actual app assembly version.");
   Check(AppIdentity.VersionNumber != "0.0.0", "App must have a meaningful version.");
   Check(StartScreen.VersionLabel == AppIdentity.VersionLabel, "Welcome screen shares diagnostic identity.");
   Check(Path.IsPathFullyQualified(CrashReporter.DefaultLogDirectory), "Default log path must be absolute.");
   Check(CrashReporter.DefaultLogDirectory.Contains("HexManiacAdvance"), "Logs have a dedicated app folder.");
   string message = null;
   ProcessModel[] links = null;
   var fs = new StubFileSystem { ShowCustomMessageBox = (text, choices, actions) => {
      Check(!choices, "Crash dialog must not offer misleading save/discard choices.");
      message = text; links = actions; return null;
   }};
   var exception = new InvalidOperationException("outer diagnostic", new IOException("inner diagnostic"));
   var report = CrashReporter.BuildReport(null, exception);
   Check(report.Contains("Version Number: " + AppIdentity.VersionNumber), "Report uses app identity even with no editor.");
   Check(report.Contains("No Editor ViewModel found."), "Early startup crash is reportable.");
   Check(report.Contains("inner diagnostic") && report.Contains("outer diagnostic"), "Keep inner exception details.");
   Check(report.Contains("Architecture:") && report.Contains("Runtime:") && report.Contains("OS:"), "Report platform diagnostics.");
   Check(report.Contains(AppIdentity.RepositoryUrl), "Include fork identity.");
   var success = Path.Combine(root, "logs");
   CrashReporter.Report(null, fs, "HexManiacAdvance", exception, success);
   var path = Path.Combine(success, "crash.log");
   Check(File.Exists(path) && File.ReadAllText(path).Contains("inner diagnostic"), "Create directory and retain complete report.");
   Check(message.Contains(path) && !message.Contains("Could not save"), "Show actual saved path.");
   Check(links.Single(l => l.DisplayText.Contains("Finder")).Content == "/" + path, "Finder action targets saved file.");
   var github = links.Single(l => l.DisplayText.Contains("GitHub")).Content;
   Check(github.StartsWith("https://github.com/arkitekt13/HexManiacAdvance-mac/issues/new?"), "Report to our fork.");
   Check(!links.Any(l => l.Content.Contains("discord.gg")), "Do not direct fork reports to upstream Discord.");
   Check(links.Single(l => l.Content.StartsWith("~")).Content.Contains("inner diagnostic"), "Clipboard retains full exception chain.");
   var firstLength = new FileInfo(path).Length;
   CrashReporter.Report(null, fs, "HexManiacAdvance", exception, success);
   Check(new FileInfo(path).Length > firstLength, "Append instead of replacing prior crash reports.");
   var blocked = Path.Combine(root, "blocked"); File.WriteAllText(blocked, "keep");
   CrashReporter.Report(null, fs, "HexManiacAdvance", exception, blocked);
   Check(message.Contains("Could not save crash.log") && !message.Contains("has been logged"), "Report logging failure truthfully.");
   Check(!links.Any(l => l.DisplayText.Contains("Finder")), "Do not offer Finder for an unsaved report.");
   Check(links.Any(l => l.Content.StartsWith("~") && l.Content.Contains("inner diagnostic")), "Copy remains available after disk failure.");
   Check(File.ReadAllText(blocked) == "keep", "Failed logging preserves unrelated file.");
   var longError = new Exception(new string('x', 100000));
   Check(CrashReporter.BuildIssueUrl(longError).Length < 2000, "Issue URL stays usable for enormous exceptions.");
   string assertionTitle = null;
   var assertionFs = new StubFileSystem { ShowOptions = (title, text, details, options) => {
      assertionTitle = title; return 2;
   }};
   using (var listener = new CustomTraceListener(assertionFs, null)) listener.Fail("synthetic assertion", "test details");
   Check(assertionTitle.Contains(AppIdentity.VersionNumber) && assertionTitle.Contains(AppIdentity.BuildConfiguration), "Assertion reports use the same app identity.");
   var previous = message;
   CrashReporter.Report(null, fs, "HexManiacAdvance", null, success);
   Check(message == previous, "Null exception is a no-op.");
   // Invalid paths are another logging failure, not a second exception.
   CrashReporter.Report(null, fs, "HexManiacAdvance", exception, "\0");
   Check(message.Contains("Could not save crash.log"), "Invalid log paths retain the report dialog.");
} finally { Directory.Delete(root, true); }
Console.WriteLine("PASS: shared app identity, fork reporting, complete exception chains, stable log paths, append and failed-write behavior.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
