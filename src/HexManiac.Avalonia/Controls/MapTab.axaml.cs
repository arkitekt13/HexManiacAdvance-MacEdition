using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using ViewPort = HavenSoft.HexManiac.Core.ViewModels.ViewPort;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using CoreBlockEditor = HavenSoft.HexManiac.Core.ViewModels.Map.BlockEditor;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>
/// Ported from HexManiac.WPF/Controls/MapTab.xaml.cs.
///
/// The map surface is driven by raw pointer interaction rather than commands, so this is the
/// densest input translation in the port:
/// * WPF's separate MouseLeftButtonDown / MiddleButton / RightButton checks become one
///   PointerPressed that reads PointerPointProperties.
/// * CaptureMouse()/IsMouseCaptured become e.Pointer.Capture plus a tracked flag, since Avalonia
///   has no per-element IsMouseCaptured.
/// * IsMouseDirectlyOver becomes an e.Source identity check.
/// * ToolTipService.SetIsEnabled becomes the ToolTip attached properties.
/// </summary>
public partial class MapTab : UserControl {
   public static string BlockBagShortcutText { get; } = OperatingSystem.IsMacOS()
      ? "Command-click (⌘-click) blocks to add/remove them from the mixing bag.\nCommand-click the bag icon to empty it."
      : "Ctrl-click blocks to add/remove them from the mixing bag.\nCtrl-click the bag icon to empty it.";

   private MapEditorViewModel ViewModel => DataContext as MapEditorViewModel;
   private MapEditorViewModel subscribed;
   private readonly MapHoverPreview hoverPreview;
   private readonly BlocksetHoverPreview blocksetPreview;
   private WindowBase keyboardWindow;
   private int blocksetRefreshGeneration;

   public event EventHandler FocusElement;

   public MapTab() {
      InitializeComponent();
      hoverPreview = new MapHoverPreview(MapView, HoverPreviewLayer);
      blocksetPreview = new BlocksetHoverPreview(BlocksetPreviewLayer, BlocksetPreviewBox, BlocksetPreviewImage);
      HeaderPanel.PropertyChanged += (_, e) => {
         if (e.Property == IsVisibleProperty && !HeaderPanel.IsVisible) blocksetPreview.Hide();
      };
      DataContextChanged += (sender, e) => UpdateDataContext();
      AddHandler(KeyDownEvent, HandleKeyDown, RoutingStrategies.Bubble);
      AddHandler(KeyUpEvent, HandleKeyUp, RoutingStrategies.Bubble);
      AttachedToVisualTree += (sender, e) => {
         keyboardWindow = TopLevel.GetTopLevel(this) as WindowBase;
         if (keyboardWindow != null) keyboardWindow.Deactivated += HandleWindowDeactivated;
         Focus();
      };
      DetachedFromVisualTree += (sender, e) => {
         blocksetRefreshGeneration++;
         ResetTransientKeys(subscribed);
         if (keyboardWindow != null) keyboardWindow.Deactivated -= HandleWindowDeactivated;
         keyboardWindow = null;
         InterruptPointerInteraction();
         HideMapTooltip();
         blocksetPreview.Hide();
      };
      // Inspect the actual target before bubbling map handlers can reuse an NPC preview.
      AddHandler(PointerMovedEvent, DismissTooltipOutsideMap, RoutingStrategies.Tunnel, handledEventsToo: true);
      SizeChanged += (sender, e) => {
         var partial = Bounds.Width / 2 - (int)(Bounds.Width / 2);
         if (MapView.RenderTransform is TranslateTransform transform) transform.X = -partial;
      };

      BuildKeyBindings();
      WireMapSurface();
      SetUpReveal();
      SetUpSelectionPop();
      WatchBlockEditor();
   }

   private void SignpostScriptClick(object sender, RoutedEventArgs e) {
      if (sender is Control { DataContext: SignpostEventViewModel signpost } && signpost.ShowPointer && signpost.CanGotoScript)
         QueueScriptSourceFocus(signpost.Pointer, selectDialogue: true);
   }

   private void EventScriptClick(object sender, RoutedEventArgs e) {
      if (sender is Control { DataContext: ObjectEventViewModel obj }) QueueScriptSourceFocus(obj.ScriptAddress);
      else if (sender is Control { DataContext: ScriptEventViewModel script }) QueueScriptSourceFocus(script.ScriptAddress);
   }

   private void HeaderScriptClick(object sender, RoutedEventArgs e) {
      if (sender is Control { DataContext: MapScriptViewModel script }) QueueScriptSourceFocus(script.ScriptAddress);
      else if (sender is Control { DataContext: MapSubScriptViewModel child }) QueueScriptSourceFocus(child.ScriptAddress);
   }

   private void QueueScriptSourceFocus(int address, bool selectDialogue = false) {
      var model = ViewModel?.ViewPort.Model;
      if (model == null || TopLevel.GetTopLevel(this) is not MainWindow window) return;
      // Keep bound commands and their CanExecute behavior. Click captures the host before
      // navigation detaches the map; focus only the source actually opened by that command.
      Dispatcher.UIThread.Post(() => {
         var editor = window.Editor;
         if (editor?.SelectedTab is not ViewPort selected || !ReferenceEquals(selected.Model, model) ||
             editor.GotoViewModel.ControlVisible || editor.FindControlVisible || editor.HexConverterVisible || editor.ShowError ||
             selected.Tools.SelectedTool is not CodeTool { Mode: CodeMode.Script } || !selected.Tools.CodeTool.Contents.Any(body => body.Address == address)) return;
         var tab = window.GetVisualDescendants().OfType<TabView>()
            .FirstOrDefault(tab => ReferenceEquals(tab.DataContext, selected) && tab.IsEffectivelyVisible);
         var field = tab?.GetVisualDescendants().OfType<TextEditor>()
            .FirstOrDefault(field => field.Tag is CodeBody body && body.Address == address && field.IsEffectivelyVisible);
         var input = field?.EditBox;
         if (input?.Focus() != true || !selectDialogue) return;
         SelectSignpostDialogue(input, (CodeBody)field.Tag);
      }, DispatcherPriority.Background);
   }

