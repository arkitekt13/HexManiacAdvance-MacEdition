using System;
using System.Collections.Generic;
using System.Linq;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

namespace HavenSoft.HexManiac.Performance;

// Isolate the actual trainer portrait enum field, not the entire trainer/team table.
public sealed class PortraitFixture {
   public const int Parents = 0x1000, Pointers = 0x2000, Palette = 0x3000, Pixels = 0x4000, Slot = 0x1000;
   public PokemonModel Model { get; }
   public ViewPort ViewPort { get; }
   public int Count { get; }
   public List<string> Errors { get; } = new();
   public PortraitFixture(int count, bool compressed) {
      Count = count;
      var data = new byte[0x100000]; Array.Fill(data, (byte)0xFF);
      for (int i = 0; i < 128; i++) data[Parents + i] = (byte)(i % count);
      for (int i = 0; i < 16; i++) BitConverter.GetBytes((short)(i | i << 5 | i << 10)).CopyTo(data, Palette + i * 2);
      for (int i = 0; i < count; i++) {
         BitConverter.GetBytes(0x08000000 + Pixels + i * Slot).CopyTo(data, Pointers + i * 4);
         var raw = new byte[2048];
         for (int b = 0; b < raw.Length; b++) raw[b] = (byte)((1 + (i + b) % 15) * 17);
         if (compressed) LZRun.Compress(raw).ToArray().CopyTo(data, Pixels + i * Slot);
         else raw.CopyTo(data, Pixels + i * Slot);
      }
      var encoding = compressed ? "lzs" : "ucs";
      var metadata = new StoredMetadata(anchors: new[] {
         new StoredAnchor(Parents, "benchmark.trainers", "[sprite.graphics.trainers.sprites.front]128"),
         new StoredAnchor(Pointers, "graphics.trainers.sprites.front", $"[sprite<`{encoding}4x8x8|graphics.trainers.palette`>]{count}"),
         new StoredAnchor(Palette, "graphics.trainers.palette", "`ucp4`"),
      });
      var singletons = new Singletons(); Model = new PokemonModel(data, metadata, singletons);
      Model.FreeSpaceStart = 0xD0000;
      ViewPort = new ViewPort("portrait-only", Model, InstantDispatch.Instance, singletons, fs: new StubFileSystem());
      ViewPort.OnError += (_, message) => Errors.Add(message);
      ViewPort.InitializationWorkload.GetAwaiter().GetResult(); Navigate(0);
   }
   public void Navigate(int row) => ViewPort.Goto.Execute(Parents + row);
   public ComboBoxArrayElementViewModel Field => ViewPort.Tools.TableTool.Groups.SelectMany(group => group.Members)
      .OfType<ComboBoxArrayElementViewModel>().Single(field => field.Name == "sprite");
   public ComboBoxArrayElementViewModel Candidate(int row = 0) => new(ViewPort, null, "sprite", Parents + row, 1);
   public ISpriteRun Sprite(int index = 0) => (ISpriteRun)Model.GetNextRun(Model.ReadPointer(Pointers + index * 4));
}
