using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;

internal static class RealBlocksetReview {
   public static void Run(string path) {
      int checks = 0;
      void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
      var bytes = File.ReadAllBytes(path);
      var sidecar = Path.ChangeExtension(path, ".toml");
      var metadata = File.ReadAllBytes(sidecar);
      // Stub storage: all new maps and selections exist only in this model, never on disk.
      var fs = new StubFileSystem { MetadataFor = file => File.Exists(Path.ChangeExtension(file, ".toml"))
         ? File.ReadAllLines(Path.ChangeExtension(file, ".toml")) : Array.Empty<string>() };
      var editor = new EditorViewModel(fs, InstantDispatch.Instance);
      Application.Current.Resources["FileSystem"] = fs;
      App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
      var window = new MainWindow { Width = 1440, Height = 1000, Editor = editor, FileSystem = fs };
      window.Resources["FileSystem"] = fs;
      window.Show();
      try {
         void PumpUntil(Func<bool> ready) {
            var clock = Stopwatch.StartNew();
            while (!ready()) {
               Dispatcher.UIThread.RunJobs();
               if (clock.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException("Project initialization timed out.");
               Thread.Sleep(1);
            }
         }
         void Layout() {
            Dispatcher.UIThread.RunJobs();
            window.Measure(new Size(1440, 1000)); window.Arrange(new Rect(0, 0, 1440, 1000));
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
         }
         var opening = editor.OpenFileAsync(new LoadedFile(path, bytes.ToArray()));
         PumpUntil(() => opening.IsCompleted);
         var viewport = opening.GetAwaiter().GetResult();
         PumpUntil(() => viewport.InitializationWorkload.IsCompleted && viewport.Model.InitializationWorkload.IsCompleted && viewport.MapEditor != null);
         viewport.Goto.Execute("maps.bank3.PALLET TOWN.3-0");
         var mapper = viewport.MapEditor;
         editor.Add(mapper); mapper.ShowHeaderPanel = true; Layout();
         var view = window.GetVisualDescendants().OfType<MapTab>().Single();
         void VisiblePointers() {
            Layout();
            foreach (var (name, field) in new[] { ("PrimaryBlocksetSelector", Format.PrimaryBlockset), ("SecondaryBlocksetSelector", Format.SecondaryBlockset) }) {
               var combo = view.FindControl<AngleComboBox>(name)!;
               var address = mapper.PrimaryMap.GetLayout().GetAddress(field);
               Check(combo.SelectedItem is BlocksetOption option && option.Address == address, name + " must display the committed pointer after navigation.");
               Check(combo.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text?.StartsWith(address.ToString("X6")) == true), name + " must render the address text.");
            }
         }
         VisiblePointers();
         var pallet = mapper.PrimaryMap;
         var palletBlockCount = pallet.BlockRenders.Count;
         var primary = pallet.GetLayout().GetAddress(Format.PrimaryBlockset);
         var secondary = pallet.GetLayout().GetAddress(Format.SecondaryBlockset);
         var beforeNavigation = viewport.Model.RawData.ToArray();
         mapper.NavigateTo(3, 1, 0, 0); VisiblePointers();
         mapper.NavigateTo(3, 0, 0, 0); VisiblePointers();
         Check(beforeNavigation.SequenceEqual(viewport.Model.RawData), "Navigation and restoring selection must not edit ROM bytes.");
         // Exercise the same creation method used by connected/warp map creation.
         var create = typeof(BlockMapViewModel).GetMethod("CreateNewMap", BindingFlags.Instance | BindingFlags.NonPublic)!;
         var newMap = (BlockMapViewModel)create.Invoke(mapper.PrimaryMap, new object[] { viewport.ChangeHistory.CurrentChange, 3, 9, 9 })!;
         Check(newMap != null, "A new map is created in memory.");
         mapper.NavigateTo(newMap.MapID / 1000, newMap.MapID % 1000, 0, 0); VisiblePointers();
         Check(newMap.GetLayout().GetAddress(Format.PrimaryBlockset) == primary && newMap.GetLayout().GetAddress(Format.SecondaryBlockset) == secondary,
            "The new map inherits both Pallet blocksets.");
         var header = mapper.PrimaryMap.Header;
         var selector = view.FindControl<AngleComboBox>("SecondaryBlocksetSelector")!;
         var other = header.SecondaryOptions.Select((option, index) => (option, index)).First(pair => pair.option.Address != secondary);
         var beforeSelection = viewport.Model.RawData.ToArray();
         selector.SelectedIndex = other.index; Layout(); VisiblePointers();
         Check(newMap.GetLayout().GetAddress(Format.SecondaryBlockset) == other.option.Address, "Secondary selection updates the newly created map.");
         var expected = beforeSelection.ToArray();
         BitConverter.GetBytes(other.option.Address + 0x08000000).CopyTo(expected, newMap.GetLayout().Start + 20);
         Check(expected.SequenceEqual(viewport.Model.RawData), "Selection changes only the new map's four-byte secondary pointer.");
         selector.SelectedIndex = header.SecondaryOptions.Select((option, index) => (option, index)).Single(pair => pair.option.Address == secondary).index;
         Layout(); VisiblePointers();
         Check(beforeSelection.SequenceEqual(viewport.Model.RawData), "Pallet's secondary blockset can be restored exactly.");
         Check(newMap.BlockRenders.SelectMany(render => render.PixelData).Distinct().Count() > 1, "The new map's selectable blocks contain rendered graphics.");
         Check(bytes.SequenceEqual(File.ReadAllBytes(path)) && metadata.SequenceEqual(File.ReadAllBytes(sidecar)), "Original project files remain unchanged.");
         Console.WriteLine($"BLOCK_PALETTE_DIAGNOSTIC:pallet={palletBlockCount},new_map={newMap.BlockRenders.Count}");
         Console.WriteLine($"REAL_BLOCKSET_SELECTION_PASS:primary={header.PrimaryOptions.Count},secondary={header.SecondaryOptions.Count}");
         Console.WriteLine($"CHECKS_PASSED:{checks}");
      } finally { window.Close(); }
   }
}