   private static void SelectSignpostDialogue(TextBox input, CodeBody body) {
      // Decompilation records stream types, including loadpointer's generic pointer.
      // Select only the first text stream; movement/item braces and commands stay intact.
      var text = input.Text ?? string.Empty;
      var offset = 0;
      var stream = -1;
      var start = -1;
      foreach (var line in text.Split('\n')) {
         var trimmed = line.Trim();
         if (trimmed == "{") {
            stream++;
            if (body.StreamTypes != null && stream < body.StreamTypes.Count &&
                body.StreamTypes[stream] == HavenSoft.HexManiac.Core.Models.Code.ExpectedPointerType.Text)
               start = Math.Min(text.Length, offset + line.Length + 1);
         } else if (trimmed == "}" && start >= 0) {
            var end = offset;
            if (end > start && text[end - 1] == '\n') end--;
            if (end > start && text[end - 1] == '\r') end--;
            input.SelectionStart = start;
            input.SelectionEnd = end;
            return;
         }
         offset += line.Length + 1;
      }
      input.SelectionStart = input.SelectionEnd = input.CaretIndex;
   }

   private void BuildKeyBindings() {
      void Add(Key key, Func<MapEditorViewModel, ICommand> command, object parameter = null) {
         // Ancestor KeyBindings can run before a focused field handles its editing keys.
         // Declining the map command lets the TextBox (and its native shortcuts) handle them.
         KeyBindings.Add(new KeyBinding {
            Gesture = new KeyGesture(key), CommandParameter = parameter,
            Command = new StubCommand {
               CanExecute = arg => !IsTextEntryFocused() && ViewModel != null && command(ViewModel).CanExecute(arg),
               Execute = arg => {
                  if (!IsTextEntryFocused() && ViewModel != null && command(ViewModel).CanExecute(arg))
                     command(ViewModel).Execute(arg);
               },
            },
         });
      }
      Add(Key.Left, vm => vm.PanCommand, MapDirection.Left);
      Add(Key.Right, vm => vm.PanCommand, MapDirection.Right);
      Add(Key.Up, vm => vm.PanCommand, MapDirection.Up);
      Add(Key.Down, vm => vm.PanCommand, MapDirection.Down);
      Add(Key.Escape, vm => vm.CancelCommand);
      Add(Key.Delete, vm => vm.DeleteCommand);
      Add(Key.Back, vm => vm.DeleteCommand);
   }

   private void UpdateDataContext() {
      blocksetRefreshGeneration++;
      blocksetPreview?.Hide();
      ResetTransientKeys(subscribed);
      InterruptPointerInteraction();
      if (subscribed != null) {
         subscribed.PropertyChanged -= HandleContextPropertyChanged;
         subscribed.InteractionInterruptRequested -= HandleInteractionInterrupt;
         subscribed.AutoscrollBlocks -= AutoscrollBlocks;
         subscribed.AutoscrollTiles -= AutoscrollTiles;
      }
      subscribed = ViewModel;
      if (subscribed != null) {
         subscribed.PropertyChanged += HandleContextPropertyChanged;
         subscribed.InteractionInterruptRequested += HandleInteractionInterrupt;
         subscribed.AutoscrollBlocks += AutoscrollBlocks;
         subscribed.AutoscrollTiles += AutoscrollTiles;
      }
      UpdateReveal(ViewModel?.ShowBeneath ?? false);
      UpdateStampPreviewVisibility();
   }

   private void HandleInteractionInterrupt(object sender, EventArgs e) {
      if (ReferenceEquals(sender, ViewModel)) ResetTransientKeys(ViewModel);
      if (ReferenceEquals(sender, interactionOwner)) InterruptPointerInteraction();
   }

   private void HandleContextPropertyChanged(object sender, PropertyChangedEventArgs e) {
      if (!ReferenceEquals(sender, ViewModel)) return;
      if (e.PropertyName == nameof(MapEditorViewModel.HoverContentVersion)) hoverPreview.Invalidate();
      if (e.PropertyName == nameof(MapEditorViewModel.ShowBeneath)) UpdateReveal(ViewModel.ShowBeneath);
      if (e.PropertyName == nameof(MapEditorViewModel.BlockSelectionToggle)) StartSelectionPop();
      // The tile highlight's toggle lives on the block editor, one level down, and that object is
      // replaced whenever the primary map changes - so the subscription has to follow it.
      if (e.PropertyName == nameof(MapEditorViewModel.PrimaryMap)) WatchBlockEditor();
      UpdateStampPreviewVisibility();
   }

   private void UpdateStampPreviewVisibility() {
      var vm = ViewModel;
      StampPreviewLayer.IsVisible = vm != null && withinMapInteraction == MouseButton.XButton1 &&
         vm.ShowHighlightCursor && !vm.ShowBeneath && vm.DrawBlockIndex >= 0 &&
         vm.MultiTileDrawRender != null && (vm.DrawMultipleTiles || vm.IsSingleBlock) &&
         !(vm.Use9Grid && vm.IsValid9GridSelection);
   }

   private CoreBlockEditor watchedBlockEditor;

