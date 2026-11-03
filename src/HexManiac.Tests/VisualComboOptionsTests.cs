using System.Linq;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class VisualComboOptionsTests {
      private VisualComboOption Image(string text = "portrait", int index = 0, int width = 8,
         int height = 8, short color = 1, double scale = 1, bool displayIndex = true) =>
         VisualComboOption.CreateFromSprite(text, Enumerable.Repeat(color, width * height).ToArray(), width, index, scale, displayIndex);

      [Theory]
      [InlineData("pixels")]
      [InlineData("width")]
      [InlineData("height")]
      [InlineData("scale")]
      [InlineData("index")]
      [InlineData("displayIndex")]
      [InlineData("text")]
      [InlineData("type")]
      public void SameLabel_NewVisualOrIndex_UpdatesBothChoiceCollections(string change) {
         var options = new FilteringComboOptions();
         options.Update(new[] { Image() }, 0);
         var old = options.AllOptions;
         ComboOption next = change switch {
            "pixels" => Image(color: 2), "width" => Image(width: 16), "height" => Image(height: 16),
            "scale" => Image(scale: 2), "index" => Image(index: 7), "displayIndex" => Image(displayIndex: false),
            "text" => Image(text: "changed"), _ => new ComboOption("portrait", 0),
         };
         options.Update(new[] { next }, 0);
         Assert.NotSame(old, options.AllOptions);
         Assert.Same(next, options.AllOptions[0]);
         Assert.Same(next, options.FilteredOptions[0]);
         Assert.Equal(next.Index, options.ModelValue);
         Assert.Equal(next.Text, options.DisplayText);
      }

      [Fact]
      public void EqualVisualPixels_RetainCollectionsAcrossSelectionChanges() {
         var options = new FilteringComboOptions(); options.Update(new[] { Image(), Image(index: 1) }, 0);
         var all = options.AllOptions; var filtered = options.FilteredOptions;
         options.Update(new[] { Image(), Image(index: 1) }, 1);
         Assert.Same(all, options.AllOptions); Assert.Same(filtered, options.FilteredOptions);
         Assert.Equal(1, options.ModelValue);
      }

      [Fact]
      public void TextOptions_KeepFilteringAndRefreshChangedIndices() {
         var options = new FilteringComboOptions();
         options.Update(new[] { new ComboOption("Alpha", 2), new ComboOption("Beta", 5) }, 0);
         options.DisplayText = "Bet"; options.SelectConfirm();
         Assert.Equal(5, options.ModelValue);
         options.Update(new[] { new ComboOption("Alpha", 2), new ComboOption("Beta", 9) }, 1);
         Assert.Equal(9, options.ModelValue); Assert.Equal("Beta", options.DisplayText);
         Assert.False(options.DropDownIsOpen);
      }
   }
}
