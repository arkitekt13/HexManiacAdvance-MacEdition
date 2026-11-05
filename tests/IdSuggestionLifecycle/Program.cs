using HavenSoft.HexManiac.Review;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Performance;

if(args.Length==0) {
 int total=0;
 foreach(var name in new[]{"deletion-flag","deletion-variable","manual-flag","manual-variable","shared-flag","shared-variable","scopes","pending-syntax","drafts"}) {
  var start=new System.Diagnostics.ProcessStartInfo("dotnet"){RedirectStandardOutput=true,RedirectStandardError=true};
  start.ArgumentList.Add(System.Reflection.Assembly.GetExecutingAssembly().Location);start.ArgumentList.Add(name);
  using var child=System.Diagnostics.Process.Start(start)!;var output=child.StandardOutput.ReadToEnd();var errors=child.StandardError.ReadToEnd();child.WaitForExit();
  if(child.ExitCode!=0)throw new InvalidOperationException(name+": "+output+errors);
  total+=int.Parse(output.Split('\n').Single(l=>l.StartsWith("CHILD_CHECKS:")).Split(':')[1]);Console.WriteLine("PASS:"+name);
 }
 Console.WriteLine("CHECKS_PASSED:"+total);return;
}
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks=0;
void Check(bool ok,string message) { checks++;if(!ok)throw new InvalidOperationException(message); }
var fixture=new SyntheticMapFixture(24,20,withScriptEvent:false);var model=fixture.Model;
model.SetList(new NoDataChangeDeltaModel(),"MapAttributeBehaviors","Normal");
var token=new ModelDelta();
foreach(var at in new[]{0x104,0x108,0x10C})model.WritePointer(token,at,0x110);
model.WritePointer(token,0x128,0x160);
var format=model.GetNextRun(0x100).FormatString.Replace("events<>","events<"+new HavenSoft.HexManiac.Core.Models.Map.Format(model).EventsFormat+">");
var error=ArrayRun.TryParse(model,format[..^1]+"4",0x100,SortedSpan<int>.None,out var root);
Check(!error.HasError,"Synthetic map banks must parse.");
model.ObserveAnchorWritten(new NoDataChangeDeltaModel(),"data.maps.banks",root);
((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new("synthetic.layout","data.maps.banks/3/maps/0/map/0/layout/","Maps"));
var fs=new StubFileSystem{MetadataFor=_=>Array.Empty<string>()};
Application.Current!.Resources["FileSystem"]=fs;
var editor=new EditorViewModel(fs,InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
var viewport=new ViewPort("sprite-selection-fixture.bin",model,InstantDispatch.Instance,new Singletons(),tutorials:editor.MapTutorials,fs:fs);
viewport.InitializationWorkload.GetAwaiter().GetResult();
var mapper=viewport.MapEditor;

model.FreeSpaceStart=0x18000;
Array.Fill(model.RawData,(byte)0xFF,0x18000,model.Count-0x18000);
model.SetList(new NoDataChangeDeltaModel(),"FacingOptions","NoMovement","LookAround","Wander");
var parser=new HavenSoft.HexManiac.Core.Models.Code.ScriptParser(model.GetShortGameCode(),viewport.Singletons.ScriptLines,2);


int Peek(EventTemplate templates,bool variable=false){using var allocation=templates.BeginIdAllocation();return variable?templates.FindNextUnusedVariable():templates.FindNextUnusedFlag();}
if(args[0]=="scopes") {
 var templates=mapper.Templates;
 using(var outer=templates.BeginIdAllocation()) {
  var first=templates.FindNextUnusedFlag();var second=templates.FindNextUnusedFlag();Check(first==0x21&&second==0x22,"Multiple IDs in one action remain distinct before ROM writes.");
  using(var inner=templates.BeginIdAllocation())Check(templates.FindNextUnusedFlag()==0x23,"Nested actions respect their outer reservations.");
  Check(templates.FindNextUnusedFlag()==0x23,"Ending a nested action releases only its uncommitted reservation.");
 }
 Check(Peek(templates)==0x21&&Peek(templates,true)==0x4034,"Completed actions do not permanently consume IDs absent from ROM/drafts.");
 var one=templates.FindNextUnusedFlag();var two=templates.FindNextUnusedFlag();Check(one!=two,"Unowned standalone requests retain conservative distinct reservations.");
} else if(args[0] is "pending-syntax" or "drafts") {
 var f=new UsageDetectionFixture("BPRE");var dispatch=new WorkflowDispatch();var services=new Singletons(dispatch);
 Array.Fill(f.Model.RawData,(byte)0xFF,0x16000,f.Model.Count-0x16000);f.Model.FreeSpaceStart=0x16000;
 var change=new ModelDelta();change.ChangeData(f.Model,UsageDetectionFixture.Code+1,Enumerable.Repeat((byte)0xFF,64).ToArray());
 f.Parser.FormatScript<XSERun>(change,f.Model,UsageDetectionFixture.Code,1);
 f.Model.ObserveAnchorWritten(change,"draft.root",new XSERun(UsageDetectionFixture.Code,new SortedSpan<int>(0x300)));
 change.ChangeData(f.Model,UsageDetectionFixture.Child,new byte[]{2});f.Model.ObserveAnchorWritten(change,"draft.other",new XSERun(UsageDetectionFixture.Child));
 var view=new ViewPort("drafts.bin",f.Model,dispatch,services,fs:fs);view.InitializationWorkload.GetAwaiter().GetResult();
 var templates=new EventTemplate(InstantDispatch.Instance,f.Model,f.Parser,new[]{new ReadonlyPixelViewModel(16,16,new short[256])});
 var tool=view.Tools.CodeTool;tool.Investigator=templates;tool.Contents.Clear();view.Goto.Execute(UsageDetectionFixture.Code);tool.Mode=CodeMode.Script;tool.IsSelected=true;dispatch.Flush();
 view.ChangeHistory.ClearHistory(false);var baseline=f.Model.RawData.ToArray();var meta=f.Model.ExportMetadata(null,services.MetadataInfo).Serialize();
 if(args[0]=="pending-syntax") {
  var owner=new object();
  foreach(var tokenText in new[]{"0x4034","0X4034","$4034","16436"}) {
   templates.SetPendingScripts(owner,new[]{(f.Parser,$"setvar {tokenText} 1\nend")});Check(Peek(templates,true)!=0x4034,"Pending numeric forms use the compiler's variable conversion.");
  }
  foreach(var source in new[]{"setvar 0x4034","copyvar 0x4036 0x4034\nbadcommand","random 0x4034\nend","random 0x4034 extra","additem 1 0x4034 extra","subvar 0x40FE 0x4034 extra"}) {
   templates.SetPendingScripts(owner,new[]{(f.Parser,source)});Check(Peek(templates,true)!=0x4034,"Incomplete commands and confirmed source/VarGet operands remain protected.");
  }
  foreach(var source in new[]{"setvar 0x40FE 0x4034\nend","setvar 0x40FE 0x4034 extra","msgbox.npc <auto>\n{\nsetflag 0x21\nsetvar 0x4034 1\n}\nend","# setvar 0x4034 1\nend"}) {
   templates.SetPendingScripts(owner,new[]{(f.Parser,source)});Check(Peek(templates,true)==0x4034&&Peek(templates)==0x21,"Literal values, dialogue/resource content and comments do not consume IDs.");
  }
  foreach(var tokenText in new[]{"0x21","0X21","$21"}) {
   templates.SetPendingScripts(owner,new[]{(f.Parser,$"setflag {tokenText}\nbadcommand")});Check(Peek(templates)!=0x21,"Pending and rejected flag commands remain protected.");
  }
  templates.SetPendingScripts(owner,Array.Empty<(ScriptParser,string)>());Check(Peek(templates)==0x21&&Peek(templates,true)==0x4034,"Removing the source owner releases only its pending IDs.");
  Check(baseline.SequenceEqual(f.Model.RawData)&&meta.SequenceEqual(f.Model.ExportMetadata(null,services.MetadataInfo).Serialize())&&!view.Undo.CanExecute(null),"Pending-source analysis and suggestions never write ROM bytes, metadata or history.");
  var orphanSource="setflag 0x21\nsetvar 0x4034 1\nend";var orphanBytes=f.Parser.Compile(change,f.Model,UsageDetectionFixture.Child,ref orphanSource,out _,out _);
  change.ChangeData(f.Model,UsageDetectionFixture.Child,orphanBytes);f.Parser.FormatScript<XSERun>(change,f.Model,UsageDetectionFixture.Child,orphanBytes.Length);
  Check(Peek(templates)!=0x21&&Peek(templates,true)!=0x4034,"Known formatted scripts remain protected after losing their event roots.");
  change.ChangeData(f.Model,UsageDetectionFixture.Child,new byte[]{2});
  Check(Peek(templates)==0x21&&Peek(templates,true)==0x4034,"Removing the final known script references releases those IDs.");
  change.ChangeData(f.Model,0x15000,orphanBytes);Check(Peek(templates)==0x21&&Peek(templates,true)==0x4034,"Unformatted disconnected bytes are outside this bounded analysis.");
  f.Model.ObserveRunWritten(new NoDataChangeDeltaModel(),new XSERun(0x15000));
  Check(Peek(templates)!=0x21&&Peek(templates,true)!=0x4034,"Metadata-only discovery of a formatted script invalidates usage snapshots.");
 } else {
  var body=tool.Contents[0];var rejected="setflag 0x21\nsetvar 0x4034\nbadcommand";body.Content=rejected;
  Check(tool.HasPendingScriptChanges&&Peek(templates)!=0x21&&Peek(templates,true)!=0x4034,"Debounced manual source is protected before compilation.");
  dispatch.Flush();Check(body.HasError&&body.Content==rejected&&tool.HasUncompiledScriptChanges,"Rejected source and diagnostics are retained.");
  Check(Peek(templates)!=0x21&&Peek(templates,true)!=0x4034,"Failed compilation does not release retained source IDs.");
  view.Goto.Execute(UsageDetectionFixture.Child);dispatch.Flush();Check(Peek(templates)!=0x21&&Peek(templates,true)!=0x4034,"Navigation retains offscreen rejected-draft reservations.");
  view.Goto.Execute(UsageDetectionFixture.Code);dispatch.Flush();view.Undo.Execute(null);dispatch.Flush();
  Check(!tool.HasUncompiledScriptChanges&&baseline.SequenceEqual(f.Model.RawData),"Undo explicitly discards the rejected draft without writing model data.");
  Check(Peek(templates)==0x21&&Peek(templates,true)==0x4034,"Discarding a rejected source releases its IDs.");
  body=tool.Contents[0];body.Content="setflag \nend";body.Editor.CaretIndex="setflag ".Length;Check(body.CanInsertFlag,"Real assisted flag insertion is available.");body.InsertFlag();
  Check(body.Content.Contains("0x0021")&&Peek(templates)!=0x21,"Generated flag is reserved in pending source after its action scope ends.");
  dispatch.Flush();view.ChangeHistory.ChangeCompleted();Check(!body.HasError&&!tool.HasUncompiledScriptChanges&&Peek(templates)!=0x21,$"Successful compilation transfers protection to live ROM usage: error={body.HasError}/{body.ErrorText}, draft={tool.HasUncompiledScriptChanges}, content={body.Content}, root={f.Model.ReadPointer(0x300):X6}, used={string.Join(",",Flags.GetUsedItemFlags(f.Model,f.Parser))}.");
  var compiled=f.Model.RawData.ToArray();body=tool.Contents[0];body.Content="end";dispatch.Flush();view.ChangeHistory.ChangeCompleted();
  Check(Peek(templates)==0x21,"Correcting/removing the final compiled reference releases its flag.");
  view.Undo.Execute(null);dispatch.Flush();Check(compiled.SequenceEqual(f.Model.RawData)&&Peek(templates)!=0x21,"Undo restores exact bytes and excludes the restored flag.");
  view.Redo.Execute(null);dispatch.Flush();Check(Peek(templates)==0x21,"Redo releases the removed flag without retaining a ghost reservation.");
 }
 Check(!tool.HasUncompiledScriptChanges,"Test leaves no pending source behind.");
} else {
bool variable=args[0].EndsWith("variable");bool manual=args[0].StartsWith("manual");bool shared=args[0].StartsWith("shared");
var map=mapper.PrimaryMap;
if(!variable) {
 var first=map.CreateObjectEvent(0,HavenSoft.HexManiac.Core.ViewModels.DataFormats.Pointer.NULL);
 if(manual)first.Flag=0x21;else first.GenerateNewFlag();
 var id=first.Flag;Check(id==0x21,"Start with the first available ordinary flag.");
 if(shared){var other=map.CreateObjectEvent(0,HavenSoft.HexManiac.Core.ViewModels.DataFormats.Pointer.NULL);other.Flag=id;first=other;}
 viewport.ChangeHistory.ChangeCompleted();
 if(!manual){
  var before=model.RawData.ToArray();Check(first.Delete(),"Delete the actual object event.");viewport.ChangeHistory.ChangeCompleted();var deleted=model.RawData.ToArray();
  viewport.Undo.Execute(null);Check(before.SequenceEqual(model.RawData)&&Peek(mapper.Templates)!=id&&viewport.Redo.CanExecute(null),"Undo restores the event, blocks its flag and retains Redo.");
  viewport.Redo.Execute(null);Check(deleted.SequenceEqual(model.RawData),"Redo restores exact object deletion bytes.");
 }
 var used=Flags.GetUsedItemFlags(model,parser);Check(used.Contains(id)==(manual||shared),"Fresh scan must reflect the remaining flag references.");
 var next=map.CreateObjectEvent(0,HavenSoft.HexManiac.Core.ViewModels.DataFormats.Pointer.NULL);next.GenerateNewFlag();
 Check(next.Flag==(manual||shared?id+1:id),"Flag suggestion must reuse a deleted last reference and exclude manual/shared references.");
} else {
 var first=map.CreateScriptEvent();if(manual)first.Trigger=0x4034;else first.GenerateNewTrigger();
 var id=first.Trigger;Check(id==0x4034,"Start with the first available persistent variable.");
 if(shared){var other=map.CreateScriptEvent();other.Trigger=id;first=other;}
 viewport.ChangeHistory.ChangeCompleted();
 if(!manual){
  var before=model.RawData.ToArray();Check(first.Delete(),"Delete the actual script event.");viewport.ChangeHistory.ChangeCompleted();var deleted=model.RawData.ToArray();
  viewport.Undo.Execute(null);Check(before.SequenceEqual(model.RawData)&&Peek(mapper.Templates,true)!=id&&viewport.Redo.CanExecute(null),"Undo restores the event, blocks its variable and retains Redo.");
  viewport.Redo.Execute(null);Check(deleted.SequenceEqual(model.RawData),"Redo restores exact script-event deletion bytes.");
 }
 var used=Flags.GetUsedVariables(model,parser);Check(used.Contains(id)==(manual||shared),"Fresh scan must reflect remaining variable references.");
 var next=map.CreateScriptEvent();next.GenerateNewTrigger();
 Check(next.Trigger==(manual||shared?id+1:id),"Variable suggestion must reuse a deleted last reference and exclude manual/shared references.");
}
}
Console.WriteLine("CHILD_CHECKS:"+checks);
