using HavenSoft.HexManiac.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class BasicLoadTests {
      private static readonly Singletons singletons = new Singletons();
      public static IEnumerable<object[]> SampleFiles { get; } = DiscoverSamples("sampleFiles");

      internal static object[][] DiscoverSamples(string directory) {
         var files = Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory)
               .Where(file => string.Equals(Path.GetExtension(file), ".gba", StringComparison.OrdinalIgnoreCase))
               .OrderBy(file => file, StringComparer.Ordinal)
               .Select(file => new object[] { file }).ToArray()
            : Array.Empty<object[]>();
         // Keep one discoverable case so absent optional fixtures produce an explicit skip, not a no-data failure.
         return files.Length > 0 ? files : new[] { new object[] { null } };
      }

      [SkippableTheory]
      [MemberData(nameof(SampleFiles))]
      public void CanLoad(string file) {
         Skip.If(file == null, "No optional .gba ROM fixtures were supplied in sampleFiles.");
         var data = File.ReadAllBytes(file);
         new HardcodeTablesModel(singletons, data);
      }
   }
}
