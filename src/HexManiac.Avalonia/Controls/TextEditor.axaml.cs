using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.ViewModels;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>Ported from HexManiac.WPF/Controls/TextEditor.xaml.cs.</summary>
public partial class TextEditor : UserControl {
   #region IsReadOnly

   public static readonly StyledProperty<bool> IsReadOnlyProperty =
      AvaloniaProperty.Register<TextEditor, bool>(nameof(IsReadOnly), false);

   public bool IsReadOnly {
      get => GetValue(IsReadOnlyProperty);
      set => SetValue(IsReadOnlyProperty, value);
   }

   protected virtual void OnIsReadOnlyChanged() {
      TransparentLayer.IsReadOnly = IsReadOnly;
   }

   #endregion

   #region ContextMenuOverride

   public ContextMenu ContextMenuOverride {
      get => GetValue(ContextMenuOverrideProperty);
      set => SetValue(ContextMenuOverrideProperty, value);
   }

   public static readonly StyledProperty<ContextMenu> ContextMenuOverrideProperty =
      AvaloniaProperty.Register<TextEditor, ContextMenu>(nameof(ContextMenuOverride));

   #endregion

   #region TextBox-Like properties

   public event EventHandler<RoutedEventArgs> SelectionChanged;

   public double VerticalOffset => scrollViewer?.Offset.Y ?? 0;

   #endregion

   public TextEditorViewModel ViewModel => DataContext as TextEditorViewModel;

   /// The real TextBox under the colour layers; AutocompleteOverlay attaches to this.
   public TextBox EditBox => TransparentLayer;

   private IEnumerable<TextBlock> Layers => new[] { BasicLayer, AccentLayer, ConstantsLayer, NumericLayer, CommentLayer, TextLayer };

   private readonly Dictionary<TextBlock, TranslateTransform> layerTransforms = new();
   private ScrollViewer scrollViewer;
   private TextEditorViewModel subscribed;
   private bool errorDecorationUpdateQueued;
   private bool widthUpdateQueued;
   private bool widthUpdatesAllowed = true;
   private bool modelSubscriptionsAllowed;
   private int widthUpdateGeneration;
   private int scrollGeometryVersion;
   private int widthRequestScrollVersion;

   static TextEditor() {
      IsReadOnlyProperty.Changed.AddClassHandler<TextEditor>((self, e) => self.OnIsReadOnlyChanged());
   }

   public TextEditor() {
      InitializeComponent();
      DiagnosticLayer.Target = TransparentLayer;
      TransparentLayer.LayoutUpdated += (_, _) => DiagnosticLayer.InvalidateVisual();
      foreach (var layer in Layers) {
         var transform = new TranslateTransform();
         layer.RenderTransform = transform;
         layerTransforms[layer] = transform;
      }

      TransparentLayer.PropertyChanged += (sender, e) => {
         if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty) {
            SelectionChanged?.Invoke(this, new RoutedEventArgs());
         }
      };
      DataContextChanged += (sender, e) => HandleDataContextChanged();

      // ExtentWidth is not observable, so check for the horizontal scroll bar when the text changes
      TransparentLayer.TextChanged += (sender, e) => RequestWidthUpdate();

