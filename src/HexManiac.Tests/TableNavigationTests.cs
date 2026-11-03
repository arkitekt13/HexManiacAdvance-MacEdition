using System.Collections.Generic;
using System.Linq;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class TableNavigationTests : BaseViewModelTestClass {
      private TableTool Tool => ViewPort.Tools.TableTool;

      private void ArrangeTables() {
         SetFullModel(0xFF);
         ViewPort.Edit("@20 ^data.pokemon.names[value:]4 @40 ^data.pokemon.stats[hp:]data.pokemon.names @60 ^other.items[value:]4 ");
         ViewPort.Goto.Execute(0x20);
         ViewPort.ChangeHistory.ChangeCompleted();
         Assert.Empty(Errors);
      }

      [Theory]
      [InlineData(0)]
      [InlineData(250)]
      public void RefreshAndBindingReads_DiscoverTablesOnce_GettersOutsideRefreshRemainLive(int extraTables) {
         var anchors = new List<StoredAnchor> { new(0x100, "data.pokemon.names", "[value:]4") };
         for (int i = 0; i < extraTables; i++) anchors.Add(new(0x1000 + i * 8, $"other.section.table{i}", "[value:]4"));
         var model = new NavigationCountingModel(new byte[0x4000], new StoredMetadata(anchors: anchors));
         var viewport = new ViewPort("navigation", model, InstantDispatch.Instance, Singletons);
         viewport.InitializationWorkload.GetAwaiter().GetResult();
         viewport.Goto.Execute(0x100);
         var tool = viewport.Tools.TableTool;
         var notifications = 0;
         tool.PropertyChanged += (_, e) => {
            if (e.PropertyName != nameof(TableTool.TableSections) && e.PropertyName != nameof(TableTool.TableList)) return;
            notifications++;
            // Simulate synchronous binding reads while the refresh is publishing notifications.
            var sections = tool.TableSections;
            Assert.Same(sections, tool.TableSections);
            Assert.Equal(tool.TableList, tool.TableList);
         };
         var bytes = model.RawData.ToArray();
         model.AnchorReads = 0;

         tool.DataForCurrentRunChanged();

         Assert.Equal(2, notifications);
         Assert.Equal(1, model.AnchorReads);
         Assert.Equal(bytes, model.RawData);
         Assert.False(viewport.Undo.CanExecute(null));
         model.AnchorReads = 0;
         _ = tool.TableSections;
         _ = tool.TableList;
         Assert.Equal(2, model.AnchorReads); // no persistent cross-refresh snapshot
      }

      [Fact]
      public void RenameTable_UndoRedo_ListsFollowCurrentAnchors() {
         ArrangeTables();
         ViewPort.Edit("@60 ^changed.items[value:]4 ");
         ViewPort.ChangeHistory.ChangeCompleted();
         Assert.Contains("changed", Tool.TableSections);
         Assert.DoesNotContain("other", Tool.TableSections);

         ViewPort.Undo.Execute(null);
         Assert.Contains("other", Tool.TableSections);
         Assert.DoesNotContain("changed", Tool.TableSections);
         ViewPort.Redo.Execute(null);
         Assert.Contains("changed", Tool.TableSections);
         Assert.DoesNotContain("other", Tool.TableSections);
         Assert.Empty(Errors);
      }

      [Fact]
      public void CreateAndRemoveTable_ListReadsDoNotRetainPreviousRefresh() {
         ArrangeTables();
         ViewPort.Edit("@80 ^new.section.table[value:]4 ");
         Assert.Contains("new.section", Tool.TableSections);

         Model.ClearFormat(Token, 0x80, 8);

         Assert.DoesNotContain("new.section", Tool.TableSections);
         Tool.DataForCurrentRunChanged();
         Assert.DoesNotContain("new.section", Tool.TableSections);
         Assert.Empty(Errors);
      }

      [Fact]
      public void MatchedLengthFormatChanged_UndoRedo_DiscoveryKeepsParentFiltering() {
         ArrangeTables();
         Assert.Equal(new[] { "names" }, Tool.TableList);
         ViewPort.Edit("@40 ^data.pokemon.stats[hp:]4 ");
         ViewPort.ChangeHistory.ChangeCompleted();
         Assert.Equal(new[] { "names", "stats" }, Tool.TableList);

         ViewPort.Undo.Execute(null);
         ViewPort.Goto.Execute(0x20);
         Assert.Equal(new[] { "names" }, Tool.TableList);
         ViewPort.Redo.Execute(null);
         ViewPort.Goto.Execute(0x20);
         Assert.Equal(new[] { "names", "stats" }, Tool.TableList);
         Assert.Empty(Errors);
      }

      [Fact]
      public void ModelReload_ListReadsSeeReplacementMetadataBeforeNextRefresh() {
         ArrangeTables();
         Assert.Contains("other", Tool.TableSections);
         Model.Load(new byte[0x200], new StoredMetadata(anchors: new[] {
            new StoredAnchor(0x20, "replacement.tables.first", "[value:]4"),
            new StoredAnchor(0x40, "replacement.tables.second", "[value:]4"),
         }));

         Assert.Equal(new[] { "replacement.tables" }, Tool.TableSections);
         ViewPort.Refresh();
         Assert.Equal(new[] { "first", "second" }, Tool.TableList);
         Assert.Empty(Errors);
      }

      [Fact]
      public void ForcedTableRelocation_UndoRedo_NavigationAndPointerRemainCorrect() {
         ArrangeTables();
         ViewPort.Edit("@00 <000020> ");
         Model[0x28] = 1; // occupied byte forces real extension/repointing
         ViewPort.Goto.Execute(0x26);
         ViewPort.ChangeHistory.ChangeCompleted();
         var bytes = Model.RawData.ToArray();

         Tool.Append.Execute(null);
         ViewPort.ChangeHistory.ChangeCompleted();
         var moved = Model.GetTable("data.pokemon.names");
         Assert.NotEqual(0x20, moved.Start);
         Assert.Equal(5, moved.ElementCount);
         Assert.Equal(moved.Start, Model.ReadPointer(0));
         Assert.Equal(new[] { "names" }, Tool.TableList);

         ViewPort.Undo.Execute(null);
         Assert.Equal(bytes, Model.RawData);
         Assert.Equal(0x20, Model.GetTable("data.pokemon.names").Start);
         Assert.Equal(0x20, Model.ReadPointer(0));
         ViewPort.Redo.Execute(null);
         Assert.Equal(moved.Start, Model.GetTable("data.pokemon.names").Start);
         Assert.Equal(moved.Start, Model.ReadPointer(0));
         ViewPort.Goto.Execute(moved.Start);
         Assert.Equal(new[] { "names" }, Tool.TableList);
         Assert.Empty(Errors);
      }

      private sealed class NavigationCountingModel : PokemonModel {
         public int AnchorReads;
         public NavigationCountingModel(byte[] data, StoredMetadata metadata) : base(data, metadata, Singletons) => Initialize(metadata);
         public override IReadOnlyList<string> Anchors { get { AnchorReads++; return base.Anchors; } }
      }
   }
}
