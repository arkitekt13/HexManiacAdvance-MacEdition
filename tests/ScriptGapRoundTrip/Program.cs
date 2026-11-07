using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Review;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
foreach(int gap in new[]{1,2,3})foreach(bool shared in new[]{false,true}){
 using var f=new GapFixture(gap,0xFF,shared);
 var before=f.State();var bodies=f.Tool.Contents.ToArray();
 Check(bodies.Length==2,"Referenced scripts across FF padding must remain separate editable bodies.");
 Check(bodies.All(b=>!b.Content.Contains(".raw")),"Opening padded sections must not generate uncompilable raw directives.");
 Check(bodies[0].CompiledLength==5&&bodies[1].Address==f.Child,"Root extent must stop at goto and preserve the child address.");
 Check(before.EqualsModel(f.View.Model),"Reading padded sections must preserve bytes and metadata.");
 foreach(var body in bodies){var source=body.Content;var copy=source;var bytes=f.Parser.Compile(new ModelDelta(),f.Model,body.Address,ref copy,out _,out _);Check(bytes!=null,"Every generated source body must compile.");}
 // Root edit stays the same size and must not absorb or clear the child/gap.
 f.Edit(bodies[0],bodies[0].Content+"\n# root comment");
 Check(!f.Tool.HasUncompiledScriptChanges&&before.EqualsModel(f.Model),"Comment edit must preserve exact layout and metadata.");
 var childBody=f.Tool.Contents.Single(b=>b.Address==f.Child);var sourceText=childBody.Content;
 f.Edit(childBody,sourceText.Replace("  end","  setvar temp1 8\n  end"));
 Check(!f.Tool.HasUncompiledScriptChanges&&f.Model.ReadPointer(f.Root+1)==f.Child,"Child growth must preserve its owner pointer when space is available.");
 Check(Enumerable.Range(f.Root+5,gap).All(at=>f.Model[at]==0xFF),"Child edits must preserve gap bytes.");
 var grown=f.State();f.View.Undo.Execute(null);f.Dispatch.Flush();Check(before.EqualsModel(f.Model),"One Undo must restore every byte and structural metadata.");
 f.View.Redo.Execute(null);f.Dispatch.Flush();Check(grown.EqualsModel(f.Model),"Redo must restore the successful child compilation exactly.");
 f.View.Undo.Execute(null);f.Dispatch.Flush();f.Tool.FlushPendingScriptChanges();
 var bodyAgain=f.Tool.Contents.Single(b=>b.Address==f.Child);
 f.Edit(bodyAgain,bodyAgain.Content+"\nnot_a_command");
 Check(f.Tool.HasUncompiledScriptChanges&&bodyAgain.HasError&&before.EqualsModel(f.Model),"Rejected child source must retain diagnostics and preserve bytes/metadata.");
 f.Edit(bodyAgain,sourceText);Check(!f.Tool.HasUncompiledScriptChanges&&!bodyAgain.HasError&&before.EqualsModel(f.Model),"Correction/retry must settle without altering padded sections.");
 // Force real relocation beyond a named guard; the parent and any shared pointer must follow.
 var guard=f.Child+6;var token=new ModelDelta();token.ChangeData(f.Model,guard,new byte[]{0xDE,0xAD,0xBE,0xEF});f.Model.ObserveAnchorWritten(token,"gap.guard",new NoInfoRun(guard));f.View.ChangeHistory.ClearHistory(false);f.View.ChangeHistory.TagAsSaved();
 var guarded=f.State();var oldPointer=f.Child;bodyAgain=f.Tool.Contents.Single(b=>b.Address==f.Child);
 var longSource="section0:\nmsgbox.npc <auto>\n{\nPreserved dialogue resource.\n}\n"+string.Concat(Enumerable.Repeat("setvar temp1 9\n",20))+"end";f.Edit(bodyAgain,longSource);
 var moved=f.Model.ReadPointer(f.Root+1);Check(!f.Tool.HasUncompiledScriptChanges&&moved!=oldPointer&&moved>=0,"Blocked child growth must relocate and repoint its parent.");
 if(shared)Check(f.Model.ReadPointer(0x200)==moved,"Shared callers must follow child relocation.");
 var textAddress=f.Model.ReadPointer(moved+2);
 Check(f.Model.GetNextRun(textAddress) is PCSRun&&PCSString.ReadString(f.Model,textAddress,true)>1,"Deferred dialogue bytes and resource metadata must survive script relocation.");
 Check(f.Model.RawData.Skip(guard).Take(4).SequenceEqual(new byte[]{0xDE,0xAD,0xBE,0xEF})&&Enumerable.Range(f.Root+5,gap).All(at=>f.Model[at]==0xFF),"Relocation must preserve the gap and named neighbor.");
 var movedState=f.State();f.View.Undo.Execute(null);f.Dispatch.Flush();Check(guarded.EqualsModel(f.Model),"Relocation Undo must restore bytes, pointers and metadata.");f.View.Redo.Execute(null);f.Dispatch.Flush();Check(movedState.EqualsModel(f.Model),"Relocation Redo must restore bytes, pointers and metadata.");
}
foreach(var config in new[]{(0,(byte)0xFF),(1,(byte)0x02),(1,(byte)0x00)}){
 using var f=new GapFixture(config.Item1,config.Item2,false);
 Check(f.Tool.Contents.Count==1,"Adjacent sections and existing valid one-byte commands keep their combined editing behavior.");
 var source=f.Tool.Contents[0].Content;var bytes=f.Parser.Compile(new ModelDelta(),f.Model,f.Root,ref source,out _,out _);
 Check(bytes!=null&&!source.Contains(".raw"),"Legacy combined sections must remain compilable.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
sealed class GapFixture:IDisposable{
 public int Root=>0x1000;public int Child{get;}public WorkflowDispatch Dispatch{get;}=new();public Singletons Services{get;}public PokemonModel Model{get;}public ViewPort View{get;}public CodeTool Tool=>View.Tools.CodeTool;public ScriptParser Parser=>Tool.ScriptParser;
 public GapFixture(int gap,byte padding,bool shared){
  Services=new Singletons(Dispatch);var data=Enumerable.Repeat((byte)0xFF,0x10000).ToArray();System.Text.Encoding.ASCII.GetBytes("BPRE").CopyTo(data,0xAC);data[0xBC]=0;
  Model=new PokemonModel(data,singletons:Services);Model.LoadMetadata(BaseModel.GetDefaultMetadatas().First());var parser=new ScriptParser(Model.GetShortGameCode(),Services.ScriptLines,2);var t=new ModelDelta();Child=Root+5+gap;
  t.ChangeData(Model,Root,5);Model.WritePointer(t,Root+1,Child);for(int i=0;i<gap;i++)t.ChangeData(Model,Root+5+i,padding);t.ChangeData(Model,Child,new byte[]{0x16,1,0x40,7,0,2});parser.FormatScript<XSERun>(t,Model,Root);Model.ObserveAnchorWritten(t,"gap.root",new XSERun(Root));
  if(shared){Model.WritePointer(t,0x200,Child);Model.ObserveRunWritten(t,new PointerRun(0x200));}
  View=new ViewPort("synthetic-gap.gba",Model,Dispatch,Services);View.InitializationWorkload.GetAwaiter().GetResult();View.Goto.Execute(Root);Tool.Mode=CodeMode.Script;Tool.IsSelected=true;Dispatch.Flush();View.ChangeHistory.ClearHistory(false);View.ChangeHistory.TagAsSaved();
 }
 public Snapshot State()=>new(Model.RawData.ToArray(),Metadata(Model));
 static readonly Singletons metadataServices=new();
 public static string[] Metadata(IDataModel m)=>m.ExportMetadata(null,metadataServices.MetadataInfo).Serialize().Where(l=>!l.StartsWith("FreeSpaceSearch = ")).ToArray();
 public void Edit(CodeBody b,string source){b.Editor.Content=source;Dispatch.Flush();View.ChangeHistory.ChangeCompleted();}
 public void Dispose()=>Dispatch.Clear();
}
record Snapshot(byte[] Bytes,string[] Metadata){public bool EqualsModel(IDataModel m)=>Bytes.SequenceEqual(m.RawData)&&Metadata.SequenceEqual(GapFixture.Metadata(m));}
