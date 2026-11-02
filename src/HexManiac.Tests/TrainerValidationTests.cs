using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class TrainerValidationTests : BaseViewModelTestClass {
      private const int EvolutionStart = 0x40;
      private const int EvolutionRowLength = 16;

      private void CreateNames() => CreateTextTable(HardcodeTablesModel.PokemonNameTable, 0x180,
         "none", "sprout", "bloom", "flower");

      private void CreateEvolutions() {
         CreateNames();
         var error = ArrayRun.TryParse(Model,
            "[method1: argument1: species1: unused1: method2: argument2: species2: unused2:]4",
            EvolutionStart, SortedSpan<int>.None, out var run);
         Assert.False(error.HasError);
         Model.ObserveAnchorWritten(Token, HardcodeTablesModel.EvolutionTableName, run);
      }

      private void WriteEvolution(int source, int slot, int method, int argument, int destination) {
         var address = EvolutionStart + source * EvolutionRowLength + slot * 8;
         Model.WriteMultiByteValue(address, 2, Token, method);
         Model.WriteMultiByteValue(address + 2, 2, Token, argument);
         Model.WriteMultiByteValue(address + 4, 2, Token, destination);
      }

      [Fact]
      public void MissingEvolutionTable_DerivedHintsEmpty_ModelUnchanged() {
         var bytes = Model.RawData.ToArray();
         var metadata = Model.ExportMetadata(Singletons.MetadataInfo).Serialize();

         Assert.Empty(Flags.GetMinimumLevelForPokemon(Model));
         Assert.Empty(Flags.GetPokemonDevolutions(Model));

         Assert.Equal(bytes, Model.RawData);
         Assert.Equal(metadata, Model.ExportMetadata(Singletons.MetadataInfo).Serialize());
      }

      [Fact]
      public void KnownPokemonWithoutEvolutionTable_TrainerValidation_NoEvolutionWarningOrMutation() {
         CreateNames();
         var bytes = Model.RawData.ToArray();
         var metadata = Model.ExportMetadata(Singletons.MetadataInfo).Serialize();

         var errors = new TrainerTextFormatter(Model).FindErrors("1 bloom").ToArray();

         Assert.Empty(errors);
         Assert.Equal(bytes, Model.RawData);
         Assert.Equal(metadata, Model.ExportMetadata(Singletons.MetadataInfo).Serialize());
      }

      [Theory]
      [InlineData(15, 1)]
      [InlineData(16, 0)]
      [InlineData(20, 0)]
      public void KnownLevelEvolution_TrainerValidation_WarnsOnlyBelowRequiredLevel(int level, int warnings) {
         CreateEvolutions();
         WriteEvolution(1, 0, 4, 16, 2);

         var errors = new TrainerTextFormatter(Model).FindErrors(level + " bloom").ToArray();

         Assert.Equal(16, Flags.GetMinimumLevelForPokemon(Model)[2]);
         Assert.Equal(warnings, errors.Length);
         Assert.All(errors, error => {
            Assert.Equal(SegmentType.Warning, error.Type);
            Assert.Equal(0, error.Line);
            Assert.Equal(3, error.Start);
            Assert.Equal("bloom".Length, error.Length);
         });
      }

      [Theory]
      [InlineData(15, 1)]
      [InlineData(16, 0)]
      public void StoneEvolutionAfterLevelEvolution_TrainerValidation_RetainsAncestorLevelWarning(int level, int warnings) {
         CreateEvolutions();
         WriteEvolution(1, 0, 4, 16, 2);
         WriteEvolution(2, 0, 7, 99, 3);

         var errors = new TrainerTextFormatter(Model).FindErrors(level + " flower").ToArray();

         Assert.Equal(2, Flags.GetPokemonDevolutions(Model)[3]);
         Assert.Equal(warnings, errors.Length);
         Assert.All(errors, error => Assert.Equal(SegmentType.Warning, error.Type));
      }

      [Fact]
      public void StoneEvolutionArgument_IsNotTreatedAsMinimumLevel() {
         CreateEvolutions();
         WriteEvolution(1, 0, 7, 99, 2);

         Assert.False(Flags.GetMinimumLevelForPokemon(Model).ContainsKey(2));
         Assert.Empty(new TrainerTextFormatter(Model).FindErrors("1 bloom"));
      }

      [Fact]
      public void SecondEvolutionSlot_RetainsLevelAndParentHints() {
         CreateEvolutions();
         WriteEvolution(1, 0, 4, 16, 2);
         WriteEvolution(1, 1, 4, 30, 3);

         var levels = Flags.GetMinimumLevelForPokemon(Model);
         var parents = Flags.GetPokemonDevolutions(Model);

         Assert.Equal(16, levels[2]);
         Assert.Equal(30, levels[3]);
         Assert.Equal(1, parents[2]);
         Assert.Equal(1, parents[3]);
      }
   }
}
