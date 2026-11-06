using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
var report = new List<object>();
void Record(object row) { report.Add(row); Console.WriteLine(JsonSerializer.Serialize(row)); }
void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
long Heap() => GC.GetTotalMemory(false);
long Rss() { using var process = Process.GetCurrentProcess(); return process.WorkingSet64; }
SyntheticMapFixture Fixture(int width, int height, int romMiB) {
   var f = new SyntheticMapFixture(width, height);
   f.Model.ExpandData(new ModelDelta(), romMiB * 1024 * 1024 - 1);
   f.Model.ObserveRunWritten(new NoDataChangeDeltaModel(), new XSERun(0x18000));
   f.Model[0x18000] = 2; f.ViewPort.Goto.Execute(0x18000);
   f.ViewPort.Tools.CodeTool.Mode = CodeMode.Script; f.ViewPort.Tools.SelectedIndex = 3;
   f.ViewPort.ScriptReferenceMapID = 0;
   f.ViewPort.ChangeHistory.ClearHistory(false); f.ViewPort.ChangeHistory.TagAsSaved();
   return f;
}
[MethodImpl(MethodImplOptions.NoInlining)]
WeakReference[] RenderAndRelease(ViewPort source, ScriptMapReferenceView pane) {
   var snapshot = ScriptMapSnapshot.Create(source, 0);
   pane.ShowSnapshot(snapshot);
   var bitmap = typeof(ScriptMapReferenceView).GetField("bitmap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(pane);
   return new[] { new WeakReference(snapshot), new WeakReference(snapshot.Image.PixelData), new WeakReference(bitmap) };
}
[MethodImpl(MethodImplOptions.NoInlining)]
WeakReference[] Burst(ViewPort source, int count, out long peakHeap, out long allocated, out double elapsed) {
   long start = GC.GetTotalAllocatedBytes(true); var time = Stopwatch.StartNew();
   var tasks = Enumerable.Range(0, count).Select(_ => ScriptMapSnapshot.CreateAsync(source, 0)).ToArray();
   peakHeap = Heap();
   while (!tasks.All(task => task.IsCompleted)) { peakHeap = Math.Max(peakHeap, Heap()); Thread.Sleep(1); }
   Task.WhenAll(tasks).GetAwaiter().GetResult();
   peakHeap = Math.Max(peakHeap, Heap()); allocated = GC.GetTotalAllocatedBytes(true) - start; elapsed = time.Elapsed.TotalMilliseconds;
   return tasks.SelectMany(task => new[] { new WeakReference(task.Result), new WeakReference(task.Result.Image.PixelData) }).ToArray();
}
if (!args.Contains("--closing-only") && !args.Contains("--trace-only"))
foreach (var (width, height, romMiB) in new[] { (80, 52, 16), (128, 128, 32), (256, 256, 32) }) {
   var f = Fixture(width, height, romMiB); var source = f.ViewPort;
   var hash = SHA256.HashData(f.Model.RawData); var metadata = f.Model.ExportMetadata(null, source.Singletons.MetadataInfo).Serialize();
   var pane = new ScriptMapReferenceView();
   RenderAndRelease(source, pane); pane.Clear(); Collect(); // Warm font/renderer/Skia caches before sampling.
   long baseline = Heap(), baselineRss = Rss(), allocatedStart = GC.GetTotalAllocatedBytes(true);
   var refs = new List<WeakReference>(); var samples = new List<long>(); var times = new List<double>();
   for (int cycle = 0; cycle < 8; cycle++) {
      var watch = Stopwatch.StartNew(); refs.AddRange(RenderAndRelease(source, pane)); times.Add(watch.Elapsed.TotalMilliseconds);
      pane.Clear(); Collect(); samples.Add(Heap());
   }
   Record(new { phase = "display-clear", width, height, romMiB, cycles = 8, baselineHeap = baseline,
      afterGcHeap = samples, baselineRss, afterRss = Rss(), allocated = GC.GetTotalAllocatedBytes(true) - allocatedStart,
      retainedSnapshots = refs.Where((_, i) => i % 3 == 0).Count(r => r.IsAlive),
      retainedPixelArrays = refs.Where((_, i) => i % 3 == 1).Count(r => r.IsAlive),
      retainedBitmapWrappers = refs.Where((_, i) => i % 3 == 2).Count(r => r.IsAlive), renderAndUploadMs = times });
   Collect(); baseline = Heap();
   var burstRefs = Burst(source, 4, out var peak, out var allocated, out var milliseconds);
   Collect();
   Record(new { phase = "four-background-renders", width, height, romMiB, baselineHeap = baseline, peakSampledHeap = peak,
      afterGcHeap = Heap(), allocated, elapsedMs = milliseconds, retainedPayloadObjects = burstRefs.Count(r => r.IsAlive) });
   bool unchanged = hash.SequenceEqual(SHA256.HashData(f.Model.RawData)) && metadata.SequenceEqual(f.Model.ExportMetadata(null, source.Singletons.MetadataInfo).Serialize());
   if (!unchanged || !source.ChangeHistory.IsSaved || source.ChangeHistory.Undo.CanExecute(null)) throw new Exception("Memory sampling changed the source model/history.");
   GC.KeepAlive(f); GC.KeepAlive(pane);
}

[MethodImpl(MethodImplOptions.NoInlining)]
WeakReference[] ObserveTab(TabView tab) {
   var pane=tab.FindControl<ScriptMapReferenceView>("ScriptMapPane");
   var bitmap=typeof(ScriptMapReferenceView).GetField("bitmap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(pane);
   return new[]{new WeakReference(tab),new WeakReference(bitmap)};
}
[MethodImpl(MethodImplOptions.NoInlining)]
(int Distinct,int Detached,int Handlers) Retention(ViewPort source,List<WeakReference> watched) {
   var tabs=watched.Select(r=>r.Target).OfType<TabView>().Distinct().ToArray();
   var handlers=(MulticastDelegate)typeof(ViewModelCore).GetField("PropertyChanged",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(source);
   return(tabs.Length,tabs.Count(t=>t.GetVisualRoot()==null),handlers?.GetInvocationList().Count(h=>h.Target is TabView)??0);
}
[MethodImpl(MethodImplOptions.NoInlining)]
int ClearDetachedContexts(ViewPort source) {
   var handlers=(MulticastDelegate)typeof(ViewModelCore).GetField("PropertyChanged",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(source);
   var detached=handlers.GetInvocationList().Select(h=>h.Target).OfType<TabView>().Distinct().Where(t=>t.GetVisualRoot()==null).ToArray();
   foreach(var tab in detached) {
      tab.DataContext=null;
      tab.FindControl<HexContent>("HexContent").ViewPort=null;
      foreach(var field in tab.GetVisualDescendants().OfType<TextEditor>()) field.DataContext=null;
   }
   return detached.Length;
}

// Actual TabView lifecycle: complete switches, rapid switching, then close/discard the UI host.
[MethodImpl(MethodImplOptions.NoInlining)]
(object Result, WeakReference[] References) UiCycles(bool mapView) {
   var a = Fixture(80, 52, 16); var b = Fixture(128, 128, 16);
   var hashA=SHA256.HashData(a.Model.RawData);var hashB=SHA256.HashData(b.Model.RawData);
   var fs = new StubFileSystem { MetadataFor = _ => Array.Empty<string>() };
   Application.Current.Resources["FileSystem"] = fs;
   var editor = new EditorViewModel(fs, InstantDispatch.Instance) { PreferScriptMapView = mapView };
   App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
   editor.Add(a.ViewPort); editor.Add(b.ViewPort); editor.GotoViewModel.ControlVisible = false;
   var window = new MainWindow { Width = 1400, Height = 900, FileSystem = fs, Editor = editor };
   window.Resources["FileSystem"] = fs; window.Show();
   void Pump() { Dispatcher.UIThread.RunJobs(); window.Measure(new Size(1400, 900)); window.Arrange(new Rect(0, 0, 1400, 900)); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
   TabView Active() => window.GetVisualDescendants().OfType<TabView>().First(t => ReferenceEquals(t.DataContext, editor.SelectedTab));
   void Ready() {
      var watch = Stopwatch.StartNew();
      while (true) {
         Pump();
         if (!editor.PreferScriptMapView) return;
         if (Active().FindControl<ScriptMapReferenceView>("ScriptMapPane").GetVisualDescendants().OfType<Image>().Single().Source != null) return;
         if (watch.Elapsed.TotalSeconds > 30) throw new TimeoutException("Preview did not settle.");
         Thread.Sleep(1);
      }
   }
   Ready(); Collect(); long baseline = Heap();
   var bitmaps = new List<WeakReference>(); var tabs = new List<WeakReference>(); var heaps = new List<long>();
   for (int i = 0; i < 12; i++) {
      editor.SelectedIndex = i % 2; Ready();
      var observed=ObserveTab(Active());tabs.Add(observed[0]);bitmaps.Add(observed[1]);
      editor.PreferScriptMapView = false; Pump(); Collect(); heaps.Add(Heap());
      editor.PreferScriptMapView = mapView; Ready();
   }
   long burstStart = Heap(), allocatedStart = GC.GetTotalAllocatedBytes(true); var burstWatch = Stopwatch.StartNew();
   for (int i = 0; i < 12; i++) { editor.SelectedIndex = i % 2; Pump(); }
   Ready(); editor.PreferScriptMapView = false; Pump();
   // Let invalidated workers complete before measuring retention rather than calling pending work a leak.
   var settle = Stopwatch.StartNew(); while (settle.ElapsedMilliseconds < 1500) { Pump(); Thread.Sleep(5); }
   Collect(); long afterBurst = Heap();
   var settledHeaps = new List<long>();
   for(int pass=0;pass<3;pass++) {
      var delay=Stopwatch.StartNew(); while(delay.ElapsedMilliseconds<1000) {Pump();Thread.Sleep(5);}
      Collect();settledHeaps.Add(Heap());
   }
   var references = bitmaps.Concat(tabs).Concat(new[] { new WeakReference(a.Model), new WeakReference(b.Model), new WeakReference(editor) }).ToArray();
   var retained=Retention(a.ViewPort,tabs);
   int cleaned=ClearDetachedContexts(a.ViewPort)+ClearDetachedContexts(b.ViewPort);
   Pump(); var cleanupWait=Stopwatch.StartNew();while(cleanupWait.ElapsedMilliseconds<1000){Pump();Thread.Sleep(5);}
   Collect();long afterDetachedCleanupHeap=Heap();
   var remaining=Retention(a.ViewPort,tabs);
   if(args.Contains("--trace-only") && !args.Contains("--no-root-trace")) {
      if (tabs[0].Target is TabView oldA) Console.WriteLine("RETAINING_PATH_A: "+RetentionTrace.Find(a.ViewPort,oldA));
      if (tabs[1].Target is TabView oldB) Console.WriteLine("RETAINING_PATH_B: "+RetentionTrace.Find(b.ViewPort,oldB));
   }
   bool sourceBytesUnchanged=hashA.SequenceEqual(SHA256.HashData(a.Model.RawData))&&hashB.SequenceEqual(SHA256.HashData(b.Model.RawData));
   if(!sourceBytesUnchanged)throw new Exception("Tab-lifecycle sampling changed ROM bytes.");
   var result = new { phase = "actual-tab-lifecycle", mapView, distinctTabViewsAlive = retained.Distinct,
      detachedTabViewsAlive = retained.Detached, sourceTabHandlers=retained.Handlers, cleanedDetachedControls=cleaned,
      afterDetachedCleanupHeap, detachedAfterCleanup=remaining.Detached, sourceTabHandlersAfterCleanup=remaining.Handlers, sourceBytesUnchanged, baselineHeap = baseline, afterGcHexHeaps = heaps,
      oldBitmapWrappersAlive = bitmaps.Count(r => r.IsAlive), oldTabViewsAlive = tabs.Count(r => r.IsAlive), settledHeaps, burstStartHeap = burstStart, afterBurstGcHeap = afterBurst,
      burstAllocated = GC.GetTotalAllocatedBytes(true) - allocatedStart, burstMs = burstWatch.Elapsed.TotalMilliseconds };
   window.Editor = null; window.Close(); Pump();
   return (result, references);
}
if (!args.Contains("--closing-only"))
foreach(bool mapView in new[]{false,true}) {
   var ui = UiCycles(mapView); Collect(); Dispatcher.UIThread.RunJobs(); Collect();
   Record(ui.Result);
   Record(new { phase = "after-window-close", mapView, watchedObjects = ui.References.Length, objectsStillAlive = ui.References.Count(r => r.IsAlive) });
}
// Closing individual documents while a different document/window remains open.
[MethodImpl(MethodImplOptions.NoInlining)]
WeakReference[] AddRenderClose(EditorViewModel editor, MainWindow window, StubFileSystem fs, Action ready) {
   var f=Fixture(80,52,16);editor.Add(f.ViewPort);ready();
   var tab=window.GetVisualDescendants().OfType<TabView>().First(t=>ReferenceEquals(t.DataContext,f.ViewPort));
   var watched=ObserveTab(tab).Concat(new[]{new WeakReference(f.ViewPort),new WeakReference(f.Model)}).ToArray();
   f.ViewPort.Close.Execute(fs);
   if(ReferenceEquals(editor.SelectedTab,f.ViewPort))throw new Exception("Owned synthetic document did not close.");
   return watched;
}
[MethodImpl(MethodImplOptions.NoInlining)]
object ClosingDocuments() {
   var keep=Fixture(80,52,16);var hash=SHA256.HashData(keep.Model.RawData);
   var fs=new StubFileSystem{MetadataFor=_=>Array.Empty<string>()};Application.Current.Resources["FileSystem"]=fs;
   var editor=new EditorViewModel(fs,InstantDispatch.Instance){PreferScriptMapView=true};
   App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);editor.Add(keep.ViewPort);editor.GotoViewModel.ControlVisible=false;
   var window=new MainWindow{Width=1400,Height=900,FileSystem=fs,Editor=editor};window.Resources["FileSystem"]=fs;window.Show();
   void Pump(){Dispatcher.UIThread.RunJobs();window.Measure(new Size(1400,900));window.Arrange(new Rect(0,0,1400,900));AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
   void Ready(){var watch=Stopwatch.StartNew();while(true){Pump();var tab=window.GetVisualDescendants().OfType<TabView>().First(t=>ReferenceEquals(t.DataContext,editor.SelectedTab));
      if(tab.FindControl<ScriptMapReferenceView>("ScriptMapPane").GetVisualDescendants().OfType<Image>().FirstOrDefault()?.Source!=null)return;
      if(watch.Elapsed.TotalSeconds>30)throw new TimeoutException("Close probe preview did not settle.");Thread.Sleep(1);}}
   Ready();Collect();long baseline=Heap();var watched=new List<WeakReference>();var heaps=new List<long>();
   for(int i=0;i<4;i++){watched.AddRange(AddRenderClose(editor,window,fs,Ready));Ready();var wait=Stopwatch.StartNew();while(wait.ElapsedMilliseconds<500){Pump();Thread.Sleep(5);}Collect();heaps.Add(Heap());}
   bool unchanged=hash.SequenceEqual(SHA256.HashData(keep.Model.RawData));if(!unchanged)throw new Exception("Closing documents changed the remaining ROM.");
   var result=new{phase="close-four-documents-window-stays-open",baselineHeap=baseline,afterGcHeaps=heaps,
      closedTabViewsAlive=watched.Where((_,i)=>i%4==0).Count(r=>r.IsAlive),closedBitmapWrappersAlive=watched.Where((_,i)=>i%4==1).Count(r=>r.IsAlive),
      closedDocumentsAlive=watched.Where((_,i)=>i%4==2).Count(r=>r.IsAlive),closedModelsAlive=watched.Where((_,i)=>i%4==3).Count(r=>r.IsAlive),remainingSourceBytesUnchanged=unchanged};
   window.Editor=null;window.Close();Pump();return result;
}
if(!args.Contains("--trace-only"))Record(ClosingDocuments());
var output=args.FirstOrDefault(a=>!a.StartsWith("--"));
if (output!=null) File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
