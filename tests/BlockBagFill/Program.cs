using Avalonia;
using Avalonia.Threading;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<Application>().UseStandardRuntimePlatformSubsystem()
   .UseWindowingSubsystem(() => { }, "offscreen").UseSkia().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
const int width = 7, height = 5;
int Address(int x, int y) => SyntheticMapFixture.Cells + (y * width + x) * 2;

foreach (var originalPermission in new[] { 0, 1, 16, 63 })
foreach (var permission in new[] { -1, 0, 1, 16, 62 })
foreach (var bag in new[] { new List<int> { 1 }, new List<int> { 0, 1 } })
foreach (var origin in new[] { (x: 0, y: 0), (x: 4, y: 3) }) {
   var fixture = new SyntheticMapFixture(width, height);
   var (model, map, viewport) = (fixture.Model, fixture.Map, fixture.ViewPort);
   var seed = new ModelDelta();
   // A connected L touches two edges. Same block/different permissions and an
   // isolated exact match must remain untouched: region matching uses the full word.
   for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
      model.WriteMultiByteValue(Address(x, y), 2, seed, (originalPermission ^ 32) << 10);
   var region = new[] { origin, (origin.x + 1, origin.y), (origin.x + 2, origin.y), (origin.x + 2, origin.y + 1) };
   foreach (var (x, y) in region) model.WriteMultiByteValue(Address(x, y), 2, seed, originalPermission << 10);
   model.WriteMultiByteValue(Address(0, 4), 2, seed, originalPermission << 10);
   viewport.ChangeHistory.ClearHistory(false);
   var before = model.RawData.ToArray();
   var metadata = model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize();
   map.SpriteScale = 2; map.LeftEdge = 13; map.TopEdge = -17;
   var xPoint = map.LeftEdge + (origin.x + .5) * 16 * map.SpriteScale;
   var yPoint = map.TopEdge + (origin.y + .5) * 16 * map.SpriteScale;
   _ = map.PixelData; // Load the real block renders before validating the first stamp.
   // The actual double-click path stamps first, then fills neighboring matches.
   _ = map.GetBlock(xPoint, yPoint); // Begin a fresh stroke, as picking a brush does.
   map.DrawBlock(viewport.ChangeHistory.CurrentChange, 1, permission, origin.x, origin.y);
   map.PaintBlockBag(viewport.ChangeHistory.CurrentChange, bag, permission, xPoint, yPoint);
   viewport.ChangeHistory.ChangeCompleted();
   var after = model.RawData.ToArray();
   var expected = before.ToArray();
   foreach (var (x, y) in region) {
      var actual = model.ReadMultiByteValue(Address(x, y), 2);
      Check((actual >> 10) == (permission < 0 ? originalPermission : permission), $"Permission {permission}: fill must preserve/set all movement bits at ({x},{y}); actual={actual:X4}.");
      Check((x == origin.x && y == origin.y) ? (actual & 0x3FF) == 1 : bag.Contains(actual & 0x3FF), "Each replacement must belong to the bag; the first stamp keeps the chosen brush.");
      BitConverter.GetBytes((ushort)actual).CopyTo(expected, Address(x, y));
   }
   Check(expected.SequenceEqual(after), "Fill must preserve every byte outside its connected full-word region, including pointers and adjacent data.");
   Check(metadata.SequenceEqual(model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()), "Fill must preserve all metadata.");
   Check(viewport.Undo.CanExecute(null), "The complete stamp/fill gesture must be undoable.");

   byte[] saved = null; string[] savedMetadata = null;
   var fs = new StubFileSystem {
      Save = file => { saved = file.Contents.ToArray(); return true; },
      SaveMetadata = (_, lines) => { savedMetadata = lines.ToArray(); return true; },
   };
   void SaveAndReload(byte[] bytes) {
      Check(viewport.Save.CanExecute(fs), "Changed map must be saveable.");
      viewport.Save.Execute(fs);
      Check(viewport.ChangeHistory.IsSaved && saved.SequenceEqual(bytes), "Save must preserve exact bytes and mark history saved.");
      var reloaded = new PokemonModel(saved, new StoredMetadata(savedMetadata), viewport.Singletons);
      Check(reloaded.RawData.SequenceEqual(bytes), "Reload must preserve exact map bytes.");
      Check(metadata.SequenceEqual(reloaded.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()), "Reload must preserve metadata.");
   }
   SaveAndReload(after);
   viewport.Undo.Execute(null);
   Check(before.SequenceEqual(model.RawData) && !viewport.ChangeHistory.IsSaved, "One Undo after saving restores all original bytes and marks dirty.");
   SaveAndReload(before);
   Check(viewport.Redo.CanExecute(null), "Saving the undo must retain Redo.");
   viewport.Redo.Execute(null);
   Check(after.SequenceEqual(model.RawData) && !viewport.ChangeHistory.IsSaved, "Redo restores the exact random result, without rerolling.");
   SaveAndReload(after);
}
// Empty bags are inert, even following a valid initial stamp.
{
   var fixture = new SyntheticMapFixture(width, height);
   var (model, map, viewport) = (fixture.Model, fixture.Map, fixture.ViewPort);
   map.DrawBlock(viewport.ChangeHistory.CurrentChange, 1, -1, 0, 0);
   viewport.ChangeHistory.ChangeCompleted(); viewport.ChangeHistory.ClearHistory(false);
   var before = model.RawData.ToArray();
   map.PaintBlockBag(viewport.ChangeHistory.CurrentChange, new(), -1, map.LeftEdge + 8 * map.SpriteScale, map.TopEdge + 8 * map.SpriteScale);
   viewport.ChangeHistory.ChangeCompleted();
   Check(before.SequenceEqual(model.RawData) && !viewport.Undo.CanExecute(null), "Empty bag fill must change no bytes or history.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
