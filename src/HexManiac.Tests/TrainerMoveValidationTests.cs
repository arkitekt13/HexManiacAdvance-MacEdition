using System;
using System.Linq;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Performance;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class TrainerMoveValidationTests : BaseViewModelTestClass {
      public TrainerMoveValidationTests() : base(0x4000) {
         CreateTextTable(HardcodeTablesModel.PokemonNameTable, 0x2000, "none", "sprout", "bloom");
         CreateTextTable(HardcodeTablesModel.MoveNamesTable, 0x1000,
            "none", "Mega Punch", "Punch", "Double-Edge", "King's Shield", "Duplicate", "Duplicate", "Flame", "Flame Burst", "Tackle");
         var allowed = new[] { 2, 3, 4, 6, 7, 9 };
         for (int species = 0; species < 3; species++) {
            var start = 0x300 + species * 0x40;
            Model.WritePointer(Token, 0x100 + species * 4, start);
            for (int i = 0; i < 6; i++) {
               Model[start + i * 3] = 1;
               Model.WriteMultiByteValue(start + i * 3 + 1, 2, Token, species == 1 ? allowed[i] : 1);
            }
         }
         var error = ArrayRun.TryParse(Model, "[movesFromLevel<[level. move:]6>]3", 0x100, SortedSpan<int>.None, out var table);
         Assert.False(error.HasError);
         Model.ObserveAnchorWritten(Token, HardcodeTablesModel.LevelMovesTableName, table);
         Model.ClearCacheScope();
         Assert.Equal(allowed, Flags.GetPokemonLevelupMoves(Model)[1]);
         Assert.Empty(Errors);
      }

      [Theory]
      [InlineData("- Punch", false, 2, 5)]
      [InlineData("- puNCh", false, 2, 5)]
      [InlineData("- \"Punch\"", false, 2, 7)]
      [InlineData("- Pun", false, 2, 3)]
      [InlineData("- Double-Edge", false, 2, 11)]
      [InlineData("- King's Shield", false, 2, 13)]
      [InlineData("- Duplicate~2", false, 2, 11)]
      [InlineData("- Duplicate~1", true, 2, 11)]
      [InlineData("- Mega Punch", true, 2, 10)]
      [InlineData("- \"Mega Punch\"", true, 2, 12)]
      [InlineData("- Flame", false, 2, 5)]
      [InlineData("- Flame Burst", true, 2, 11)]
      [InlineData("- 2", false, 2, 1)]
      [InlineData("- 1", true, 2, 1)]
      [InlineData("- \"\"", false, 0, 0)]
      [InlineData("- Mega Punch   ", true, 2, 10)]
      [InlineData("   -   Mega Punch  ", true, 7, 10)]
      [InlineData("\t- \t\"Mega Punch\" \t", true, 4, 12)]
      [InlineData("-", false, 0, 0)]
      [InlineData(" -   ", false, 0, 0)]
      [InlineData("- zzzzzz", false, 0, 0)]
      public void MoveToken_KnownLearnsets_ResolveExactPartialQuotedAndDuplicateNames(string line, bool warning, int start, int length) {
         var bytes = Model.RawData.ToArray();
         var metadata = Model.ExportMetadata(Singletons.MetadataInfo).Serialize();
         var content = "10 sprout" + Environment.NewLine + line;
         var errors = new TrainerTextFormatter(Model).FindErrors(content).ToArray();
         if (warning) Assert.Equal(new[] { new TextSegment(1, start, length, SegmentType.Warning) }, errors);
         else Assert.Empty(errors);
         Assert.Equal(bytes, Model.RawData);
         Assert.Equal(metadata, Model.ExportMetadata(Singletons.MetadataInfo).Serialize());
      }

      [Fact]
      public void SixMemberValidation_AvoidsFullListAllocationForEveryMoveLine() {
         var fixture = new SyntheticTrainerFixture(6, 3, false);
         fixture.Navigate(99);
         var formatter = new TrainerTextFormatter(fixture.Model);
         var text = fixture.TeamEditor.Content;
         var expected = formatter.FindErrors(text).ToArray();
         var bytes = fixture.Model.RawData.ToArray();
         var metadata = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize();
         var samples = new long[5];
         for (int i = 0; i < samples.Length; i++) {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var errors = formatter.FindErrors(text).ToArray();
            samples[i] = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(expected, errors);
         }
         var median = samples.OrderBy(value => value).ElementAt(2);
         Assert.True(median < 800_000, $"Six-member move validation allocated {median:N0} bytes; budget is 800,000.");
         Assert.Equal(bytes, fixture.Model.RawData);
         Assert.Equal(metadata, fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize());
      }

      [Fact]
      public void MultipleSpecies_ValidationTracksTheCurrentTeamMember() {
         var errors = new TrainerTextFormatter(Model).FindErrors("10 none\n- Punch\n10 sprout\n- Punch").ToArray();
         Assert.Equal(new[] { new TextSegment(1, 2, 5, SegmentType.Warning) }, errors);
      }

      [Fact]
      public void RenamedMoveAndUndo_ValidationUsesCurrentNames() {
         ViewPort.Goto.Execute(0x1000 + 2 * Model.GetTable(HardcodeTablesModel.MoveNamesTable).ElementLength);
         ViewPort.Edit("\"Jab\" "); ViewPort.ChangeHistory.ChangeCompleted();
         Assert.Empty(new TrainerTextFormatter(Model).FindErrors("10 sprout\n- Jab"));
         ViewPort.Undo.Execute(null);
         Assert.Empty(new TrainerTextFormatter(Model).FindErrors("10 sprout\n- Punch"));
      }
   }
}