   private void WatchBlockEditor() {
      var blockEditor = ViewModel?.PrimaryMap?.BlockEditor;
      if (ReferenceEquals(blockEditor, watchedBlockEditor)) return;
      if (watchedBlockEditor != null) watchedBlockEditor.PropertyChanged -= HandleBlockEditorPropertyChanged;
      watchedBlockEditor = blockEditor;
      if (watchedBlockEditor != null) watchedBlockEditor.PropertyChanged += HandleBlockEditorPropertyChanged;
   }

   private void HandleBlockEditorPropertyChanged(object sender, PropertyChangedEventArgs e) {
      if (e.PropertyName == nameof(CoreBlockEditor.TileSelectionToggle)) StartTilePop();
   }

   private void AutoscrollBlocks(object sender, EventArgs e) {
      if (ViewModel == null) return;
      var scrollRange = BlockViewer.Extent.Height - BlockViewer.Viewport.Height;
      var blockHeight = (ViewModel.Blocks.PixelHeight / 16.0) * 8 - 16;
      var scrollPercent = ((ViewModel.DrawBlockIndex - 8) / blockHeight).LimitToRange(0, 1);
      BlockViewer.Offset = new Vector(BlockViewer.Offset.X, scrollRange * scrollPercent);
   }

   private void AutoscrollTiles(object sender, EventArgs e) {
      if (ViewModel?.PrimaryMap == null) return;
      var scrollRange = TileViewer.Extent.Height - TileViewer.Viewport.Height;
      var tileHeight = ViewModel.PrimaryMap.BlockEditor.TileRender.PixelHeight * 3 - 24;
      var scrollPercent = (ViewModel.PrimaryMap.BlockEditor.TileSelectionY / (double)tileHeight).LimitToRange(0, 1);
      TileViewer.Offset = new Vector(TileViewer.Offset.X, scrollRange * scrollPercent);
   }

   private void UpdateTooltipContent(object content) {
      if (ViewModel == null) return;
      var vm = ViewModel;
      var center = MapView.TranslatePoint(new Avalonia.Point(
         MapView.Bounds.Width / 2 + vm.HighlightCursorX,
         MapView.Bounds.Height / 2 + vm.HighlightCursorY), HoverPreviewLayer);
      if (center is not Avalonia.Point point) return;
      hoverPreview.Update(content, new Rect(point.X - vm.HighlightCursorWidth / 2,
         point.Y - vm.HighlightCursorHeight / 2, vm.HighlightCursorWidth, vm.HighlightCursorHeight));
   }

   private void HideMapTooltip() => hoverPreview.Hide();

   private void DismissTooltipOutsideMap(object sender, PointerEventArgs e) {
      if (!hoverPreview.OwnsSource(e.Source)) HideMapTooltip();
   }

   private void OnEnterTutorial(object sender, MapTutorialViewModel tutorial) {
      HideMapTooltip();
      var index = (Tutorial)(tutorial.Index - 1);
      if (index == Tutorial.LeftClickBlock_SelectBlock) FocusElement.Raise(BlockViewer);
      if (index == Tutorial.BlockButton_EditBlocks) FocusElement.Raise(EditBlockButton);
      if (index == Tutorial.ToolbarButton_GotoWildData) FocusElement.Raise(WildButton);
      if (index == Tutorial.ToolbarButton_EditMapHeader) FocusElement.Raise(MapHeaderButton);
      if (index == Tutorial.BackButton_GoBack) FocusElement.Raise(BackButtonWidget);
      if (index == Tutorial.ToolbarUndo_Undo) FocusElement.Raise(UndoButtonWidget);
      if (index == Tutorial.ToolbarButton_EditBorderBlock) FocusElement.Raise(EditBorderButton);
   }

   #region Map Interaction

   // track which button is being used. Set to XButton1 when not in use.
   private MouseButton withinMapInteraction = MouseButton.XButton1;
   private bool controlSelectGesture;
   private bool optionPanGesture;
   private bool backgroundCaptured, blocksCaptured, borderCaptured;

   private IPointer interactionPointer;
   private Control interactionSurface;
   private MapEditorViewModel interactionOwner;
   private Action<Point> interruptedInteraction;
   private Point interactionPosition;
   private MouseButton interactionButton;

   private void TrackPointerInteraction(PointerPressedEventArgs e, Control surface, MapEditorViewModel owner, Point position, Action<Point> finish) {
      InterruptPointerInteraction();
      var pointer = e.Pointer;
      interactionButton = PressedButton(e, surface);
      interactionPointer = pointer;
      interactionSurface = surface;
      interactionOwner = owner;
      interactionPosition = position;
      interruptedInteraction = finish;
      surface.PointerCaptureLost += InteractionCaptureLost;
      pointer.Capture(surface);
   }

   private void InteractionCaptureLost(object sender, PointerCaptureLostEventArgs e) {
      if (ReferenceEquals(sender, interactionSurface) && ReferenceEquals(e.Pointer, interactionPointer) && !ReferenceEquals(e.Pointer.Captured, interactionSurface))
         InterruptPointerInteraction();
   }

   private void InterruptPointerInteraction() => ReleasePointerInteraction(true);

   private void ReleasePointerInteraction(bool interrupted = false) {
      if (interruptedInteraction == null) return;
      var finish = interruptedInteraction;
      var pointer = interactionPointer;
      var surface = interactionSurface;
      var owner = interactionOwner;
      var position = interactionPosition;
      // Disarm before releasing capture: normal releases also raise CaptureLost synchronously.
      interruptedInteraction = null;
      interactionPointer = null;
      interactionSurface = null;
      interactionOwner = null;
      surface.PointerCaptureLost -= InteractionCaptureLost;
      withinMapInteraction = MouseButton.XButton1;
      controlSelectGesture = optionPanGesture = false;
      backgroundCaptured = blocksCaptured = borderCaptured = false;
      withinShiftInteraction = false;
      if (ReferenceEquals(pointer.Captured, surface)) pointer.Capture(null);
      if (interrupted) {
         finish(position);
         owner.ShowHighlightCursor = false;
         HideMapTooltip();
      }
      UpdateStampPreviewVisibility();
   }

