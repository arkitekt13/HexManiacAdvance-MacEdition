using System;
using System.IO;
using Xunit;

namespace HavenSoft.HexManiac.Integration {
   // These checks need no ROM and distinguish a working host from skipped game coverage.
   public class HarnessTests {
      [Theory]
      [InlineData("resources/scriptReference.txt")]
      [InlineData("resources/default.toml")]
      [InlineData("resources/Scripts/Add Mechanics From Later Generations/AnyGame_FairyType.hma")]
      [InlineData("resources/Scripts/Add Mechanics From Later Generations/AnyGame_PixilateStyleAbilities.hma")]
      public void RequiredResource_IsAvailableFromHostOutput(string relativePath) {
         var path = Path.Combine(AppContext.BaseDirectory, relativePath);
         Assert.True(File.Exists(path), "Missing integration resource: " + path);
         Assert.NotEmpty(File.ReadAllBytes(path));
      }
   }
}
