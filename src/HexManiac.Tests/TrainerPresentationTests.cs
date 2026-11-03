using System.Linq;
using HavenSoft.HexManiac.Performance;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class TrainerPresentationTests {
      [Fact]
      public void UnchangedSixMemberTeam_RefreshPreservesPresentationWithoutDuplicatePreparationAllocation() {
         var fixture = new SyntheticTrainerFixture(6, 3, false);
         fixture.Navigate(99);
         var tool = fixture.ViewPort.Tools.TableTool;
         for (int warmup = 0; warmup < 3; warmup++) tool.DataForCurrentRunChanged();
         var editor = fixture.TeamEditor;
         var text = editor.Content;
         string[] Layers() => new[] { editor.ElementContent.PlainContent, editor.ElementContent.AccentContent,
            editor.ElementContent.ConstantContent, editor.ElementContent.NumericContent,
            editor.ElementContent.CommentContent, editor.ElementContent.TextContent };
         var layers = Layers();
         var errors = editor.ElementContent.ErrorLocations.ToArray();
         var bytes = fixture.Model.RawData.ToArray();
         var metadata = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize();
         var allocations = new long[5];
         for (int sample = 0; sample < allocations.Length; sample++) {
            var before = System.GC.GetAllocatedBytesForCurrentThread();
            tool.DataForCurrentRunChanged();
            allocations[sample] = System.GC.GetAllocatedBytesForCurrentThread() - before;
         }

         // Component baseline allocates >2 MB per refresh by preparing both generic and
         // trainer-specific candidates; allow headroom for one candidate without a time gate.
         var median = allocations.OrderBy(value => value).ElementAt(2);
         Assert.True(median < 1_800_000, $"Warmed trainer refresh allocated {median:N0} bytes; budget is 1,800,000.");
         Assert.Same(editor, fixture.TeamEditor);
         Assert.Equal(text, editor.Content);
         Assert.Equal(layers, Layers());
         Assert.Equal(errors, editor.ElementContent.ErrorLocations);
         Assert.Equal(bytes, fixture.Model.RawData);
         Assert.Equal(metadata, fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize());
         Assert.False(fixture.ViewPort.Undo.CanExecute(null));
         Assert.Empty(fixture.Errors);
      }
   }
}
