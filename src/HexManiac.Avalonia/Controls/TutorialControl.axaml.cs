using System;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using HavenSoft.HexManiac.AvaloniaUI.Resources;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.ViewModels.Map;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>
/// Ported from HexManiac.WPF/Controls/TutorialControl.xaml.cs.
///
/// WPF drove this entirely with DoubleAnimations and ItemContainerGenerator lookups. Avalonia has
/// neither imperative BeginAnimation nor ItemContainerGenerator, so card movement goes through
/// AnimationExtensions (property transitions) and containers are found with ContainerFromItem.
/// </summary>
public partial class TutorialControl : UserControl {
   private const double ElementHeight = 80;
   private const double CollapsedWidth = 72;
   private const double ExpandedWidth = 320;

   // Core tracks tutorial rows independently of the Mac interface's card size.
   public static readonly IValueConverter RowOffsetConverter =
      new FuncValueConverter<double, double>(row => row * ElementHeight);

   // Shared Core tutorials retain Windows wording; match the Mac map input bindings at display time.
   public static readonly IValueConverter ModifierTextConverter =
      new FuncValueConverter<string, string>(text => OperatingSystem.IsMacOS()
         ? text?.Replace("Control", "Command (⌘)") : text);

   public static readonly IValueConverter ModifierIconConverter =
      new FuncValueConverter<string, Geometry>(icon => IconExtension.GetIcon(
         OperatingSystem.IsMacOS() && icon == "CtrlKey" ? "CommandKey" : icon));

   public event EventHandler<MapTutorialViewModel> EnterTutorial;

   public MapTutorialsViewModel ViewModel => DataContext as MapTutorialsViewModel;
   private MapTutorialsViewModel subscribed;

   public TutorialControl() {
      InitializeComponent();
      DataContextChanged += (sender, e) => UpdateTutorialHandlers();
      ContentPanel.Transitions = new Transitions {
         new DoubleTransition { Property = WidthProperty, Duration = TimeSpan.FromMilliseconds(160), Easing = new CubicEaseOut() },
      };
      // Tutorial interaction must not bubble into the map's drawing/panning/zoom handlers.
      AddHandler(PointerPressedEvent, (_, e) => e.Handled = true, RoutingStrategies.Bubble);
      AddHandler(PointerReleasedEvent, (_, e) => e.Handled = true, RoutingStrategies.Bubble);
      AddHandler(PointerWheelChangedEvent, (_, e) => e.Handled = true, RoutingStrategies.Bubble);
   }

   protected override void OnPointerEntered(PointerEventArgs e) {
      base.OnPointerEntered(e);
      // The map anchors this control to the right. Growing its actual layout width opens
      // the cards to the left and keeps their descriptions inside the hover/clip bounds.
      ContentPanel.Width = ExpandedWidth;
   }

   protected override void OnPointerExited(PointerEventArgs e) {
      base.OnPointerExited(e);
      ContentPanel.Width = CollapsedWidth;
   }

   private void UpdateTutorialHandlers() {
      if (subscribed != null) {
         foreach (var tut in subscribed.Tutorials) {
            tut.PropertyChanged -= HandleTutorialChanged;
            tut.AnimateMovement -= HandleTutorialChanged;
         }
         subscribed.CompletedTutorial -= ShowCheck;
      }
      subscribed = ViewModel;
      if (subscribed != null) {
         foreach (var tut in subscribed.Tutorials) {
            tut.PropertyChanged += HandleTutorialChanged;
            tut.AnimateMovement += HandleTutorialChanged;
         }
         subscribed.CompletedTutorial += ShowCheck;
      }
   }

   private void HandleTutorialChanged(object sender, EventArgs e) {
      var tut = (MapTutorialViewModel)sender;
      var ui = Tutorials.ContainerFromItem(tut);
      if (ui == null) return;
      ui.IsVisible = true;
      ui.AnimateTop(tut.TargetPosition * ElementHeight);
      ui.AnimateOpacity(double.NaN, tut.Incomplete ? 1 : 0, 0);
      ui.AnimateLeft(tut.Incomplete ? 0 : 100);
   }

   private void ShowCheck(object sender, EventArgs e) {
      var tut = (MapTutorialViewModel)sender;
      if (tut.Index >= 5) return; // don't show the checkmark for off-screen tutorials
      var ui = Tutorials.ContainerFromItem(tut);
      if (ui == null) return;
      Canvas.SetTop(Check, tut.TargetPosition * ElementHeight + 20);
      // Start over the icon column in either layout state, then fly off the right edge.
      Check.AnimateRight(ContentPanel.Width - 56, -56);
      Check.AnimateOpacity(1, 0, 1);
   }

   /// WPF raised this from MouseEnter on each card's Border.
   private void OnEnterTutorial(object sender, PointerEventArgs e) {
      if (sender is not Control element) return;
      if (element.DataContext is not MapTutorialViewModel tutorial) return;
      EnterTutorial.Raise(this, tutorial);
   }
}
