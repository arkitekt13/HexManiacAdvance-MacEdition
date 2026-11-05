using System.Diagnostics;
using System.Text.Json;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Review;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
foreach(int count in new[]{100,1000}) {
 var f=new UsageDetectionFixture("BPRE");var m=f.Model;var token=new ModelDelta();const int start=0x20000,tailCount=25;
 m.ExpandData(token,start+(count+tailCount+1)*256);
 int Tail(int i)=>start+(count+i)*256;
 for(int i=0;i<count+tailCount;i++){
  int at=start+i*256;var bytes=new List<byte>();
  if(i<count){bytes.Add(4);bytes.AddRange(BitConverter.GetBytes(0x08000000+Tail(0)));}
  else {
   for(int j=0;j<16;j++)bytes.AddRange(new byte[]{0x29,0x21,0});
   bytes.AddRange(new byte[]{0x16,0xFE,0x40,1,0});
   if(i<count+tailCount-1){bytes.Add(4);bytes.AddRange(BitConverter.GetBytes(0x08000000+Tail(i-count+1)));}
  }
  bytes.Add(2);token.ChangeData(m,at,bytes.ToArray());m.ObserveRunWritten(token,new XSERun(at));
 }
 var clock=Stopwatch.StartNew();var t=new EventTemplate(InstantDispatch.Instance,m,f.Parser,new[]{new ReadonlyPixelViewModel(16,16,new short[256])});
 double initialize=clock.Elapsed.TotalMilliseconds;
 int Query(bool variable){using var scope=t.BeginIdAllocation();return variable?t.FindNextUnusedVariable():t.FindNextUnusedFlag();}
 Query(false);Query(true);
 var before=m.RawData.ToArray();long allocations=GC.GetAllocatedBytesForCurrentThread();clock.Restart();for(int i=0;i<100;i++)Query(false);double cached=clock.Elapsed.TotalMilliseconds/100;long cachedBytes=(GC.GetAllocatedBytesForCurrentThread()-allocations)/100;
 token.ChangeData(m,0x1000,(byte)(m[0x1000]^1));clock.Restart();int flag=Query(false);double refresh=clock.Elapsed.TotalMilliseconds;
 Console.WriteLine(JsonSerializer.Serialize(new{count,tailCount,initialize_ms=initialize,cached_query_ms=cached,cached_query_bytes=cachedBytes,refresh_ms=refresh,flag}));
}