   private static readonly object NoTooltip = new object[0];

   private void WireMapSurface() {
      MapView.PointerPressed += ButtonDown;
      MapView.PointerMoved += ButtonMove;
      MapView.PointerReleased += ButtonUp;
      MapView.PointerExited += ButtonLeave;
      MapView.PointerWheelChanged += Wheel;
      TutorialsControl.PointerEntered += (sender, e) => {
         HideMapTooltip();
         if (ViewModel != null) ViewModel.ShowHighlightCursor = false;
      };

      MapBackground.PointerPressed += BackgroundDown;
      MapBackground.PointerMoved += BackgroundMove;
      MapBackground.PointerReleased += BackgroundUp;
      MapBackground.PointerWheelChanged += Wheel;

      // WPF put these on the PixelImage itself. They must stay there: the handlers turn the
      // pointer position into a block index by dividing by 16, and a position measured against
      // the ScrollViewer is short by the scroll offset, so the wrong block gets picked.
      BlockRenderImage.PointerPressed += BlocksDown;
      BlockRenderImage.PointerMoved += BlocksMove;
      BlockRenderImage.PointerReleased += BlocksUp;
      BlockViewer.PointerWheelChanged += EatMouseWheel;

      BlockBag.PointerPressed += BlockBagMouseDown;

      // The border image draws with left-drag and picks a block with right-click.
      BorderImage.PointerPressed += BorderPointerPressed;
      BorderImage.PointerMoved += BorderMove;
      BorderImage.PointerReleased += BorderUp;

      TileRenderImage.PointerPressed += TilesDown;
      TileViewer.PointerWheelChanged += EatMouseWheel;
      EventPanel.PointerWheelChanged += EatMouseWheel;
   }

   /// WPF had MouseLeftButtonDown -> BorderDown and MouseRightButtonDown -> BorderSelect.
   private void BorderPointerPressed(object sender, PointerPressedEventArgs e) {
      var properties = e.GetCurrentPoint((Control)sender).Properties;
      if (properties.IsRightButtonPressed) BorderSelect(sender, e);
      else if (properties.IsLeftButtonPressed) BorderDown(sender, e);
   }

   private static MouseButton PressedButton(PointerPressedEventArgs e, Visual over) {
      var p = e.GetCurrentPoint(over).Properties;
      if (p.IsLeftButtonPressed) return MouseButton.Left;
      if (p.IsMiddleButtonPressed) return MouseButton.Middle;
      if (p.IsRightButtonPressed) return MouseButton.Right;
      if (p.IsXButton1Pressed) return MouseButton.XButton1;
      if (p.IsXButton2Pressed) return MouseButton.XButton2;
      return MouseButton.None;
   }

   private static MouseButton ReleasedButton(PointerReleasedEventArgs e, Visual over) =>
      e.GetCurrentPoint(over).Properties.PointerUpdateKind switch {
         PointerUpdateKind.LeftButtonReleased => MouseButton.Left,
         PointerUpdateKind.MiddleButtonReleased => MouseButton.Middle,
         PointerUpdateKind.RightButtonReleased => MouseButton.Right,
         PointerUpdateKind.XButton1Released => MouseButton.XButton1,
         PointerUpdateKind.XButton2Released => MouseButton.XButton2,
         _ => e.InitialPressMouseButton,
      };

   private void ButtonDown(object sender, PointerPressedEventArgs e) {
      var vm = ViewModel;
      if (vm == null) return;
      var element = (Control)sender;
      var button = PressedButton(e, element);

      if (button == MouseButton.XButton1 && vm.Back.CanExecute(null)) { vm.Back.Execute(); return; }
      if (button == MouseButton.XButton2 && vm.Forward.CanExecute(null)) { vm.Forward.Execute(); return; }
      if (withinMapInteraction != MouseButton.XButton1) return;
      // macOS reserves Control-left-click for secondary-button actions. Retain
      // the physical button for release, even if Control is released first.
      controlSelectGesture = OperatingSystem.IsMacOS() && button == MouseButton.Left &&
         e.KeyModifiers.HasFlag(KeyModifiers.Control);
      // Reuse middle-button panning for trackpads, retaining the physical button
      // so releasing Option before the mouse does not end or strand the gesture.
      optionPanGesture = OperatingSystem.IsMacOS() && button == MouseButton.Left &&
         e.KeyModifiers == KeyModifiers.Alt;
      if (controlSelectGesture) button = MouseButton.Right;
      else if (optionPanGesture) button = MouseButton.Middle;
      UpdateTooltipContent(NoTooltip);
      Focus();
      e.Handled = true;
      var p = GetCoordinates(element, e);
      TrackPointerInteraction(e, element, vm, p, position => {
         if (button == MouseButton.Left) vm.CompleteInteraction();
         else if (button == MouseButton.Middle) vm.DragUp(position.X, position.Y);
         else if (button == MouseButton.Right) vm.SelectUp(position.X, position.Y);
      });

      if (button == MouseButton.Left) {
         withinMapInteraction = MouseButton.Left;
         var interactionStart = PrimaryInteractionStart.Click;
         if (e.ClickCount == 2) interactionStart = PrimaryInteractionStart.DoubleClick;
         if (e.KeyModifiers == KeyModifiers.Shift) interactionStart = PrimaryInteractionStart.ShiftClick;
         if (e.KeyModifiers == HexContent.CommandModifier) interactionStart = PrimaryInteractionStart.ControlClick;
         if (e.ClickCount == 2 && e.KeyModifiers == HexContent.CommandModifier) interactionStart = PrimaryInteractionStart.ControlClick | PrimaryInteractionStart.DoubleClick;
         vm.PrimaryDown(p.X, p.Y, interactionStart);
      } else if (button == MouseButton.Middle) {
         withinMapInteraction = MouseButton.Middle;
         vm.DragDown(p.X, p.Y);
      } else if (button == MouseButton.Right) {
         withinMapInteraction = MouseButton.Right;
         var result = vm.SelectDown(p.X, p.Y);
         if (result == SelectionInteractionResult.ShowMenu) {
            withinMapInteraction = MouseButton.XButton1;
            controlSelectGesture = false;
            ReleasePointerInteraction();
            ShowMenu(element);
         }
      }
      UpdateStampPreviewVisibility();
   }

