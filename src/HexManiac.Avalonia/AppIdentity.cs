using System;
using System.Reflection;

namespace HavenSoft.HexManiac.AvaloniaUI;

/// <summary>Mac application identity; independent of Core's metadata format/migration version.</summary>
public static class AppIdentity {
   public const string RepositoryUrl = "https://github.com/arkitekt13/HexManiacAdvance-mac";
   public const string MacHelpUrl = RepositoryUrl + "/blob/avalonia-port/README.md#on-macos";
   public const string IssuesUrl = RepositoryUrl + "/issues";
   public static string VersionNumber {
      get {
         var version = typeof(AppIdentity).Assembly.GetName().Version;
         return version == null ? "unknown" : version.ToString(version.Revision > 0 ? 4 : 3);
      }
   }
   public static string InformationalVersion => typeof(AppIdentity).Assembly
      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? VersionNumber;
#if DEBUG
   public const string BuildConfiguration = "Debug";
#else
   public const string BuildConfiguration = "Release";
#endif
   public static string VersionLabel => $"Version {VersionNumber} · Mac · {BuildConfiguration} build";
}
