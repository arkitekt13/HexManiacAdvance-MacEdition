using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

/// <summary>Ported from HexManiac.WPF/Windows/AboutWindow.xaml.cs.</summary>
public partial class AboutWindow : Window {
   const string LicenseUrl = "https://github.com/haven1433/HexManiacAdvance/blob/master/LICENSE";

   public AboutWindow() : this(null) { }

   public AboutWindow(IMetadataInfo metadata) {
      InitializeComponent();
      Version.Text = AppIdentity.VersionLabel;
      Usage.Text = GetUsageText(metadata);
   }

   public static string GetUsageText(IMetadataInfo metadata) {
      if (metadata != null && metadata.IsPublicRelease) {
         return "This is a Mac preview release. Please report bugs to the Mac fork on GitHub.";
      } else {
         return "This is an unstable version: use at your own risk!";
      }
   }

   /// WPF used NativeProcess.Start on the Hyperlink's Uri; on macOS that is `open`.
   private async void Navigate(object sender, RoutedEventArgs e) {
      e.Handled = true;
      var error = await MacHelpLink.OpenAsync(LicenseUrl);
      LinkError.Text = error == null ? string.Empty : "Could not open license: " + error;
   }
}