   /// <summary>
   /// This has to be an event rather than a command, otherwise the +/- interactions would happen
   /// even while typing in a textbox in the event panel.
   /// </summary>
   private void HandleKeyDown(object sender, KeyEventArgs e) {
      if (IsTextEntryFocused()) return;
      var vm = ViewModel;
      if (vm == null) return;
      e.Handled = true;
      if (e.Key == Key.OemPlus || e.Key == Key.Add) vm.ZoomCommand.Execute(ZoomDirection.Enlarge);
      else if (e.Key == Key.OemMinus || e.Key == Key.Subtract) vm.ZoomCommand.Execute(ZoomDirection.Shrink);
      else if (e.Key == Key.Space) {
         vm.ShowBeneath = true;
      } else if (e.KeyModifiers == HexContent.CommandModifier) {
         vm.HideEvents |= !waitingForControlUp;
         waitingForControlUp = true;
         e.Handled = false;
      } else e.Handled = false;
   }

   // sentinel to watch for ctrl keyup, since key repeat doesn't report reliably for Ctrl after Ctrl+Z
   private bool waitingForControlUp;

   private void HandleKeyUp(object sender, KeyEventArgs e) {
      if (IsTextEntryFocused()) return;
      var vm = ViewModel;
      if (vm == null) return;
      if (e.Key == Key.Space) {
         vm.ShowBeneath = false;
         e.Handled = true;
      }
      if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || e.Key == Key.LWin || e.Key == Key.RWin) {
         waitingForControlUp = false;
         vm.HideEvents = false;
      }
   }

   /// WPF checked Keyboard.FocusedElement against TextBoxBase / TextBoxLookAlike.
   private bool IsTextEntryFocused() {
      var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
      return focused is TextBox || focused is TextBoxLookAlike || focused is AngleTextBox || focused is ComboBox;
   }

   protected override void OnLostFocus(RoutedEventArgs e) {
      base.OnLostFocus(e);
      ResetTransientKeys(ViewModel);
   }

   private void HandleWindowDeactivated(object sender, EventArgs e) {
      ResetTransientKeys(ViewModel);
      blocksetPreview.Hide();
   }

   private void ResetTransientKeys(MapEditorViewModel owner) {
      waitingForControlUp = false;
      if (owner == null) return;
      owner.ShowBeneath = false;
      owner.HideEvents = false;
   }

   private void ButtonMove(object sender, PointerEventArgs e) {
      var element = (Control)sender;
      var vm = ViewModel;
      if (vm == null) return;
      var p = GetCoordinates(element, e);
      e.Handled = true;
      if (withinMapInteraction == MouseButton.XButton1) {
         if (MapReadyForPointer(vm)) UpdateTooltipContent(vm.Hover(p.X, p.Y));
         return;
      }
      if (!MapReadyForPointer(vm)) return;
      interactionPosition = p;
      if (withinMapInteraction == MouseButton.Left) {
         vm.PrimaryMove(p.X, p.Y);
      } else if (withinMapInteraction == MouseButton.Middle) {
         vm.DragMove(p.X, p.Y, true);
      } else if (withinMapInteraction == MouseButton.Right) {
         vm.SelectMove(p.X, p.Y);
      }
   }

   /// <summary>
   /// Whether the map is far enough along to take a pointer position.
   ///
   /// MapEditorViewModel.Hover eventually calls LimitToRange(0, width - 1), where width comes from
   /// the map's rendered PixelWidth. Before the first render that is 0, the range becomes (0, -1),
   /// and Core throws - which took the whole editor down when a mouse move happened to land in that
   /// window. Core cannot be modified here, so the call is simply not made until the map has a size.
   /// </summary>
   private static bool MapReadyForPointer(MapEditorViewModel vm) =>
      vm?.PrimaryMap != null && vm.PrimaryMap.PixelWidth > 0 && vm.PrimaryMap.PixelHeight > 0;

   private void ButtonUp(object sender, PointerReleasedEventArgs e) {
      e.Handled = true;
      var element = (Control)sender;
      var previousInteraction = withinMapInteraction;
      var releaseButton = controlSelectGesture || optionPanGesture ? MouseButton.Left : previousInteraction;
      if (previousInteraction == MouseButton.XButton1 || ReleasedButton(e, element) != releaseButton) return;
      ReleasePointerInteraction();
      var vm = ViewModel;
      if (vm == null) return;
      var p = GetCoordinates(element, e);
      if (previousInteraction == MouseButton.Left) {
         vm.PrimaryUp(p.X, p.Y);
      } else if (previousInteraction == MouseButton.Middle) {
         vm.DragUp(p.X, p.Y);
      } else if (previousInteraction == MouseButton.Right) {
         vm.SelectUp(p.X, p.Y);
      }
      UpdateStampPreviewVisibility();
   }

   private void ButtonLeave(object sender, PointerEventArgs e) {
      HideMapTooltip();
      if (ViewModel == null) return;
      ViewModel.ShowHighlightCursor = false;
   }

   private void BackgroundDown(object sender, PointerPressedEventArgs e) {
      var vm = ViewModel;
      if (vm == null) return;
      var element = (Control)sender;
      var button = PressedButton(e, element);
      if (button == MouseButton.XButton1 && vm.Back.CanExecute(null)) { vm.Back.Execute(); return; }
      if (button == MouseButton.XButton2 && vm.Forward.CanExecute(null)) { vm.Forward.Execute(); return; }
      if (withinMapInteraction != MouseButton.XButton1) return;
      // right-click is not a valid drag operation, because that's confusing.
      if (button == MouseButton.Right) return;
      UpdateTooltipContent(NoTooltip);
      // WPF used IsMouseDirectlyOver to ignore clicks that landed on a child.
      if (!ReferenceEquals(e.Source, element)) return;
      e.Handled = true;
      vm.ClearSelection();
      var p = GetCoordinates(element, e);
      TrackPointerInteraction(e, element, vm, p, position => vm.DragUp(position.X, position.Y));
      backgroundButton = button;
      backgroundCaptured = true;
      withinMapInteraction = MouseButton.Middle;
      UpdateStampPreviewVisibility();
      vm.DragDown(p.X, p.Y);
   }

   private MouseButton backgroundButton;

   private void BackgroundMove(object sender, PointerEventArgs e) {
      if (!backgroundCaptured) return;
      var element = (Control)sender;
      var vm = ViewModel;
      if (vm == null) return;
      var p = GetCoordinates(element, e);
      e.Handled = true;
      if (withinMapInteraction == MouseButton.XButton1) return;
      interactionPosition = p;
      vm.DragMove(p.X, p.Y, false);
   }

   private void BackgroundUp(object sender, PointerReleasedEventArgs e) {
      if (!backgroundCaptured) return;
      var element = (Control)sender;
      if (ReleasedButton(e, element) != backgroundButton) return;
      ReleasePointerInteraction();
      var vm = ViewModel;
      if (vm == null) return;
      e.Handled = true;
      var p = GetCoordinates(element, e);
      vm.DragUp(p.X, p.Y);
      UpdateStampPreviewVisibility();
   }

   private void BlocksetPreviewEntered(object sender, PointerEventArgs e) {
      if (sender is Control { DataContext: BlocksetOption option } row &&
          (PrimaryBlocksetSelector.IsDropDownOpen || SecondaryBlocksetSelector.IsDropDownOpen)) {
         HideMapTooltip();
         blocksetPreview.Show(row, option.Render);
      }
   }

   private void BlocksetSelectionChanged(object sender, SelectionChangedEventArgs e) {
      blocksetPreview?.Hide();
      if (sender is not Control { DataContext: MapHeaderViewModel header }) return;
      var context = ViewModel;
      var map = context?.PrimaryMap;
      var generation = ++blocksetRefreshGeneration;
      // ROM writes have already completed; only refresh presentation after Avalonia
      // finishes enumerating the selected items from this event.
      Dispatcher.UIThread.Post(() => {
         if (generation == blocksetRefreshGeneration && ReferenceEquals(ViewModel, context) &&
             map != null && ReferenceEquals(context.PrimaryMap, map) && ReferenceEquals(map.Header, header))
            header.CompleteBlocksetSelection();
      }, DispatcherPriority.Background);
   }

   private void BlocksetPreviewClosed(object sender, EventArgs e) => blocksetPreview?.Hide();

   private void ZoomOutClick(object sender, RoutedEventArgs e) => ZoomFromStatusBar(false, e);
   private void ZoomInClick(object sender, RoutedEventArgs e) => ZoomFromStatusBar(true, e);

   private void ZoomFromStatusBar(bool enlarge, RoutedEventArgs e) {
      var vm = ViewModel;
      if (vm?.PrimaryMap == null) return;
      InterruptPointerInteraction();
      HideMapTooltip();
      // Map coordinates are relative to the view center; do not zoom around the button.
      vm.Zoom(0, 0, enlarge);
      e.Handled = true;
   }

   private void Wheel(object sender, PointerWheelEventArgs e) {
      var element = (Control)sender;
      var vm = ViewModel;
      if (vm == null) return;
      var p = GetCoordinates(element, e);
      HideMapTooltip();
      vm.Zoom(p.X, p.Y, e.Delta.Y > 0);
      e.Handled = true;
   }

   /// We have this so that mouse-wheel over certain elements won't get taken by Wheel above.
   private void EatMouseWheel(object sender, PointerWheelEventArgs e) => e.Handled = true;

   private void BlockBagMouseDown(object sender, PointerPressedEventArgs e) {
      if (e.KeyModifiers != HexContent.CommandModifier) return;
      ViewModel?.ClearBlockBag();
   }

   /// <summary>
   /// The position of the pointer in the image's *source pixels*, which is the coordinate space
   /// every one of these handlers was written against.
   ///
   /// WPF applied SpriteScale with a LayoutTransform. A LayoutTransform changes the space the
   /// parent lays the element out in but leaves the element's own coordinate space alone, so
   /// GetPosition there returned unscaled source pixels no matter how the image was zoomed.
   /// Avalonia has no LayoutTransform on arbitrary controls, so PixelImage sizes itself to
   /// PixelWidth * SpriteScale instead - which means GetPosition here comes back already
   /// multiplied by SpriteScale, and it has to be divided back out before WPF's arithmetic
   /// applies. Getting this wrong is what made the block and tile pickers select the wrong cell:
   /// the block list draws at 2x and the tile list at 3x, so the error scaled with the zoom.
   /// </summary>
   private static Point SourcePixelPosition(Control element, PointerEventArgs e) {
      var p = e.GetPosition(element);
      var scale = (element.DataContext as IPixelViewModel)?.SpriteScale ?? 1;
      if (scale <= 0) scale = 1;
      return new Point(p.X / scale, p.Y / scale);
   }

   private void BlocksDown(object sender, PointerPressedEventArgs e) {
      if (blocksCaptured || ViewModel == null) return;
      var element = (Control)sender;
      var p = SourcePixelPosition(element, e);
      var owner = ViewModel;
      TrackPointerInteraction(e, element, owner, p, position => owner.ReleaseBlock((int)(position.X / 16), (int)(position.Y / 16)));
      blocksCaptured = true;
      var x = (int)(p.X / 16);
      var y = (int)(p.Y / 16);
      if (e.KeyModifiers == HexContent.CommandModifier) {
         ViewModel.ToggleBlockInBag(x, y);
      }
      ViewModel.SelectBlock(x, y);
      e.Handled = true;
   }

   private void BlocksMove(object sender, PointerEventArgs e) {
      if (!blocksCaptured || ViewModel == null) return;
      if (e.KeyModifiers == HexContent.CommandModifier) return;
      var element = (Control)sender;
      var p = SourcePixelPosition(element, e);
      interactionPosition = p;
      ViewModel.DragBlock((int)(p.X / 16), (int)(p.Y / 16));
      e.Handled = true;
   }

   private void BlocksUp(object sender, PointerReleasedEventArgs e) {
      if (!blocksCaptured || ViewModel == null || ReleasedButton(e, (Control)sender) != interactionButton) return;
      ReleasePointerInteraction();
      var element = (Control)sender;
      var p = SourcePixelPosition(element, e);
      ViewModel.ReleaseBlock((int)(p.X / 16), (int)(p.Y / 16));
      e.Handled = true;
   }

   private void TilesDown(object sender, PointerPressedEventArgs e) {
      if (ViewModel?.PrimaryMap == null) return;
      var element = (Control)sender;
      var vm = ViewModel.PrimaryMap.BlockEditor;
      // BlockEditor.TileSelectionX/Y are in scaled units (PixelPerTile is 24 = an 8px tile at 3x),
      // which is why WPF multiplied its unscaled position back up by SpriteScale.
      var p = SourcePixelPosition(element, e);
      vm.TileSelectionY = (int)(p.Y * vm.TileRender.SpriteScale);
      vm.TileSelectionX = (int)(p.X * vm.TileRender.SpriteScale);
   }

   private Point GetCoordinates(Control element, PointerEventArgs e) {
      var p = e.GetPosition(element);
      return new Point(p.X - element.Bounds.Width / 2, p.Y - element.Bounds.Height / 2);
   }

   private void ShowMenu(Control element) {
      // Open explicitly rather than attaching a default secondary-click menu to the map:
      // empty cells must keep tile selection, never reopen a previous event's commands.
      if (Resources["MapEventMenu"] is not ContextMenu menu) return;
      menu.DataContext = ViewModel;
      menu.Open(element);
   }

   #endregion

   #region Border Interaction

   private void BorderDown(object sender, PointerPressedEventArgs e) {
      if (ViewModel == null || borderCaptured) return;
      var element = (Control)sender;
      var owner = ViewModel;
      TrackPointerInteraction(e, element, owner, SourcePixelPosition(element, e), _ => owner.CompleteBorderDraw());
      borderCaptured = true;
      BorderMove(sender, e);
   }

   private void BorderSelect(object sender, PointerPressedEventArgs e) {
      var element = (Control)sender;
      if (ViewModel == null || borderCaptured) return;
      var owner = ViewModel;
      TrackPointerInteraction(e, element, owner, SourcePixelPosition(element, e), _ => owner.CompleteBorderDraw());
      borderCaptured = true;
      // BorderEditor.GetBlock divides by 16, so it wants source pixels; the border renders at 3x.
      var p = SourcePixelPosition(element, e);
      ViewModel?.ReadBorderBlock(p.X, p.Y);
   }

   private void BorderMove(object sender, PointerEventArgs e) {
      var element = (Control)sender;
      if (!borderCaptured || !e.GetCurrentPoint(element).Properties.IsLeftButtonPressed) return;
      var p = SourcePixelPosition(element, e);
      ViewModel?.DrawBorder(p.X, p.Y);
   }

   private void BorderUp(object sender, PointerReleasedEventArgs e) {
      if (!borderCaptured || ReleasedButton(e, (Control)sender) != interactionButton) return;
      ReleasePointerInteraction();
      ViewModel?.CompleteBorderDraw();
   }

   #endregion

   #region Block selection pop

   /// <summary>
   /// WPF scaled the block-selection highlight from 3x to 1x over half a second whenever
   /// BlockSelectionToggle flipped - the same Storyboard on both the enter and the exit action.
   /// Avalonia has no DataTriggers, so the toggle is watched here and the scale eased directly.
   /// </summary>
   private readonly DispatcherTimer selectionPopTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
   private double selectionPopProgress = 1;
   private readonly ScaleTransform selectionScale = new(1, 1);

   /// The tile highlight keeps the TranslateTransform that positions it, so only the scale part of
   /// its TransformGroup is animated.
   private readonly DispatcherTimer tilePopTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
   private double tilePopProgress = 1;

   private ScaleTransform TileSelectionScaleTransform =>
      (TileSelectionHighlight.RenderTransform as TransformGroup)?.Children[0] as ScaleTransform;

   private void StartTilePop() {
      tilePopProgress = 0;
      var scale = TileSelectionScaleTransform;
      if (scale == null) return;
      scale.ScaleX = scale.ScaleY = 3;
      tilePopTimer.Start();
   }

   private void SetUpSelectionPop() {
      tilePopTimer.Tick += (sender, e) => {
         tilePopProgress = Math.Min(1, tilePopProgress + 16 / 500.0);
         var eased = 1 - (1 - tilePopProgress) * (1 - tilePopProgress);
         var scale = TileSelectionScaleTransform;
         if (scale != null) scale.ScaleX = scale.ScaleY = 3 - eased * 2;
         if (tilePopProgress >= 1) tilePopTimer.Stop();
      };

      BlockSelectionHighlight.RenderTransform = selectionScale;
      selectionPopTimer.Tick += (sender, e) => {
         selectionPopProgress = Math.Min(1, selectionPopProgress + 16 / 500.0);
         var eased = 1 - (1 - selectionPopProgress) * (1 - selectionPopProgress); // DecelerationRatio 1
         var scale = 3 - eased * 2;
         selectionScale.ScaleX = selectionScale.ScaleY = scale;
         if (selectionPopProgress >= 1) selectionPopTimer.Stop();
      };
   }

   private void StartSelectionPop() {
      selectionPopProgress = 0;
      selectionScale.ScaleX = selectionScale.ScaleY = 3;
      selectionPopTimer.Start();
   }

   #endregion

   #region "Show Beneath" reveal

   /// <summary>
   /// WPF animated the two GradientStop offsets of each map's OpacityMask with a Storyboard,
   /// started and reversed by a DataTrigger on BlockMapViewModel.ShowBeneath. Avalonia has no
   /// DataTriggers, and GradientStop is not Animatable so it cannot be the target of an
   /// Animation either - the same ease-out over 0.5s is driven from here instead.
   /// </summary>
   private readonly DispatcherTimer revealTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
   private double revealProgress;
   private bool revealOpening;

   private void SetUpReveal() {
      revealTimer.Tick += (sender, e) => {
         // 0.5s, DecelerationRatio 1 in WPF - an ease-out, so progress squared from the far end.
         var step = 16 / 500.0;
         revealProgress = (revealProgress + (revealOpening ? step : -step)).LimitToRange(0, 1);
         var eased = 1 - (1 - revealProgress) * (1 - revealProgress);
         ApplyReveal(eased);
         if (revealProgress <= 0 || revealProgress >= 1) revealTimer.Stop();
      };
   }

   private void UpdateReveal(bool showBeneath) {
      if (revealOpening == showBeneath && revealTimer.IsEnabled) return;
      revealOpening = showBeneath;
      revealTimer.Start();
   }

   private void ApplyReveal(double eased) {
      foreach (var visual in MapView.GetVisualDescendants()) {
         if (visual is not PixelImage image || image.OpacityMask is not RadialGradientBrush brush) continue;
         if (brush.GradientStops.Count < 2) continue;
         brush.GradientStops[0].Offset = eased * .8;
         brush.GradientStops[1].Offset = eased;
      }
   }

   #endregion

   #region Shifter Interaction

   private bool withinShiftInteraction;

   private void ShifterDown(object sender, PointerPressedEventArgs e) {
      if (withinShiftInteraction || ViewModel == null) return;
      var element = (Control)sender;
      var button = PressedButton(e, element);
      // WPF wired only MouseLeftButtonDown here. Leave secondary-button events
      // unhandled so Avalonia can request the arrow's connection menu on release.
      if (button != MouseButton.Left) return;
      if (OperatingSystem.IsMacOS() && e.KeyModifiers.HasFlag(KeyModifiers.Control)) {
         // Accept Control-click even when the input backend reports a left
         // button with modifiers instead of translating it to a right button.
         if (element.ContextMenu is { IsEnabled: true } menu) menu.Open(element);
         e.Handled = true;
         return;
      }
      var p = GetCoordinates(MapButtons, e);
      var owner = ViewModel;
      TrackPointerInteraction(e, element, owner, p, position => owner.ShiftUp(position.X, position.Y));
      withinShiftInteraction = true;
      ViewModel.ShiftDown(p.X, p.Y);
      e.Handled = true;
   }

   private void ShifterMove(object sender, PointerEventArgs e) {
      if (!withinShiftInteraction || ViewModel == null) return;
      var p = GetCoordinates(MapButtons, e);
      interactionPosition = p;
      ViewModel.ShiftMove(p.X, p.Y);
      e.Handled = true;
   }

   private void ShifterUp(object sender, PointerReleasedEventArgs e) {
      // Do not release capture or consume a context-menu click we never started.
      if (!withinShiftInteraction || ReleasedButton(e, (Control)sender) != MouseButton.Left || ViewModel == null) return;
      ReleasePointerInteraction();
      var p = GetCoordinates(MapButtons, e);
      ViewModel.ShiftUp(p.X, p.Y);
      withinShiftInteraction = false;
      e.Handled = true;
   }

   #endregion

   #region Event Template Interaction

   private void EventTemplateDown(object sender, PointerPressedEventArgs e) {
      if (ViewModel?.PrimaryMap == null) return;
      var target = (EventCreationType)((Control)sender).Tag;
      if (target == EventCreationType.Fly && !ViewModel.PrimaryMap.CanCreateFlyEvent) return;
      var owner = ViewModel;
      TrackPointerInteraction(e, MapView, owner, GetCoordinates(MapView, e), _ => owner.CompleteInteraction());
      withinMapInteraction = MouseButton.Left;
      controlSelectGesture = false;
      ViewModel.StartEventCreationInteraction(target);
      e.Handled = true;
   }

   #endregion

   /// WPF used NativeProcess.Start on the Hyperlink Uri; on macOS that is `open`.
   private void Navigate(object sender, RoutedEventArgs e) {
      if (sender is Control control && control.Tag is string url) {
         Process.Start(new ProcessStartInfo("open", $"\"{url}\"") { UseShellExecute = false });
      }
      e.Handled = true;
   }
}
