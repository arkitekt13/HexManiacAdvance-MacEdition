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
   private int referenceGeneration;
   private bool referenceActive, referenceDirty = true, referenceRendering, referenceAttached;
   private int? displayedReferenceMap;
   private static readonly ConditionalWeakTable<ViewPort, Dictionary<int, Vector>> savedReferenceOffsets = new();
   private Dictionary<int, Vector> referenceOffsets = new();
   public bool IsScriptMapVisible => ScriptMapPane.IsVisible;
   public int ReferenceRenderCount { get; private set; }
   public void CancelReferencePan() => ScriptMapPane.CancelPan();

   private void InitializeScriptMapReference() {
      AttachedToVisualTree += (_, _) => { referenceAttached = true; BindScriptReference(); };
      DetachedFromVisualTree += (_, _) => { referenceAttached = false; UnbindScriptReference(); };
      DataContextChanged += (_, _) => { if (referenceAttached) BindScriptReference(); };
      AddHandler(GotFocusEvent, (_, e) => {
         if (e.Source is TextBox box && box.GetVisualAncestors().Contains(CodeToolPanel)) lastScriptInput = box;
      });
      ScriptMapPane.InteractionStarted += (_, _) => FocusScriptInput();
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
      }
      if (referenceEditor != null) referenceEditor.PropertyChanged -= ReferenceStateChanged;
      if (referenceWindow != null) referenceWindow.Deactivated -= ReferenceWindowDeactivated;
      referenceEditor = null; referenceSource = null; referenceWindow = null;
      lastScriptInput = null;
   }
   private void ReferenceWindowDeactivated(object sender, EventArgs e) => ScriptMapPane.CancelPan();
   private void ReferenceInvalidated(object sender, EventArgs e) {
      SaveReferenceOffset();
      referenceDirty = true;
      referenceGeneration++;
      Dispatcher.UIThread.Post(RefreshScriptReference, DispatcherPriority.Background);
   }
   private void ReferenceStateChanged(object sender, PropertyChangedEventArgs e) {
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

   private void ShowScriptHexClick(object sender, RoutedEventArgs e) {
      if (referenceEditor != null) referenceEditor.PreferScriptMapView = false;
   }
   private void ShowScriptMapClick(object sender, RoutedEventArgs e) {
      if (referenceEditor != null) referenceEditor.PreferScriptMapView = true;
   }

   private async void RefreshScriptReference() {
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
