using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Performance;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
{
 var f=new HeaderFixture();f.Collection.AddScript();var row=f.Collection.Scripts.Single();row.ScriptTypeIndex=1;f.Reset();var before=f.State();
 row.AddSubScript();
 Check(row.SubScripts.Count==2,"Add Table Script must publish the newly written second row immediately.");
 int table=f.Model.ReadPointer(f.Collection.Address+1);var child=row.SubScripts[1];
 Check(child.Start==table+8&&child.Variable=="4000"&&child.Value=="0","The new condition row starts at its variable field and preserves copied/default values.");
 Check(child.ScriptAddress==f.Model.ReadPointer(table+12)&&f.Model[child.ScriptAddress]==2,"New condition points at its generated end script.");
 var added=f.State();f.View.Undo.Execute(null);f.Reload();Check(before.Matches(f),"One Undo restores exact bytes and metadata before adding the row.");
 Check(f.View.Redo.CanExecute(null),"Refreshing header script view models must preserve Redo.");f.View.Redo.Execute(null);f.Reload();Check(added.Matches(f),"Redo restores the added row exactly.");
 row=f.Collection.Scripts.Single();row.SubScripts[0].Variable="4001";row.SubScripts[1].Variable="4002";f.View.ChangeHistory.ChangeCompleted();f.Reset();before=f.State();
 row.SubScripts[0].Delete();Check(row.SubScripts.Count==1,"Deleting the first condition leaves one row.");
 table=f.Model.ReadPointer(f.Collection.Address+1);Check(row.SubScripts[0].Start==table,"Surviving child model must rebind to its shifted ROM row.");
 var deleted=f.State();row.SubScripts[0].Variable="403F";row.SubScripts[0].Value="7";f.View.ChangeHistory.ChangeCompleted();
 Check(f.Model.ReadMultiByteValue(table,2)==0x403F&&f.Model.ReadMultiByteValue(table+2,2)==7&&f.Model.ReadMultiByteValue(table+8,2)==0,"Editing the shifted child updates its row without overwriting the terminator.");
 f.View.Undo.Execute(null);f.Reload();Check(deleted.Matches(f),"Undo of an edit after deletion restores the post-delete state as a separate action.");
 f.View.Undo.Execute(null);f.Reload();Check(before.Matches(f),"Second Undo restores the deleted row and exact metadata.");
 f.View.Redo.Execute(null);f.Reload();Check(deleted.Matches(f),"Redo restores condition deletion exactly.");
}
// An empty condition table must be usable again, and stale removed models must not delete a new row.
{
 var f=new HeaderFixture();f.Collection.AddScript();var row=f.Collection.Scripts.Single();row.ScriptTypeIndex=1;var stale=row.SubScripts[0];stale.Delete();
 Check(row.SubScripts.Count==0,"Deleting the final condition leaves a valid empty table.");f.Reset();row=f.Collection.Scripts.Single();var empty=f.State();row.AddSubScript();
 Check(row.SubScripts.Count==1&&row.SubScripts[0].Variable=="4000","Adding to an empty condition table creates an active temp0 condition, not a zero terminator.");
 var added=f.State();stale.Delete();Check(added.Matches(f),"A removed child model cannot delete a newly added condition.");
 f.View.Undo.Execute(null);f.Reload();Check(empty.Matches(f),"Repopulating an empty table is exactly undoable.");f.View.Redo.Execute(null);f.Reload();Check(added.Matches(f),"Repopulating an empty table is exactly redoable.");
}
// Force the condition table itself to relocate, not just its generated script.
{
 var f=new HeaderFixture();f.Collection.AddScript();var row=f.Collection.Scripts.Single();row.ScriptTypeIndex=1;int table=row.ScriptAddress;
 f.Model.ObserveAnchorWritten(new ModelDelta(),"synthetic.table.guard",new NoInfoRun(table+10,SortedSpan<int>.None));f.Reset();var before=f.State();row=f.Collection.Scripts.Single();row.AddSubScript();
 Check(row.ScriptAddress!=table&&f.Model.ReadPointer(f.Collection.Address+1)==row.ScriptAddress,"Growing the table updates its owning header pointer after relocation.");
 Check(row.SubScripts.Count==2&&row.SubScripts[0].Start==row.ScriptAddress&&row.SubScripts[1].Start==row.ScriptAddress+8,"Relocated child models use current table coordinates.");
 var after=f.State();f.View.Undo.Execute(null);f.Reload();Check(before.Matches(f),"Table relocation Undo restores every byte and structural metadata record.");f.View.Redo.Execute(null);f.Reload();Check(after.Matches(f),"Table relocation Redo is exact.");
}
// Every transition preserves the surviving entry and round-trips through history.
{
 var f=new HeaderFixture();f.Collection.AddScript();
 foreach(int type in new[]{2,4,3,4,5,6,7,2,1}){
  f.Reset();var before=f.State();var row=f.Collection.Scripts.Single();int entry=row.HasSubScripts?row.SubScripts[0].ScriptAddress:row.ScriptAddress;
  row.ScriptTypeIndex=type-1;
  Check(f.Model[f.Collection.Address]==type&&row.HasSubScripts==(type is 2 or 4),"Header script type "+type+" agrees between bytes and UI.");
  Check((row.HasSubScripts?row.SubScripts[0].ScriptAddress:row.ScriptAddress)==entry,"Type transition preserves the existing script entry.");
  var after=f.State();f.View.Undo.Execute(null);f.Reload();Check(before.Matches(f),"Type "+type+" Undo restores bytes and metadata exactly.");
  Check(f.View.Redo.CanExecute(null),"Read-only refresh after type Undo retains Redo.");f.View.Redo.Execute(null);f.Reload();Check(after.Matches(f),"Type "+type+" Redo restores bytes and metadata exactly.");
 }
}
// Parent rows shift safely, and each Add/Delete is separately undoable.
{
 var f=new HeaderFixture();f.Collection.AddScript();f.Collection.AddScript();f.Reset();var before=f.State();int survivor=f.Collection.Scripts[1].ScriptAddress;
 f.Collection.Scripts[0].Delete();Check(f.Collection.Scripts.Count==1&&f.Collection.Scripts[0].ScriptAddress==survivor,"Deleting first parent preserves the next entry and view model.");var after=f.State();
 f.View.Undo.Execute(null);f.Reload();Check(before.Matches(f),"Parent delete Undo is exact.");f.View.Redo.Execute(null);f.Reload();Check(after.Matches(f),"Parent delete Redo is exact.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
class HeaderFixture{
 public readonly PokemonModel Model;public readonly ViewPort View;public readonly MapScriptCollection Collection;readonly ModelArrayElement owner;
 public HeaderFixture(){var f=new SyntheticMapFixture(24,20);Model=f.Model;View=f.ViewPort;Model.InitializationWorkload.GetAwaiter().GetResult();var token=new ModelDelta();var format=Model.GetNextRun(0x120).FormatString.Replace("mapscripts<>","mapscripts<[type. pointer<>]!00>");
  var error=ArrayRun.TryParse(Model,format,0x120,SortedSpan.One(0x110),out var header);if(error.HasError)throw new Exception(error.ErrorMessage);Model.ObserveAnchorWritten(token,"synthetic.header",header);
  Model.FreeSpaceStart=0x1C000;token.ChangeData(Model,0x1C000,Enumerable.Repeat((byte)255,0x4000).ToArray());owner=Model.GetTableModel("synthetic.header",()=>View.ChangeHistory.CurrentChange)[0];
  var sprites=Enumerable.Range(0,256).Select(_=>(IPixelViewModel)new ReadonlyPixelViewModel(16,32,new short[512],-1)).ToArray();var templates=new EventTemplate(InstantDispatch.Instance,Model,View.Tools.CodeTool.ScriptParser,sprites);
  Collection=new MapScriptCollection(View,templates);Collection.Load(owner);Collection.CreateCollection();View.ChangeHistory.ChangeCompleted();
 }
 public void Reset(){Reload();View.ChangeHistory.ClearHistory(false);View.ChangeHistory.TagAsSaved();}
 public void Reload()=>Collection.Load(owner);
 public HeaderState State()=>new(Model.RawData.ToArray(),Model.ExportMetadata(null,View.Singletons.MetadataInfo).Serialize().Where(line=>!line.StartsWith("FreeSpaceSearch = ",StringComparison.Ordinal)).ToArray());
}
record HeaderState(byte[] Bytes,string[] Metadata){public bool Matches(HeaderFixture f){var state=f.State();bool same=Bytes.SequenceEqual(state.Bytes)&&Metadata.SequenceEqual(state.Metadata);if(!same){Console.WriteLine("BYTE_DIFFS:"+string.Join(",",Enumerable.Range(0,Math.Min(Bytes.Length,state.Bytes.Length)).Where(i=>Bytes[i]!=state.Bytes[i]).Take(16).Select(i=>i.ToString("X"))));Console.WriteLine("META_REMOVED:"+string.Join(" | ",Metadata.Except(state.Metadata)));Console.WriteLine("META_ADDED:"+string.Join(" | ",state.Metadata.Except(Metadata)));}return same;}}
