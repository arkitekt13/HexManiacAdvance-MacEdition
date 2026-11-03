using HavenSoft.HexManiac.Core.Models;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class MetadataReopenAssertionTests {
      private static StoredMetadata Create(string changedCollection = null, string change = null) {
         var names = new[] { "first", "second" };
         string[] Keys(string collection) => collection != changedCollection ? names : change switch {
            "missing" => new[] { "first" },
            "extra" => new[] { "first", "second", "third" },
            "duplicate" => new[] { "first", "first" },
            "reverse" => new[] { "second", "first" },
            _ => names,
         };
         int Address(string name) => name == "first" ? 0x10 : name == "second" ? 0x20 : 0x30;
         return new StoredMetadata(
            anchors: Keys("anchors").Select(n => new StoredAnchor(Address(n), n, "[value.]1")).ToArray(),
            lists: Keys("lists").Select(n => new StoredList(n, new[] { "entry" }, null)).ToArray(),
            matchedWords: Keys("words").Select(n => new StoredMatchedWord(Address(n), n, 1, 0, 1, "note")).ToArray(),
            offsetPointers: Keys("pointers").Select(n => new StoredOffsetPointer(Address(n), 4)).ToArray());
      }

      [Theory]
      [InlineData("anchors", "missing")]
      [InlineData("lists", "missing")]
      [InlineData("words", "missing")]
      [InlineData("pointers", "missing")]
      [InlineData("anchors", "extra")]
      [InlineData("lists", "extra")]
      [InlineData("words", "extra")]
      [InlineData("pointers", "extra")]
      [InlineData("anchors", "duplicate")]
      [InlineData("lists", "duplicate")]
      [InlineData("words", "duplicate")]
      [InlineData("pointers", "duplicate")]
      public void MissingExtraOrDuplicatedMetadata_IsRejected(string collection, string change) {
         var error = Record.Exception(() => UpgradeTests.AssertReopenedMetadataMatches(Create(), Create(collection, change)));
         Assert.IsAssignableFrom<Xunit.Sdk.XunitException>(error);
      }

      [Theory]
      [InlineData("anchors")]
      [InlineData("lists")]
      [InlineData("words")]
      [InlineData("pointers")]
      public void ReorderedMetadata_StillMatches(string collection) =>
         UpgradeTests.AssertReopenedMetadataMatches(Create(), Create(collection, "reverse"));

      [Fact]
      public void MatchingMetadata_StillPasses() => UpgradeTests.AssertReopenedMetadataMatches(Create(), Create());
   }
}
