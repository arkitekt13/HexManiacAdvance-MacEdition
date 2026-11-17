using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core.Models;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

public partial class MainWindow {
   private async void CreateScadeIkarusClick(object sender, EventArgs e) {
      if (applyingIkarus) return;
      var owner = editor;
      var source = IkarusSource();
      var package = scadeIkarus;
      var reason = package == null ? scadeIkarusUnavailable : ScadeIkarusDisabledReason();
      if (reason != null) { if (owner != null) owner.ErrorMessage = reason; return; }
      // Capture the validated input before either dialog yields to another tab or edit.
      var bytes = source.Model.RawData.ToArray();
      var sourceName = source.FullFileName;
      applyingIkarus = true;
      RefreshIkarusMenu();
      try {
         if (!await new IkarusUpgradeDialog(package.Credits, forScade: true).ShowDialog<bool>(this)) return;
         var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = "Create experimental Ikarus/SCADE ROM — choose a folder and new name",
            SuggestedFileName = (string.IsNullOrEmpty(sourceName) ? "MyScadeGame" : Path.GetFileNameWithoutExtension(sourceName)) + "_Ikarus.gba",
            DefaultExtension = "gba", ShowOverwritePrompt = true,
            FileTypeChoices = new[] { new FilePickerFileType("Game Boy Advance ROM") { Patterns = new[] { "*.gba" } } },
         });
         if (file == null) return;
         var destination = file.TryGetLocalPath();
         if (destination == null) throw new IOException("Choose a local folder for the new ROM and TOML.");
         if (!ReferenceEquals(editor, owner) || !IkarusRelatedViews(owner, source).Contains(source))
            throw new InvalidOperationException("The source project was closed. Run the utility again.");
         if (!string.IsNullOrEmpty(sourceName) && string.Equals(Path.GetFullPath(sourceName), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Choose a new name, separate from the source project.");
         var output = await Task.Run(() => {
            var result = package.Create(bytes);
            package.SaveNewCopy(destination, result);
            return result;
         });
         if (!ReferenceEquals(editor, owner)) {
            owner.ErrorMessage = "The new ROM and TOML were created at " + destination + ". Open that ROM to continue.";
            return;
         }
         // The supplied matching TOML is present before the normal project loader runs.
         var opened = await owner.OpenFileAsync(new LoadedFile(destination, output));
         if (opened == null) owner.ErrorMessage = "The new ROM and TOML were created, but could not be opened. " + owner.ErrorMessage;
      } catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException) {
         owner.ErrorMessage = "Could not create the experimental Ikarus/SCADE ROM: " + ex.Message;
      } finally {
         applyingIkarus = false;
         RefreshIkarusMenu();
      }
   }
}
