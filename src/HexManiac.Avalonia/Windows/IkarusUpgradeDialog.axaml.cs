using Avalonia.Controls;
using Avalonia.Interactivity;
using HavenSoft.HexManiac.AvaloniaUI.Project;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

public partial class IkarusUpgradeDialog : Window {
   public IkarusUpgradeDialog() => InitializeComponent();
   public IkarusUpgradeDialog(string credits, bool forScade = false) : this() {
      Title = forScade ? ScadeIkarusPackage.Title : IkarusUpgrade.Title;
      HeadingText.Text = Title;
      RecommendationText.Text = IkarusUpgrade.Recommendation;
      ChangesText.Text = IkarusUpgrade.Changes;
      CompatibilityText.Text = IkarusUpgrade.Compatibility;
      CreditsText.Text = credits;
      if (forScade) {
         RecommendationText.Text = "Requires the exact fresh SCADE " + ScadeIkarusPackage.SourceVersion + " ROM created by HMA. Edited projects, other SCADE versions and already-patched ROMs are rejected.";
         CompatibilityText.Text = ScadeIkarusPackage.ExperimentalNotice;
         WorkflowText.Text = "Choose a folder and a new ROM name. A matching TOML with all SCADE definitions and relocated graphics definitions will be installed beside the new ROM before it opens. Your source ROM and TOML stay unchanged. Existing files will never be replaced.";
         ContinueButton.Content = "Create New ROM…";
      }
   }
   private void Cancel(object sender, RoutedEventArgs e) => Close(false);
   private void Accept(object sender, RoutedEventArgs e) => Close(true);
}
