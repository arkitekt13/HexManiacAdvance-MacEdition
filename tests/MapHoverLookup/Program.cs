using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Performance;
using System.Collections;
using System.Reflection;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
var seed = new SyntheticMapFixture(24, 20, withNpcText: true);
var data = seed.Model.RawData.ToArray();
void Word(int at, int value) => BitConverter.GetBytes(value).CopyTo(data, at);
void Ptr(int at, int target) => Word(at, 0x08000000 + target);
void Short(int at, int value) => BitConverter.GetBytes((ushort)value).CopyTo(data, at);
const string objects = "[id. graphics. unused:1 x:|z y:|z elevation.11 moveType. range:|t|x::|y:: trainerType: trainerRangeOrBerryID: script<> flag:|h padding:]/objectCount";
const string warps = "[x:|z y:|z elevation. warpID. map. bank.]/warpCount";
const string scripts = "[x:|z y:|z elevation: trigger:|h index:: script<>]/scriptCount";
const string signs = "[x:|z y:|z elevation. kind. unused: arg::|h]/signpostCount";
var anchors = seed.Model.ExportMetadata(null, seed.ViewPort.Singletons.MetadataInfo).NamedAnchors;
var layout = anchors.Single(a => a.Address == 0x140).Format;
var header = $"[layout<{layout}> connections<> events<[objectCount. warpCount. scriptCount. signpostCount. objects<{objects}> warps<{warps}> scripts<{scripts}> signposts<{signs}>]1> mapscripts<> regionSectionID.synthetic.regions]1";
var bank = $"[map<{header}>]1";
var metadata = new StoredMetadata(anchors: anchors.Where(a => a.Address != 0x100 && a.Address != 0x110 && a.Address != 0x120).Concat(new[] {
   new StoredAnchor(0x100, "data.maps.banks", $"[maps<{bank}>]4"),
   new StoredAnchor(0x110, "synthetic.bank", bank), new StoredAnchor(0x120, "synthetic.header", header),
   new StoredAnchor(0x17500, HardcodeTablesModel.FlySpawns, "[bank. map. x: y:]3"),
}).ToArray());
foreach (int at in new[] { 0x104, 0x108, 0x10C }) Ptr(at, 0x110);
data[0x160] = 3; data[0x161] = 2; data[0x162] = 2; data[0x163] = 2;
Ptr(0x168, 0x16200); Ptr(0x16C, 0x16100); Ptr(0x170, 0x16300);
// Preserve the first NPC and place a second on top, plus an unrelated NPC elsewhere.
for (int i = 1; i < 3; i++) {
   Array.Copy(data, 0x17000, data, 0x17000 + 24 * i, 24);
   data[0x17000 + 24 * i] = (byte)(i + 1);
}
Short(0x17000 + 48 + 4, 7); Short(0x17000 + 48 + 6, 6);
foreach (var (start, size) in new[] { (0x16200, 8), (0x16100, 16), (0x16300, 12) }) {
   Short(start, 2); Short(start + 2, 0); Short(start + size, 23); Short(start + size + 2, 19);
}
Ptr(0x1610C, 0x18000); Ptr(0x1611C, 0x18000);
data[0x16305] = data[0x16311] = 8; // hidden-item signposts, no pointer discovery
data[0x17500] = 3; Short(0x17502, 2); Short(0x17504, 0);
data[0x17506] = 3; Short(0x17508, 23); Short(0x1750A, 19);
data[0x1750C] = 0; Short(0x1750E, 2); // fly spawn on another bank must be excluded
var model = new PokemonModel(data, metadata, new Singletons());
model.SetList(new NoDataChangeDeltaModel(), "FacingOptions", "NoMovement");
model.SetList(new NoDataChangeDeltaModel(), "MapAttributeBehaviors", "Normal");
model.SetList(new NoDataChangeDeltaModel(), "synthetic.regions", "Synthetic region");
((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new("synthetic.layout", "data.maps.banks/3/maps/0/map/0/layout/", "Maps"));
var viewport = new ViewPort("hover-lookup.bin", model, InstantDispatch.Instance, new Singletons(), tutorials: new MapTutorialsViewModel(), fs: new StubFileSystem());
viewport.InitializationWorkload.GetAwaiter().GetResult();
var mapper = viewport.MapEditor; var map = mapper.PrimaryMap; mapper.HideEvents = false;
_ = map.PixelData;
string Identity(IEventViewModel ev) => $"{ev.GetType().Name}:{ev.Element.Start}:{ev.X},{ev.Y}";
var all = map.EventGroup.All.ToArray();
Check(all.Length == 11 && all.OfType<FlyEventViewModel>().Count() == 2, "Fixture includes all event kinds and map-specific fly spawns.");
var spriteReads = new SpriteReads(BlockMapViewModel.GetDefaultOW(model));
typeof(BlockMapViewModel).GetField("allOverworldSprites", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(map, spriteReads);
IReadOnlyList<IEventViewModel> At(int x, int y, bool select = false) {
   var p = mapper.ToMapPosition(map, x, y);
   return map.EventsUnderCursor(p.x + 8 * map.SpriteScale, p.y + 8 * map.SpriteScale, select);
}
spriteReads.Reads = 0;
Check(At(5, 5).Count == 0, "Empty tile returns no events.");
Check(spriteReads.Reads == 0, "Empty hover must not construct unrelated NPC sprite selectors.");
spriteReads.Reads = 0;
var stack = At(2, 0);
Check(stack.Select(Identity).SequenceEqual(all.Where(e => e.X == 2 && e.Y == 0).Select(Identity)), "Stacked objects, warp, script, signpost and fly spawn retain order and identity.");
int matchingReads = spriteReads.Reads;
spriteReads.Reads = 0; _ = At(7, 6);
Check(matchingReads > 0 && matchingReads == spriteReads.Reads * 2, "Only the two matching NPC editors are constructed on the stacked tile.");
foreach (bool borders in new[] { false, true }) foreach (double scale in new[] { .5, 1.0, 2.0 }) {
   map.IncludeBorders = borders; map.SpriteScale = scale;
   foreach (var (x, y) in new[] { (2, 0), (7, 6), (23, 19), (0, 0), (-1, -1), (24, 20) })
      Check(At(x, y).Select(Identity).SequenceEqual(all.Where(e => e.X == x && e.Y == y).Select(Identity)), "Border/scale/edge matching agrees with full event enumeration.");
}
map.IncludeBorders = false; map.SpriteScale = 1;
var selected = At(7, 6, true).Single();
Check(ReferenceEquals(map.SelectedEvent, selected), "Ordinary selection still selects its returned editor.");
_ = At(2, 0);
Check(ReferenceEquals(map.SelectedEvent, selected), "Hover does not replace the selected editor.");
mapper.HideEvents = true; spriteReads.Reads = 0;
Check(At(2, 0).Count == 0 && spriteReads.Reads == 0, "Hidden events do not allocate NPC editors.");
mapper.HideEvents = false;
viewport.ChangeHistory.ClearHistory(false);
var bytes = model.RawData.ToArray(); var stored = model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize();
var npc = At(7, 6).OfType<ObjectEventViewModel>().Single();
npc.X = 8; viewport.ChangeHistory.ChangeCompleted();
Check(At(7, 6).Count == 0 && At(8, 6).Single().Element.Start == npc.Element.Start, "Coordinate edit immediately changes lookup without a cache.");
viewport.Undo.Execute(null);
Check(At(7, 6).Count == 1 && At(8, 6).Count == 0 && viewport.Redo.CanExecute(null), "Undo restores lookup and hover preserves pending Redo.");
Check(bytes.SequenceEqual(model.RawData) && stored.SequenceEqual(model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()), "Hover after Undo preserves every byte and metadata.");
viewport.Redo.Execute(null);
Check(At(8, 6).Count == 1 && At(7, 6).Count == 0, "Redo restores the edited event position.");
viewport.Undo.Execute(null); viewport.ChangeHistory.ClearHistory(false);
for (int i = 0; i < 20; i++) { _ = At(5, 5); _ = At(2, 0); }
Check(bytes.SequenceEqual(model.RawData) && stored.SequenceEqual(model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()), "Repeated warm lookup preserves bytes and metadata.");
Check(!viewport.Undo.CanExecute(null) && !viewport.Redo.CanExecute(null), "Repeated warm lookup leaves clean history.");
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class SpriteReads(IPixelViewModel sprite) : IReadOnlyList<IPixelViewModel> {
   public int Reads;
   public int Count => 1;
   public IPixelViewModel this[int index] { get { Reads++; return sprite; } }
   public IEnumerator<IPixelViewModel> GetEnumerator() { yield return this[0]; }
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
