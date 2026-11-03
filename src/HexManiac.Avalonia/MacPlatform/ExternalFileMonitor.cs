using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace HavenSoft.HexManiac.AvaloniaUI.MacPlatform;

/// <summary>Debounced, content-based ROM watching with bounded retries and cancelable UI delivery.</summary>
public sealed class ExternalFileMonitor : IDisposable {
   private readonly object gate = new();
   private readonly string path;
   private readonly Action<Action> dispatch;
   private readonly Action<byte[]> changed;
   private readonly List<FileSystemWatcher> watchers = new();
   private readonly Timer timer, reconciliationTimer;
   private (long Length, DateTime Modified) observedStamp;
   private bool retryExhausted;
   private readonly int quietMilliseconds;
   private long generation;
   private int suspended, retries;
   private bool disposed, running;
   private byte[] acceptedHash;

   public ExternalFileMonitor(string path, Action<Action> dispatch, Action<byte[]> changed, int quietMilliseconds = 150) {
      this.path = Path.GetFullPath(path);
      this.dispatch = dispatch;
      this.changed = changed;
      this.quietMilliseconds = quietMilliseconds;
      var initial = TryRead(this.path);
      if (initial != null) acceptedHash = SHA256.HashData(initial);
      timer = new Timer(_ => { _ = CheckAsync(); }, null, Timeout.Infinite, Timeout.Infinite);
      observedStamp = ReadStamp();
      // Native events may be coalesced or missed: cheap metadata reconciliation avoids relying on them alone.
      reconciliationTimer = new Timer(_ => Reconcile(), null, 1000, 1000);
      try {
         var paths = new HashSet<string>(StringComparer.Ordinal) { this.path };
         var target = new FileInfo(this.path).ResolveLinkTarget(true);
         if (target != null) paths.Add(target.FullName);
         foreach (var watchedPath in paths) {
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(watchedPath)) {
               NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes,
            };
            watchers.Add(watcher);
            void OnChange(object sender, FileSystemEventArgs e) {
               if (e.FullPath == watchedPath) Signal();
            }
            watcher.Changed += OnChange;
            watcher.Created += OnChange;
            watcher.Deleted += OnChange;
            watcher.Renamed += (_, e) => { if (e.FullPath == watchedPath || e.OldFullPath == watchedPath) Signal(); };
            // Overflow means notifications may have been lost: rescan the actual file.
            watcher.Error += (_, _) => Signal();
            watcher.EnableRaisingEvents = true;
         }
         Signal(); // cover changes between the initial snapshot and watcher activation
      } catch { Dispose(); throw; }
   }

   /// <summary>Also used on native watcher errors; event handlers never read the ROM.</summary>
   public void Signal() {
      lock (gate) {
         if (disposed) return;
         generation++;
         retries = 0;
         retryExhausted = false;
         if (suspended == 0) timer.Change(quietMilliseconds, Timeout.Infinite);
      }
   }

   private (long Length, DateTime Modified) ReadStamp() {
      try {
         var file = new FileInfo(path);
         return file.Exists ? (file.Length, file.LastWriteTimeUtc) : (-1, DateTime.MinValue);
      } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return (-1, DateTime.MinValue); }
   }

   private void Reconcile() {
      var stamp = ReadStamp();
      lock (gate) {
         if (disposed || suspended > 0) return;
         if (stamp == observedStamp && !retryExhausted) return;
         observedStamp = stamp;
      }
      Signal();
   }

   public void Suspend() {
      lock (gate) {
         if (disposed) return;
         suspended++;
         generation++;
         timer.Change(Timeout.Infinite, Timeout.Infinite);
      }
   }

   /// <summary>Acknowledge successful local writes; failed writes retain the old comparison baseline.</summary>
   public void Resume(byte[] savedContents = null) {
      lock (gate) {
         if (disposed) return;
         if (savedContents != null) acceptedHash = SHA256.HashData(savedContents);
         if (suspended > 0) suspended--;
         generation++;
         retries = 0;
         retryExhausted = false;
         if (suspended == 0) timer.Change(quietMilliseconds, Timeout.Infinite);
      }
   }

   private async Task CheckAsync() {
      long observed;
      lock (gate) {
         if (disposed || suspended > 0 || running) return;
         running = true;
         observed = generation;
      }
      var retry = false;
      try {
         var first = TryRead(path);
         if (first == null) { retry = true; return; }
         var hash = SHA256.HashData(first);
         lock (gate) {
            if (disposed || suspended > 0 || observed != generation) return;
            if (acceptedHash != null && acceptedHash.SequenceEqual(hash)) return;
         }
         // Two complete equal snapshots reduce the chance of reloading an in-progress write.
         await Task.Delay(quietMilliseconds).ConfigureAwait(false);
         var second = TryRead(path);
         if (second == null || !SHA256.HashData(second).SequenceEqual(hash)) { retry = true; return; }
         dispatch(() => {
            lock (gate) {
               if (disposed || suspended > 0 || observed != generation ||
                   (acceptedHash != null && acceptedHash.SequenceEqual(hash))) return;
            }
            // Never hold the watcher lock across application callbacks or modal UI work.
            changed(second);
            lock (gate) {
               if (!disposed && observed == generation) acceptedHash = hash;
            }
         });
      } finally {
         lock (gate) {
            running = false;
            if (!disposed && suspended == 0) {
               if (generation != observed || (retry && retries++ < 20)) timer.Change(quietMilliseconds, Timeout.Infinite);
               else if (retry) {
                  var stamp = ReadStamp();
                  retryExhausted = stamp.Length > 0 && stamp.Length <= 64 * 1024 * 1024;
                  // Existing locked files retry later; missing/empty files wait for an event or metadata change.
               }
            }
         }
      }
   }

   private static byte[] TryRead(string path) {
      try {
         using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
         if (stream.Length == 0 || stream.Length > 64 * 1024 * 1024) return null;
         var length = stream.Length;
         var modified = File.GetLastWriteTimeUtc(path);
         var data = new byte[(int)length];
         stream.ReadExactly(data);
         if (stream.Length != length || File.GetLastWriteTimeUtc(path) != modified) return null;
         return data;
      } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return null; }
   }

   public void Dispose() {
      lock (gate) {
         if (disposed) return;
         disposed = true;
         generation++;
         timer.Dispose();
         reconciliationTimer.Dispose();
      }
      foreach (var watcher in watchers) watcher.Dispose();
   }
}
