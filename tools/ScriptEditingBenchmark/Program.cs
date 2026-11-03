using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

// Investigation only: production editor/compiler, fabricated data, no supplied ROM or network.
if(args.Length<1 || args.Length>2 || (args.Length==2 && args[1]!="--probe" && args[1]!="--lookup"))throw new ArgumentException("Expected an output JSON path and optional --probe or --lookup.");
var probe=args.Length==2 && args[1]=="--probe";
var lookupOnly=args.Length==2 && args[1]=="--lookup";
var output=Path.GetFullPath(args[0]);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var rows=new List<object>();
var checks=0;
void WriteReport(bool complete)=>File.WriteAllText(output,JsonSerializer.Serialize(new {
   revision=Environment.GetEnvironmentVariable("HMA_BENCHMARK_REVISION")??"working tree; identify revision before comparison",runtime=RuntimeInformation.FrameworkDescription,
   platform=RuntimeInformation.OSDescription,architecture=RuntimeInformation.ProcessArchitecture.ToString(),
   warmups=2,samplesPerCase=7,checks,complete,componentProbe=probe,lookupProbe=lookupOnly,
   scope="Synthetic one-character same-size edits; standalone production TextEditor/CodeTool, manually flushed debounce; excludes the 500ms wait, full TabView/map/event bindings, native input/compositor, relocation and real ROM workloads. UI-thread allocations only. Component probes are isolated calls, not additive attribution of a complete update.",results=rows,
},new JsonSerializerOptions { WriteIndented=true }));
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
void Check(bool condition,string message){checks++;if(!condition)throw new InvalidOperationException(message);}
const int start=0x1000;
const int warmups=2,samples=7;
string Source(string workload,int statements,int digit) {
   var text=new StringBuilder("lock\nsetvar 0x4000 0x000"+digit+"\n");
   for(int i=0;i<statements;i++) {
      text.AppendLine(workload switch {
         "commands"=>"setvar 0x4001 0x0001",
         "comments"=>"setvar 0x4001 0x0001 # NPC interaction",
         "dialogue" when i%20==0=>$"msgbox.default <{0x20000+i*32:X6}>\n{{\nHello there!\n}}",
         _=>"setvar 0x4001 0x0001",
      });
   }
   return text.Append("release\nend").ToString();
}
double Median(IEnumerable<double> values)=>values.Order().ElementAt(samples/2);
long MedianBytes(IEnumerable<long> values)=>values.Order().ElementAt(samples/2);
(double Ms,long Bytes) Measure(Action action) {
   var bytes=GC.GetAllocatedBytesForCurrentThread();var timestamp=Stopwatch.GetTimestamp();
   action();return(Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds,GC.GetAllocatedBytesForCurrentThread()-bytes);
}
foreach(var workload in probe||lookupOnly?new[]{"commands"}:new[]{"commands","comments","dialogue"})foreach(var statements in lookupOnly?new[]{500}:probe?new[]{500,2000}:new[]{100,500,2000}) {
   foreach(var attached in probe||lookupOnly?new[]{false}:new[]{false,true}) {
      var dispatch=new BenchmarkDispatch();var singletons=new Singletons(dispatch);
      var data=Enumerable.Repeat((byte)0xFF,0x40000).ToArray();
      // A synthetic BPRE header enables the same script definitions; it contains no game assets.
      Encoding.ASCII.GetBytes("BPRE").CopyTo(data,0xAC);data[0xBC]=0;
      var model=new PokemonModel(data,singletons:singletons);
      model.LoadMetadata(BaseModel.GetDefaultMetadatas().First());
      var viewport=new ViewPort("synthetic-script.bin",model,InstantDispatch.Instance,singletons);
      viewport.InitializationWorkload.GetAwaiter().GetResult();
      var tool=viewport.Tools.CodeTool;
      var seed=Source(workload,statements,1);var token=new ModelDelta();
      var code=tool.ScriptParser.Compile(token,model,start,ref seed,out var _,out var _);
      Check(code!=null,"Seed script must compile.");token.ChangeData(model,start,code);
      model.ObserveRunWritten(token,new XSERun(start));
      viewport.Goto.Execute(start);tool.Mode=CodeMode.Script;tool.IsSelected=true;
      Check(tool.Contents.Count>0,"CodeTool must expose a real script body.");
      var body=tool.Contents[0];
      // Decompilation cannot reconstruct source comments; put the original compiled source
      // into the live body before measuring so the comment workload actually contains them.
      body.Content=seed;dispatch.Flush();
      var canonical=body.Content;
      Check(workload!="comments" || canonical.Contains("# NPC interaction"),"Comment workload must retain source comments.");
      Check(canonical.Contains("0x0001"),"Editable immediate must survive initial decompilation.");
      if(lookupOnly) {
         var before=model.RawData.ToArray();var options=model.GetOptions("scriptvariablealiases");
         var variants=new Dictionary<string,string> {
            ["hexadecimal"]=canonical,["decimal"]=canonical.Replace("0x4001","16385"),["alias"]=canonical.Replace("0x4001","temp1"),
         };
         foreach(var (name,source) in variants) {
            var times=new List<double>();var allocations=new List<long>();
            for(int iteration=0;iteration<warmups+samples;iteration++) {
               byte[] result=null;var local=source;
               var measurement=Measure(()=>result=tool.ScriptParser.Compile(new ModelDelta(),model,start,ref local,out var _,out var _));
               Check(result!=null && result.SequenceEqual(code),"Equivalent argument spellings must compile to identical bytes.");
               Check(model.RawData.SequenceEqual(before),"Isolated argument compilation must not mutate the synthetic model.");
               if(iteration>=warmups){times.Add(measurement.Ms);allocations.Add(measurement.Bytes);}
            }
            rows.Add(new { name,statements,optionsCount=options.Count,emptyOptions=options.Count(string.IsNullOrEmpty),medianMs=Median(times),medianAllocatedBytes=MedianBytes(allocations),samplesMs=times,compiledSha256=Convert.ToHexString(SHA256.HashData(code)) });
            Console.WriteLine($"{name}: {Median(times):F2} ms; {MedianBytes(allocations):N0} allocated bytes; {options.Count} alias slots");
            WriteReport(false);
         }
         continue;
      }
      var first=canonical.IndexOf("0x0001",StringComparison.Ordinal)+5;
      var inputs=new[]{canonical[..first]+"2"+canonical[(first+1)..],canonical[..first]+"3"+canonical[(first+1)..]};
      var initial=model.RawData.ToArray();
      var editTimes=new List<double>();var compileTimes=new List<double>();var presentationTimes=new List<double>();
      var editBytes=new List<long>();var compileBytes=new List<long>();var presentationBytes=new List<long>();
      var timerSamples=new List<object>();
      var highlightEvents=0;var contentEvents=0;
      body.Editor.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(TextEditorViewModel.PlainContent))highlightEvents++;if(e.PropertyName==nameof(TextEditorViewModel.Content))contentEvents++;};
      Window window=null;TextEditor control=null;
      void Layout() {
         Dispatcher.UIThread.RunJobs();
         if(window!=null){window.Measure(new Size(900,650));window.Arrange(new Rect(0,0,900,650));AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
      }
      if(attached) {
         var editor=new EditorViewModel(new StubFileSystem(),InstantDispatch.Instance);
         App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
         control=new TextEditor { DataContext=body.Editor,FontSize=14 };
         window=new Window { Width=900,Height=650,Content=control };window.Show();Layout();
      }
      try {
         for(int iteration=0;iteration<warmups+samples;iteration++) {
            var input=inputs[iteration%2];body.Editor.CaretIndex=first;
            var before=model.RawData.ToArray();var oldHighlights=highlightEvents;var oldContents=contentEvents;
            var edit=Measure(()=>{if(control==null)body.Editor.Content=input;else control.EditBox.Text=input;});
            // Drain text notifications/layout before the delayed compiler, like the native timer gap.
            var presentation=Measure(Layout);
            Check(body.Content==input,"A one-character edit must reach CodeBody unchanged.");
            Check(model.RawData.SequenceEqual(before),"Immediate typing must leave ROM bytes untouched until the delayed compile.");
            Check(dispatch.PendingCount>0,"Typing must schedule deferred work.");
            var compile=Measure(dispatch.Flush);
            var afterCompile=Measure(Layout);
            Check(!body.HasError && !tool.ShowErrorText,"Measured scripts must compile without errors.");
            Check(body.Address==start,"Same-size edits must not relocate the script.");
            Check(model[start+4]==(iteration%2==0?2:3),"Compiler must write the changed immediate.");
            Check(model.RawData.Where((value,index)=>value!=initial[index]).Count()==1,
               "Same-size immediate edits must not modify unrelated bytes.");
            if(iteration>=warmups) {
               editTimes.Add(edit.Ms);editBytes.Add(edit.Bytes);compileTimes.Add(compile.Ms);compileBytes.Add(compile.Bytes);
               presentationTimes.Add(presentation.Ms+afterCompile.Ms);presentationBytes.Add(presentation.Bytes+afterCompile.Bytes);
               timerSamples.Add(dispatch.LastFlush.ToArray());
            }
            Check(contentEvents-oldContents>=1 && highlightEvents-oldHighlights>=1,"Measured edits must run the real content/highlight path.");
         }
         var layerHashes=new[]{body.Editor.PlainContent,body.Editor.AccentContent,body.Editor.ConstantContent,
            body.Editor.NumericContent,body.Editor.CommentContent,body.Editor.TextContent}
            .Select(layer=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(layer)))).ToArray();
         var components=new List<object>();
         if(probe) {
            var actions=new Dictionary<string,Action> {
               ["compile-only"]=()=>{var source=body.Content;var bytes=tool.ScriptParser.Compile(new ModelDelta(),model,start,ref source,out var moved,out var _);Check(bytes!=null && moved.Count==0,"Probe compile must succeed without moving data.");},
               ["find-length"]=()=>tool.ScriptParser.FindLength(model,start),
               ["collect-scripts"]=()=>tool.ScriptParser.CollectScripts(model,start),
               ["format-script"]=()=>tool.ScriptParser.FormatScript<XSERun>(new ModelDelta(),model,start,body.CompiledLength),
               ["text-length-check"]=body.EvaluateTextLength,
            };
            foreach(var (name,action) in actions) {
               var times=new List<double>();var allocations=new List<long>();
               for(int sample=0;sample<warmups+samples;sample++){var measurement=Measure(action);if(sample>=warmups){times.Add(measurement.Ms);allocations.Add(measurement.Bytes);}}
               components.Add(new {name,medianMs=Median(times),medianAllocatedBytes=MedianBytes(allocations),samplesMs=times});
               Console.WriteLine($"  {name}: {Median(times):F2} ms");
            }
         }
         rows.Add(new { workload,statements,sourceLines=canonical.Count(c=>c=='\n')+1,characters=canonical.Length,
            attachedEditor=attached,keywords=body.Editor.Keywords.Count,constants=body.Editor.Constants.Count,
            immediateMedianMs=Median(editTimes),deferredCompileMedianMs=Median(compileTimes),presentationMedianMs=Median(presentationTimes),
            immediateAllocatedBytes=MedianBytes(editBytes),deferredCompileAllocatedBytes=MedianBytes(compileBytes),presentationAllocatedBytes=MedianBytes(presentationBytes),
            immediateSamplesMs=editTimes,deferredCompileSamplesMs=compileTimes,presentationSamplesMs=presentationTimes,
            contentEvents,highlightEvents,layerSha256=layerHashes,components,timerSamples });
         Console.WriteLine($"{workload} {statements} statements UI={attached}: edit {Median(editTimes):F2} ms, deferred {Median(compileTimes):F2} ms, presentation {Median(presentationTimes):F2} ms");
         WriteReport(false);
      } finally { if(window!=null){window.Content=null;window.Close();Layout();} }
   }
}
WriteReport(true);
Console.WriteLine($"CHECKS_PASSED:{checks}");

