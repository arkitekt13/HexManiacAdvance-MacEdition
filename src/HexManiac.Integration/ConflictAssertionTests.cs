using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.DataFormats;
using HavenSoft.HexManiac.Tests;
using System;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Integration {
   public class ConflictAssertionTests : IntegrationTests {
      [Fact]
      public void EmptyModel_HasNoConflicts() {
         var fixture = new BaseViewModelTestClass();
         AssertNoConflicts(fixture.ViewPort);
      }

      [Fact]
      public void ConsistentPointerMetadata_PassesWithoutChangingBytesOrRuns() {
         var fixture = new BaseViewModelTestClass();
         fixture.AddPointer(0x10, 0x40);
         var bytes = fixture.Model.RawData.ToArray();
         var runs = fixture.Model.All<IFormattedRun>().ToArray();

         AssertNoConflicts(fixture.ViewPort);

         Assert.Equal(bytes, fixture.Model.RawData);
         Assert.Equal(runs, fixture.Model.All<IFormattedRun>().ToArray());
      }

      [Fact]
      public void PointerBytesDisagreeWithBacklinks_RaisesAnAssertion() {
         var fixture = new BaseViewModelTestClass();
         fixture.AddPointer(0x10, 0x40);
         // Deliberately alter only bytes, leaving the previous destination's metadata stale.
         fixture.Model.WritePointer(new ModelDelta(), 0x10, 0x60);
         var error = Record.Exception(() => AssertNoConflicts(fixture.ViewPort));
         Assert.IsAssignableFrom<Xunit.Sdk.XunitException>(error);
      }

      [Fact]
      public void OverlappingRuns_RaiseAnAssertion() {
         var fixture = new BaseViewModelTestClass();
         var first = new MutableTestRun(0x20, 4);
         fixture.Model.ObserveRunWritten(new ModelDelta(), first);
         fixture.Model.ObserveRunWritten(new ModelDelta(), new MutableTestRun(0x30, 4));
         // Corrupt the test run after insertion, bypassing normal overlap prevention.
         first.TestLength = 0x20;
         var error = Record.Exception(() => AssertNoConflicts(fixture.ViewPort));
         Assert.IsAssignableFrom<Xunit.Sdk.XunitException>(error);
      }

      [Theory]
      [InlineData(false)]
      [InlineData(true)]
      public void ConsistentTablePointers_IncludingInnerElements_Pass(bool inner) {
         var fixture = new BaseViewModelTestClass();
         fixture.ViewPort.Edit(inner
            ? "@040 ^target^[value:]4 @010 <000042> "
            : "@040 ^target[value:]4 @010 ^pointers[ptr<>]1 <000040> ");
         Assert.Empty(fixture.Errors);
         Assert.NotEmpty(fixture.Model.Arrays);
         if (inner) Assert.True(fixture.Model.Arrays.Single().SupportsInnerPointers);
         AssertNoConflicts(fixture.ViewPort);
      }

      [Fact]
      public void ParentDependentLengths_Pass() {
         var fixture = new BaseViewModelTestClass();
         fixture.ViewPort.Edit("@040 ^parent[value:]4 @080 ^child[value:]parent ");
         Assert.Empty(fixture.Errors);
         Assert.Equal(2, fixture.Model.Arrays.Count);
         AssertNoConflicts(fixture.ViewPort);
      }

      [Theory]
      [InlineData(-0x08000000)]
      [InlineData(-1)]
      [InlineData(0x400)]
      public void OutOfRangeRomPointers_KeepTheOriginalDiagnosticsExemption(int destination) {
         var fixture = new BaseViewModelTestClass();
         fixture.AddPointer(0x10, destination);
         AssertNoConflicts(fixture.ViewPort);
      }

      [Theory]
      [InlineData(false)]
      [InlineData(true)]
      public void ChangedPointerBytes_AreDetectedForTablesAndInnerElements(bool inner) {
         var fixture = new BaseViewModelTestClass();
         fixture.ViewPort.Edit(inner
            ? "@040 ^target^[value:]4 @010 <000042> "
            : "@040 ^target[value:]4 @010 ^pointers[ptr<>]1 <000040> ");
         Assert.Empty(fixture.Errors);
         fixture.Model.WritePointer(new ModelDelta(), 0x10, inner ? 0x44 : 0x60);
         var error = Record.Exception(() => AssertNoConflicts(fixture.ViewPort));
         Assert.IsAssignableFrom<Xunit.Sdk.XunitException>(error);
      }

      [Theory]
      [InlineData(false)]
      [InlineData(true)]
      public void MatchedWordLengths_AreCheckedWithoutRepairingThem(bool corrupt) {
         var fixture = new BaseViewModelTestClass();
         fixture.ViewPort.Edit("@010 04 @010 .synthetic.length @040 ^table[value:]synthetic.length ");
         Assert.Empty(fixture.Errors);
         Assert.Equal(4, fixture.Model.Arrays.Single().ElementCount);
         if (corrupt) fixture.Model.RawData[0x10] = 3;

         var error = Record.Exception(() => AssertNoConflicts(fixture.ViewPort));

         if (corrupt) Assert.IsAssignableFrom<Xunit.Sdk.XunitException>(error);
         else Assert.Null(error);
         Assert.Equal(corrupt ? 3 : 4, fixture.Model.RawData[0x10]);
         Assert.Equal(4, fixture.Model.Arrays.Single().ElementCount);
      }

      // Only the synthetic fixture can mutate its run length; application run types are unchanged.
      private sealed class MutableTestRun : BaseRun {
         public int TestLength { get; set; }
         public override int Length => TestLength;
         public override string FormatString => string.Empty;
         public MutableTestRun(int start, int length, SortedSpan<int> sources = null) : base(start, sources) => TestLength = length;
         public override IDataFormat CreateDataFormat(IDataModel model, int index) => throw new NotImplementedException();
         protected override BaseRun Clone(SortedSpan<int> sources) => new MutableTestRun(Start, TestLength, sources);
      }
   }
}
