using HavenSoft.HexManiac.Core.Models;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class AutoSearchFixtureTests : IDisposable {
      private readonly string directory = Path.Combine(Path.GetTempPath(), "hma-autosearch-" + Guid.NewGuid().ToString("N"));
      private readonly AutoSearchFixture fixture = new AutoSearchFixture(Array.Empty<string>());

      public void Dispose() {
         if (Directory.Exists(directory)) Directory.Delete(directory, true);
      }

      private string WriteSyntheticFile() {
         Directory.CreateDirectory(directory);
         var path = Path.Combine(directory, "synthetic.gba");
         // An unknown-game, 512-byte buffer; no commercial ROM or known address definitions.
         File.WriteAllBytes(path, new byte[0x200]);
         return path;
      }

      [Theory]
      [InlineData(false)]
      [InlineData(true)]
      public void MissingFixture_SkipNamesTheRequiredPath(bool copy) {
         var path = Path.Combine(directory, "missing.gba");
         var error = Assert.Throws<SkipException>(() => {
            if (copy) fixture.LoadModelNoCache(path);
            else fixture.LoadModel(path);
         });
         Assert.Contains(path, error.Message);
      }

      [Theory]
      [InlineData(false)]
      [InlineData(true)]
      public void UpgradeMissingFixture_SkipNamesTheRequiredPath(bool historical) {
         var path = Path.Combine(directory, "missing.gba");
         var tests = new UpgradeTests();
         var error = Assert.Throws<SkipException>(() => {
            if (historical) tests.OldMetadata_Upgrade_MetadataMatchesNewLoad(path, "_0.3.0.toml");
            else tests.NewMetadata_Reopen_MetadataMatches(path);
         });
         Assert.Contains(path, error.Message);
      }

      [Fact]
      public void CopyOfFileNotPreloaded_LoadsInsteadOfThrowingCacheLookupError() {
         var path = WriteSyntheticFile();
         var copy = fixture.LoadModelNoCache(path);
         Assert.Equal(File.ReadAllBytes(path), copy.RawData);
      }

      [Fact]
      public void CopiesAndCachedTemplate_HaveIndependentBytesAndMetadata() {
         var path = WriteSyntheticFile();
         var template = fixture.LoadModel(path);
         var original = template.RawData.ToArray();
         var first = fixture.LoadModelNoCache(path);
         var second = fixture.LoadModelNoCache(path);

         first.RawData[0x10] = 42;
         first.SetList(new ModelDelta(), "synthetic.only", "entry");

         Assert.Equal(original, template.RawData);
         Assert.Equal(original, second.RawData);
         Assert.False(template.TryGetList("synthetic.only", out _));
         Assert.False(second.TryGetList("synthetic.only", out _));
         Assert.Equal(original, File.ReadAllBytes(path));
      }
   }
}