// Keep timer scheduling intact, but flush its action explicitly outside the immediate-edit timer.
sealed class BenchmarkDispatch : IWorkDispatcher {
   readonly List<Timer> timers=new();
   public List<object> LastFlush { get; }=new();
   public int PendingCount=>timers.Count(timer=>timer.HasScheduledWork);
   public void Flush(){LastFlush.Clear();for(int pass=0;PendingCount>0;pass++){if(pass>20)throw new InvalidOperationException("Deferred work did not settle.");foreach(var timer in timers.ToArray())if(timer.HasScheduledWork){var timestamp=Stopwatch.GetTimestamp();timer.Flush();LastFlush.Add(new{timer.Owner,elapsedMs=Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds});}}}
   public Task WaitForRenderingAsync()=>Task.CompletedTask;
   public void BlockOnUIWork(Action action)=>action();
   public Task DispatchWork(Action action){action();return Task.CompletedTask;}
   public Task RunBackgroundWork(Action action)=>DispatchWork(action);
   public IDelayWorkTimer CreateDelayTimer(){var timer=new Timer { Owner=new StackTrace(1,false).GetFrame(0)?.GetMethod()?.DeclaringType?.Name??"unknown" };timers.Add(timer);return timer;}
   sealed class Timer : IDelayWorkTimer {
      public string Owner;
      Action pending;
      public bool HasScheduledWork=>pending!=null;
      public DelayWorkResult DelayCall(TimeSpan delay,Action action){var result=pending==null?DelayWorkResult.WorkScheduled:DelayWorkResult.WorkScheduledAndPreviousWorkCleared;pending=action;return result;}
      public void Reset()=>pending=null;
      public void Flush(){var work=pending;pending=null;work?.Invoke();}
   }
}
