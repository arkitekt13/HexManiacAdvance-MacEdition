using System;
using System.Linq;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class PortraitPreparationTests {
      private static VisualComboOption[] Options(PortraitFixture fixture) => fixture.Candidate().FilteringComboOptions.AllOptions.OfType<VisualComboOption>().ToArray();
      private static void AssertOriginalRenderer(PortraitFixture fixture) {
         var options = Options(fixture);
         for (int i = 0; i < options.Length; i++) {
            var run = fixture.Sprite(i);
            var sprite = run.GetPixels(fixture.Model, 0, i);
            var palette = fixture.Model.GetNextRun(SpriteTool.FindMatchingPalette(fixture.Model, run, 0)) as IPaletteRun;
            var colors = palette?.GetPalette(fixture.Model, palette.PaletteFormat.InitialBlankPages) ?? TileViewModel.CreateDefaultPalette(16);
            var expected = SpriteTool.Render(sprite, colors, palette?.PaletteFormat.InitialBlankPages ?? 0, 0);
            Assert.Equal(sprite.GetLength(0), options[i].PixelWidth);
            Assert.Equal(expected, options[i].PixelData);
         }
      }

      [Theory]
      [InlineData(false)]
      [InlineData(true)]
      public void WarmCandidates_ReusePixelsWithinModelScopeAndBoundAllocation(bool compressed) {
         var fixture = new PortraitFixture(64, compressed);
         var before = fixture.Model.RawData.ToArray();
         var metadata = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize();
         var first = Options(fixture); fixture.Candidate(); fixture.Candidate();
         var start = GC.GetAllocatedBytesForCurrentThread();
         var candidate = fixture.Candidate();
         var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
         Assert.True(allocated < 400000, $"Warmed 64-portrait candidate allocated {allocated:N0} bytes; budget is 400,000.");
         var next = candidate.FilteringComboOptions.AllOptions.OfType<VisualComboOption>().ToArray();
         for (int i = 0; i < first.Length; i++) Assert.Same(first[i].PixelData, next[i].PixelData);
         Assert.Equal(before, fixture.Model.RawData);
         Assert.Equal(metadata, fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize());
         Assert.False(fixture.ViewPort.Undo.CanExecute(null));
         AssertOriginalRenderer(fixture);
      }

      [Theory]
      [InlineData(false)]
      [InlineData(true)]
      public void ByteWrites_InvalidatePixelsWithoutExplicitRefresh(bool compressed) {
         var fixture = new PortraitFixture(16, compressed);
         var first = Options(fixture);
         var pixels = fixture.Sprite().GetPixels(fixture.Model, 0, 0); pixels[0, 0] = pixels[0, 0] == 9 ? 8 : 9;
         fixture.Model.ObserveRunWritten(fixture.ViewPort.CurrentChange, fixture.Sprite().SetPixels(fixture.Model, fixture.ViewPort.CurrentChange, 0, pixels));
         var next = Options(fixture);
         Assert.NotSame(first[0].PixelData, next[0].PixelData);
         Assert.NotEqual(first[0].PixelData[0], next[0].PixelData[0]);
         AssertOriginalRenderer(fixture);
      }

      [Theory]
      [InlineData(false, "sprite")]
      [InlineData(true, "sprite")]
      [InlineData(false, "palette")]
      [InlineData(true, "palette")]
      [InlineData(false, "hint")]
      [InlineData(true, "hint")]
      [InlineData(false, "defaultpalette")]
      [InlineData(true, "defaultpalette")]
      public void MetadataOnlyFormatChanges_UseNewSpriteAndPaletteContext(bool compressed, string context) {
         var fixture = new PortraitFixture(16, compressed);
         for (int i = 0; i < 16; i++) fixture.Model.WriteMultiByteValue(0x3800 + i * 2, 2, new ModelDelta(), 31 << 5);
         fixture.Model.ObserveAnchorWritten(new NoDataChangeDeltaModel(), "benchmark.other.palette", new PaletteRun(0x3800, new PaletteFormat(4, 1)));
         var first = Options(fixture); var scope = fixture.Model.CurrentCacheScope;
         var run = fixture.Sprite();
         if (context == "sprite") {
            fixture.Model.ObserveRunWritten(new NoDataChangeDeltaModel(), run.Duplicate(new SpriteFormat(4, 4, 16, "graphics.trainers.palette")));
         } else if (context == "palette") {
            var palette = (IPaletteRun)fixture.Model.GetNextRun(PortraitFixture.Palette);
            fixture.Model.ObserveRunWritten(new NoDataChangeDeltaModel(), palette.Duplicate(new PaletteFormat(4, 2, 1)));
         } else {
            fixture.Model.ObserveRunWritten(new NoDataChangeDeltaModel(), run.Duplicate(new SpriteFormat(context == "defaultpalette" ? 2 : 4, 8, 8, "benchmark.other.palette")));
         }
         Assert.Same(scope, fixture.Model.CurrentCacheScope); // Exercise keys without relying on a scope reset.
         var next = Options(fixture);
         Assert.Equal(context == "sprite" ? 32 : 64, next[0].PixelWidth);
         Assert.Equal(context == "sprite" ? 128 : 64, next[0].PixelHeight);
         Assert.NotSame(first[0].PixelData, next[0].PixelData);
         if (context != "sprite") Assert.False(first[0].PixelData.SequenceEqual(next[0].PixelData));
         AssertOriginalRenderer(fixture);
      }

      [Theory]
      [InlineData(false)]
      [InlineData(true)]
      public void CompressedPaletteWrites_RefreshUsingTheOriginalPagePolicy(bool compressedSprite) {
         var fixture = new PortraitFixture(16, compressedSprite);
         var bytes = LZRun.Compress(fixture.Model.RawData.Skip(PortraitFixture.Palette).Take(32).ToArray()).ToArray();
         new ModelDelta().ChangeData(fixture.Model, 0x3800, bytes);
         fixture.Model.ObserveAnchorWritten(new NoDataChangeDeltaModel(), "benchmark.compressed.palette", new LzPaletteRun(new PaletteFormat(4, 1), fixture.Model, 0x3800));
         var sprite = fixture.Sprite();
         fixture.Model.ObserveRunWritten(new NoDataChangeDeltaModel(), sprite.Duplicate(new SpriteFormat(4, 8, 8, "benchmark.compressed.palette")));
         var first = Options(fixture);
         var palette = (IPaletteRun)fixture.Model.GetNextRun(0x3800);
         var colors = palette.GetPalette(fixture.Model, 0).ToArray(); colors[1] = 31 << 5;
         fixture.Model.ObserveRunWritten(fixture.ViewPort.CurrentChange, palette.SetPalette(fixture.Model, fixture.ViewPort.CurrentChange, 0, colors));
         var next = Options(fixture);
         Assert.NotSame(first[0].PixelData, next[0].PixelData);
         Assert.False(first[0].PixelData.SequenceEqual(next[0].PixelData));
         AssertOriginalRenderer(fixture);
      }

      private sealed class ContextSprite : SpriteRun, ISpriteRun {
         public int Color { get; set; } = 1;
         public ContextSprite(PortraitFixture fixture) : base(fixture.Model, PortraitFixture.Pixels, fixture.Sprite().SpriteFormat, fixture.Sprite().PointerSources) { }
         public new int[,] GetPixels(IDataModel model, int page, int tableIndex) {
            var pixels = new int[64, 64]; pixels[0, 0] = Color; return pixels;
         }
      }

      [Fact]
      public void UnrecognizedSpriteImplementations_RenderFreshForAdditionalContext() {
         var fixture = new PortraitFixture(16, false); var custom = new ContextSprite(fixture);
         fixture.Model.ObserveRunWritten(new NoDataChangeDeltaModel(), custom);
         Assert.Same(custom, fixture.Sprite());
         var first = Options(fixture); custom.Color = 3;
         var next = Options(fixture);
         Assert.NotSame(first[0].PixelData, next[0].PixelData);
         Assert.NotEqual(first[0].PixelData[0], next[0].PixelData[0]);
         AssertOriginalRenderer(fixture);
      }

      [Fact]
      public void AliasedPointersAndSeparateModels_DoNotShareContextEntries() {
         var fixture = new PortraitFixture(16, false);
         fixture.Model.WritePointer(new ModelDelta(), PortraitFixture.Pointers + 4, PortraitFixture.Pixels);
         var options = Options(fixture);
         Assert.Equal(options[0].PixelData, options[1].PixelData);
         Assert.NotSame(options[0].PixelData, options[1].PixelData);
         var other = new PortraitFixture(16, false);
         Assert.NotSame(options[0].PixelData, Options(other)[0].PixelData);
         AssertOriginalRenderer(fixture);
      }
   }
}
