using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HexManiac.WPF.Resources;

namespace HavenSoft.HexManiac.AvaloniaUI.MacPlatform;

/// <summary>
/// macOS/Avalonia replacement for HexManiac.WPF's WindowsFileSystem (file access, dialogs,
/// clipboard, message boxes, app metadata).
///
/// HOW TO FINISH THIS CLASS
/// The first build will list every IFileSystem member that's missing. For each one:
///   1. Open src/HexManiac.WPF/Implementations/WindowsFileSystem.cs and find the same member.
///   2. Copy its exact signature here.
///   3. Implement it with the platform helpers below instead of WPF / Win32 calls.
/// (Your IDE's "Implement interface" action generates all the stubs in one go.)
///
/// Example of the pattern (match the real signature from IFileSystem.cs):
///
///   public LoadedFile OpenFile(string extensionDescription = null, params string[] extensionOptions) {
///      var path = PickFileToOpen(extensionDescription, extensionOptions);
///      return path == null ? null : new LoadedFile(path, File.ReadAllBytes(path));
///   }
/// </summary>
public partial class MacFileSystem : IFileSystem, IWorkDispatcher, IFileRevealer, IMapImageExporter, IDisposable {
   readonly Func<Window> getOwner;

   private readonly Action<Action> dispatchFileChange;
   private readonly AsyncLocal<LoadedFile> reloadSnapshot = new();
   private readonly string appSupportDirectory;
   private bool appMetadataReadFailed;
   public string AppMetadataWarning { get; private set; }
   public MacFileSystem(Func<Window> getOwner, Action<Action> dispatchFileChange = null, string appSupportDirectory = null) {
      this.getOwner = getOwner;
      this.appSupportDirectory = appSupportDirectory;
      this.dispatchFileChange = dispatchFileChange ?? (action => Dispatcher.UIThread.Post(action, DispatcherPriority.ApplicationIdle));
   }

   TopLevel Owner => getOwner();

   // ---------------- App-level metadata (settings, theme, recent files) ----------------

