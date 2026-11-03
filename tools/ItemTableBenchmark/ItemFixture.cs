using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

namespace HavenSoft.HexManiac.Performance;

public sealed class ItemFixture {
   public const int StatsAddress = 0x4000, IconsAddress = 0x20000, DescriptionsAddress = 0x40000, ImageAddress = 0x300000;
   public IDataModel Model { get; }
   public ViewPort ViewPort { get; }
   public ITableRun Stats => Model.GetTable(HardcodeTablesModel.ItemsTableName);
   public ITableRun Icons => Model.GetTable(HardcodeTablesModel.ItemImagesTableName);
   public List<string> Errors { get; } = new();
   public IEnumerable<IArrayElementViewModel> Members => ViewPort.Tools.TableTool.Groups.SelectMany(group => group.Members);
   public TextStreamElementViewModel Description => Members.OfType<TextStreamElementViewModel>().Single(field => field.ParentName == "description");
   public ItemFixture(int count, int descriptionLength, bool images) : this(BuildModel(count, descriptionLength, images)) { }
   private ItemFixture(IDataModel model) {
      Model = model;
      var dispatcher = new ItemBenchmarkDispatch();
      ViewPort = new ViewPort("item-benchmark", model, dispatcher, new Singletons(dispatcher), fs: new StubFileSystem());
      ViewPort.OnError += (_, message) => Errors.Add(message);
      ViewPort.InitializationWorkload.GetAwaiter().GetResult(); Navigate(0);
   }
   public static ItemFixture FromRom(string path) {
      var singletons = new Singletons();
      return new ItemFixture(new HardcodeTablesModel(singletons, File.ReadAllBytes(path), new StoredMetadata(Array.Empty<string>())));
   }
   public void Navigate(int row) => ViewPort.Goto.Execute(Stats.Start + row * Stats.ElementLength);
   public int FieldAddress(string name, int row = 0) {
      var offset = Stats.ElementContent.TakeWhile(field => field.Name != name).Sum(field => field.Length);
      return Stats.Start + row * Stats.ElementLength + offset;
   }
   private static IDataModel BuildModel(int count, int descriptionLength, bool images) {
      var data = new byte[0x800000]; Array.Fill(data, (byte)0xFF);
      var reference = File.ReadLines("resources/tableReference.txt").First(line => line.StartsWith("data.items.stats,"));
      var format = reference[reference.IndexOf('[')..].Trim() + count;
      for (int i = 0; i < count; i++) {
         var start = StatsAddress + i * 44; Array.Clear(data, start, 44);
         PCSString.Convert($"\"ITEM{i:D4}\"").CopyTo(data, start);
         BitConverter.GetBytes((short)i).CopyTo(data, start + 14);
         BitConverter.GetBytes((short)(100 + i)).CopyTo(data, start + 16);
         BitConverter.GetBytes(0x08000000 + DescriptionsAddress + i * 2048).CopyTo(data, start + 20);
         var text = "Item " + i.ToString("D4") + " " + new string('A', descriptionLength);
         PCSString.Convert("\"" + text + "\"").CopyTo(data, DescriptionsAddress + i * 2048);
         data[start + 26] = (byte)(i % 5 + 1);
      }
      var anchors = new List<StoredAnchor> { new(StatsAddress, HardcodeTablesModel.ItemsTableName, format) };
      if (images) {
         for (int i = 0; i <= count; i++) {
            var address = ImageAddress + i * 0x400;
            BitConverter.GetBytes(0x08000000 + address).CopyTo(data, IconsAddress + i * 8);
            BitConverter.GetBytes(0x08000000 + address + 0x200).CopyTo(data, IconsAddress + i * 8 + 4);
            var pixels = Enumerable.Range(0, 288).Select(b => (byte)((1 + (i + b) % 15) * 17)).ToArray();
            LZRun.Compress(pixels).ToArray().CopyTo(data, address);
            var palette = Enumerable.Range(0, 16).SelectMany(c => BitConverter.GetBytes((short)(c | c << 5 | c << 10))).ToArray();
            LZRun.Compress(palette).ToArray().CopyTo(data, address + 0x200);
         }
         anchors.Add(new(IconsAddress, HardcodeTablesModel.ItemImagesTableName, "[sprite<`lzs4x3x3`> palette<`lzp4`>]data.items.stats+1"));
      }
      var defaults = new StoredMetadata(File.ReadAllLines("resources/default.toml"));
      var metadata = new StoredMetadata(anchors: anchors, lists: defaults.Lists,
         tableGroups: new[] { new TableGroup("Stats", new[] { HardcodeTablesModel.ItemsTableName }), new TableGroup("More", new[] { HardcodeTablesModel.ItemImagesTableName }) });
      var model = new PokemonModel(data, metadata, new Singletons()); model.FreeSpaceStart = 0x700000; return model;
   }
}

// Match browsing's deferred/coalesced timers rather than forcing a full map-usage scan
// for each row. Timed phases intentionally exclude that separately scheduled work.
public sealed class ItemBenchmarkDispatch : IWorkDispatcher {
   public Task WaitForRenderingAsync() => Task.CompletedTask;
   public void BlockOnUIWork(Action action) => action();
   public Task DispatchWork(Action action) { action?.Invoke(); return Task.CompletedTask; }
   public Task RunBackgroundWork(Action action) => DispatchWork(action);
   public IDelayWorkTimer CreateDelayTimer() => new ManualWorkTimer();
}
