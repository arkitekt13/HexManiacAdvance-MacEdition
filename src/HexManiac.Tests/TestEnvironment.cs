using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace HavenSoft.HexManiac.Tests {
   /// <summary>Resolve legacy relative fixture paths independently of the caller's working directory.</summary>
   internal static class TestEnvironment {
      [ModuleInitializer]
      internal static void Initialize() {
         Directory.SetCurrentDirectory(AppContext.BaseDirectory);
         foreach (var name in new[] { "armReference.txt", "scriptReference.txt", "battleScriptReference.txt",
            "animationScriptReference.txt", "battleAIScriptReference.txt", "tableReference.txt", "pcsReference.txt", "hma.py", "default.toml" }) {
            var path = Path.Combine(AppContext.BaseDirectory, "resources", name);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
               throw new InvalidOperationException("Required upstream test resource is missing or empty: " + path);
         }
      }
   }
}
