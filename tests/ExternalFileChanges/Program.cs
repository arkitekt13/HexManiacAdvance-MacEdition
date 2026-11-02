using System.Collections.Concurrent;
using System.Diagnostics;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
var root = Path.Combine(Path.GetTempPath(), "hma-file-changes-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var queue = new ConcurrentQueue<Action>();
void Pump() { while (queue.TryDequeue(out var action)) action(); }
async Task Wait(Func<bool> condition, string message, bool pump = true) {
   var deadline = Stopwatch.StartNew();
   while (deadline.Elapsed < TimeSpan.FromSeconds(6)) {
      if (pump) Pump();
      if (condition()) { Check(true, message); return; }
      await Task.Delay(25);
   }
   throw new Exception("Timed out: " + message);
}
async Task Settle() { await Task.Delay(650); Pump(); }
try {
   var path = Path.Combine(root,"test.gba");
   File.WriteAllBytes(path, new byte[] { 1 });
   var calls = 0; byte[] latest = null;
   using (var monitor = new ExternalFileMonitor(path, queue.Enqueue, data => { calls++; latest = data; })) {
      File.WriteAllBytes(path, new byte[] { 2, 3 });
      await Wait(() => latest?.SequenceEqual(new byte[] { 2, 3 }) == true, "Direct write detected.");
      await Settle(); Check(calls == 1, "Duplicate native notifications are coalesced by content.");
      for (byte i = 4; i < 12; i++) File.WriteAllBytes(path, new[] { i });
      await Wait(() => latest?.SequenceEqual(new byte[] { 11 }) == true, "Rapid writes deliver the final bytes.");
      await Settle(); Check(calls == 2, "Rapid writes form one reload batch.");
      AtomicFileWriter.Write(path, stream => stream.WriteByte(12));
      await Wait(() => latest?[0] == 12, "Atomic replacement detected.");
      File.Delete(path);
      await Settle(); Check(latest?[0] == 12, "Deletion does not replace editor data with empty content.");
      File.WriteAllBytes(path, new byte[] { 13 });
      await Wait(() => latest?[0] == 13, "Recreated path detected.");
      var away = Path.Combine(root,"away.gba");
      File.Move(path, away);
      await Settle(); Check(latest?[0] == 13, "Rename away retains existing editor data.");
      File.Move(away, path);
      File.WriteAllBytes(path, new byte[] { 14 });
      await Wait(() => latest?[0] == 14, "Rename back resumes detection.");
      var before = calls;
      File.WriteAllBytes(Path.Combine(root,"prefix-test.gba"), new byte[] { 88 });
      await Settle(); Check(calls == before, "Similar filenames are not matched by suffix.");
      File.WriteAllBytes(path, new byte[] { 14 });
      await Settle(); Check(calls == before, "Identical bytes do not reload.");
      using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
         locked.WriteByte(15); locked.Flush(true); monitor.Signal();
         await Settle(); Check(calls == before, "Temporary exclusive lock causes no callback or dialog.");
      }
      await Wait(() => latest?[0] == 15, "Retry detects unlocked content without another write event.");
      using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
         locked.WriteByte(21); locked.Flush(true); monitor.Signal();
         await Task.Delay(3800); Pump();
         Check(latest?[0] == 15, "A lock outlasting the first retry batch never replaces editor data.");
      }
      await Wait(() => latest?[0] == 21, "Reconciliation resumes checks after a longer lock.");
      File.WriteAllBytes(path, new byte[] { 16 });
      await Wait(() => !queue.IsEmpty, "Reload queued for UI.", pump:false);
      monitor.Suspend(); File.WriteAllBytes(path, new byte[] { 17 }); monitor.Resume(new byte[] { 17 });
      Pump(); Check(latest?[0] == 21, "Local save invalidates a queued external snapshot.");
      await Settle(); Check(latest?[0] == 21, "Acknowledged local save is not reloaded.");
      File.WriteAllBytes(path, new byte[] { 18 }); monitor.Signal();
      await Wait(() => latest?[0] == 18, "Rescan delivers new content after local save.");
      File.WriteAllBytes(path, Array.Empty<byte>());
      await Settle(); Check(latest?[0] == 18, "Transient truncation to zero is not loaded.");
      File.WriteAllBytes(path, new byte[] { 19 });
      await Wait(() => latest?[0] == 19, "Completed write after truncation is loaded.");
      File.WriteAllBytes(path, new byte[] { 20 });
      await Wait(() => !queue.IsEmpty, "Disposal test has queued work.", pump:false);
      monitor.Dispose(); Pump();
      Check(latest?[0] == 19, "Disposed monitor rejects already queued delivery.");
   }
   var throwPath = Path.Combine(root,"throw.gba"); File.WriteAllBytes(throwPath,new byte[] { 1 });
   var shouldThrow = true; var callbackRecovered = false;
   using (var monitor = new ExternalFileMonitor(throwPath, queue.Enqueue, _ => {
      if (shouldThrow) throw new InvalidOperationException("injected callback failure");
      callbackRecovered = true;
   })) {
      File.WriteAllBytes(throwPath,new byte[] { 2 });
      await Wait(() => !queue.IsEmpty, "Throwing callback queued.", pump:false);
      try { Pump(); throw new Exception("Expected callback failure"); }
      catch (InvalidOperationException) { Check(true,"Callback failure surfaced normally."); }
      shouldThrow = false; monitor.Signal();
      await Wait(() => callbackRecovered, "Callback failure does not wedge future notifications.");
   }
   if (!OperatingSystem.IsWindows()) {
      var target = Path.Combine(root,"target.gba"); var link = Path.Combine(root,"link.gba");
      File.WriteAllBytes(target,new byte[] { 1 }); File.CreateSymbolicLink(link,target);
      byte[] linkData = null;
      using var monitor = new ExternalFileMonitor(link, queue.Enqueue, data => linkData = data);
      AtomicFileWriter.Write(target, stream => stream.WriteByte(2));
      await Wait(() => linkData?[0] == 2, "Replacement of a symlink target is detected.");
   }
   // Exercise the real Mac adapter, including canonical paths, snapshot delivery and own-save suppression.
   var adapterPath = Path.Combine(root,"adapter.gba"); File.WriteAllBytes(adapterPath,new byte[] { 1 });
   using (var fs = new MacFileSystem(() => null, queue.Enqueue)) {
      var notifications = 0; byte last = 0;
      Action<IFileSystem> listener = system => { notifications++; last = system.LoadFile(adapterPath).Contents[0]; };
      fs.AddListenerToFile(adapterPath,listener);
      fs.AddListenerToFile(Path.Combine(root,".","adapter.gba"),listener);
      File.WriteAllBytes(adapterPath,new byte[] { 2 });
      await Wait(() => !queue.IsEmpty, "Adapter snapshot queued.", pump:false);
      using (var locked = new FileStream(adapterPath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) Pump();
      Check(last == 2 && notifications == 1, "Reload uses verified snapshot even if file is locked before UI delivery.");
      Check(fs.Save(new LoadedFile(adapterPath,new byte[] { 3 })), "Local save succeeds with watcher active.");
      await Settle(); Check(notifications == 1, "Real adapter suppresses its own save.");
      File.WriteAllBytes(adapterPath,new byte[] { 4 });
      await Wait(() => last == 4, "External changes still detected after local save.");
      File.SetAttributes(adapterPath, File.GetAttributes(adapterPath) | FileAttributes.ReadOnly);
      try { Check(fs.LoadFile(adapterPath).Contents[0] == 4, "Read-only ROMs do not require write access to load."); }
      finally { File.SetAttributes(adapterPath, File.GetAttributes(adapterPath) & ~FileAttributes.ReadOnly); }
      fs.RemoveListenerForFile(Path.Combine(root,".","adapter.gba"),listener);
      File.WriteAllBytes(adapterPath,new byte[] { 5 });
      await Settle(); Check(last == 4, "Canonical removal stops callbacks.");
      fs.AddListenerToFile(adapterPath,listener);
      File.WriteAllBytes(adapterPath,new byte[] { 6 });
      await Wait(() => !queue.IsEmpty, "Adapter disposal work queued.",pump:false);
      fs.Dispose(); Pump(); Check(last == 4, "Filesystem disposal cancels pending tab callbacks.");
   }
   Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(App).Assembly.Location)!);
   var singletons = new Singletons();
   var view = new ViewPort("synthetic.gba", new PokemonModel(new byte[512],singletons:singletons),InstantDispatch.Instance,singletons);
   view.ChangeHistory.TagAsSaved();
   var reads = 0;
   var stub = new StubFileSystem { LoadFile = _ => { reads++; return new LoadedFile("synthetic.gba",new byte[512]); } };
   view.Model.WriteValue(view.ChangeHistory.CurrentChange,0,42); view.ChangeHistory.ChangeCompleted();
   view.ConsiderReload(stub);
   Check(reads == 0 && view.Model[0] == 42, "Unsaved edits are not read over or replaced.");
   view.ChangeHistory.TagAsSaved();
   var deferred = new DeferredFileSystem { LoadFile = _ => { reads++; return new LoadedFile("synthetic.gba",new byte[512]); } };
   view.ConsiderReload(deferred);
   view.Model.WriteValue(view.ChangeHistory.CurrentChange,0,43); view.ChangeHistory.ChangeCompleted();
   deferred.Pending();
   Check(reads == 0 && view.Model[0] == 43, "Edits made while UI reload is queued are preserved.");
} finally { Directory.Delete(root,true); }
Console.WriteLine("PASS: native changes/replacements/renames, debounce, locks, snapshots, local saves, disposal and dirty-edit protection.");
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class DeferredFileSystem : StubFileSystem, IWorkDispatcher {
   public Action Pending;
   public Task WaitForRenderingAsync() => Task.CompletedTask;
   public void BlockOnUIWork(Action action) => Pending = action;
   public Task DispatchWork(Action action) { action(); return Task.CompletedTask; }
   public Task RunBackgroundWork(Action action) => Task.Run(action);
   public IDelayWorkTimer CreateDelayTimer() => throw new NotSupportedException();
}
