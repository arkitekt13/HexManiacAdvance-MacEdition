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
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory); Application.Current!.Resources["FileSystem"] = new StubFileSystem();
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
var fixture = new ItemFixture(16, 40, true); var model = fixture.Model;
// Occupied, formatted bytes immediately after the original description force normal relocation.
var oldDescription = model.GetNextRun(model.ReadPointer(fixture.FieldAddress("description")));
new ModelDelta().ChangeData(model, oldDescription.Start + oldDescription.Length, new byte[16]);
var guardError = ArrayRun.TryParse(model, "[guard.]16", oldDescription.Start + oldDescription.Length, SortedSpan<int>.None, out var guard);
Check(!guardError.HasError, "Could not format the description relocation guard.");
model.ObserveAnchorWritten(new NoDataChangeDeltaModel(), "benchmark.description.guard", guard);
fixture.ViewPort.Refresh();
var view = new TabView { DataContext = fixture.ViewPort }; var window = new Window { Width = 1440, Height = 1000, Content = view }; window.Show();
void Layout() { Dispatcher.UIThread.RunJobs(); view.Measure(new Size(1440, 1000)); view.Arrange(new Rect(0, 0, 1440, 1000)); }
Layout();
SpriteElementViewModel Sprite() => fixture.Members.OfType<SpriteElementViewModel>().Single();
PixelImage Icon() => view.GetVisualDescendants().OfType<PixelImage>().Single(image => ReferenceEquals(image.DataContext, Sprite()));
string BitmapHash() {
   using var frame = ((WriteableBitmap)Icon().Source).Lock(); var bytes = new byte[frame.RowBytes * frame.Size.Height];
   Marshal.Copy(frame.Address, bytes, 0, bytes.Length); return Convert.ToHexString(SHA256.HashData(bytes));
}
var sprite = Sprite(); var notifications = new Dictionary<string, int>();
sprite.PropertyChanged += (_, e) => notifications[e.PropertyName] = notifications.GetValueOrDefault(e.PropertyName) + 1;
var originalBitmap = BitmapHash();
for (int i = 0; i < 10; i++) { sprite.UpdateTiles(); Layout(); }
Check(notifications.GetValueOrDefault("PixelWidth") == 0 && notifications.GetValueOrDefault("PixelHeight") == 0, "Unchanged item dimensions must not request redundant bitmap uploads.");
Check(notifications.GetValueOrDefault("SpriteScale") == 0, "Unchanged item scale must not request layout.");
Check(notifications.GetValueOrDefault("PixelData") == 10, "Explicit refreshes must still publish fresh pixel data.");
Check(BitmapHash() == originalBitmap, "Refresh deduplication changed actual pixels.");

void EditAndHistory(Action edit, Action verify) {
   var before = model.RawData.ToArray(); edit(); fixture.ViewPort.ChangeHistory.ChangeCompleted(); Layout(); verify();
   var after = model.RawData.ToArray(); Check(!before.SequenceEqual(after), "Edit did not change ROM bytes.");
   for (int i = 0; fixture.ViewPort.Undo.CanExecute(null) && i < 10; i++) fixture.ViewPort.Undo.Execute(null);
   Layout(); Check(before.SequenceEqual(model.RawData), "Undo must restore exact bytes/pointers.");
   for (int i = 0; fixture.ViewPort.Redo.CanExecute(null) && i < 10; i++) fixture.ViewPort.Redo.Execute(null);
   Layout(); Check(after.SequenceEqual(model.RawData), "Redo must restore exact edited bytes/pointers."); verify();
   for (int i = 0; fixture.ViewPort.Undo.CanExecute(null) && i < 10; i++) fixture.ViewPort.Undo.Execute(null);
   Layout(); Check(before.SequenceEqual(model.RawData), "Cleanup undo did not restore original bytes.");
}
TextBox FieldBox(string name) {
   var field = fixture.Members.OfType<FieldArrayElementViewModel>().Single(f => f.Name == name);
   return view.GetVisualDescendants().OfType<AngleTextBox>().Single(c => ReferenceEquals(c.DataContext, field)).GetTextBox();
}
EditAndHistory(() => FieldBox("price").Text = "250", () => Check(model.ReadMultiByteValue(fixture.FieldAddress("price"), 2) == 250, "Bound price edit targeted wrong bytes."));
EditAndHistory(() => FieldBox("name").Text = "TEST ITEM", () => Check(fixture.ViewPort.Tools.TableTool.CurrentElementSelector.DisplayText == "\"TEST ITEM\"", "Renamed item selector did not refresh."));
var originalPointer = model.ReadPointer(fixture.FieldAddress("description"));
var otherPointers = Enumerable.Range(1, 15).Select(i => model.ReadPointer(fixture.FieldAddress("description", i))).ToArray();
EditAndHistory(() => {
   var text = view.GetVisualDescendants().OfType<TextBox>().Single(c => c.Name == "StreamTextBox" && ReferenceEquals(c.DataContext, fixture.Description));
   text.Text = new string('B', 400);
}, () => {
   Check(model.ReadPointer(fixture.FieldAddress("description")) != originalPointer, "Description growth must use the normal repointing path.");
   Check(fixture.Description.Content == new string('B', 400), "Bound description content differs after expansion.");
   Check(otherPointers.SequenceEqual(Enumerable.Range(1, 15).Select(i => model.ReadPointer(fixture.FieldAddress("description", i)))), "Description expansion changed unrelated pointers.");
});
EditAndHistory(() => {
   var run = Sprite().GetRun(); var pixels = run.GetPixels(model, 0, 0); pixels[0, 0] = pixels[0, 0] == 9 ? 8 : 9;
   model.ObserveRunWritten(fixture.ViewPort.CurrentChange, run.SetPixels(model, fixture.ViewPort.CurrentChange, 0, pixels)); fixture.ViewPort.Refresh();
}, () => Check(BitmapHash() != originalBitmap, "Actual icon bitmap must reflect sprite edits."));
EditAndHistory(() => {
   var palette = (IPaletteRun)model.GetNextRun(model.ReadPointer(fixture.Icons.Start + 4)); var colors = palette.GetPalette(model, 0).ToArray();
   for (int i = 1; i < colors.Length; i++) colors[i] = 31 << 5;
   model.ObserveRunWritten(fixture.ViewPort.CurrentChange, palette.SetPalette(model, fixture.ViewPort.CurrentChange, 0, colors)); fixture.ViewPort.Refresh();
}, () => Check(BitmapHash() != originalBitmap, "Actual icon bitmap must reflect palette edits."));

