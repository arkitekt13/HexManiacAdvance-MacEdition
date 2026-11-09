using Avalonia.Controls;
using Avalonia.Interactivity;
using HavenSoft.HexManiac.AvaloniaUI.Project;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

public partial class IkarusUpgradeDialog : Window {
   public IkarusUpgradeDialog() => InitializeComponent();
   public IkarusUpgradeDialog(string credits) : this() {
      RecommendationText.Text = IkarusUpgrade.Recommendation;
      ChangesText.Text = IkarusUpgrade.Changes;
      CompatibilityText.Text = IkarusUpgrade.Compatibility;
      CreditsText.Text = credits;
   }
   private void Cancel(object sender, RoutedEventArgs e) => Close(false);
   private void Accept(object sender, RoutedEventArgs e) => Close(true);
}
