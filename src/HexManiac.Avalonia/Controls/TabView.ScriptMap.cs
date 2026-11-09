using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

public partial class TabView {
   private EditorViewModel referenceEditor;
   private ViewPort referenceSource;
   private Window referenceWindow;
   private TextBox lastScriptInput;
   private ScriptMapEvent referenceEvent;
   private int referenceGeneration;
   private bool referenceActive, referenceDirty = true, referenceRendering, referenceAttached;
   private int? displayedReferenceMap;
   private static readonly ConditionalWeakTable<ViewPort, Dictionary<int, Vector>> savedReferenceOffsets = new();
   private Dictionary<int, Vector> referenceOffsets = new();
   public bool IsScriptMapVisible => ScriptMapPane.IsVisible;
   public int ReferenceRenderCount { get; private set; }
   public void CancelReferencePan() => ScriptMapPane.CancelPan();

   private void InitializeScriptMapReference() {
      // Inline help must not consume the first event click. TabView closes it on press.
      CodeContentsPopup.OverlayInputPassThroughElement = ScriptMapPane;
      AttachedToVisualTree += (_, _) => { referenceAttached = true; BindScriptReference(); };
      DetachedFromVisualTree += (_, _) => { referenceAttached = false; UnbindScriptReference(); };
      DataContextChanged += (_, _) => { if (referenceAttached) BindScriptReference(); };
      AddHandler(GotFocusEvent, (_, e) => {
         if (e.Source is TextBox box && box.GetVisualAncestors().Contains(CodeToolPanel)) lastScriptInput = box;
      });
      ScriptMapPane.InteractionStarted += (_, _) => { if (referenceActive) FocusScriptInput(); };
      ScriptMapPane.EventRequested += ReferenceEventRequested;
      ScriptMapPane.GridPreferenceChanged += (_, _) => {
         if (referenceEditor != null) referenceEditor.ShowScriptReferenceGrid = ScriptMapPane.ShowGrid;
      };
   }

   public TextBox FocusScriptInput() {
      var box = lastScriptInput;
      if (box?.IsEffectivelyVisible != true || box.GetVisualRoot() != this.GetVisualRoot())
         box = CodeToolPanel.GetVisualDescendants().OfType<TextEditor>().Select(e => e.EditBox).FirstOrDefault(b => b.IsEffectivelyVisible);
      if (box != null && !box.IsFocused) {
         int caret = box.CaretIndex, start = box.SelectionStart, end = box.SelectionEnd;
         box.Focus();
         box.CaretIndex = caret; box.SelectionStart = start; box.SelectionEnd = end;
      }
      return box;
   }

   private void BindScriptReference() {
      UnbindScriptReference();
      referenceWindow = this.GetVisualRoot() as Window;
      referenceEditor = (referenceWindow as MainWindow)?.Editor;
      referenceSource = DataContext as ViewPort;
      if (referenceSource != null) {
         referenceOffsets = savedReferenceOffsets.GetOrCreateValue(referenceSource);
         referenceSource.PropertyChanged += ReferenceStateChanged;
         referenceSource.ReferenceMapRefreshRequested += ReferenceInvalidated;
         referenceSource.ChangeHistory.Reverted += ReferenceInvalidated;
         referenceSource.Tools.PropertyChanged += ReferenceStateChanged;
         referenceSource.Tools.CodeTool.PropertyChanged += ReferenceStateChanged;
         referenceSource.Tools.CodeTool.ScriptStateChanged += ReferenceScriptSettled;
      }
      if (referenceEditor != null) referenceEditor.PropertyChanged += ReferenceStateChanged;
      if (referenceWindow != null) referenceWindow.Deactivated += ReferenceWindowDeactivated;
      RefreshScriptReference();
   }