// Reshape the same nine tiles: metadata-only changes must still publish dimensions/scale.
notifications.Clear(); var originalRun = Sprite().GetRun();
model.ObserveRunWritten(new NoDataChangeDeltaModel(), originalRun.Duplicate(new SpriteFormat(4, 9, 1, originalRun.SpriteFormat.PaletteHint)));
fixture.ViewPort.Refresh(); Layout();
Check(Sprite().PixelWidth == 72 && Sprite().PixelHeight == 8, "Changed sprite dimensions were lost.");
Check(notifications.GetValueOrDefault("PixelWidth") == 1 && notifications.GetValueOrDefault("PixelHeight") == 1, "Changed dimensions must each notify once.");
Check(Icon().Source is WriteableBitmap bitmap && bitmap.PixelSize == new PixelSize(72, 8), "Bound bitmap must resize after format changes.");
Check(notifications.GetValueOrDefault("SpriteScale") == 0, "This reshape retains scale two and must not announce a scale change.");
model.ObserveRunWritten(new NoDataChangeDeltaModel(), originalRun); fixture.ViewPort.Refresh(); Layout();
Check(BitmapHash() == originalBitmap, "Restoring the original format must restore the bound bitmap.");
// Exercise a genuine scale transition through the normal paged-view-model copy API.
notifications.Clear();
var wideData = Enumerable.Repeat((byte)0x11, 1024).ToArray();
new ModelDelta().ChangeData(model, 0x500000, LZRun.Compress(wideData).ToArray());
model.WritePointer(new ModelDelta(), fixture.Icons.Start, 0x500000);
var wideRun = new LzSpriteRun(new SpriteFormat(4, 32, 1, originalRun.SpriteFormat.PaletteHint), model, 0x500000, new SortedSpan<int>(fixture.Icons.Start));
model.ObserveRunWritten(new NoDataChangeDeltaModel(), wideRun);
var candidate = new SpriteElementViewModel(fixture.ViewPort, "sprite", wideRun.FormatString, wideRun.SpriteFormat, fixture.Icons.Start);
Check(Sprite().TryCopy(candidate), "Sprite presentation copy failed."); Sprite().UpdateTiles(); Layout();
Check(Sprite().SpriteScale == 1 && notifications.GetValueOrDefault("SpriteScale") == 1, "A genuine scale change must notify once.");
Check(Icon().Source is WriteableBitmap wideBitmap && wideBitmap.PixelSize == new PixelSize(256, 8), "Wide source must publish its actual bitmap dimensions.");
Check(fixture.Errors.Count == 0, "Item editing raised editor errors.");
window.Close();
Console.WriteLine("PASS: item icon notification deduplication, actual bitmaps/dimensions/scale, bound price/name/description edits, relocation, palettes, and exact-byte undo/redo.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
