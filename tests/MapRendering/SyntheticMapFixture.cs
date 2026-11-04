using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;

namespace HavenSoft.HexManiac.Performance;

// Fabricated addresses and graphics only. Exercise the real map renderer and edit history;
// this is not a game ROM and never uses or changes researched game addresses.
public sealed class SyntheticMapFixture {
   public const int Cells = 0x10000;
   public PokemonModel Model { get; }
   public ViewPort ViewPort { get; }
   public BlockMapViewModel Map { get; }
   public SyntheticMapFixture(int width, int height, bool withScriptEvent = false, bool withNpcText = false) {
      var data = new byte[Math.Max(0x20000, Cells + width * height * 2)];
      void Word(int address, int value) => BitConverter.GetBytes(value).CopyTo(data, address);
      void Pointer(int address, int target) => Word(address, 0x08000000 + target);
      Pointer(0x100, 0x110); Pointer(0x110, 0x120); Pointer(0x120, 0x140);
      Word(0x140, width); Word(0x144, height);
      Pointer(0x148, 0x900); Pointer(0x14C, Cells);
      Pointer(0x150, 0x180); Pointer(0x154, 0x1A0);
      foreach (var (set, tiles, palette, blocks, attributes) in new[] {
         (0x180, 0x1000, 0x800, 0x6000, 0x6020), (0x1A0, 0x5000, 0xA00, 0x6100, 0x6120),
      }) {
         data[set + 1] = (byte)(set == 0x180 ? 0 : 1);
         Pointer(set + 4, tiles); Pointer(set + 8, palette);
         Pointer(set + 12, blocks); Pointer(set + 16, attributes);
         Array.Fill(data, (byte)0x11, tiles, 32);
         Array.Fill(data, (byte)0x22, tiles + 32, 32);
         data[palette + 2] = 0x1F; data[palette + 4] = 0xE0; data[palette + 5] = 3;
         for (int i = 0; i < 8; i++) data[blocks + 16 + i * 2] = 1;
      }
      for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
         data[Cells + (y * width + x) * 2] = (byte)((x + y) % 2);
      const string blocksetFormat = "[isCompressed. isSecondary. padding: tileset<> pal<> blockset<> attributes<> animation<>]1";
      var layoutFormat = $"[width:: height:: borderblock<> blockmap<> blockdata1<{blocksetFormat}> blockdata2<{blocksetFormat}>]1";
      var headerFormat = $"[layout<{layoutFormat}> connections<> events<> mapscripts<>]1";
      if (withScriptEvent) {
         Pointer(0x128, 0x160); data[0x162] = 1; Pointer(0x16C, 0x16000);
         headerFormat = $"[layout<{layoutFormat}> connections<> events<[objectCount. warpCount. scriptCount. signpostCount. objects<> warps<> scripts<[x: y: elevation: trigger:|h index:: script<`xse`>]/scriptCount> signposts<>]1> mapscripts<>]1";
      }
      if (withNpcText) {
         Pointer(0x128, 0x160); data[0x160] = 1; Pointer(0x164, 0x17000);
         data[0x17000] = 1; Word(0x17004, 2); Word(0x17008, 0);
         Pointer(0x17010, 0x18000);
         new byte[] { 0x0F, 0, 0, 0x90, 1, 8, 0x16, 1, 0x40, 1, 0, 2 }.CopyTo(data, 0x18000);
         PCSString.Convert("\"Discovered NPC text.\"").CopyTo(data, 0x19000);
         // Keep script/text pointers unformatted so lazy event discovery must annotate them.
         const string objects = "[id. graphics. unused:1 x:|z y:|z elevation.11 moveType. range:|t|x::|y:: trainerType: trainerRangeOrBerryID: script<> flag:|h padding:]/objectCount";
         headerFormat = $"[layout<{layoutFormat}> connections<> events<[objectCount. warpCount. scriptCount. signpostCount. objects<{objects}> warps<> scripts<> signposts<>]1> mapscripts<>]1";
      }
      var bankFormat = $"[map<{headerFormat}>]1";
      var metadata = new StoredMetadata(anchors: new[] {
         new StoredAnchor(0x100, "data.maps.banks", $"[maps<{bankFormat}>]1"),
         new StoredAnchor(0x110, "synthetic.bank", bankFormat),
         new StoredAnchor(0x120, "synthetic.header", headerFormat),
         new StoredAnchor(0x140, "synthetic.layout", layoutFormat),
         new StoredAnchor(0x180, "synthetic.primary", blocksetFormat),
         new StoredAnchor(0x1A0, "synthetic.secondary", blocksetFormat),
      });
      var singletons = new Singletons();
      Model = new PokemonModel(data, metadata, singletons);
      ViewPort = new ViewPort("synthetic-map", Model, InstantDispatch.Instance, singletons, fs: new StubFileSystem());
      Map = new BlockMapViewModel(new StubFileSystem(), new MapTutorialsViewModel(), ViewPort,
         new Format(Model), null, 0, 0) { ShowEvents = MapDisplayOptions.NoEvents, IncludeBorders = false };
   }
}
