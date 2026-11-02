using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

namespace HavenSoft.HexManiac.Performance;

// Fabricated binary data only; parent/team formats are the editor's existing definitions.
public sealed class SyntheticTrainerFixture {
   public const int Trainers = 0x10000, Teams = 0x30000, TeamSlot = 128, Rows = 768;
   public PokemonModel Model { get; }
   public ViewPort ViewPort { get; }
   public ITableRun ParentTable { get; }
   public List<string> Errors { get; } = new();
   public bool Sprites { get; }
   public bool Portraits { get; }
   public bool Mixed { get; }
   public int Members { get; }
   public byte StructType { get; }
   public SyntheticTrainerFixture(int members, byte structType, bool sprites, bool portraits = false, bool mixed = false) {
      (Members, StructType, Sprites, Mixed) = (members, structType, sprites, mixed);
      Portraits = portraits;
      var data = new byte[0x200000]; Array.Fill(data, (byte)0xFF);
      void Value(int address, int value, int length = 4) {
         for (int b = 0; b < length; b++) data[address + b] = (byte)(value >> (b * 8));
      }
      void Name(int address, int length, string text) {
         Array.Fill(data, (byte)0xFF, address, length);
         PCSString.Convert($"\"{text}\"").CopyTo(data, address);
      }
      void Names(int address, int count, int length, string prefix) {
         for (int i = 0; i < count; i++) Name(address + i * length, length, $"{prefix}{i:D4}");
      }
      Names(0x4000, 412, 11, "MON"); Names(0x6000, 512, 13, "MOVE");
      Names(0x8000, 512, 14, "ITEM"); Names(0xA000, 64, 13, "CLASS");
      Names(0xB000, 64, 12, "SPRITE");
      for (int i = 0; i < 64; i++) {
         Value(0xC000 + i * 4, i, 1); Value(0xC001 + i * 4, 10, 1); Value(0xC002 + i * 4, 0, 2);
      }
      for (int row = 0; row < Rows; row++) {
         var parent = Parent(row); var team = Teams + row * TeamSlot;
         var type = Type(row); var count = Count(row); var stride = (type & 1) == 0 ? 8 : 16;
         Array.Fill(data, (byte)0, parent, TrainerPokemonTeamRun.TrainerFormat_Width);
         data[parent] = type; data[parent + 1] = (byte)(row % 64); data[parent + 3] = (byte)(row % 64);
         Name(parent + 4, 12, $"TRN{row:D4}");
         for (int item = 0; item < 4; item++) Value(parent + 16 + item * 2, 1 + (row + item) % 511, 2);
         Value(parent + 32, count); Value(parent + 36, 0x08000000 + team);
         Array.Fill(data, (byte)0, team, stride * count);
         for (int member = 0; member < count; member++) {
            var start = team + member * stride;
            Value(start, 255, 2); Value(start + 2, 10 + row % 80, 2); Value(start + 4, (row + member) % 412, 2);
            var offset = 6;
            if ((type & 2) != 0) { Value(start + offset, 1 + (row + member) % 511, 2); offset += 2; }
            if ((type & 1) != 0) for (int move = 0; move < 4; move++) Value(start + offset + move * 2, 1 + (row + member + move) % 511, 2);
         }
         // Occupied data immediately after the team forces actual repointing on growth.
         data[team + stride * count] = 0xEE;
      }
      var reference = File.ReadLines("resources/tableReference.txt").Single(line => line.StartsWith("data.trainers.stats,", StringComparison.Ordinal));
      var anchors = new List<StoredAnchor> {
         new(Trainers, "data.trainers.stats", reference[reference.IndexOf('[')..].Trim() + Rows),
         new(0x4000, HardcodeTablesModel.PokemonNameTable, "[name\"\"11]412"),
         new(0x6000, HardcodeTablesModel.MoveNamesTable, "[name\"\"13]512"),
         new(0x8000, HardcodeTablesModel.ItemsTableName, "[name\"\"14]512"),
         new(0xA000, "data.trainers.classes.names", "[name\"\"13]64"),
         new(0xB000, "graphics.trainers.sprites.front", portraits ? "[sprite<`ucs4x8x8|synthetic.palette`>]64" : "[name\"\"12]64"),
         new(0xC000, "data.trainers.money", "[class.data.trainers.classes.names moneyrate. unused:]64"),
      };
      // Synthetic uncompressed 64x64 graphics isolate preview work from decompression.
      if (sprites) {
         for (int i = 0; i < 412; i++) {
            Value(0x50000 + i * 4, 0x08000000 + 0x60000 + i * 2048);
            for (int b = 0; b < 2048; b++) data[0x60000 + i * 2048 + b] = (byte)((1 + (i + b) % 15) * 17);
         }
         for (int i = 0; i < 16; i++) Value(0x51000 + i * 2, i | i << 5 | i << 10, 2);
         anchors.Add(new(0x50000, HardcodeTablesModel.FrontSpritesTable, "[sprite<`ucs4x8x8|synthetic.palette`>]data.pokemon.names"));
      }
      if (portraits) for (int i = 0; i < 64; i++) {
         Value(0xB000 + i * 4, 0x08000000 + 0x140000 + i * 2048);
         Array.Fill(data, (byte)((1 + i % 15) * 17), 0x140000 + i * 2048, 2048);
      }
      if (sprites || portraits) anchors.Add(new(0x51000, "synthetic.palette", "`ucp4`"));
      if (portraits && !sprites) for (int i = 0; i < 16; i++) Value(0x51000 + i * 2, i | i << 5 | i << 10, 2);
      var defaults = new StoredMetadata(File.ReadAllLines("resources/default.toml"));
      // Only symbolic lists are imported, never game addresses; the 32 AI bits set its real width.
      var fireRedLists = new StoredMetadata(File.ReadAllLines("resources/default.bpre.bpge.toml")).Lists;
      var lists = defaults.Lists.Concat(fireRedLists).GroupBy(list => list.Name).Select(group => group.Last()).ToArray();
      var singletons = new Singletons();
      var groups = defaults.TableGroups.Where(group => group.Tables.Any(table => table.StartsWith("data.trainers.stats|", StringComparison.Ordinal))).ToArray();
      Model = new PokemonModel(data, new StoredMetadata(anchors: anchors, lists: lists, tableGroups: groups,
         generalInfo: singletons.MetadataInfo), singletons);
      Model.FreeSpaceStart = 0x180000;
      ParentTable = (ITableRun)Model.GetNextRun(Trainers);
      if (ParentTable.ElementLength != 40 || ParentTable.ElementCount != Rows)
         throw new InvalidOperationException($"Trainer schema/length mismatch: {ParentTable.ElementLength} bytes, {ParentTable.ElementCount} rows; {string.Join(", ", ParentTable.ElementContent.Select(field => $"{field.Name}:{field.Length}"))}");
      ViewPort = new ViewPort("synthetic-trainers", Model, InstantDispatch.Instance, singletons, fs: new StubFileSystem());
      ViewPort.OnError += (_, message) => Errors.Add(message);
      ViewPort.InitializationWorkload.GetAwaiter().GetResult();
   }
   public int Parent(int row) => Trainers + row * TrainerPokemonTeamRun.TrainerFormat_Width;
   public byte Type(int row) => Mixed ? (byte)(row % 4) : StructType;
   public int Count(int row) => Mixed ? 1 + row % 6 : Members;
   public TrainerPokemonTeamRun Team(int row) => (TrainerPokemonTeamRun)Model.GetNextRun(Model.ReadPointer(Parent(row) + 36));
   public void Navigate(int row) => ViewPort.Goto.Execute(Parent(row));
   public TrainerPokemonTeamElementViewModel TeamEditor => ViewPort.Tools.TableTool.Groups
      .SelectMany(group => group.Members).OfType<TrainerPokemonTeamElementViewModel>().Single();
}
