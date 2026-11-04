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
if(args.Length<1 || args.Length>2 || (args.Length==2 && args[1]!="--probe" && args[1]!="--lookup" && args[1]!="--npc"))throw new ArgumentException("Expected an output JSON path and optional --probe, --lookup, or --npc.");
var probe=args.Length==2 && args[1]=="--probe";
var lookupOnly=args.Length==2 && args[1]=="--lookup";
var npc=args.Length==2 && args[1]=="--npc";
var warmups=npc?10:2;
const int samples=7;
var output=Path.GetFullPath(args[0]);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var rows=new List<object>();
var completionRows=new List<object>();
var checks=0;
void WriteReport(bool complete)=>File.WriteAllText(output,JsonSerializer.Serialize(new {
   revision=Environment.GetEnvironmentVariable("HMA_BENCHMARK_REVISION")??"working tree; identify revision before comparison",runtime=RuntimeInformation.FrameworkDescription,
   platform=RuntimeInformation.OSDescription,architecture=RuntimeInformation.ProcessArchitecture.ToString(),
   warmups,samplesPerCase=samples,checks,complete,componentProbe=probe,lookupProbe=lookupOnly,npc,
   scope="Synthetic one-character same-size edits; standalone production TextEditor/CodeTool, manually flushed debounce; excludes the 500ms wait, full TabView/map/event bindings, native input/compositor, relocation and real ROM workloads. UI-thread allocations only. Component probes are isolated calls, not additive attribution of a complete update. NPC sizes count actual source lines, including comments/dialogue; completion queries are measured separately.",results=rows,completions=completionRows,
},new JsonSerializerOptions { WriteIndented=true }));
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
void Check(bool condition,string message){checks++;if(!condition)throw new InvalidOperationException(message);}
const int start=0x1000;
string Source(string workload,int statements,int digit) {
   if(workload.StartsWith("npc-",StringComparison.Ordinal)) {
      var lines=new List<string>{"lock",$"setvar 0x4000 0x000{digit}","faceplayer","# NPC interaction 1"};
      var tail=new[]{"release","end","reaction:","setvar temp3 3","return"};
      for(int part=0;lines.Count+10+tail.Length<=statements;part++) {
         lines.AddRange(new[]{"# Ask about the village",$"if.compare.call temp1 == {part%3} <reaction>",
            $"msgbox.default <{0x20000+part*128:X6}>","{","Welcome to our village.","Take your time exploring.","}",
            $"setvar temp{part%10} {part%5}",$"checkflag 0x{0x200+part%16:X4}",$"setflag 0x{0x220+part%16:X4}"});
      }
      while(lines.Count+tail.Length<statements)lines.Add("# Additional conversation note");
      lines.AddRange(tail);return string.Join("\n",lines);
   }
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
foreach(var workload in npc?new[]{"npc-argument","npc-dialogue","npc-comment"}:probe||lookupOnly?new[]{"commands"}:new[]{"commands","comments","dialogue"})foreach(var statements in npc?new[]{100,250,500}:lookupOnly?new[]{500}:probe?new[]{500,2000}:new[]{100,500,2000}) {
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
      // A multi-section source may consolidate/relocate during initial setup. Measure
      // subsequent same-size edits at its settled address, not that one-time setup.
      var editingStart=body.Address;
      var canonical=body.Content;
      if(npc)Check(canonical.Count(c=>c=='\n')+1==statements,"NPC size must count actual displayed source lines.");
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
      var first=workload=="npc-dialogue"?canonical.IndexOf("Welcome",StringComparison.Ordinal):
         workload=="npc-comment"?canonical.IndexOf("# NPC interaction 1",StringComparison.Ordinal)+18:canonical.IndexOf("0x0001",StringComparison.Ordinal)+5;
      var replacements=workload=="npc-dialogue"?new[]{"X","Y"}:new[]{"2","3"};
      var inputs=replacements.Select(value=>canonical[..first]+value+canonical[(first+1)..]).ToArray();
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
            Check(body.Address==editingStart,"Same-size edits must not relocate the settled script.");
            if(workload=="npc-dialogue")Check(model[0x20000]==model.TextConverter.Convert(replacements[iteration%2],out _)[0],"Dialogue edit must update the intended PCS character.");
            else if(workload!="npc-comment")Check(model[editingStart+4]==(iteration%2==0?2:3),"Compiler must write the changed immediate.");
            var expected=initial.ToArray();
            if(workload=="npc-dialogue")expected[0x20000]=model.TextConverter.Convert(replacements[iteration%2],out _)[0];
            else if(workload!="npc-comment")expected[editingStart+4]=(byte)(iteration%2==0?2:3);
            Check(model.RawData.SequenceEqual(expected),"Edits must change exactly their intended byte; comments must not change any ROM bytes.");
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
            sizeUnit=npc?"actual source lines":"repeated statements",branchCalls=canonical.Split('\n').Count(line=>line.StartsWith("if.compare.call",StringComparison.Ordinal)),
            dialogueBlocks=canonical.Split('\n').Count(line=>line=="{"),
            attachedEditor=attached,keywords=body.Editor.Keywords.Count,constants=body.Editor.Constants.Count,
            immediateMedianMs=Median(editTimes),deferredCompileMedianMs=Median(compileTimes),presentationMedianMs=Median(presentationTimes),
            immediateAllocatedBytes=MedianBytes(editBytes),deferredCompileAllocatedBytes=MedianBytes(compileBytes),presentationAllocatedBytes=MedianBytes(presentationBytes),
            immediateSamplesMs=editTimes,deferredCompileSamplesMs=compileTimes,presentationSamplesMs=presentationTimes,
            contentEvents,highlightEvents,layerSha256=layerHashes,components,timerSamples,
            scriptStart=editingStart,sourceSha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))) });
         Console.WriteLine($"{workload} {statements} {(npc?"lines":"statements")} UI={attached}: edit {Median(editTimes):F2} ms, deferred {Median(compileTimes):F2} ms, presentation {Median(presentationTimes):F2} ms");
         WriteReport(false);
         if(npc && attached && workload=="npc-argument") {
            // Use an independent CodeBody: incomplete prefixes must not schedule a ROM compile.
            var completionBody=new CodeBody(model,tool.ScriptParser,null);tool.ScriptParser.AddKeywords(model,completionBody);
            var lines=canonical.Split('\n');
            var commandLines=Enumerable.Range(0,lines.Length).Where(i=>lines[i].StartsWith("setvar",StringComparison.Ordinal)).ToArray();
            var positions=new[]{0,commandLines[0],commandLines[commandLines.Length/2],commandLines[^1]};
            var completionControl=new TextEditor{DataContext=completionBody.Editor,FontSize=14};
            var overlay=new AutocompleteOverlay{DataContext=completionBody,Target=completionControl};
            window.Content=new Grid{Children={completionControl,overlay}};Layout();
            foreach(var query in new[]{"msgb","setvar tem"})foreach(var lineIndex in positions) {
               overlay.Target=null;
               var altered=lines.ToArray();altered[lineIndex]=query;completionBody.Content=string.Join("\n",altered);Layout();
               var before=model.RawData.ToArray();var times=new List<double>();var allocations=new List<long>();
               IReadOnlyList<AutocompleteItem> options=null;
               for(int sample=0;sample<warmups+samples;sample++) {
                  var result=Measure(()=>options=completionBody.GetTokenComplete(query,lineIndex,query.Length));
                  Check(options?.Count>0,"A valid command/variable prefix must offer completions.");
                  if(sample>=warmups){times.Add(result.Ms);allocations.Add(result.Bytes);}
               }
               var expectedOptions=options.Select(option=>option.Text).ToArray();
               completionControl.EditBox.CaretIndex=altered.Take(lineIndex).Sum(line=>line.Length+1)+query.Length;
               Layout();overlay.Target=completionControl;completionControl.EditBox.Focus();Layout();overlay.ClearAutocompleteOptions();
               string[] Shown()=>(overlay.FindControl<ItemsControl>("AutocompleteItems")!.ItemsSource as IEnumerable<AutoCompleteSelectionItem>)?.Select(option=>option.DisplayText).ToArray()??Array.Empty<string>();
               completionControl.EditBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));var immediate=Shown();Layout();
               var shown=Shown();
               Check(lineIndex!=0 || expectedOptions.SequenceEqual(shown),"The first-line autocomplete control must prove the routed handler is wired correctly.");
               Check(model.RawData.SequenceEqual(before),"Autocomplete must not edit ROM bytes.");
               completionRows.Add(new{sourceLines=statements,query,lineIndex,medianMs=Median(times),medianAllocatedBytes=MedianBytes(allocations),samplesMs=times,
                  expectedOptions,immediateOptions=immediate,shownOptions=shown,overlayMatches=expectedOptions.SequenceEqual(shown)});
               Console.WriteLine($"AUTOCOMPLETE {statements} lines line={lineIndex} {query}: {Median(times):F3} ms; core={expectedOptions.Length} overlay={shown.Length} matches={expectedOptions.SequenceEqual(shown)}");
            }
            overlay.Target=null;window.Content=control;Layout();WriteReport(false);
         }
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
