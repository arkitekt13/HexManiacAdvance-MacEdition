using Avalonia.Controls;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>The empty editor's welcome screen; opening a ROM uses the normal editor command.</summary>
public partial class StartScreen : Grid {
   public static string VersionLabel {
      get {
         var version = typeof(StartScreen).Assembly.GetName().Version?.ToString(3) ?? "unknown";
#if DEBUG
         const string build = "Debug build";
#else
         const string build = "Release build";
#endif
         return $"Version {version} · Mac · {build}";
      }
   }

   public StartScreen() {
      InitializeComponent();
      VersionInformation.Text = VersionLabel;
   }
}

/// <summary>Measures its child without reserving any space for it.</summary>
public class MeasureEmptyDecorator : Decorator {
   protected override Avalonia.Size MeasureOverride(Avalonia.Size constraint) {
      Child?.Measure(default);
      return default;
   }
}