      // WPF used the ScrollViewer.ScrollChanged attached event; Avalonia exposes the TextBox's
      // own ScrollViewer only once the template is applied.
      TransparentLayer.TemplateApplied += (sender, e) => {
         if (scrollViewer != null) scrollViewer.ScrollChanged -= TextScrollChanged;
         scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
         if (scrollViewer != null) scrollViewer.ScrollChanged += TextScrollChanged;
         CancelWidthUpdate();
         RequestWidthUpdate();
      };
   }


   protected virtual void RequestWidthUpdate() {
      if (!widthUpdatesAllowed) return;
      widthRequestScrollVersion = scrollGeometryVersion;
      if (widthUpdateQueued) return;
      widthUpdateQueued = true;
      var generation = widthUpdateGeneration;
      Dispatcher.UIThread.Post(() => {
         if (generation != widthUpdateGeneration) return;
         widthUpdateQueued = false;
         // Read the live editor/viewport here, never a source or template captured by an old event.
         if (widthUpdatesAllowed) {
            UpdateDocumentWidth();
            UpdateSyntaxClip();
            // A ScrollChanged after the last request would have won over the old immediate
            // measurement. Keep that ordering when this calculation is deferred.
            if (scrollViewer != null && widthRequestScrollVersion != scrollGeometryVersion) {
               foreach (var layer in Layers) layer.Width = scrollViewer.Extent.Width;
            }
         }
      }, DispatcherPriority.Render);
   }

   private void CancelWidthUpdate() {
      widthUpdateGeneration++;
      widthUpdateQueued = false;
   }

   protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
      base.OnAttachedToVisualTree(e);
      widthUpdatesAllowed = true;
      modelSubscriptionsAllowed = true;
      HandleDataContextChanged();
      if (subscribed != null) HandleViewModelCaretMove(subscribed, EventArgs.Empty);
   }

   protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
      widthUpdatesAllowed = false;
      modelSubscriptionsAllowed = false;
      UnbindModel();
      CancelWidthUpdate();
      base.OnDetachedFromVisualTree(e);
   }

   protected virtual void UpdateDocumentWidth() {
      var typeface = new Typeface(TransparentLayer.FontFamily, FontStyle.Normal, FontWeight.Normal);
      var width = new FormattedText(TransparentLayer.Text ?? string.Empty, CultureInfo.CurrentCulture,
         FlowDirection.LeftToRight, typeface, TransparentLayer.FontSize, Brushes.Transparent).Width;
      foreach (var layer in Layers) layer.Width = width;
      if (scrollViewer != null && width > scrollViewer.Viewport.Width && scrollViewer.Viewport.Height > scrollViewer.Extent.Height) {
         CornerCover.Width = 16;
         CornerCover.Height = 17;
      } else {
         CornerCover.Width = 0;
         CornerCover.Height = 0;
      }
   }

   private void UnbindModel() {
      if (subscribed != null) {
         subscribed.PropertyChanged -= HandleViewModelPropertyChanged;
         subscribed.RequestCaretMove -= HandleViewModelCaretMove;
         subscribed.RequestKeyboardFocus -= HandleViewModelRequestKeyboardFocus;
         subscribed.ErrorLocations.CollectionChanged -= HandleViewModelErrorUpdate;
      }
      subscribed = null;
   }

   private void HandleDataContextChanged() {
      CancelWidthUpdate();
      UnbindModel();
      subscribed = modelSubscriptionsAllowed ? ViewModel : null;
      if (subscribed != null) {
         subscribed.PropertyChanged += HandleViewModelPropertyChanged;
         subscribed.RequestCaretMove += HandleViewModelCaretMove;
         subscribed.RequestKeyboardFocus += HandleViewModelRequestKeyboardFocus;
         subscribed.ErrorLocations.CollectionChanged += HandleViewModelErrorUpdate;
         QueueErrorDecorationUpdate();
      }
      RequestWidthUpdate();
   }

   private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
      if (e.PropertyName == nameof(TextEditorViewModel.CommentContent)) {
         QueueErrorDecorationUpdate();
      }
   }

   private void HandleViewModelCaretMove(object sender, EventArgs e) {
      var vm = (TextEditorViewModel)sender;
      var caret = vm.CaretIndex;
      // Assistance can change source reentrantly while a two-way TextBox edit is being
      // published. Synchronize that source before positioning the requested caret.
      if (TransparentLayer.Text != vm.Content) TransparentLayer.SetCurrentValue(TextBox.TextProperty, vm.Content);
      if (TransparentLayer.CaretIndex != caret) TransparentLayer.CaretIndex = caret;
   }

   /// The multi-script host disables whole-control focus reveal so long boxes stay put.
   /// Native caret bring-into-view requests remain enabled for keyboard navigation.
   private void HandleViewModelRequestKeyboardFocus(object sender, EventArgs e) => TransparentLayer.Focus();

   private void HandleViewModelErrorUpdate(object sender, EventArgs e) => QueueErrorDecorationUpdate();

   // Validation publishes diagnostics individually. Paint their final state once per UI turn,
   // without delaying validation or changing the script, diagnostic records, or ROM model.
   private void QueueErrorDecorationUpdate() {
      if (errorDecorationUpdateQueued) return;
      errorDecorationUpdateQueued = true;
      Dispatcher.UIThread.Post(() => {
         errorDecorationUpdateQueued = false;
         // Resolve the current DataContext here: the user may have switched scripts meanwhile.
         UpdateErrorDecorations();
      }, DispatcherPriority.Render);
   }

   public void ScrollToVerticalOffset(double offset) {
      if (scrollViewer != null) scrollViewer.Offset = new Vector(scrollViewer.Offset.X, offset);
   }

   protected override void OnPointerPressed(PointerPressedEventArgs e) {
      base.OnPointerPressed(e);
      if (e.ClickCount != 2 || ViewModel == null) return;

      // expand the selection left until the next whitespace
      var start = Math.Min(TransparentLayer.SelectionStart, TransparentLayer.SelectionEnd);
      var length = Math.Abs(TransparentLayer.SelectionEnd - TransparentLayer.SelectionStart);
      while (start > 0 && !char.IsWhiteSpace(ViewModel.Content[start - 1])) { start--; length++; }
      while ((start + length - 1).InRange(0, ViewModel.Content.Length) && !char.IsWhiteSpace(ViewModel.Content[start + length - 1])) length++;
      TransparentLayer.SelectionStart = start;
      TransparentLayer.SelectionEnd = start + length;
   }

   private void TextScrollChanged(object sender, ScrollChangedEventArgs e) {
      scrollGeometryVersion++;
      UpdateSyntaxClip();
      DiagnosticLayer.InvalidateVisual();
      foreach (var layer in Layers) {
         var transform = layerTransforms[layer];
         transform.Y = -scrollViewer.Offset.Y;
         transform.X = -scrollViewer.Offset.X;
         layer.Width = scrollViewer.Extent.Width;
      }
   }

   // The glyph layers sit behind a transparent TextBox. Match its actual viewport so
   // painted syntax cannot extend under either reserved scrollbar gutter.
   private void UpdateSyntaxClip() {
      var viewport = scrollViewer?.GetVisualDescendants().OfType<ScrollContentPresenter>().FirstOrDefault();
      if (viewport?.TranslatePoint(default, SyntaxLayers) is not Point origin) return;
      var bounds = new Rect(origin, viewport.Bounds.Size);
      if (SyntaxLayers.Clip is not RectangleGeometry clip || clip.Rect != bounds)
         SyntaxLayers.Clip = new RectangleGeometry(bounds);
   }

   private void UpdateErrorDecorations() {
      var inlines = CommentLayer.Inlines;
      inlines.Clear();
      inlines.Add(new Run(SyntaxLayerTextConverter.Normalize(ViewModel?.CommentContent)));
      DiagnosticLayer.SetDiagnostics(ViewModel?.ErrorLocations.ToArray() ?? Array.Empty<TextSegment>());
   }
}
