using Avalonia;
using Avalonia.Headless;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Performance;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.Models;

AppBuilder.Configure<Application>().UseStandardRuntimePlatformSubsystem()
   .UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var host = new Window { Width = 1200, Height = 800 }; host.Show();
var checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
void PumpUntil(Func<bool> predicate) {
   var clock = Stopwatch.StartNew();
   while (!predicate()) {
      Dispatcher.UIThread.RunJobs();
      if (clock.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Map presentation timed out.");
      Thread.Yield();
   }
   Dispatcher.UIThread.RunJobs();
}
byte[] BitmapBytes(PixelImage image) {
   var bitmap = (WriteableBitmap)image.Source;
   using var frame = bitmap.Lock();
   var bytes = new byte[bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4];
   for (int y = 0; y < bitmap.PixelSize.Height; y++)
      Marshal.Copy(frame.Address + y * frame.RowBytes, bytes, y * bitmap.PixelSize.Width * 4, bitmap.PixelSize.Width * 4);
   return bytes;
}
foreach (var (width, height) in new[] { (16, 16), (48, 48), (96, 64) }) {
   var fixture = new SyntheticMapFixture(width, height);
   var pixels = fixture.Map.PixelData.ToArray();
   Check(pixels.Length == width * height * 256, "Map dimensions must match actual renderer output.");
   Check(pixels.Distinct().Count() >= 2, "Synthetic map must contain two distinct real rendered blocks.");
   var bytes = fixture.Model.RawData.ToArray();
   var metadata = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize();
   var image = new PixelImage { DataContext = fixture.Map };
   host.Content = image;
   var revealMask = new RadialGradientBrush();
   image.OpacityMask = revealMask;
   PumpUntil(() => image.Source is WriteableBitmap);
   var initial = image.Source;
   var initialBitmap = BitmapBytes(image);
   var uploads = 0;
   image.PropertyChanged += (_, e) => { if (e.Property == Image.SourceProperty) uploads++; };
   foreach (var scale in new[] { 2d, .5, 4, 1 }) {
      fixture.Map.SpriteScale = scale;
      PumpUntil(() => image.Width == width * 16 * scale);
      Check(image.Height == height * 16 * scale, "Zoom must update both layout dimensions.");
      Check(ReferenceEquals(initial, image.Source), "Zoom must resize the map without rebuilding its bitmap.");
   }
   Check(uploads == 0, "Zoom-only notifications must publish no new bitmaps.");
   Check(initialBitmap.SequenceEqual(BitmapBytes(image)), "Zoom must preserve every displayed BGRA byte.");
   Check(ReferenceEquals(revealMask, image.OpacityMask), "Map-reveal masks must survive zoom.");
   Check(bytes.SequenceEqual(fixture.Model.RawData), "Presentation must not modify synthetic map bytes.");
   Check(metadata.SequenceEqual(fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize()),
      "Presentation must preserve pointer/anchor metadata.");
   Check(pixels.SequenceEqual(fixture.Map.PixelData), "Zoom must preserve exact map pixels.");
   Check(!fixture.ViewPort.Undo.CanExecute(null), "Zoom must not create undo history.");

   image.ScaleAffectsLayout = false;
   Check(image.Width == width * 16 && image.Height == height * 16, "Layout opt-out must restore unscaled dimensions.");
   fixture.Map.SpriteScale = 3;
   Dispatcher.UIThread.RunJobs();
   Check(image.Width == width * 16 && image.Height == height * 16 && uploads == 0,
      "Zoom with external scaling must not change layout or republish pixels.");
   image.ScaleAffectsLayout = true;
   Check(image.Width == width * 48 && image.Height == height * 48 && uploads == 0,
      "Re-enabling layout scaling must resize without republishing pixels.");

   // Use the real map edit and undo/redo paths, not a simulated pixel edit.
   var target = 16 * fixture.Map.PixelWidth + 32;
   var pointerFields = fixture.Model.RawData[0x100..0x1B8];
   var oldSource = image.Source;
   fixture.Map.DrawBlock(fixture.ViewPort.ChangeHistory.CurrentChange, 0, -1, 2, 1);
   fixture.ViewPort.ChangeHistory.ChangeCompleted();
   fixture.Map.SpriteScale = 2; // a layout update must not cancel the pending pixel update
   PumpUntil(() => !ReferenceEquals(oldSource, image.Source));
   Check(fixture.Map.PixelData[target] == pixels[0] && fixture.Map.PixelData[target] != pixels[target],
      "Painting must change the intended block to its real rendered color.");
   Check(!BitmapBytes(image).SequenceEqual(initialBitmap), "A real edit must still upload changed pixels.");
   Check(fixture.Model.RawData[SyntheticMapFixture.Cells + (width + 2) * 2] == 0,
      "Painting must write the intended map cell.");
   Check(pointerFields.SequenceEqual(fixture.Model.RawData[0x100..0x1B8]), "Painting must leave synthetic header/event pointers intact.");
   fixture.ViewPort.Undo.Execute(null);
   fixture.Map.ClearCaches(); // fixture Map is intentionally outside the native map-tab lifecycle
   Check(bytes.SequenceEqual(fixture.Model.RawData), "Undo must restore all fixture bytes.");
   Check(pixels.SequenceEqual(fixture.Map.PixelData), "Undo must restore the exact rendered map.");
   fixture.ViewPort.Redo.Execute(null);
   fixture.Map.ClearCaches();
   Check(fixture.Map.PixelData[target] == pixels[0], "Redo must restore the painted block.");
   var beforeSelectionBytes = fixture.Model.RawData.ToArray();
   var beforeSelectionPixels = fixture.Map.PixelData.ToArray();
   oldSource = image.Source;
   fixture.Map.IsSelected = true;
   PumpUntil(() => !ReferenceEquals(oldSource, image.Source));
   Check(!beforeSelectionPixels.SequenceEqual(fixture.Map.PixelData), "Selection overlays must still refresh pixel content.");
   Check(beforeSelectionBytes.SequenceEqual(fixture.Model.RawData), "Selection overlays must not modify map data.");
}

// Shared PixelImage also serves ordinary sprite canvases; preserve that use and update routing.
var canvas = new CanvasPixelViewModel(16, 8);
var sprite = new PixelImage { DataContext = canvas };
host.Content = sprite;
var spriteSource = sprite.Source;
canvas.SpriteScale = 4;
Check(sprite.Width == 64 && sprite.Height == 32 && ReferenceEquals(spriteSource, sprite.Source),
   "Ordinary sprite scaling must also reuse pixels.");
sprite.ShowDebugGrid = true;
Check(!ReferenceEquals(spriteSource, sprite.Source), "Debug-grid changes must still regenerate pixels.");
spriteSource = sprite.Source;
sprite.UseTrueTransparency = true;
Check(!ReferenceEquals(spriteSource, sprite.Source), "Transparency changes must still regenerate pixels.");

// Zoom before the first background map frame arrives must retain that pending frame.
var pendingFixture = new SyntheticMapFixture(16, 16);
var pendingImage = new PixelImage { DataContext = pendingFixture.Map };
host.Content = pendingImage;
pendingFixture.Map.SpriteScale = 3;
PumpUntil(() => pendingImage.Source is WriteableBitmap && pendingImage.Width == 768);
Check(BitmapBytes(pendingImage).Any(value => value != 0), "Pending pixel work must survive a zoom-only notification.");
var replacementFixture = new SyntheticMapFixture(8, 8);
var replacementImage = new PixelImage { DataContext = pendingFixture.Map };
host.Content = replacementImage;
replacementImage.DataContext = replacementFixture.Map;
PumpUntil(() => replacementImage.Source is WriteableBitmap && replacementImage.Width == 128);
var replacementSource = replacementImage.Source;
pendingFixture.Map.SpriteScale = 8;
Dispatcher.UIThread.RunJobs();
Check(replacementImage.Width == 128 && ReferenceEquals(replacementSource, replacementImage.Source),
   "An old map must not affect the image after a context switch.");
Check(BitmapBytes(replacementImage).Length == 128 * 128 * 4, "A pending old frame must not replace the current map's dimensions.");
host.Content = null; host.Close();
Console.WriteLine("PASS: actual synthetic map rendering, zoom bitmap reuse, exact pixels/bytes/metadata, painting, undo/redo, masks, and pending updates.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
