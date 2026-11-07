using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;
using HavenSoft.HexManiac.Review;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
foreach(bool sharedObject in new[]{false,true}){
 var f=new SignFixture(sharedObject);var bytes=f.Bytes();var meta=f.Metadata();var originalSource=f.Body.Content;
 f.Edit(string.Concat(Enumerable.Repeat("setvar 0x4001 4\n",30))+"end");int moved=f.Body.Address;
 Check(!f.Body.HasError&&moved!=SignFixture.Root,"A formatted script with no ordinary pointer sources genuinely relocates.");
 for(int i=0;i<5;i++)Check(f.Model.ReadPointer(SignFixture.Signs+i*12+8)==moved,"Script signpost kind "+i+" must follow relocation.");
 for(int i=5;i<10;i++)Check(f.Model.ReadPointer(SignFixture.Signs+i*12+8)==SignFixture.Root,"Hidden/base/unsupported payload must remain bit-identical despite looking like a pointer.");
 if(sharedObject)Check(f.Model.ReadPointer(0x17010)==moved,"Ordinary object references still relocate alongside signpost references.");
 var grown=f.Bytes();var grownMeta=f.Metadata();f.View.Undo.Execute(null);f.Dispatch.Flush();
 Check(bytes.SequenceEqual(f.Bytes())&&meta.SequenceEqual(f.Metadata())&&f.View.ChangeHistory.IsSaved,"One Undo restores all script/signpost/object bytes, metadata and saved state.");
 Check(f.View.Redo.CanExecute(null),"Undo preserves Redo after pointer repair.");f.View.Redo.Execute(null);f.Dispatch.Flush();
 Check(grown.SequenceEqual(f.Bytes())&&grownMeta.SequenceEqual(f.Metadata()),"Redo restores all relocated pointers exactly.");
 // Failed dialogue/resource edits remain atomic, including every raw signpost argument.
 bytes=f.Bytes();meta=f.Metadata();var rejected="msgbox.npc <auto>\n{\n"+new string('A',240)+"\n}\nnot_a_command\nend";
 f.Edit(rejected);
 Check(f.Body.HasError&&f.Body.Content==rejected&&f.Body.Editor.ErrorLocations.Count>0,"Rejected growth retains exact source and diagnostics.");
 Check(bytes.SequenceEqual(f.Bytes())&&meta.SequenceEqual(f.Metadata()),"Rejected growth preserves every byte and metadata record, including signpost args.");
 f.Edit("setvar 0x4001 5\nend");Check(!f.Body.HasError,"Correction and retry succeed after a rejected resource edit.");
}
// Dialogue growth moves its own resource while the script entry remains connected.
{
 var f=new SignFixture(false);var token=new ModelDelta();var source="loadpointer 0 <019000>\n{\nShort.\n}\ncallstd 3\nend";
 var code=f.View.Tools.CodeTool.ScriptParser.Compile(token,f.Model,SignFixture.Root,ref source,out _,out _);Check(code!=null,"Dialogue seed compiles.");
 token.ChangeData(f.Model,SignFixture.Root,code);f.View.Tools.CodeTool.ScriptParser.FormatScript<XSERun>(token,f.Model,SignFixture.Root,code.Length);
 f.View.Goto.Execute(SignFixture.Root);f.Dispatch.Flush();f.View.ChangeHistory.ClearHistory(false);f.View.ChangeHistory.TagAsSaved();
 var bytes=f.Bytes();var meta=f.Metadata();int originalText=f.Model.ReadPointer(SignFixture.Root+2);
 f.Edit("loadpointer 0 <019000>\n{\n"+new string('A',200)+"\n}\ncallstd 3\nend");
 Check(!f.Body.HasError&&f.Body.Address==SignFixture.Root&&f.Model.ReadPointer(SignFixture.Root+2)!=originalText,"Dialogue expansion relocates text while keeping script entry stable.");
 for(int i=0;i<5;i++)Check(f.Model.ReadPointer(SignFixture.Signs+i*12+8)==SignFixture.Root,"Dialogue-only growth keeps signpost entry pointers unchanged.");
 f.View.Undo.Execute(null);f.Dispatch.Flush();Check(bytes.SequenceEqual(f.Bytes())&&meta.SequenceEqual(f.Metadata()),"Undo restores dialogue bytes and pointer metadata exactly.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
class SignFixture{
 public const int Root=0x18800,Signs=0x1B000;public readonly PokemonModel Model;public readonly ViewPort View;public readonly WorkflowDispatch Dispatch=new();public CodeBody Body=>View.Tools.CodeTool.Contents[0];
 public SignFixture(bool shared){var map=new SyntheticMapFixture(24,20,withNpcText:shared,withSignpost:true);Model=map.Model;Model.SetList(new NoDataChangeDeltaModel(),"FacingOptions","NoMovement");var token=new ModelDelta();
  Model[Root]=2;Model[0x163]=10;for(int i=0;i<10;i++){Model.WriteMultiByteValue(Signs+i*12,2,token,i);Model[Signs+i*12+5]=(byte)(i==9?255:i);Model.WritePointer(token,Signs+i*12+8,Root);}
  var error=ArrayRun.TryParse(Model,"[x: y: elevation. kind. unused: arg::|h]10",Signs,SortedSpan.One(0x170),out var rows);if(error.HasError)throw new Exception(error.ErrorMessage);Model.ObserveAnchorWritten(token,"synthetic.signposts",rows);
  if(shared)Model.WritePointer(token,0x17010,Root);Model.ObserveRunWritten(token,new XSERun(Root,shared?SortedSpan.One(0x17010):SortedSpan<int>.None));
  Model.ObserveAnchorWritten(token,"synthetic.guard",new NoInfoRun(Root+9,SortedSpan<int>.None));Model.ObserveAnchorWritten(token,"synthetic.textguard",new NoInfoRun(0x19008,SortedSpan<int>.None));
  Model.FreeSpaceStart=0x1C000;token.ChangeData(Model,0x1C000,Enumerable.Repeat((byte)255,0x4000).ToArray());
  View=new ViewPort("synthetic-sign.gba",Model,Dispatch,new Singletons(Dispatch),fs:new StubFileSystem());View.InitializationWorkload.GetAwaiter().GetResult();View.Goto.Execute(Root);View.Tools.CodeTool.Mode=CodeMode.Script;View.Tools.SelectedIndex=3;Dispatch.Flush();View.ChangeHistory.ClearHistory(false);View.ChangeHistory.TagAsSaved();
 }
 public void Edit(string source){Body.Editor.Content=source;Dispatch.Flush();View.ChangeHistory.ChangeCompleted();}
 public byte[] Bytes()=>Model.RawData.ToArray();public string[] Metadata()=>Model.ExportMetadata(null,View.Singletons.MetadataInfo).Serialize();
}
