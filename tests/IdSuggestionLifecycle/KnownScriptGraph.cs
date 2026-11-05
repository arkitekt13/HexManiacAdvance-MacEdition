using System.Reflection;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Review;

static class KnownScriptGraph {
 public static void Run(Action<bool,string> check) {
  var f=new UsageDetectionFixture("BPRE");var m=f.Model;var token=new ModelDelta();
  const int a=0x15000,b=0x15100,c=0x15200,unformatted=0x15300;
  byte[] Call(int address)=>new byte[]{4,(byte)address,(byte)(address>>8),(byte)(address>>16),8};
  void Write(int address,byte[] bytes,bool formatted){token.ChangeData(m,address,bytes);if(formatted)m.ObserveRunWritten(token,new XSERun(address));}
  Write(a,Call(b).Concat(new byte[]{2}).ToArray(),true);
  Write(b,new byte[]{0x29,0x21,0,0x16,0x34,0x40,1,0}.Concat(Call(unformatted)).Append((byte)2).ToArray(),true);
  Write(c,Call(b).Concat(new byte[]{2}).ToArray(),true);
  Write(unformatted,new byte[]{0x29,0x22,0,0x19,0x36,0x40,0x34,0x40}.Concat(Call(a)).Append((byte)2).ToArray(),false);
  var starts=new[]{a,b,c};var commands=new byte[]{0x29,0x16,0x19};
  var old=Flags.GetAllScriptSpots(m,f.Parser,starts,false,commands).Select(spot=>spot.Address).ToArray();
  var method=typeof(Flags).GetMethod("GetKnownScriptSpots",BindingFlags.Static|BindingFlags.NonPublic)!;
  var current=((IEnumerable<ScriptSpot>)method.Invoke(null,new object[]{m,f.Parser,starts,commands})!).Select(spot=>spot.Address).ToArray();
  check(old.ToHashSet().SetEquals(current),"Known-entry traversal retains the original references through sharing, cycles and unformatted targets.");
  check(current.Length<old.Length,"Shared formatted tails are visited fewer times without dropping references.");
  var before=m.RawData.ToArray();var services=new Singletons();var metadata=m.ExportMetadata(null,services.MetadataInfo).Serialize();
  var templates=new EventTemplate(InstantDispatch.Instance,m,f.Parser,new[]{new ReadonlyPixelViewModel(16,16,new short[256])});
  using(templates.BeginIdAllocation()) {
   check(templates.FindNextUnusedFlag()==0x23,"Live suggestions preserve flags found only in shared/unformatted callees.");
   var variable=templates.FindNextUnusedVariable();check(variable!=0x4034&&variable!=0x4036,"Live suggestions preserve direct and source-only variable operands.");
  }
  check(before.SequenceEqual(m.RawData)&&metadata.SequenceEqual(m.ExportMetadata(null,services.MetadataInfo).Serialize()),"Graph scanning and suggestions preserve bytes and metadata.");
 }
}
