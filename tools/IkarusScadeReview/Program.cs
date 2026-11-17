using System.Security.Cryptography;
using System.Text.Json;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HexManiac.Core.Models.Runs.Sprites;
if(args.Length!=3) throw new ArgumentException("Pass the absolute repository root, exact clean FireRed 1.0 ROM, and a new JSON report path.");
if(!args.All(Path.IsPathFullyQualified)||Path.GetExtension(args[2])!=".json"||File.Exists(args[2])) throw new ArgumentException("Use absolute paths and an unused .json output filename.");
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
var repo=args[0];var clean=File.ReadAllBytes(args[1]);var package=ScadePackage.Load(Path.Combine(repo,"OptionalAssets/SCADE"),out var error)??throw new Exception(error);
var scade=package.Create(clean);var before=scade.ToArray();var after=scade.ToArray();
var ips=File.ReadAllBytes(Path.Combine(repo,"OptionalAssets/Ikarus/Ikarus.ips"));var writes=new bool[scade.Length];int pos=5,records=0,requested=0;
while(!ips.AsSpan(pos,3).SequenceEqual("EOF"u8)){int off=(ips[pos]<<16)|(ips[pos+1]<<8)|ips[pos+2];int n=(ips[pos+3]<<8)|ips[pos+4];pos+=5;
 if(n>0){Array.Copy(ips,pos,after,off,n);pos+=n;}else{n=(ips[pos]<<8)|ips[pos+1];pos+=2;Array.Fill(after,ips[pos++],off,n);}Array.Fill(writes,true,off,n);records++;requested+=n;}
var hmaPatched=new BasicModel(before.ToArray());
if(Patcher.ApplyIPSPatch(hmaPatched,ips,new ModelDelta())<0||!hmaPatched.RawData.SequenceEqual(after))throw new Exception("Independent IPS application disagrees with HMA.");
var fs=new StubFileSystem {MetadataFor=f=>f.EndsWith("virtual-scade.gba")?File.ReadAllLines(Path.Combine(repo,"OptionalAssets/SCADE/SCADE.toml")):Array.Empty<string>()};
var editor=new EditorViewModel(fs,InstantDispatch.Instance);var v=await editor.OpenFileAsync(new LoadedFile("/private/tmp/virtual-scade.gba",scade));await v.Model.InitializationWorkload;await v.InitializationWorkload;
v.Goto.Execute("maps.bank3.PALLET TOWN.3-0");_=v.MapEditor.PrimaryMap.PixelData;
var m=v.Model;
int Changed(int start,int length)=>Enumerable.Range(start,length).Count(i=>before[i]!=after[i]);
var sprites=m.All<OverworldSpriteListRun>().ToArray();var frames=new Dictionary<(int,int),HashSet<int>>();
foreach(var list in sprites){for(int i=0;i<list.ElementCount;i++){int addr=m.ReadPointer(list.Start+i*8);int length=m.ReadMultiByteValue(list.Start+i*8+4,2);if(addr<0||length<1||addr>m.Count-length)continue;var key=(addr,length);if(!frames.TryGetValue(key,out var owners))frames[key]=owners=new();owners.Add(list.Start);}}
var frameHits=frames.Where(p=>Changed(p.Key.Item1,p.Key.Item2)>0).Select(p=>new{start=p.Key.Item1,length=p.Key.Item2,changed=Changed(p.Key.Item1,p.Key.Item2),lists=p.Value.Order().ToArray()}).OrderBy(p=>p.start).ToArray();
var runHits=m.All<IFormattedRun>().Where(r=>r.Start>=0&&r.Length>0&&r.Start<=m.Count-r.Length&&Changed(r.Start,r.Length)>0).GroupBy(r=>r.GetType().Name).ToDictionary(g=>g.Key,g=>new{runs=g.Count(),changedBytes=g.Sum(r=>Changed(r.Start,r.Length))});
var anchorHits=m.ExportMetadata(null,v.Singletons.MetadataInfo).NamedAnchors.Select(a=>new{anchor=a,run=m.GetNextRun(a.Address)}).Where(p=>p.run.Start==p.anchor.Address&&p.run.Length>0&&Changed(p.run.Start,p.run.Length)>0).Select(p=>new{name=p.anchor.Name,start=p.anchor.Address,length=p.run.Length,changed=Changed(p.run.Start,p.run.Length)}).ToArray();
var pointerRefs=new List<object>();for(int i=0;i<=ips.Length-4;i++){int value=BitConverter.ToInt32(ips,i);if(value>=0x087B0000&&value<0x087D0070||value>=0x08900000&&value<0x08953F83||value>=0x08EA1B68&&value<0x08EA463C)pointerRefs.Add(new{patchOffset=i,value});}
var all=AllMapsModel.Create(m,null);var blocksets=all.SelectMany(b=>b).Where(map=>map!=null).SelectMany(map=>new[]{map.Layout.PrimaryBlockset?.Start??-1,map.Layout.SecondaryBlockset?.Start??-1}).Where(a=>a>=0).Distinct().Order().ToArray();
var headers=blocksets.Select(a=>new{header=a,changed=Changed(a,24),primary=m[a+1]==0,beforePointers=Enumerable.Range(0,5).Select(i=>m.ReadPointer(a+4+i*4)).ToArray(),afterPointers=Enumerable.Range(0,5).Select(i=>BitConverter.ToInt32(after,a+4+i*4)-0x08000000).ToArray()}).ToArray();
var free=new List<object>();for(int i=0;i<before.Length;){if(before[i]!=0xFF){i++;continue;}int start=i;while(i<before.Length&&before[i]==0xFF)i++;if(i-start>=0x40000)free.Add(new{start,length=i-start});}
var report=new{scadeSourceSha256=Hash(before),ikarusSha256=Hash(ips),naiveCombinedSha256=Hash(after),length=before.Length,records,requested,distinctWrittenBytes=writes.Count(x=>x),changedBytes=Enumerable.Range(0,before.Length).Count(i=>before[i]!=after[i]),overworldLists=sprites.Length,uniqueFrameRanges=frames.Count,overwrittenFrameRanges=frameHits.Length,overwrittenFrameBytes=frameHits.Sum(x=>x.changed),frameHits,runHits,anchorHits,blocksetHeaders=headers,rawPointerLikeOccurrences=pointerRefs.Count,rawPointerLikeOccurrencesAreNotVerifiedReferences=true,ffRunsOver256KiB=free,ffRunsAreUnverifiedCandidates=true,ipsApplicationMatchesHma=true,sourceUnchanged=Hash(scade)==Hash(before),cleanInputUnchanged=Hash(File.ReadAllBytes(args[1]))==Hash(clean)};
using(var output=new FileStream(args[2],FileMode.CreateNew,FileAccess.Write))JsonSerializer.Serialize(output,report,new JsonSerializerOptions{WriteIndented=true});
Console.WriteLine($"AUDIT: frames overwritten {frameHits.Length}/{frames.Count}; bytes {frameHits.Sum(x=>x.changed)}; named anchors affected {anchorHits.Length}; blocks headers {headers.Length}; raw pointer-like {pointerRefs.Count}");
