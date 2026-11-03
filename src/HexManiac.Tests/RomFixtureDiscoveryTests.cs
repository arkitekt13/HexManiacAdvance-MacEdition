using System;
using System.IO;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class RomFixtureDiscoveryTests : IDisposable {
      private readonly string directory = Path.Combine(Path.GetTempPath(), "hma-fixture-discovery-" + Guid.NewGuid().ToString("N"));

      public void Dispose() {
         if (Directory.Exists(directory)) Directory.Delete(directory, true);
      }

      [Fact]
      public void MissingFolder_ProducesOneExplicitAbsentFixtureCase() {
         var cases = BasicLoadTests.DiscoverSamples(directory);
         Assert.Null(Assert.Single(Assert.Single(cases)));
      }

      [Fact]
      public void EmptyFolder_ProducesOneExplicitAbsentFixtureCase() {
         Directory.CreateDirectory(directory);
         Assert.Null(Assert.Single(Assert.Single(BasicLoadTests.DiscoverSamples(directory))));
      }

      [Fact]
      public void SuppliedRomPaths_AreSortedAndIncludeUppercaseExtensionOnly() {
         Directory.CreateDirectory(directory);
         var lower = Path.Combine(directory, "a.gba");
         var upper = Path.Combine(directory, "b.GBA");
         File.WriteAllBytes(upper, new byte[] { 0 });
         File.WriteAllBytes(lower, new byte[] { 0 });
         File.WriteAllText(Path.Combine(directory, "notes.txt"), "unrelated");

         var cases = BasicLoadTests.DiscoverSamples(directory);

         Assert.Equal(new[] { lower, upper }, cases.Select(row => (string)Assert.Single(row)).ToArray());
      }

      [Fact]
      public void AbsentFixtureCase_SkipsWithAnExplanation() {
         var error = Assert.Throws<SkipException>(() => new BasicLoadTests().CanLoad(null));
         Assert.Contains("No optional .gba ROM fixtures", error.Message);
      }

      [Fact]
      public void ExplicitFixturePathThatDisappears_FailsInsteadOfSkipping() {
         Directory.CreateDirectory(directory);
         var path = Path.Combine(directory, "missing.gba");
         Assert.Throws<FileNotFoundException>(() => new BasicLoadTests().CanLoad(path));
      }
   }
}
