using System;
using System.Linq;
using System.Threading.Tasks;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Map;

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

/// <summary>Draw through a detached model: lazy event formatting never touches the live document.</summary>
public sealed record ScriptMapSnapshot(string Caption, ReadonlyPixelViewModel Image) {
   public static ScriptMapSnapshot Create(ViewPort source, int mapID) {
      var (bytes, metadata) = Capture(source);
      return Render(bytes, metadata, mapID);
   }
   public static Task<ScriptMapSnapshot> CreateAsync(ViewPort source, int mapID) {
      var (bytes, metadata) = Capture(source);
      return Task.Run(() => Render(bytes, metadata, mapID));
   }
   private static (byte[] Bytes, string[] Metadata) Capture(ViewPort source) {
      if (source.UpdateInProgress || !source.InitializationWorkload.IsCompleted)
         throw new InvalidOperationException("Wait for the ROM to finish loading.");
      var metadata = source.Model.ExportMetadata(null, source.Singletons.MetadataInfo).Serialize();
      return (source.Model.RawData.ToArray(), metadata);
   }
   private static ScriptMapSnapshot Render(byte[] bytes, string[] metadata, int mapID) {
      var services = new Singletons(InstantDispatch.Instance);
      var model = new PokemonModel(bytes, new StoredMetadata(metadata), services);
      model.InitializationWorkload.GetAwaiter().GetResult();
      // Supplying independent history skips ViewPort's first-document cascade/template initialization.
      var history = new ChangeHistory<ModelDelta>(change => change.Revert(model));
      var view = new ViewPort("map-reference", model, InstantDispatch.Instance, services, changeHistory: history);
      view.InitializationWorkload.GetAwaiter().GetResult();
      var header = BlockMapViewModel.GetMapModel(model, mapID / 1000, mapID % 1000, () => new NoTrackChange());
      var layout = header?.GetSubTable("layout")?.FirstOrDefault();
      if (layout == null) throw new InvalidOperationException("The reference map is no longer available.");
      int width = layout.GetValue("width"), height = layout.GetValue("height");
      if (width <= 0 || height <= 0 || (long)width * height * 256 > 16_777_216)
         throw new InvalidOperationException("This map is too large for the reference preview.");
      var map = new BlockMapViewModel(new StubFileSystem(), new MapTutorialsViewModel(), view,
         new Format(model), null, mapID / 1000, mapID % 1000) {
         IncludeBorders = false, ShowEvents = MapDisplayOptions.AllEvents, IsSelected = false,
      };
      var pixels = map.PixelData;
      if (pixels == null) throw new InvalidOperationException("The reference map could not be rendered.");
      return new ScriptMapSnapshot($"Reference: {map.FullName} — {width} × {height} tiles",
         new ReadonlyPixelViewModel(map.PixelWidth, map.PixelHeight, pixels.ToArray()));
   }
}
