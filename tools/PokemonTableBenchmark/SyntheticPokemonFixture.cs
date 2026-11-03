using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;

namespace HavenSoft.HexManiac.Performance;

public sealed class SyntheticPokemonFixture {
   public const int Names = 0x10000, Stats = 0x20000;
   public PokemonModel Model { get; }
   public ViewPort ViewPort { get; }
   public ITableRun StatsTable { get; }
   public List<string> Errors { get; } = new();
   public SyntheticPokemonFixture(int rows, int auxiliaryTables, bool instrument = false) {
      var data = new byte[0x80000];
      void WriteNames(int address, int count, int length, string prefix) {
         Array.Fill(data, (byte)0xFF, address, count * length);
         for (int i = 0; i < count; i++) {
            var bytes = PCSString.Convert($"\"{prefix}{i:D4}\"");
            if (bytes.Count > length) throw new InvalidOperationException("Fixture name does not fit.");
            bytes.CopyTo(data, address + i * length);
         }
      }
      WriteNames(Names, rows, 11, "MON");
      WriteNames(0x6000, 18, 11, "TYPE");
      WriteNames(0x7000, 128, 13, "ABILITY");
      WriteNames(0x8000, 512, 14, "ITEM");
      var reference = File.ReadLines("resources/tableReference.txt")
         .Single(line => line.StartsWith("data.pokemon.stats,", StringComparison.Ordinal));
      var statsFormat = reference[reference.IndexOf('[')..].Trim();
      var anchors = new List<StoredAnchor> {
         new(Names, "data.pokemon.names", $"[name\"\"11]{rows}"),
         new(Stats, "data.pokemon.stats", statsFormat),
         new(0x6000, "data.pokemon.type.names", "[name\"\"11]18"),
         new(0x7000, "data.abilities.names", "[name\"\"13]128"),
         new(0x8000, "data.items.stats", "[name\"\"14]512"),
      };
      // Small unrelated arrays isolate the cost of discovering/sorting table navigation lists.
      for (int i = 0; i < auxiliaryTables; i++)
         anchors.Add(new(0x1000 + i * 16, $"auxiliary.section{i % 10}.table{i}", "[value:]4"));
      var defaults = new StoredMetadata(File.ReadAllLines("resources/default.toml"));
      var singletons = new Singletons();
      var metadata = new StoredMetadata(anchors: anchors, lists: defaults.Lists,
         tableGroups: new[] { new TableGroup("Pokemon", new[] { "data.pokemon.names", "data.pokemon.stats" }) },
         generalInfo: singletons.MetadataInfo);
      Model = instrument ? new CountingPokemonModel(data, metadata, singletons) : new PokemonModel(data, metadata, singletons);
      StatsTable = (ITableRun)Model.GetNextRun(Stats);
      for (int row = 0; row < rows; row++) {
         int offset = 0;
         foreach (var field in StatsTable.ElementContent) {
            var value = field.Name switch {
               "hp" or "attack" or "def" or "speed" or "spatk" or "spdef" => 40 + (row + offset) % 120,
               "type1" or "type2" => (row + offset) % 18,
               "item1" or "item2" => row % 512,
               "ability1" or "ability2" => row % 128,
               "catchRate" => 45, "baseExp" => 64, "genderratio" => 128,
               "steps2hatch" => 20, "basehappiness" => 70,
               _ => 0,
            };
            for (int b = 0; b < field.Length; b++) data[Stats + row * StatsTable.ElementLength + offset + b] = (byte)(value >> (b * 8));
            offset += field.Length;
         }
      }
      Model.ClearCacheScope();
      ViewPort = new ViewPort("synthetic-pokemon", Model, InstantDispatch.Instance, singletons, fs: new StubFileSystem());
      ViewPort.OnError += (_, message) => Errors.Add(message);
      ViewPort.InitializationWorkload.GetAwaiter().GetResult();
   }
   // Enter through the parent names table, as the grouped Pokémon editor normally does.
   public void Navigate(int row) => ViewPort.Goto.Execute(Names + row * 11);
}

// Counters are used only in separate diagnostic passes, not the timing fixtures.
public sealed class CountingPokemonModel : PokemonModel {
   public long AnchorListReads, AnchorNameLookups, RunLookups;
   public CountingPokemonModel(byte[] data, StoredMetadata metadata, Singletons singletons) : base(data, metadata, singletons) {
      // PokemonModel deliberately leaves initialization to derived types.
      Initialize(metadata);
   }
   public override IReadOnlyList<string> Anchors { get { AnchorListReads++; return base.Anchors; } }
   public override string GetAnchorFromAddress(int source, int destination) { AnchorNameLookups++; return base.GetAnchorFromAddress(source, destination); }
   public override IFormattedRun GetNextRun(int index) { RunLookups++; return base.GetNextRun(index); }
   public void ResetCounters() => (AnchorListReads, AnchorNameLookups, RunLookups) = (0, 0, 0);
}
