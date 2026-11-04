using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>The empty editor's welcome screen; opening a ROM uses the normal editor command.</summary>
public partial class StartScreen : Grid {
   public static string VersionLabel => AppIdentity.VersionLabel;

   public StartScreen() {
      InitializeComponent();
      VersionInformation.Text = VersionLabel;
   }

   private void Navigate(object sender, RoutedEventArgs e) {
      if (sender is Control { Tag: string url }) {
         var start = new ProcessStartInfo("open") { UseShellExecute = false };
         start.ArgumentList.Add(url);
         Process.Start(start);
      }
      e.Handled = true;
   }
}

/// <summary>Measures its child without reserving any space for it.</summary>
public class MeasureEmptyDecorator : Decorator {
   protected override Avalonia.Size MeasureOverride(Avalonia.Size constraint) {
      Child?.Measure(default);
      return default;
   }
}
