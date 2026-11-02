using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HavenSoft.HexManiac.Core.Models {
   /// <summary>
   /// Stage a complete file beside its destination before replacing it on the same filesystem.
   /// This protects the previous file from a failed write; it is not a transaction across files.
   /// </summary>
   public static class AtomicFileWriter {
      public static void Write(string path, Action<Stream> write) {
         if (write == null) throw new ArgumentNullException(nameof(write));
         path = Path.GetFullPath(path);
         // Keep a user-created symbolic link intact rather than replacing the link itself.
         var info = new FileInfo(path);
         if (info.LinkTarget != null) {
            path = info.ResolveLinkTarget(true)?.FullName ?? throw new IOException("Cannot resolve the destination link.");
         }
         if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0) {
            throw new UnauthorizedAccessException("The destination file is read-only.");
         }
         var directory = Path.GetDirectoryName(path);
         Directory.CreateDirectory(directory);
         var temporary = Path.Combine(directory, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
         try {
            // Copy existing attributes/permissions using the net6-compatible filesystem API.
            // The additional copy is intentional: replacement must not broaden file access.
            var replacing = File.Exists(path);
            if (replacing) File.Copy(path, temporary);
            using (var stream = new FileStream(temporary, replacing ? FileMode.Truncate : FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
               write(stream);
               stream.Flush(flushToDisk: true);
            }
            // Replacement can be refused by an external file lock; leave the old file untouched.
            File.Move(temporary, path, overwrite: true);
         } finally {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
         }
      }

      public static void WriteAllLines(string path, IEnumerable<string> lines) => Write(path, stream => {
         using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
         foreach (var line in lines) writer.WriteLine(line);
      });
   }
}
