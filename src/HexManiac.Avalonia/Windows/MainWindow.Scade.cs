using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

public partial class MainWindow {
   private ScadePackage scade;
   private string scadeUnavailable;
   private NativeMenuItem scadeMenu;
   private bool creatingScade;

   private void InitializeScadeMenu() {
      var root = NativeMenu.GetMenu(this);
      if (root == null) return;
      scade = ScadePackage.Load(ScadePackage.DefaultDirectory, out scadeUnavailable);
      scadeMenu = FindMenuItem(root, "Create SCADE ROM…");
      var utilities = FindMenuItem(root, "Utilities");
      if (utilities?.Menu != null) utilities.Menu.NeedsUpdate += (_, _) => RefreshScadeMenu();
      RefreshScadeMenu();
   }
   private IViewPort ScadeSource() => editor?.SelectedTab switch {
      MapEditorViewModel map => map.ViewPort,
      IViewPort view => view,
      _ => null,
   };
   private string ScadeDisabledReason() => scade == null ? scadeUnavailable : creatingScade ?
      "A SCADE ROM is being created." : scade.DisabledReason(ScadeSource());
   private void RefreshScadeMenu() {
      if (scadeMenu == null) return;
      var reason = ScadeDisabledReason();
      scadeMenu.IsEnabled = reason == null;
      scadeMenu.ToolTip = reason ?? $"Create SCADE ({scade.Version}) with its matching TOML as a new ROM copy; the open ROM is preserved.";
   }
   private async void CreateScadeRomClick(object sender, EventArgs e) {
      var reason = ScadeDisabledReason();
      if (reason != null) { if (editor != null) editor.ErrorMessage = reason; return; }
      var targetEditor = editor;
      // Snapshot before the picker yields: later tab switches/edits cannot change this input.
      var source = ScadeSource().Model.RawData.ToArray();
      creatingScade = true;
      RefreshScadeMenu();
      try {
         var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = "Create SCADE ROM — choose a folder and new ROM name",
            SuggestedFileName = "MyScadeGame.gba",
            DefaultExtension = "gba",
            FileTypeChoices = new[] { new FilePickerFileType("Game Boy Advance ROM") { Patterns = new[] { "*.gba" } } },
            ShowOverwritePrompt = true,
         });
         if (file == null) return;
         var destination = file.TryGetLocalPath();
         if (destination == null) throw new IOException("Choose a local folder for the SCADE ROM and matching TOML.");
         var bytes = await Task.Run(() => {
            var result = scade.Create(source);
            scade.SaveNewCopy(destination, result);
            return result;
         });
         // Both files exist, with the creator's original TOML, before the normal HMA loader runs.
         var opened = await targetEditor.OpenFileAsync(new LoadedFile(destination, bytes));
         if (opened == null) targetEditor.ErrorMessage = "The SCADE ROM and TOML were created, but could not be opened. " + targetEditor.ErrorMessage;
      } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException) {
         targetEditor.ErrorMessage = "Could not create the SCADE ROM: " + ex.Message;
      } finally {
         creatingScade = false;
         RefreshScadeMenu();
      }
   }
}