   /// ~/Library/Application Support/HexManiacAdvance
   /// (Environment.SpecialFolder.ApplicationData is ~/.config on .NET for macOS, not
   /// ~/Library/Application Support, so the path is built explicitly.)
   public static string AppSupportDirectory {
      get {
         var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "Application Support", EditorViewModel.ApplicationName);
         Directory.CreateDirectory(dir);
         return dir;
      }
   }

   // Per the architecture docs, EditorViewModel loads settings with MetadataFor(ApplicationName)
   // and saves with SaveMetadata(...). Wire those interface members to these helpers, but check
   // WindowsFileSystem first: ROM metadata (.toml) lives next to the ROM, and only app-level
   // settings should go to Application Support.
   protected string[] ReadAppMetadata(string applicationName) {
      try {
         return File.ReadAllLines(MetadataPath(applicationName));
      } catch (FileNotFoundException) { return Array.Empty<string>(); }
      catch (DirectoryNotFoundException) { return Array.Empty<string>(); }
      catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
         appMetadataReadFailed = true;
         AppMetadataWarning = "Could not read application settings. Using defaults for this session; existing settings will not be overwritten. " + error.Message;
         return Array.Empty<string>();
      }
   }

   protected bool WriteAppMetadata(string applicationName, string[] lines) {
      // Defaults from a failed read must never replace preferences we could not inspect.
      if (appMetadataReadFailed) return false;
      try {
         AtomicFileWriter.WriteAllLines(MetadataPath(applicationName), lines);
         return true;
      } catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
         AppMetadataWarning = "Could not save application settings; existing settings were preserved. " + error.Message;
         return false;
      }
   }

   // ---------------- File dialogs ----------------

   public Task<string> ChooseExternalEmulatorAsync() => ExternalEmulatorPicker.ChooseAsync();

   protected string PickFileToOpen(string description, IEnumerable<string> extensions) =>
      UiSync.Run(async () => {
         var files = await Owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Open",
            AllowMultiple = false,
            FileTypeFilter = FileTypes(description, extensions),
         });
         return files.Count == 0 ? null : files[0].TryGetLocalPath();
      });

   protected string PickFileToSave(string description, IEnumerable<string> extensions, string suggestedName = null) =>
      UiSync.Run(async () => {
         var file = await Owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = "Save",
            SuggestedFileName = suggestedName,
            DefaultExtension = extensions?.FirstOrDefault(),
            FileTypeChoices = FileTypes(description, extensions),
            ShowOverwritePrompt = true,
         });
         return file?.TryGetLocalPath();
      });

   protected string PickFolder(string title) =>
      UiSync.Run(async () => {
         var folders = await Owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title });
         return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
      });

   static IReadOnlyList<FilePickerFileType> FileTypes(string description, IEnumerable<string> extensions) {
      var patterns = extensions?.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => "*." + e.TrimStart('.', '*')).ToList();
      if (patterns == null || patterns.Count == 0) return null;
      return new[] { new FilePickerFileType(description ?? "Files") { Patterns = patterns } };
   }

   // ---------------- Clipboard ----------------

   protected string ReadClipboardText() =>
      UiSync.Run(async () => Owner.Clipboard == null ? string.Empty : await Owner.Clipboard.GetTextAsync() ?? string.Empty);

   protected void WriteClipboardText(string text) =>
      UiSync.Run(async () => { if (Owner.Clipboard != null) await Owner.Clipboard.SetTextAsync(text); });

   // ---------------- Message boxes ----------------

   protected void Alert(string title, string message) =>
      UiSync.Run(() => ShowDialog(title, message, withCancel: false));

   protected bool Confirm(string title, string message) =>
      UiSync.Run(() => ShowDialog(title, message, withCancel: true));

   Task<bool> ShowDialog(string title, string message, bool withCancel) {
      var dialog = new Window {
         Title = title,
         Width = 440,
         SizeToContent = SizeToContent.Height,
         CanResize = false,
         WindowStartupLocation = WindowStartupLocation.CenterOwner,
      };
      var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 80 };
      var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 80, IsVisible = withCancel };
      ok.Click += (_, _) => dialog.Close(true);
      cancel.Click += (_, _) => dialog.Close(false);

      dialog.Content = new StackPanel {
         Margin = new Thickness(20),
         Spacing = 16,
         Children = {
            new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            new StackPanel {
               Orientation = Orientation.Horizontal,
               HorizontalAlignment = HorizontalAlignment.Right,
               Spacing = 8,
               Children = { cancel, ok },
            },
         },
      };
      return dialog.ShowDialog<bool>(getOwner());
   }

   // ---------------- Misc ----------------

   protected static void OpenInDefaultApp(string pathOrUrl) =>
      Process.Start(new ProcessStartInfo("open", $"\"{pathOrUrl}\"") { UseShellExecute = false });

   // ==================================================================================
   // IFileSystem
   // Signatures copied from src/HexManiac.Core/Models/IFileSystem.cs; behavior mirrors
   // src/HexManiac.WPF/Implementations/WindowsFileSystem.cs with the helpers above.
   // ==================================================================================

   // ---------------- Clipboard ----------------

   public string CopyText {
      get => ReadClipboardText();
      set => WriteClipboardText(value);
   }

   /// <summary>
   /// WPF gets this from Clipboard.GetImage()/SetImage(). Avalonia has no image clipboard API,
   /// so this goes through the macOS pasteboard's PNG type. lastCopiedImage keeps copy/paste
   /// working inside the app even when the pasteboard round-trip isn't available.
   /// </summary>
   (short[] image, int width) lastCopiedImage;

   public (short[] image, int width) CopyImage {
      get {
         try {
            var png = UiSync.Run(async () => Owner?.Clipboard == null ? null : await Owner.Clipboard.GetDataAsync(PngClipboardFormat) as byte[]);
            if (png != null && png.Length > 0) {
               using var stream = new MemoryStream(png);
               return DecodeImage(stream);
            }
         } catch (Exception) {
            // pasteboard didn't hold a PNG we could read; fall through to the in-app copy
         }
         return lastCopiedImage;
      }
      set {
         lastCopiedImage = value;
         if (value.image == null || value.width <= 0) return;
         try {
            using var stream = new MemoryStream();
            EncodeImage(value.image, value.width).Save(stream);
            var data = new DataObject();
            data.Set(PngClipboardFormat, stream.ToArray());
            UiSync.Run(async () => { if (Owner?.Clipboard != null) await Owner.Clipboard.SetDataObjectAsync(data); });
         } catch (Exception) {
            // couldn't reach the pasteboard; the in-app copy above still works
         }
      }
   }

   const string PngClipboardFormat = "public.png"; // the macOS uniform type identifier

   // ---------------- Files ----------------

   public LoadedFile OpenFile(string extensionDescription = null, params string[] extensionOptions) {
      var path = PickFileToOpen(extensionDescription, extensionOptions);
      return path == null ? null : LoadFile(path);
   }

   public string OpenFolder() => PickFolder("Open Folder");

   public bool Exists(string file) => File.Exists(file);

   public Task<string> RevealFileAsync(string path) => MacFileReveal.RevealAsync(path);

   public void LaunchProcess(string file, string arguments = null) {
      try {
         if (arguments == null) {
            OpenInDefaultApp(Path.GetFullPath(file));
         } else {
            Process.Start(new ProcessStartInfo(file, arguments) { UseShellExecute = false });
         }
      } catch (System.ComponentModel.Win32Exception) {
         var nl = Environment.NewLine;
         var name = Path.GetFileName(file);
         ShowCustomMessageBox(
            $"{EditorViewModel.ApplicationName} tried to run{nl}{name}{nl}but there is no application associated with its file type.",
            showYesNoCancel: false, new ProcessModel("Show in Finder", "/" + file));
      }
   }

   public LoadedFile LoadFile(string fileName) {
      var snapshot = reloadSnapshot.Value;
      if (snapshot != null && Path.GetFullPath(snapshot.Name) == Path.GetFullPath(fileName))
         return new LoadedFile(fileName, (byte[])snapshot.Contents.Clone());
      if (!File.Exists(fileName)) return null;
      byte[] data;
      try {
         using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
         data = new byte[stream.Length];
         stream.ReadExactly(data);
      } catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
         ShowCustomMessageBox("Could not read file. Check permissions or whether another application is locking the file.", false);
         return null;
      }

      return new LoadedFile(fileName, data);
   }

   public bool Save(LoadedFile file) {
      FileSubscription[] subscriptions;
      lock (watcherGate) subscriptions = watchers.TryGetValue(Path.GetFullPath(file.Name), out var list) ? list.ToArray() : Array.Empty<FileSubscription>();
      foreach (var subscription in subscriptions) subscription.Monitor.Suspend();
      var saved = false;
      try {
         AtomicFileWriter.Write(file.Name, stream => stream.Write(file.Contents, 0, file.Contents.Length));
         saved = true;
         return true;
      } catch (Exception) {
         ShowCustomMessageBox("Could not save. The previous file was not replaced. Check permissions, available space, or whether another application is locking the file.", showYesNoCancel: false);
         return false;
      } finally {
         foreach (var subscription in subscriptions) subscription.Monitor.Resume(saved ? file.Contents : null);
      }
   }

   public bool? TrySavePrompt(LoadedFile file) {
      var displayName = string.Empty;
      if (!string.IsNullOrEmpty(file.Name)) displayName = Environment.NewLine + file.Name;
      var result = ShowCustomMessageBox($"Would you like to save{displayName}?");
      if (result != true) return result;
      if (displayName == string.Empty) displayName = RequestNewName(displayName);
      if (string.IsNullOrEmpty(displayName)) return null;
      // IFileSystem reserves false for an intentional discard, not a failed save.
         return Save(new LoadedFile(displayName.Trim(), file.Contents)) ? true : null;
   }

   public string RequestNewName(string currentName, string extensionDescription = null, params string[] extensionOptions) =>
      PickFileToSave(extensionDescription, extensionOptions, Path.GetFileName(currentName ?? string.Empty));

   // ---------------- Metadata ----------------

   /// <summary>
   /// Core asks for app settings with MetadataFor(EditorViewModel.ApplicationName), which is a
   /// bare name rather than a path; WindowsFileSystem turns that into HexManiacAdvance.toml in
   /// the working directory. On macOS that belongs in Application Support instead. Anything with
   /// a directory or an extension is a real file, so its metadata stays beside it as a sibling
   /// .toml, which is what the WPF build reads and writes.
   /// </summary>
   string MetadataPath(string fileName) {
      if (string.IsNullOrEmpty(Path.GetDirectoryName(fileName)) && string.IsNullOrEmpty(Path.GetExtension(fileName))) {
         var directory = appSupportDirectory ?? AppSupportDirectory;
         Directory.CreateDirectory(directory);
         return Path.Combine(directory, fileName + ".toml");
      }
      return fileName.ToLower().EndsWith(".gba") ? Path.ChangeExtension(fileName, ".toml") : fileName + ".toml";
   }

   static bool IsAppLevelMetadata(string fileName) =>
      string.IsNullOrEmpty(Path.GetDirectoryName(fileName)) && string.IsNullOrEmpty(Path.GetExtension(fileName));

   public string[] MetadataFor(string fileName) {
      if (IsAppLevelMetadata(fileName)) {
         var appMetadata = ReadAppMetadata(fileName);
         return appMetadata.Length == 0 ? null : appMetadata;
      }
      var path = MetadataPath(fileName);
      try {
         return File.Exists(path) ? File.ReadAllLines(path) : null;
      } catch (UnauthorizedAccessException error) {
         // ROM metadata is not optional on a failed read: let the editor reject the open
         // rather than silently rebuilding and overwriting an unread sidecar.
         throw new IOException("Could not read ROM metadata: " + path, error);
      }
   }

   public bool SaveMetadata(string originalFileName, string[] metadata) {
      if (metadata == null) return true; // nothing to save
      if (IsAppLevelMetadata(originalFileName)) return WriteAppMetadata(originalFileName, metadata);

      var metadataName = MetadataPath(originalFileName);
      for (int tryCount = 1; ; tryCount++) {
         try {
            AtomicFileWriter.WriteAllLines(metadataName, metadata);
            return true;
         } catch (Exception ex) {
            if (tryCount >= 5) {
               ShowCustomMessageBox($"Failed to write {metadataName}:{Environment.NewLine}{ex.Message}.", showYesNoCancel: false);
               return false;
            }
            Thread.Sleep(100);
         }
      }
   }

   // ---------------- File watching ----------------

   private record FileSubscription(Action<IFileSystem> Listener, ExternalFileMonitor Monitor);
   private readonly object watcherGate = new();
   private readonly Dictionary<string, List<FileSubscription>> watchers = new(StringComparer.Ordinal);
   private bool watchersDisposed;

   public void AddListenerToFile(string fileName, Action<IFileSystem> listener) {
      var path = Path.GetFullPath(fileName);
      lock (watcherGate) {
         if (watchersDisposed) return;
         if (!watchers.TryGetValue(path, out var subscriptions)) watchers[path] = subscriptions = new();
         if (subscriptions.Any(s => s.Listener == listener)) return;
         var monitor = new ExternalFileMonitor(path, dispatchFileChange, data => {
            var previous = reloadSnapshot.Value;
            reloadSnapshot.Value = new LoadedFile(path, data);
            try { listener(this); }
            finally { reloadSnapshot.Value = previous; }
         });
         subscriptions.Add(new FileSubscription(listener, monitor));
      }
   }

   public void RemoveListenerForFile(string fileName, Action<IFileSystem> listener) {
      var path = Path.GetFullPath(fileName);
      FileSubscription subscription;
      lock (watcherGate) {
         if (!watchers.TryGetValue(path, out var subscriptions)) return;
         subscription = subscriptions.FirstOrDefault(s => s.Listener == listener);
         if (subscription == null) return;
         subscriptions.Remove(subscription);
         if (subscriptions.Count == 0) watchers.Remove(path);
      }
      subscription.Monitor.Dispose();
   }

   public void Dispose() {
      FileSubscription[] subscriptions;
      lock (watcherGate) {
         if (watchersDisposed) return;
         watchersDisposed = true;
         subscriptions = watchers.Values.SelectMany(s => s).ToArray();
         watchers.Clear();
      }
      foreach (var subscription in subscriptions) subscription.Monitor.Dispose();
   }

   // ---------------- Images ----------------

   public (short[] image, int width) LoadImage(string fileName = null) {
      fileName ??= PickFileToOpen("Image Files", new[] { "png" });
      if (fileName == null) return default;

      try {
         using var stream = File.OpenRead(fileName);
         return DecodeImage(stream);
      } catch (UnauthorizedAccessException) {
         var nl = Environment.NewLine;
         ShowCustomMessageBox($"Access Denied.{nl}Do you have read access to the file?");
         return default;
      } catch (IOException io) {
         ShowCustomMessageBox($"Error: {io.Message}.", false);
         return default;
      } catch (Exception) {
         ShowCustomMessageBox("Could not decode bitmap. The file may not be a valid PNG.", false);
         return default;
      }
   }

   public bool TryLoadIndexedImage(ref string fileName, out int[,] image, out IReadOnlyList<short> palette) {
      (image, palette) = (null, null);

      fileName ??= PickFileToOpen("Image Files", new[] { "png" });
      if (fileName == null) return false;

      try {
         (image, palette) = IndexedPng.Load(fileName);
      } catch (UnauthorizedAccessException) {
         return false;
      } catch (IOException) {
         return false;
      } catch (PngArgumentException) {
         return false;
      }

      return true;
   }

   public void ExportMapImage(short[] pixels, int width, string romPath, string mapName) {
      var exporter = new MapImageExporter(
         path => ShowOptions("Export Map Image", $"{Path.GetFileName(path)} already exists. What would you like to do?", null,
            new VisualOption { Index = 0, Option = "Overwrite", ShortDescription = "Replace the existing PNG." },
            new VisualOption { Index = 1, Option = "Save Updated Copy", ShortDescription = "Keep the existing PNG and save a numbered copy." }),
         name => PickFileToSave("PNG Image", new[] { "png" }, name),
         message => ShowCustomMessageBox(message, false));
      var saved = exporter.Export(romPath, mapName, stream => {
         using var bitmap = EncodeImage(pixels, width);
         bitmap.Save(stream);
      });
      if (saved != null) {
         var owner = getOwner();
         if (owner == null) Console.Error.WriteLine("Map image saved to:\n" + saved);
         else UiSync.Run(() => {
            var dialog = new Windows.MapImageSavedDialog(saved, () => FollowLink("/" + saved));
            return dialog.ShowDialog(owner);
         });
      }
   }

   public void SaveImage(short[] image, int width, string fileName = null) {
      fileName ??= PickFileToSave("Image Files", new[] { "png" });
      if (fileName == null) return;
      using var stream = File.Create(fileName);
      EncodeImage(image, width).Save(stream);
   }

   public void SaveImage(int[,] image, IReadOnlyList<short> palette, string fileName = null) {
      fileName ??= PickFileToSave("Image Files", new[] { "png" });
      if (fileName == null) return;
      IndexedPng.Save(fileName, image, palette);
   }

   /// <summary>
   /// WPF reads pixels straight out of a BitmapFrame in whatever format the PNG used.
   /// Skia decodes to a single 32-bit format instead, so this converts that to HMA's 5r5g5b.
   /// </summary>
   static (short[] image, int width) DecodeImage(Stream stream) {
      using var bitmap = WriteableBitmap.Decode(stream);
      var (width, height) = (bitmap.PixelSize.Width, bitmap.PixelSize.Height);
      var data = new short[width * height];

      using var frame = bitmap.Lock();
      var rowStart = frame.Address;
      var isRgba = frame.Format == PixelFormat.Rgba8888;
      var raw = new byte[frame.RowBytes];
      for (int y = 0; y < height; y++) {
         Marshal.Copy(rowStart, raw, 0, frame.RowBytes);
         for (int x = 0; x < width; x++) {
            var i = x * 4;
            var (r, g, b) = isRgba
               ? (raw[i + 0], raw[i + 1], raw[i + 2])
               : (raw[i + 2], raw[i + 1], raw[i + 0]); // Bgra8888, the Skia default
            data[y * width + x] = Convert(r, g, b);
         }
         rowStart += frame.RowBytes;
      }

      return (data, width);
   }

   static WriteableBitmap EncodeImage(short[] image, int width) {
      var height = image.Length / width;
      var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
      using (var frame = bitmap.Lock()) {
         var raw = new byte[frame.RowBytes];
         var rowStart = frame.Address;
         for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
               var color = image[y * width + x];
               var i = x * 4;
               raw[i + 2] = (byte)(((color >> 10) & 0x1F) * 255 / 31);
               raw[i + 1] = (byte)(((color >> 5) & 0x1F) * 255 / 31);
               raw[i + 0] = (byte)(((color >> 0) & 0x1F) * 255 / 31);
               raw[i + 3] = 0xFF;
            }
            Marshal.Copy(raw, 0, rowStart, frame.RowBytes);
            rowStart += frame.RowBytes;
         }
      }
      return bitmap;
   }

   static short Convert(byte r, byte g, byte b) => (short)(((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3));

   // ---------------- Dialogs ----------------

   public int ShowOptions(string title, string prompt, IReadOnlyList<IReadOnlyList<object>> additionalDetails, params VisualOption[] options) =>
      UiSync.Run(async () => {
         var owner = getOwner();
         if (owner == null) return -1;
         // Mirrors WindowsFileSystem: fill the ported OptionDialog and read back its Result.
         var dialog = new Windows.OptionDialog { Title = title };
         dialog.Prompt.Text = prompt;
         dialog.AdditionalDetails.ItemsSource = additionalDetails;
         dialog.Options.ItemsSource = new ObservableCollection<VisualOption>(options);
         await dialog.ShowDialog(owner);
         return dialog.Result;
      });

   public string RequestText(string title, string prompt) =>
      UiSync.Run(async () => {
         var owner = getOwner();
         if (owner == null) return null;
         var dialog = new Windows.RequestTextDialog { Title = title };
         dialog.Prompt.Text = prompt;
         await dialog.ShowDialog(owner);
         return dialog.Result;
      });

   /// <returns>true for Yes/OK, false for No, null for Cancel (same contract as the WPF build).</returns>
   public bool? ShowCustomMessageBox(string message, bool showYesNoCancel = true, params ProcessModel[] links) {
      if (getOwner() == null) {
         // Core can ask this before the window exists (e.g. while loading app settings).
         Console.Error.WriteLine(message);
         return null;
      }
      return UiSync.Run(() => MessageBoxDialog(message, showYesNoCancel, links));
   }

   Task<bool?> MessageBoxDialog(string message, bool showYesNoCancel, ProcessModel[] links) {
      var dialog = new Window {
         Title = EditorViewModel.ApplicationName,
         Width = 480,
         SizeToContent = SizeToContent.Height,
         CanResize = false,
         WindowStartupLocation = WindowStartupLocation.CenterOwner,
      };

      var body = new StackPanel {
         Margin = new Thickness(20),
         Spacing = 12,
         Children = { new SelectableTextBlock { Text = message, TextWrapping = TextWrapping.Wrap } },
      };

      foreach (var link in links ?? new ProcessModel[0]) {
         var button = new Button { Content = link.DisplayText, Classes = { "link" }, HorizontalAlignment = HorizontalAlignment.Left };
         var content = link.Content;
         button.Click += (_, _) => FollowLink(content);
         body.Children.Add(button);
      }

      var buttons = new StackPanel {
         Orientation = Orientation.Horizontal,
         HorizontalAlignment = HorizontalAlignment.Right,
         Spacing = 8,
      };
      if (showYesNoCancel) {
         var yes = new Button { Content = "Yes", IsDefault = true, MinWidth = 70 };
         var no = new Button { Content = "No", MinWidth = 70 };
         var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 70 };
         yes.Click += (_, _) => dialog.Close((bool?)true);
         no.Click += (_, _) => dialog.Close((bool?)false);
         cancel.Click += (_, _) => dialog.Close((bool?)null);
         buttons.Children.Add(cancel);
         buttons.Children.Add(no);
         buttons.Children.Add(yes);
      } else {
         var ok = new Button { Content = "OK", IsDefault = true, IsCancel = true, MinWidth = 70 };
         ok.Click += (_, _) => dialog.Close((bool?)true);
         buttons.Children.Add(ok);
      }
      body.Children.Add(buttons);

      dialog.Content = body;
      // Closing the window with no choice means Cancel, which is what WPF's default does too.
      return dialog.ShowDialog<bool?>(getOwner());
   }

   void FollowLink(string content) {
      try {
         if (content.StartsWith("~")) {
            CopyText = content.Substring(1);
         } else if (content.StartsWith("!") || content.StartsWith("/")) {
            // WPF opens the Windows file-properties dialog; the macOS equivalent is Reveal in Finder.
            Process.Start(new ProcessStartInfo("open", $"-R \"{content.Substring(1)}\"") { UseShellExecute = false });
         } else {
            OpenInDefaultApp(content);
         }
      } catch (Exception) {
         ShowCustomMessageBox($"Could not start '{content}'.", showYesNoCancel: false);
      }
   }

   // ==================================================================================
   // IWorkDispatcher
   // HexManiac.WPF passes its file system as both services (App.xaml.cs), so Core can move
   // long jobs like ROM parsing off the UI thread. Without this Core falls back to
   // InstantDispatch, which runs that work inline and freezes the window.
   // ==================================================================================

   public async Task WaitForRenderingAsync() => await new InlineDispatch(this);

   public void BlockOnUIWork(Action action) {
      if (Dispatcher.UIThread.CheckAccess()) {
         action();
      } else {
         Dispatcher.UIThread.Invoke(action, DispatcherPriority.Normal);
      }
   }

   public Task DispatchWork(Action action) => Task.Run(() => {
      try {
         Dispatcher.UIThread.Invoke(action, DispatcherPriority.Input);
      } catch (TaskCanceledException) {
         // the app is shutting down; nothing to do
      }
   });

   public Task RunBackgroundWork(Action action) => Task.Run(action);

   public IDelayWorkTimer CreateDelayTimer() => new DelayWorkTimer();
}
