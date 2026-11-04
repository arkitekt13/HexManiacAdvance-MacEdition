using System;
using System.IO;
using System.Linq;

namespace HavenSoft.HexManiac.AvaloniaUI.MacPlatform;

/// <summary>Map PNG destination policy; all writes finish before publishing the destination.</summary>
public sealed class MapImageExporter {
   readonly Func<string, int> resolveConflict;
   readonly Func<string, string> chooseDestination;
   readonly Action<string> reportError;

   public MapImageExporter(Func<string, int> resolveConflict, Func<string, string> chooseDestination, Action<string> reportError) {
      this.resolveConflict = resolveConflict;
      this.chooseDestination = chooseDestination;
      this.reportError = reportError;
   }

   public static string FileName(string mapName) {
      var invalid = Path.GetInvalidFileNameChars().Concat("/\\:*?\"<>|").ToHashSet();
      var safe = new string((mapName ?? "Map").Select(c => char.IsControl(c) || invalid.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
      return (string.IsNullOrWhiteSpace(safe) ? "Map" : safe) + ".png";
   }

   /// <returns>The saved path, or null after cancellation or a reported failure.</returns>
   public string Export(string romPath, string mapName, Action<Stream> encode) {
      var name = FileName(mapName);
      string destination = null;
      if (!string.IsNullOrWhiteSpace(romPath) && Path.IsPathFullyQualified(romPath))
         destination = Path.Combine(Path.GetDirectoryName(romPath), "MapImages", name);
      if (destination == null) destination = chooseDestination(name);
      bool offeredFallback = false;
      while (!string.IsNullOrEmpty(destination)) {
         string temporary = null;
         try {
            bool overwrite = false;
            if (File.Exists(destination)) {
               var choice = resolveConflict(destination);
               if (choice == 0) overwrite = true;
               else if (choice == 1) {
                  var directory = Path.GetDirectoryName(destination);
                  var stem = Path.GetFileNameWithoutExtension(destination);
                  int number = 2;
                  string candidate;
                  do { candidate = Path.Combine(directory, $"{stem} ({number++}).png"); }
                  while (File.Exists(candidate) || Directory.Exists(candidate));
                  destination = candidate;
               } else return null;
            }
            var parent = Path.GetDirectoryName(Path.GetFullPath(destination));
            Directory.CreateDirectory(parent);
            temporary = Path.Combine(parent, ".hma-map-" + Guid.NewGuid().ToString("N") + ".tmp");
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write)) {
               encode(stream);
               stream.Flush(true);
            }
            File.Move(temporary, destination, overwrite);
            return destination;
         } catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) {
            reportError("Could not export the map image. Existing images were not replaced.\n" + error.Message);
            if (offeredFallback) return null;
            offeredFallback = true;
            destination = chooseDestination(name);
         } finally {
            if (temporary != null) {
               try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
         }
      }
      return null;
   }
}
