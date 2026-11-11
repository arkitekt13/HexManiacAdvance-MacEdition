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
   private TutorialPatch tutorial;
   private string tutorialUnavailable;
   private NativeMenuItem tutorialMenu;
   private bool creatingTutorial;

   private void InitializeTutorialMenu() {
      var root = NativeMenu.GetMenu(this);
      if (root == null) return;
      tutorial = TutorialPatch.Load(TutorialPatch.DefaultDirectory, out tutorialUnavailable);
      tutorialMenu = FindMenuItem(root, "Create Tutorial ROM…");
      var utilities = FindMenuItem(root, "Utilities");
      if (utilities?.Menu != null) utilities.Menu.NeedsUpdate += (_, _) => RefreshTutorialMenu();
      RefreshTutorialMenu();
   }

   private IViewPort TutorialSource() => editor?.SelectedTab switch {
      MapEditorViewModel map => map.ViewPort,
      IViewPort view => view,
      _ => null,
   };

   private string TutorialDisabledReason() {
      if (tutorial == null) return tutorialUnavailable;
      if (creatingTutorial) return "A tutorial ROM is being created.";
      return tutorial.DisabledReason(TutorialSource());
   }

   private void RefreshTutorialMenu() {
      if (tutorialMenu == null) return;
      var reason = TutorialDisabledReason();
      tutorialMenu.IsEnabled = reason == null;
      tutorialMenu.ToolTip = reason ?? "Create " + tutorial.Title + " as a new ROM/TOML pair; the open ROM is preserved.";
   }

   private async void CreateTutorialRomClick(object sender, EventArgs e) {
      var reason = TutorialDisabledReason();
      if (reason != null) { if (editor != null) editor.ErrorMessage = reason; return; }
      var targetEditor = editor;
      var source = TutorialSource().Model.RawData.ToArray();
      creatingTutorial = true;
      RefreshTutorialMenu();
      try {
         // The input is captured before yielding; cancellation does not compile source or write files.
         var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = "Create Tutorial ROM — choose a folder and new ROM name",
            SuggestedFileName = "TutorialVillage.gba",
            DefaultExtension = "gba",
            FileTypeChoices = new[] { new FilePickerFileType("Game Boy Advance ROM") { Patterns = new[] { "*.gba" } } },
            ShowOverwritePrompt = true,
         });
         if (file == null) return;
         var destination = file.TryGetLocalPath();
         if (destination == null) throw new IOException("Choose a local folder for the tutorial ROM.");
         if (!ReferenceEquals(editor, targetEditor)) throw new IOException("The original editor was closed. Run the tutorial utility again.");
         var bytes = await Task.Run(() => {
            var result = tutorial.Create(source);
            tutorial.SaveNewCopy(destination, result);
            return result;
         });
         // Both files are present, with the supplied metadata, before the normal HMA loader runs.
         var opened = await targetEditor.OpenFileAsync(new LoadedFile(destination, bytes));
         if (opened == null) targetEditor.ErrorMessage = "The tutorial ROM and TOML were created but could not be opened. " + targetEditor.ErrorMessage;
      } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException) {
         targetEditor.ErrorMessage = "Could not create the tutorial ROM: " + ex.Message;
      } finally {
         creatingTutorial = false;
         RefreshTutorialMenu();
      }
   }
}
