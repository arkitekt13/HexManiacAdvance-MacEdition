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
      var oldSource = e.OldValue;
      if (oldSource is TextEditor oldEditor) oldSource = oldEditor.TransparentLayer;
      if (oldSource is TextBox oldTarget) {
         oldTarget.TextChanged -= TargetTextChanged;
         oldTarget.RemoveHandler(KeyDownEvent, TargetKeyDown);
         oldTarget.PropertyChanged -= TargetPropertyChanged;
         oldTarget.LostFocus -= TargetLostFocus;
      }

      var newSource = e.NewValue;
      if (newSource is TextEditor newEditor) newSource = newEditor.TransparentLayer;
      if (newSource is TextBox newTarget) {
         newTarget.TextChanged += TargetTextChanged;
         newTarget.AddHandler(KeyDownEvent, TargetKeyDown, RoutingStrategies.Tunnel);
         newTarget.PropertyChanged += TargetPropertyChanged;
         newTarget.LostFocus += TargetLostFocus;
      }
   }

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
   }

   public void ClearAutocompleteOptions() {
      AutocompleteItems.ItemsSource = null;
      IsVisible = false;
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
      if (box == null) return;
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
            return;
         }
      } else if (DataContext is MultiFieldArrayElementViewModel multi) {
         getAutocomplete = multi.GetAutoComplete;
      } else if (DataContext is TrainerTeamViewModel team) {
         getAutocomplete = team.GetTrainerAutocomplete;
      } else if (DataContext is CodeBody body) {
         getAutocomplete = body.GetTokenComplete;
      } else {
         return;
      }

      var text = box.Text ?? string.Empty;
      var line = LineAt(text, box.CaretIndex);
      var lineHeight = box.FontSize * 1.4;
      var verticalStart = lineHeight * (line.Number + 1) + 2;
      double horizontalStart = 0;

      var options = getAutocomplete(text.Substring(line.Start, line.Length), line.Number, line.Column);
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
      if (AutocompleteItems.ItemsSource is not IReadOnlyList<AutoCompleteSelectionItem> items) return;
      if (items.Count == 0) return;
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
            e.Handled = AutocompleteOptionChosen(items[index]) && DataContext is not CodeBody;
         } else if (e.Key == Key.OemQuotes && quoteCount % 2 == 1) {
            AutocompleteOptionChosen(items[index]);
            e.Handled = true;
         }
      }

      if (e.Key == Key.Up) {
         index -= 1;
         if (index == -1) index = items.Count - 1;
      } else if (e.Key == Key.Down) {
         index += 1;
         if (index == items.Count) index = 0;
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

   private bool AutocompleteOptionChosen(AutoCompleteSelectionItem item) {
      var box = TargetTextBox;
      if (box == null) return false;
      var text = box.Text ?? string.Empty;
      var line = LineAt(text, box.CaretIndex);
      var needChanges = text.Substring(line.Start, line.Length) != item.CompletionText.TrimEnd();
      if (needChanges) {
         box.Text = text[..line.Start] + item.CompletionText + text[(line.Start + line.Length)..];
         box.CaretIndex = line.Start + Math.Clamp(line.Column + item.CompletionText.Length - line.Length, 0, item.CompletionText.Length);
      }

      ClearAutocompleteOptions();
      return needChanges;
   }
}