   private void SaveReferenceOffset() {
      if (displayedReferenceMap is int map) referenceOffsets[map] = ScriptMapPane.Offset;
   }
   private void UnbindScriptReference() {
      SaveReferenceOffset();
      displayedReferenceMap = null;
      referenceGeneration++; referenceActive = false; referenceDirty = true;
      ScriptMapPane.Clear();
      if (referenceSource != null) {
         referenceSource.PropertyChanged -= ReferenceStateChanged;
         referenceSource.ReferenceMapRefreshRequested -= ReferenceInvalidated;
         referenceSource.ChangeHistory.Reverted -= ReferenceInvalidated;
         referenceSource.Tools.PropertyChanged -= ReferenceStateChanged;
         referenceSource.Tools.CodeTool.PropertyChanged -= ReferenceStateChanged;
         referenceSource.Tools.CodeTool.ScriptStateChanged -= ReferenceScriptSettled;
      }
      if (referenceEditor != null) referenceEditor.PropertyChanged -= ReferenceStateChanged;
      if (referenceWindow != null) referenceWindow.Deactivated -= ReferenceWindowDeactivated;
      referenceEditor = null; referenceSource = null; referenceWindow = null;
      lastScriptInput = null; referenceEvent = null;
   }
   private void ReferenceWindowDeactivated(object sender, EventArgs e) => ScriptMapPane.CancelPan();
   private void ReferenceInvalidated(object sender, EventArgs e) {
      SaveReferenceOffset();
      referenceDirty = true;
      referenceGeneration++;
      Dispatcher.UIThread.Post(RefreshScriptReference, DispatcherPriority.Background);
   }
   private void ReferenceStateChanged(object sender, PropertyChangedEventArgs e) {
      if (e.PropertyName == nameof(EditorViewModel.ShowScriptReferenceGrid)) {
         ScriptMapPane.ShowGrid = referenceEditor?.ShowScriptReferenceGrid == true;
         return;
      }
      if (e.PropertyName == nameof(ViewPort.UpdateInProgress) && referenceSource?.UpdateInProgress == false) {
         ReferenceInvalidated(sender, EventArgs.Empty);
         return;
      }
      if (e.PropertyName == nameof(ViewPort.ScriptReferenceMapID)) {
         SaveReferenceOffset(); displayedReferenceMap = null; referenceDirty = true; referenceGeneration++;
      }
      if (e.PropertyName is nameof(EditorViewModel.PreferScriptMapView) or nameof(EditorViewModel.SelectedIndex)
          or nameof(EditorViewModel.SelectedTab) or nameof(ViewPort.ScriptReferenceMapID) or nameof(CodeTool.Mode)) RefreshScriptReference();
   }

   private void ReferenceScriptSettled(object sender, EventArgs e) {
      if (!referenceActive || referenceEvent == null || referenceSource.Tools.CodeTool.HasUncompiledScriptChanges ||
          !ScriptMapPane.ContainsEvent(referenceEvent)) return;
      if (referenceEvent.TryResolve(referenceSource, out var destination)) ScriptMapPane.ShowEventInfo(destination.Description);
   }

