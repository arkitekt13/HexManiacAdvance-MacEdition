using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI.Resources;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>
/// Ported from HexManiac.WPF/Controls/AutocompleteOverlay.xaml.cs.
///
/// Notable differences: Visibility becomes IsVisible (and drives Popup.IsOpen directly rather
/// than through a DataTrigger); PreviewKeyDown/PreviewMouseDown become tunnelling handlers; and
/// Avalonia's TextBox exposes no VerticalOffset/ExtentHeight, so the popup's vertical placement
/// is derived from the line height and caret line instead of the scroll extent.
/// </summary>
public partial class AutocompleteOverlay : UserControl {
   private bool acceptingCompletion;
   private string acceptedText;
   private TextBox acceptedTarget;
   private object acceptedContext;
   private void ResetAcceptedText() {
      acceptedText = null; acceptedTarget = null; acceptedContext = null;
   }
   #region Target

   public static readonly StyledProperty<Control> TargetProperty =
      AvaloniaProperty.Register<AutocompleteOverlay, Control>(nameof(Target));

   public Control Target {
      get => GetValue(TargetProperty);
      set => SetValue(TargetProperty, value);
   }

   static AutocompleteOverlay() {
      TargetProperty.Changed.AddClassHandler<AutocompleteOverlay>((self, e) => self.OnTargetChanged(e));
   }

   protected virtual void OnTargetChanged(AvaloniaPropertyChangedEventArgs e) {
      ResetAcceptedText();
      ClearAutocompleteOptions();
      UnsubscribeTarget();
      SubscribeTarget();
      WatchLifetime();
   }

   private TextBox subscribedTarget;
   private readonly HashSet<Visual> lifetimeAncestors = new();
   private WindowBase ownerWindow;

   private void UnsubscribeTarget() {
      if (subscribedTarget is TextBox oldTarget) {
         oldTarget.TextChanged -= TargetTextChanged;
         oldTarget.RemoveHandler(KeyDownEvent, TargetKeyDown);
         oldTarget.PropertyChanged -= TargetPropertyChanged;
         oldTarget.LostFocus -= TargetLostFocus;
         oldTarget.AttachedToVisualTree -= TargetAttached;
         oldTarget.DetachedFromVisualTree -= TargetDetached;
      }
      subscribedTarget = null;
   }

   private void SubscribeTarget() {
      if (this.GetVisualRoot() != null && TargetTextBox is TextBox newTarget) {
         subscribedTarget = newTarget;
         newTarget.TextChanged += TargetTextChanged;
         newTarget.AddHandler(KeyDownEvent, TargetKeyDown, RoutingStrategies.Tunnel);
         newTarget.PropertyChanged += TargetPropertyChanged;
         newTarget.LostFocus += TargetLostFocus;
         newTarget.AttachedToVisualTree += TargetAttached;
         newTarget.DetachedFromVisualTree += TargetDetached;
      }
   }

   protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
      base.OnAttachedToVisualTree(e);
      ClearAutocompleteOptions();
      UnsubscribeTarget();
      SubscribeTarget();
      WatchLifetime();
   }

   protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
      ResetAcceptedText();
      ClearAutocompleteOptions();
      UnsubscribeTarget();
      StopWatchingLifetime();
      base.OnDetachedFromVisualTree(e);
   }

   private void TargetAttached(object sender, VisualTreeAttachmentEventArgs e) {
      ClearAutocompleteOptions();
      WatchLifetime();
   }

   private void TargetDetached(object sender, VisualTreeAttachmentEventArgs e) {
      ClearAutocompleteOptions();
      WatchLifetime();
   }

   private void StopWatchingLifetime() {
      foreach (var ancestor in lifetimeAncestors) ancestor.PropertyChanged -= AncestorPropertyChanged;
      lifetimeAncestors.Clear();
      if (ownerWindow != null) {
         ownerWindow.Deactivated -= OwnerDeactivated;
         ownerWindow.RemoveHandler(PointerPressedEvent, OwnerPointerPressed);
      }
      ownerWindow = null;
   }

   private void WatchLifetime() {
      StopWatchingLifetime();
      if (this.GetVisualRoot() == null) return;
      // The target and overlay can occupy different branches of a template.
      foreach (var ancestor in this.GetVisualAncestors().Concat(TargetTextBox?.GetVisualAncestors() ?? Enumerable.Empty<Visual>())) {
         if (lifetimeAncestors.Add(ancestor)) ancestor.PropertyChanged += AncestorPropertyChanged;
      }
      ownerWindow = TopLevel.GetTopLevel(this) as WindowBase;
      if (ownerWindow != null) {
         ownerWindow.Deactivated += OwnerDeactivated;
         ownerWindow.AddHandler(PointerPressedEvent, OwnerPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
      }
   }

   private void AncestorPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e) {
      if ((e.Property == IsVisibleProperty || e.Property == IsEnabledProperty) && e.NewValue is false) ClearAutocompleteOptions();
   }

   private void OwnerDeactivated(object sender, EventArgs e) => ClearAutocompleteOptions();

   private void OwnerPointerPressed(object sender, PointerPressedEventArgs e) {
      if (!IsVisible || e.Source is not Visual source) return;
      bool Inside(Visual root) => root != null && (ReferenceEquals(source, root) || source.GetVisualAncestors().Contains(root));
      if (!Inside(Target) && !Inside(Popup.Child)) ClearAutocompleteOptions();
      // Observe only: the original pointer event must still reach the clicked control.
   }

   // Check the host, not this overlay's visibility: a closed overlay must be able to open.
   private bool CanOffer() => TargetTextBox is TextBox box && box.IsFocused && box.IsEffectivelyVisible && box.IsEffectivelyEnabled &&
      this.GetVisualParent()?.IsEffectivelyVisible == true && TopLevel.GetTopLevel(this) is TopLevel owner &&
      ReferenceEquals(TopLevel.GetTopLevel(box), owner) && (ownerWindow == null || ownerWindow.IsActive);

   public TextBox TargetTextBox {
      get {
         var control = Target;
         if (control is TextEditor editor) control = editor.TransparentLayer;
         return control as TextBox;
      }
   }

   #endregion

   private readonly TranslateTransform autocompleteTransform = new();

   public AutocompleteOverlay() {
      InitializeComponent();
      RenderTransform = autocompleteTransform;
      DataContextChanged += (_, _) => { ResetAcceptedText(); ClearAutocompleteOptions(); };
   }

   public void ClearAutocompleteOptions() {
      AutocompleteItems.ItemsSource = null;
      IsVisible = false;
      ignoreNextSelectionChange = false;
      if (DataContext is StreamElementViewModel streamViewModel) streamViewModel.ClearAutocomplete();
   }

   private void ShowAutocompleteOptions() => IsVisible = true;

   // Text can contain LF, CRLF, or mixed separators regardless of the host OS.
   // Keep the original span so accepting a completion never rejoins/normalizes other lines.
   private static (int Start, int Length, int Number, int Column) LineAt(string text, int caret) {
      caret = Math.Clamp(caret, 0, text.Length);
      int start = 0, number = 0;
      for (int i = 0; i < text.Length; i++) {
         if (text[i] != '\r' && text[i] != '\n') continue;
         int next = i + (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n' ? 2 : 1);
         if (caret < next) return (start, i - start, number, Math.Min(caret, i) - start);
         start = next;
         number++;
         i = next - 1;
      }
      return (start, text.Length - start, number, caret - start);
   }

   private void TargetTextChanged(object sender, TextChangedEventArgs e) {
      var box = TargetTextBox;
      if (!ReferenceEquals(sender, box) || !CanOffer()) {
         ClearAutocompleteOptions();
         return;
      }
      // TextChanged is queued by Avalonia. Notifications from applying a completion
      // can arrive after Click/Enter has already closed the popup, especially for
      // movement enums which still match their complete instruction name.
      if (acceptingCompletion || (ReferenceEquals(box, acceptedTarget) && ReferenceEquals(DataContext, acceptedContext) && box.Text == acceptedText)) {
         ClearAutocompleteOptions();
         return;
      }
      ResetAcceptedText();
      if (box.CaretIndex == 0) {
         ClearAutocompleteOptions();
         return;
      }
      Func<string, int, int, IReadOnlyList<AutocompleteItem>> getAutocomplete;
      if (DataContext is ToolTray tools) {
         getAutocomplete = tools.StringTool.GetAutocomplete;
      } else if (DataContext is StreamElementViewModel streamViewModel) {
         getAutocomplete = streamViewModel.GetAutoCompleteOptions;
      } else if (DataContext is ObjectEventViewModel mapObjectViewModel) {
         if (mapObjectViewModel.ShowMartContents) {
            getAutocomplete = mapObjectViewModel.GetMartAutocomplete;
         } else if (mapObjectViewModel.ShowTrainerContent) {
            getAutocomplete = mapObjectViewModel.GetTrainerAutocomplete;
         } else {
            ClearAutocompleteOptions();
            return;
         }
      } else if (DataContext is MultiFieldArrayElementViewModel multi) {
         getAutocomplete = multi.GetAutoComplete;
      } else if (DataContext is TrainerTeamViewModel team) {
         getAutocomplete = team.GetTrainerAutocomplete;
      } else if (DataContext is CodeBody body) {
         getAutocomplete = body.GetTokenComplete;
      } else {
         ClearAutocompleteOptions();
         return;
      }

      var text = box.Text ?? string.Empty;
      var line = LineAt(text, box.CaretIndex);
      var lineHeight = box.FontSize * 1.4;
      var verticalStart = lineHeight * (line.Number + 1) + 2;
      double horizontalStart = 0;

      var context = DataContext;
      var options = getAutocomplete(text.Substring(line.Start, line.Length), line.Number, line.Column);
      if (!CanOffer() || !ReferenceEquals(context, DataContext)) {
         ClearAutocompleteOptions();
         return;
      }
      if (options != null && options.Count > 0) {
         var offset = options[0].CharacterOffset;
         var typeFace = new Typeface(box.FontFamily, box.FontStyle, box.FontWeight);
         horizontalStart = new FormattedText(new string('_', offset), CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, typeFace, box.FontSize, null).Width;
         AutocompleteItems.ItemsSource = AutoCompleteSelectionItem.Generate(options, 0).ToList();
         ShowAutocompleteOptions();
      } else {
         ClearAutocompleteOptions();
      }

      // Flip the popup above the caret when there is no room below it.
      var top = TopLevel.GetTopLevel(this);
      var screenVertical = box.TranslatePoint(new Point(0, verticalStart), top ?? (Visual)this)?.Y ?? verticalStart;
      if (top != null && top.Bounds.Height - screenVertical < 200) verticalStart -= ScrollBorder.Bounds.Height + 12;
      autocompleteTransform.X = horizontalStart;
      autocompleteTransform.Y = verticalStart;

      Popup.Reposition();

      ignoreNextSelectionChange = true;
   }

   private void TargetKeyDown(object sender, KeyEventArgs e) {
      if (!CanOffer()) { ClearAutocompleteOptions(); return; }
      if (AutocompleteItems.ItemsSource is not IReadOnlyList<AutoCompleteSelectionItem> items) return;
      if (items.Count == 0) return;
      if (!items.Any(item => item.CanComplete)) { ClearAutocompleteOptions(); return; }
      var selected = items.FirstOrDefault(item => item.IsSelected);
      if (selected == null) return;
      var index = items.IndexOf(selected);
      var box = TargetTextBox;
      if (box == null) return;

      if (e.Key == Key.Enter) {
         AutocompleteOptionChosen(items[index]);
         e.Handled = true;
      } else if (e.Key == Key.Escape) {
         ClearAutocompleteOptions();
      }

      if (e.Key == Key.Space || e.Key == Key.OemQuotes) {
         var text = box.Text ?? string.Empty;
         var line = LineAt(text, box.CaretIndex);
         var quoteCount = text.Substring(line.Start, line.Column).Count(c => c == '"');

         if (e.Key == Key.Space && quoteCount % 2 == 0) {
            // Script space acceptance still receives the physical text-input space next.
            e.Handled = AutocompleteOptionChosen(items[index], synchronizeScriptCaret: false) && DataContext is not CodeBody;
         } else if (e.Key == Key.OemQuotes && quoteCount % 2 == 1) {
            AutocompleteOptionChosen(items[index]);
            e.Handled = true;
         }
      }

      if (e.Key == Key.Up) {
         do { index = (index + items.Count - 1) % items.Count; } while (!items[index].CanComplete);
      } else if (e.Key == Key.Down) {
         do { index = (index + 1) % items.Count; } while (!items[index].CanComplete);
      } else {
         return;
      }

      var models = items.Select(item => new AutocompleteItem(item.DisplayText, item.CompletionText));
      AutocompleteItems.ItemsSource = AutoCompleteSelectionItem.Generate(models, index).ToList();
      e.Handled = true;
   }

   private bool ignoreNextSelectionChange = false;

   /// WPF had TextBox.SelectionChanged; Avalonia surfaces selection as properties.
   private void TargetPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e) {
      if (e.Property == DataContextProperty ||
          ((e.Property == IsVisibleProperty || e.Property == IsEnabledProperty) && e.NewValue is false)) {
         ClearAutocompleteOptions();
         return;
      }
      if (e.Property != TextBox.SelectionStartProperty && e.Property != TextBox.SelectionEndProperty) return;
      if (!ignoreNextSelectionChange) {
         ClearAutocompleteOptions();
      }
      ignoreNextSelectionChange = false;
   }

   private void TargetLostFocus(object sender, EventArgs e) => ClearAutocompleteOptions();

   private void AutocompleteOptionChosen(object sender, RoutedEventArgs e) {
      if (sender is not Control element) return;
      if (element.DataContext is not AutoCompleteSelectionItem item) return;
      AutocompleteOptionChosen(item);
   }

   private bool AutocompleteOptionChosen(AutoCompleteSelectionItem item, bool synchronizeScriptCaret = true) {
      var box = TargetTextBox;
      if (!item.CanComplete || !CanOffer()) { ClearAutocompleteOptions(); return false; }
      acceptingCompletion = true;
      try {
         var text = box.Text ?? string.Empty;
         var line = LineAt(text, box.CaretIndex);
         var completion = item.CompletionText;
         // Script Space acceptance still receives the physical text-input space.
         // Omit only a generated separator; preserve whitespace already in the source.
         if (!synchronizeScriptCaret && DataContext is CodeBody && line.Column == line.Length && line.Length > 0 &&
             !char.IsWhiteSpace(text[line.Start + line.Length - 1]) && completion.EndsWith(" ")) completion = completion[..^1];
         var needChanges = text.Substring(line.Start, line.Length) != completion.TrimEnd();
         if (needChanges) {
            var replacement = text[..line.Start] + completion + text[(line.Start + line.Length)..];
            var caret = line.Start + Math.Clamp(line.Column + completion.Length - line.Length, 0, completion.Length);
            if (synchronizeScriptCaret && DataContext is CodeBody body && Target is TextEditor editor && ReferenceEquals(editor.ViewModel, body.Editor)) {
               // ScriptChanged must see the completed operand position, just as it does for typing.
               body.ApplyCompletion(replacement, caret);
            } else {
               box.Text = replacement;
               box.CaretIndex = caret;
            }
         }

         acceptedTarget = box;
         acceptedContext = DataContext;
         acceptedText = box.Text;
         ClearAutocompleteOptions();
         return needChanges;
      } finally {
         acceptingCompletion = false;
      }
   }
}
