using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using HavenSoft.HexManiac.Core.Models;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

public partial class MainWindow {
   private IkarusUpgrade ikarus;
   private string ikarusUnavailable;
   private NativeMenuItem ikarusMenu;
   private bool applyingIkarus;

   private void InitializeIkarusMenu() {
      var root = NativeMenu.GetMenu(this);
      if (root == null) return;
      ikarus = IkarusUpgrade.Load(IkarusUpgrade.DefaultDirectory, out ikarusUnavailable);
      ikarusMenu = FindMenuItem(root, "Ikarus Visual Upgrade…");
      var utilities = FindMenuItem(root, "Utilities");
      if (utilities?.Menu != null) utilities.Menu.NeedsUpdate += (_, _) => RefreshIkarusMenu();
      RefreshIkarusMenu();
   }
   private ViewPort IkarusSource() => editor?.SelectedTab switch {
      MapEditorViewModel map => map.ViewPort as ViewPort,
      ViewPort view => view,
      _ => null,
   };
   private void RefreshIkarusMenu() {
      if (ikarusMenu == null) return;
      var reason = ikarus == null ? ikarusUnavailable : applyingIkarus ?
         "An Ikarus upgrade is already in progress." : IkarusUpgrade.DisabledReason(IkarusSource());
      ikarusMenu.IsEnabled = reason == null;
      ikarusMenu.ToolTip = reason ?? "Back up your FireRed project, then apply the Ikarus Visual Upgrade.";
   }
   internal static ViewPort[] IkarusRelatedViews(EditorViewModel owner, ViewPort source) => owner
      .Select(tab => tab is MapEditorViewModel map ? map.ViewPort as ViewPort : tab as ViewPort)
      .Where(view => view != null && ReferenceEquals(view.Model, source.Model)).Distinct().ToArray();
   internal static bool FlushIkarusDrafts(EditorViewModel owner, ViewPort source) {
      foreach (var view in IkarusRelatedViews(owner, source))
         if (view.Tools?.CodeTool is { } code && !code.FlushPendingScriptChanges()) return false;
      return true;
   }
   private static readonly ConditionalWeakTable<ChangeHistory<ModelDelta>, object> ikarusMapObservers = new();
   internal static void WatchIkarusMaps(EditorViewModel owner, ViewPort source) {
      ikarusMapObservers.GetValue(source.ChangeHistory, history => {
         // No global model list: the observer disappears with this document's history.
         history.Reverted += (_, _) => RefreshIkarusMaps(owner, source);
         return new object();
      });
   }
   internal static void RefreshIkarusMaps(EditorViewModel owner, ViewPort source) {
      // Formatting discovered while refreshing is derived state, not another undoable user edit.
      // A NoDataChange token also rejects any unexpected ROM write during this refresh.
      using var formatting = source.ChangeHistory.UseTransientChange(new NoDataChangeDeltaModel());
      var maps = owner.OfType<MapEditorViewModel>().Concat(IkarusRelatedViews(owner, source).Select(view => view.MapEditor));
      foreach (var map in maps.Where(map => map != null && ReferenceEquals(map.ViewPort.Model, source.Model) && map.IsValidState).Distinct()) {
         map.Refresh();
         foreach (var visible in map.VisibleMaps) _ = visible.PixelData;
      }
   }
   private async void ApplyIkarusClick(object sender, EventArgs e) {
      if (applyingIkarus) return;
      var owner = editor;
      var source = IkarusSource();
      var reason = ikarus == null ? ikarusUnavailable : IkarusUpgrade.DisabledReason(source);
      if (reason != null) { if (owner != null) owner.ErrorMessage = reason; return; }
      var fileName = source.FullFileName;
      bool StillOpen() => ReferenceEquals(editor, owner) && source.FullFileName == fileName &&
         IkarusRelatedViews(owner, source).Contains(source);
      bool applied = false;
      applyingIkarus = true;
      RefreshIkarusMenu();
      try {
         if (!await new IkarusUpgradeDialog(ikarus.Credits).ShowDialog<bool>(this)) return;
         var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = "Back up before Ikarus — choose a folder and new ROM name",
            SuggestedFileName = (string.IsNullOrEmpty(fileName) ? "FireRed" : Path.GetFileNameWithoutExtension(fileName)) + "_before_Ikarus.gba",
            DefaultExtension = "gba", ShowOverwritePrompt = true,
            FileTypeChoices = new[] { new FilePickerFileType("Game Boy Advance ROM") { Patterns = new[] { "*.gba" } } },
         });
         if (file == null) return;
         var destination = file.TryGetLocalPath();
         if (destination == null) throw new IOException("Choose a local backup folder.");
         if (!StillOpen()) throw new InvalidOperationException("The source project was closed or changed. Run the utility again.");
         if (!FlushIkarusDrafts(owner, source)) throw new InvalidOperationException("Correct your rejected script source, or discard it with Undo, before applying Ikarus.");
         var prepared = ikarus.Prepare(source);
         var backup = await Task.Run(() => ikarus.WriteBackup(prepared, destination));
         if (!StillOpen() || IkarusRelatedViews(owner, source).Any(view => view.Tools?.CodeTool.HasUncompiledScriptChanges == true))
            throw new InvalidOperationException("The project changed during backup. Your backup is retained; run the utility again.");
         ikarus.Apply(prepared, backup);
         applied = true;
         WatchIkarusMaps(owner, source);
         foreach (var view in IkarusRelatedViews(owner, source)) view.Refresh();
         RefreshIkarusMaps(owner, source);
         source.RaiseMessage("Ikarus Visual Upgrade applied. Save to keep it, or Undo to restore the previous state. Backup: " + backup.Path);
      } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException) {
         owner.ErrorMessage = applied
            ? "Ikarus was applied, but a view could not refresh. Your backup is retained; use Undo to restore the previous ROM. " + ex.Message
            : "Could not apply Ikarus Visual Upgrade: " + ex.Message;
      } finally {
         applyingIkarus = false;
         RefreshIkarusMenu();
      }
   }
}