   private void ReferenceEventRequested(object sender, ScriptMapEvent target) {
      var source = referenceSource;
      if (!referenceActive || !referenceAttached || source == null || !ReferenceEquals(referenceEditor?.SelectedTab, source) ||
          !ScriptMapPane.ContainsEvent(target)) return;
      try {
         if (!target.TryResolve(source, out var destination)) {
            ScriptMapPane.ShowEventInfo("This event has changed. Reopen Map View to refresh it.", true); return;
         }
         if (!destination.IsScript) { ScriptMapPane.ShowEventInfo(destination.Description); return; }
         if (destination.Address < 0 || destination.Address >= source.Model.Count) {
            ScriptMapPane.ShowEventInfo(destination.Description + " — no valid script to open.", true); return;
         }
         var tool = source.Tools.CodeTool;
         // Settling source can relocate either the current or destination script. Never
         // navigate using an address copied into the displayed image or read before this.
         tool.FlushPendingScriptChanges();
         if (tool.HasActiveScriptDraft || tool.Contents.Any(body => body.HasError)) {
            ScriptMapPane.ShowEventInfo("Fix the errors in the current script before opening another.", true);
            FocusScriptInput(); return;
         }
         if (!target.TryResolve(source, out destination)) {
            ScriptMapPane.ShowEventInfo("This event has changed. Reopen Map View to refresh it.", true); return;
         }
         if (destination.Address < 0 || destination.Address >= source.Model.Count ||
             tool.ScriptParser.FindLength(source.Model, destination.Address) <= 0 ||
             tool.ScriptParser.CollectScripts(source.Model, destination.Address).Count == 0) {
            ScriptMapPane.ShowEventInfo(destination.Description + " — no valid script to open.", true); return;
         }
         referenceEvent = target;
         ScriptMapPane.ShowEventInfo(destination.Description);
         source.Goto.Execute(destination.Address); // reuse this tab, rather than GotoScript's duplicate-tab policy
         int generation = referenceGeneration;
         Dispatcher.UIThread.Post(() => {
            if (generation == referenceGeneration && referenceActive && ReferenceEquals(referenceSource, source) &&
                ReferenceEquals(referenceEditor?.SelectedTab, source) && tool.Contents.Any(body => body.Address == destination.Address)) {
               lastScriptInput = CodeToolPanel.GetVisualDescendants().OfType<TextEditor>()
                  .FirstOrDefault(editor => editor.Tag is CodeBody body && body.Address == destination.Address)?.EditBox;
               FocusScriptInput();
            }
         }, DispatcherPriority.Loaded);
      } catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IndexOutOfRangeException or NotImplementedException) {
         ScriptMapPane.ShowEventInfo("This event's script is unavailable. Reopen Map View to refresh it.", true);
      }
   }

   private void ShowScriptHexClick(object sender, RoutedEventArgs e) {
      if (referenceEditor != null) referenceEditor.PreferScriptMapView = false;
   }
   private void ShowScriptMapClick(object sender, RoutedEventArgs e) {
      if (referenceEditor != null) referenceEditor.PreferScriptMapView = true;
   }

   private async void RefreshScriptReference() {
      ScriptMapPane.ShowGrid = referenceEditor?.ShowScriptReferenceGrid == true;
      bool script = referenceSource?.Tools.SelectedTool is CodeTool { Mode: CodeMode.Script };
      bool show = script && referenceEditor?.PreferScriptMapView == true;
      ScriptHexViewButton.IsVisible = ScriptMapViewButton.IsVisible = script;
      ScriptHexViewButton.Classes.Set("activeTool", !show); ScriptMapViewButton.Classes.Set("activeTool", show);
      HexPane.IsVisible = !show; HexPane.IsEnabled = !show; ScriptMapPane.IsVisible = show;
      bool active = show && referenceAttached && IsEffectivelyVisible && ReferenceEquals(referenceEditor?.SelectedTab, referenceSource);
      if (!active) {
         if (referenceActive) { SaveReferenceOffset(); referenceGeneration++; referenceDirty = true; ScriptMapPane.Clear(); }
         referenceActive = false;
         return;
      }
      if (!referenceActive) {
         referenceDirty = true;
         // HexContent may still own keyboard focus when its pane is hidden.
         // Hand it to source before the next input event, without moving the caret.
         Dispatcher.UIThread.Post(() => {
            if (referenceActive && referenceAttached && IsEffectivelyVisible &&
                ReferenceEquals(referenceEditor?.SelectedTab, referenceSource) &&
                referenceEditor.GotoViewModel.ControlVisible != true && !referenceEditor.FindControlVisible && !referenceEditor.HexConverterVisible)
               FocusScriptInput();
         }, DispatcherPriority.Loaded);
      }
      referenceActive = true;
      if (referenceSource.ScriptReferenceMapID is not int mapID) {
         ScriptMapPane.Clear("No reference map. Open this script from a map event or map script. Hex View is still available.");
         return;
      }
      if (!referenceDirty || referenceRendering) return;
      referenceDirty = false; referenceRendering = true;
      int generation = referenceGeneration;
      var source = referenceSource;
      ScriptMapPane.Clear("Preparing reference map…");
      try {
         var snapshot = await ScriptMapSnapshot.CreateAsync(source, mapID);
         if (generation != referenceGeneration || !referenceActive || !ReferenceEquals(source, referenceSource)) return;
         ScriptMapPane.ShowSnapshot(snapshot); ReferenceRenderCount++;
         if (referenceEvent != null && !ScriptMapPane.ContainsEvent(referenceEvent)) referenceEvent = null;
         ReferenceScriptSettled(this, EventArgs.Empty);
         displayedReferenceMap = mapID;
         ScriptMapPane.Offset = referenceOffsets.TryGetValue(mapID, out var offset) ? offset : default;
      } catch (Exception ex) when (ex is not OutOfMemoryException) {
         if (generation == referenceGeneration && referenceActive)
            ScriptMapPane.Clear("Reference map unavailable: " + ex.Message + " Use Hex View to continue.");
      } finally {
         referenceRendering = false;
         if (referenceActive && referenceDirty) Dispatcher.UIThread.Post(RefreshScriptReference, DispatcherPriority.Background);
      }
   }
}
