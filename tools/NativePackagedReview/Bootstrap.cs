using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Images;

static class Bootstrap {
   [MethodImpl(MethodImplOptions.NoInlining)]
   internal static void Run(string binaries) {
      // Deliberately do not enter App's ordinary desktop startup, IPC or normal profile.
      AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().SetupWithoutStarting();
      var app = Application.Current!;
      var root = Path.GetFullPath(Path.Combine(binaries, "../../.."));
      var profile = Path.Combine(root, "isolated profile");
      var window = new MainWindow { Width = 1200, Height = 800 };
      using var fs = new MacFileSystem(() => window, appSupportDirectory: profile);
      var startup = typeof(App).GetMethod("GetViewModel", BindingFlags.Static | BindingFlags.NonPublic)!;
      var editor = (EditorViewModel)startup.Invoke(null, new object[] { "", -1, fs, new[] { true, false } })!;
      App.UpdateThemeDictionary(app.Resources, editor.Theme);
      editor.Theme.PropertyChanged += (_, _) => App.UpdateThemeDictionary(app.Resources, editor.Theme);
      app.Resources["FileSystem"] = fs; window.Resources["FileSystem"] = fs;
      var palettes = new PaletteCollection(); palettes.SetContents(new short[16]);
      window.Resources["PaletteMixer"] = palettes;
      window.Resources["IsPaletteMixerExpanded"] = new EditableValue<bool>();
      window.FileSystem = fs; window.Editor = editor;
      // Only the preference location differs; chooser and launch use the packaged native implementation.
      typeof(MainWindow).GetField("externalEmulator", BindingFlags.Instance | BindingFlags.NonPublic)!
         .SetValue(window, new ExternalEmulatorLauncher(Path.Combine(profile, "external-emulator.txt"), fs.ChooseExternalEmulatorAsync));
      var stop = new CancellationTokenSource();
      window.Closed += (_, _) => stop.Cancel();
      window.Show();
      Dispatcher.UIThread.MainLoop(stop.Token);
   }
}
