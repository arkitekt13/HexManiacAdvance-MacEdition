using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
Application.Current!.Resources["FileSystem"] = new StubFileSystem();
var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
string Hash(short[] data) { var bytes = new byte[data.Length * 2]; Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length); return Convert.ToHexString(SHA256.HashData(bytes)); }
VisualComboOption Image(PortraitFixture f) => (VisualComboOption)f.Field.FilteringComboOptions.AllOptions[0];
VisualComboOption Fresh(PortraitFixture f) => (VisualComboOption)f.Candidate().FilteringComboOptions.AllOptions[0];
void Layout(TabView view) { Dispatcher.UIThread.RunJobs(); view.Measure(new Size(1000, 700)); view.Arrange(new Rect(0, 0, 1000, 700)); }
string BitmapHash(PixelImage image) {
   using var frame = ((WriteableBitmap)image.Source).Lock();
   var bytes = new byte[frame.RowBytes * frame.Size.Height]; Marshal.Copy(frame.Address, bytes, 0, bytes.Length);
   return Convert.ToHexString(SHA256.HashData(bytes));
}
foreach (var compressed in new[] { false, true }) foreach (var operation in new[] { "sprite", "palette", "repoint", "reload" }) {
   var fixture = new PortraitFixture(16, compressed);
   if (operation == "repoint") {
      var token = new ModelDelta(); fixture.Model.WritePointer(token, 0x2800, fixture.Model.ReadPointer(PortraitFixture.Pointers));
      fixture.Model.ObserveRunWritten(token, new PointerRun(0x2800)); fixture.ViewPort.Refresh();
   }
   var before = fixture.Model.RawData.ToArray(); var originalHash = Hash(Image(fixture).PixelData);
   var view = new TabView { DataContext = fixture.ViewPort };
   var window = new Window { Width = 1000, Height = 700, Content = view }; window.Show(); Layout(view);
   PixelImage SelectedImage() {
      var images = view.GetVisualDescendants().OfType<PixelImage>().ToArray();
      var result = images.FirstOrDefault(image => ReferenceEquals(image.DataContext, Image(fixture)));
      if (result == null) throw new InvalidOperationException($"Missing selected image: {compressed}/{operation}, VM selection {fixture.Field.FilteringComboOptions.SelectedIndex}, image contexts {string.Join(",", images.Select(i => i.DataContext?.GetType().Name + ":" + (i.DataContext as ComboOption)?.Index))}; combos {string.Join(",", view.GetVisualDescendants().OfType<AngleComboBox>().Select(c => c.SelectedIndex + ":" + ReferenceEquals(c.DataContext, fixture.Field.FilteringComboOptions)))}");
      return result;
   }
   var originalBitmapHash = BitmapHash(SelectedImage());
   var choices = fixture.Field.FilteringComboOptions.AllOptions;
   fixture.ViewPort.Refresh(); Layout(view);
   Check(ReferenceEquals(choices, fixture.Field.FilteringComboOptions.AllOptions), "Unchanged pixels must retain choices.");
   if (operation == "palette") {
      var palette = (IPaletteRun)fixture.Model.GetNextRun(PortraitFixture.Palette);
      var colors = palette.GetPalette(fixture.Model, 0).ToArray();
      for (int i = 1; i < colors.Length; i++) colors[i] = (short)(31 << 5);
      fixture.Model.ObserveRunWritten(fixture.ViewPort.CurrentChange, palette.SetPalette(fixture.Model, fixture.ViewPort.CurrentChange, 0, colors));
   } else if (operation == "reload") {
      var data = fixture.Model.RawData.ToArray();
      // Build the changed file through the real sprite writer, then load that byte snapshot.
      var pixels = new int[64, 64]; for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) pixels[x, y] = 9;
      fixture.Model.ObserveRunWritten(fixture.ViewPort.CurrentChange, fixture.Sprite().SetPixels(fixture.Model, fixture.ViewPort.CurrentChange, 0, pixels));
      data = fixture.Model.RawData.ToArray();
      var metadata = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo);
      fixture.ViewPort.ChangeHistory.ChangeCompleted(); fixture.ViewPort.Undo.Execute(null);
      fixture.Model.Load(data, metadata);
   } else {
      if (operation == "repoint") { fixture.ViewPort.RepointToNewCopy(PortraitFixture.Pointers); fixture.ViewPort.ChangeHistory.ChangeCompleted(); }
      var pixels = new int[64, 64]; for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) pixels[x, y] = 9;
      fixture.Model.ObserveRunWritten(fixture.ViewPort.CurrentChange, fixture.Sprite().SetPixels(fixture.Model, fixture.ViewPort.CurrentChange, 0, pixels));
   }
   fixture.ViewPort.ChangeHistory.ChangeCompleted(); fixture.ViewPort.Refresh(); Layout(view);
   Check(Hash(Fresh(fixture).PixelData) != originalHash, "Edit must change actual rendered portrait pixels.");
   Check(Hash(Image(fixture).PixelData) == Hash(Fresh(fixture).PixelData), "Live portrait must refresh even when labels are unchanged.");
   Check(BitmapHash(SelectedImage()) != originalBitmapHash, "The actual bound bitmap must change after editing.");
   Check(fixture.Field.FilteringComboOptions.ModelValue == 0 && fixture.Model[PortraitFixture.Parents] == 0, "Refreshing visuals must preserve selection and ROM enum value.");
   var combo = view.GetVisualDescendants().OfType<AngleComboBox>().Single(control => ReferenceEquals(control.DataContext, fixture.Field.FilteringComboOptions));
   combo.IsDropDownOpen = true; Layout(view); combo.IsDropDownOpen = false; Layout(view);
   Check(combo.SelectedIndex == 0 && Hash(Image(fixture).PixelData) == Hash(Fresh(fixture).PixelData), "Popup reopen/close must preserve fresh images and selection.");
   var after = fixture.Model.RawData.ToArray();
   if (operation != "reload") {
      for (int i = 0; fixture.ViewPort.Undo.CanExecute(null) && i < 10; i++) fixture.ViewPort.Undo.Execute(null);
      fixture.ViewPort.Refresh(); Layout(view);
      Check(before.SequenceEqual(fixture.Model.RawData) && Hash(Image(fixture).PixelData) == originalHash, "Undo must restore exact bytes and live portrait pixels.");
      for (int i = 0; fixture.ViewPort.Redo.CanExecute(null) && i < 10; i++) fixture.ViewPort.Redo.Execute(null);
      fixture.ViewPort.Refresh(); Layout(view);
      Check(after.SequenceEqual(fixture.Model.RawData) && Hash(Image(fixture).PixelData) == Hash(Fresh(fixture).PixelData), "Redo must restore edited bytes and fresh portraits.");
   }
   Check(fixture.Errors.Count == 0, "Freshness scenario raised editor errors.");
   window.Close(); Dispatcher.UIThread.RunJobs();
}
Console.WriteLine("PASS: compressed/uncompressed portrait edits, palettes, shared-pointer repointing, reload, live bitmaps, popup state, selection, and undo/redo.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
