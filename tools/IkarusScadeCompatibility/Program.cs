using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using HexManiac.Core.Models.Runs.Sprites;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
if(args.Length!=3||!args.All(Path.IsPathFullyQualified))throw new ArgumentException("Pass absolute repository root, unused output folder, and clean English FireRed 1.0 path.");
var repo=args[0];var folder=args[1];if(Directory.Exists(folder)||File.Exists(folder))throw new Exception("Use an unused output folder.");var destination=Path.Combine(folder,"files");
var clean=File.ReadAllBytes(args[2]);var scade=ScadePackage.Load(Path.Combine(repo,"OptionalAssets/SCADE"),out _)!.Create(clean);
var ips=File.ReadAllBytes(Path.Combine(repo,"OptionalAssets/Ikarus/Ikarus.ips"));
if(Convert.ToHexString(SHA256.HashData(ips))!="CBC79D5613C24D682119C6A43BA191BAAE39A0C7FAB8A0CBF53311891A3C8B7C")throw new Exception("Wrong Ikarus release.");
var donor=clean.ToArray();var touched=new bool[scade.Length];int at=5;
while(!ips.AsSpan(at,3).SequenceEqual("EOF"u8)){int offset=(ips[at]<<16)|(ips[at+1]<<8)|ips[at+2];int size=(ips[at+3]<<8)|ips[at+4];at+=5;
 if(size>0){ips.AsSpan(at,size).CopyTo(donor.AsSpan(offset));at+=size;}else{size=(ips[at]<<8)|ips[at+1];at+=2;donor.AsSpan(offset,size).Fill(ips[at++]);}touched.AsSpan(offset,size).Fill(true);}
var independent=new BasicModel(clean.ToArray());if(Patcher.ApplyIPSPatch(independent,ips,new ModelDelta())<0||!independent.RawData.SequenceEqual(donor))throw new Exception("Independent IPS decoder disagrees.");
if(Convert.ToHexString(SHA256.HashData(scade))!="67C78DA450F8631B7C57D92FB9345ACD822AC2B3A37EFA585656A6F55B15F99C")throw new Exception("Unsupported SCADE output version.");
var metadataPath=Path.Combine(repo,"OptionalAssets/SCADE/SCADE.toml");if(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(metadataPath)))!="7D415B24934B740B2A3B98C31C2A79A7FA9AAF282E7E5FE7A7A34D0AF0A6E6C2")throw new Exception("Unsupported SCADE metadata.");
var originalLines=File.ReadAllLines(metadataPath);var original=new StoredMetadata(originalLines);
ViewPort Open(string name,byte[] bytes,string[] definitions){var fs=new StubFileSystem{MetadataFor=f=>f.EndsWith(name+".gba")?definitions:Array.Empty<string>()};var e=new EditorViewModel(fs,InstantDispatch.Instance);var v=e.OpenFileAsync(new LoadedFile(Path.Combine(folder,name+".gba"),bytes.ToArray())).GetAwaiter().GetResult();v.Model.InitializationWorkload.GetAwaiter().GetResult();v.InitializationWorkload.GetAwaiter().GetResult();return v;}
var dv=Open("donor",donor,Array.Empty<string>());
const int oldStart=0x900000,oldEnd=0x954371,newStart=0x1F80000,newEnd=0x1FD4400;
if(scade.AsSpan(newStart,newEnd-newStart).ContainsAnyExcept((byte)255))throw new Exception("Arena is occupied.");
var target=scade.ToArray();int conflicts=0;
for(int i=0;i<touched.Length;i++)if(touched[i]){if(scade[i]!=clean[i]&&donor[i]!=scade[i]){conflicts++;if(i<oldStart||i>=oldEnd)throw new Exception($"Unhandled conflict outside relocation region at {i:X6}");}if(i<oldStart||i>=oldEnd)target[i]=donor[i];}
donor.AsSpan(oldStart,oldEnd-oldStart).CopyTo(target.AsSpan(newStart));
var relocations=new List<object>();
foreach(var map in AllMapsModel.Create(dv.Model,null).SelectMany(b=>b).Where(m=>m!=null))foreach(var header in new[]{map.Layout.PrimaryBlockset!.Start,map.Layout.SecondaryBlockset!.Start})for(int field=4;field<24;field+=4){int source=header+field;int old=BitConverter.ToInt32(donor,source)-0x08000000;if(old>=oldStart&&old<oldEnd){int relocated=newStart+old-oldStart;BitConverter.GetBytes(0x08000000+relocated).CopyTo(target,source);if(!relocations.OfType<Relocation>().Any(r=>r.source==source))relocations.Add(new Relocation(source,old,relocated));}}
int scadeSpecific=0;for(int i=0;i<scade.Length;i++){byte originalByte=i<clean.Length?clean[i]:(byte)255;if(scade[i]!=originalByte){scadeSpecific++;if(target[i]!=scade[i])throw new Exception($"SCADE-specific data changed at {i:X6}");}}
Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"candidate.gba"),target);File.WriteAllBytes(Path.Combine(folder,"donor.gba"),donor);
var cv=Open("candidate",target,originalLines);var token=new NoDataChangeDeltaModel();
var headers=AllMapsModel.Create(dv.Model,null).SelectMany(b=>b).Where(m=>m!=null).SelectMany(m=>new[]{m.Layout.PrimaryBlockset!.Start,m.Layout.SecondaryBlockset!.Start}).Distinct().ToArray();
var blockSpans=new List<(int start,int end)>();var anchors=new List<object>();var done=new HashSet<int>();const int arena=0x1F80000,arenaEnd=0x1FD4400;
void Add(string name,int address,IFormattedRun run){if(!done.Add(address))return;cv.Model.ObserveAnchorWritten(token,name,run);anchors.Add(new{name,address,format=run.FormatString,length=run.Length});}
void AddArray(string kind,int address,int count,string fields,int source){if(done.Contains(address))return;if(kind=="blocks"){blockSpans.Add((address,address+count*16));Add($"graphics.maps.ikarus.blocks_{address:X6}",address,new NoInfoRun(address,SortedSpan.One(source)));return;}var error=ArrayRun.TryParse(cv.Model,$"[{fields}]{count}",address,(kind=="reserved"?SortedSpan<int>.None:SortedSpan.One(source)),out var run);if(error.HasError)throw new Exception(error.ToString());Add($"graphics.maps.ikarus.{kind}_{address:X6}",address,run);}
foreach(var header in headers){var bs=new BlocksetModel(dv.Model,header);var count=bs.ReadBlocks(-1).Length;
 int tile=cv.Model.ReadPointer(header+4),blocks=cv.Model.ReadPointer(header+12),attrs=cv.Model.ReadPointer(header+20);
 if(tile>=arena&&tile<arenaEnd){if(!bs.IsCompressed)throw new Exception("Explicit handling required for uncompressed relocated tile data.");Add($"graphics.maps.ikarus.tiles_{tile:X6}",tile,new LzTilesetRun(new TilesetFormat(4,null),cv.Model,tile,SortedSpan.One(header+4)));}
 if(blocks>=arena&&blocks<arenaEnd)AddArray("blocks",blocks,count,"a: b: c: d: e: f: g: h:",header+12);
 if(attrs>=arena&&attrs<arenaEnd)AddArray("attributes",attrs,count,"value::",header+20);
}
// Reserve every gap so HMA cannot allocate a new map inside the relocated arena.
var spans=cv.Model.All<IFormattedRun>().Where(r=>r.Start>=arena&&r.Start<arenaEnd).Select(r=>(start:r.Start,end:r.Start+r.Length)).Concat(blockSpans).OrderBy(r=>r.start).ToArray();
int cursor=arena;
foreach(var span in spans){if(span.start>cursor)AddArray("reserved",cursor,span.start-cursor,"byte.",cursor);cursor=Math.Max(cursor,span.end);}
if(cursor<arenaEnd)AddArray("reserved",cursor,arenaEnd-cursor,"byte.",cursor);
if(!cv.Model.RawData.SequenceEqual(target))throw new Exception("Metadata registration changed ROM bytes.");
var allocatorProbes=new List<object>();foreach(int size in new[]{1,4,28,512,4096}){cv.Model.FreeSpaceStart=arena;int free=cv.Model.FindFreeSpace(arena,size);if(free>=arena&&free<arenaEnd)throw new Exception("Allocator reused relocated assets.");allocatorProbes.Add(new{size,free});}cv.Model.FreeSpaceStart=original.FreeSpaceSearch;
var exported=cv.Model.ExportMetadata(null,cv.Singletons.MetadataInfo);
if(!original.NamedAnchors.All(a=>exported.NamedAnchors.Any(b=>a.Name==b.Name&&a.Address==b.Address&&a.Format==b.Format)))throw new Exception("Original named definition changed.");
if(!original.Lists.All(a=>exported.Lists.Any(b=>a.Name==b.Name&&a.Contents.SequenceEqual(b.Contents))))throw new Exception("Original enum/list changed.");
// Append only the verified relocation definitions, preserving every supplied metadata byte/comment.
var lines=new List<string>(originalLines);var named=exported.NamedAnchors.Where(a=>a.Name.StartsWith("graphics.maps.ikarus.")).ToArray();
foreach(var a in named){lines.Add("");lines.Add("[[NamedAnchors]]");lines.Add($"Name = '''{a.Name}'''");lines.Add($"Address = 0x{a.Address:X6}");lines.Add($"Format = '''{a.Format}'''");if(a.Hash!=null)lines.Add($"DefaultHash = '''{a.Hash}'''");}
var recheck=Open("metadata-recheck",target,lines.ToArray());var restored=recheck.Model.ExportMetadata(null,recheck.Singletons.MetadataInfo);
if(!exported.NamedAnchors.Where(a=>a.Name.StartsWith("graphics.maps.ikarus.")).All(a=>restored.NamedAnchors.Any(b=>a.Name==b.Name&&a.Address==b.Address&&a.Format==b.Format)))throw new Exception("Relocated formats did not survive reload.");
if(!original.NamedAnchors.All(a=>restored.NamedAnchors.Any(b=>a.Name==b.Name&&a.Address==b.Address&&a.Format==b.Format)))throw new Exception("Original definitions did not survive updated metadata reload.");
byte[] MakeBps(byte[] source,byte[] dest){using var s=new MemoryStream();s.Write("BPS1"u8);void N(ulong value){while(true){byte next=(byte)(value&127);value>>=7;if(value==0){s.WriteByte((byte)(next|128));return;}s.WriteByte(next);value--;}}
 N((ulong)source.Length);N((ulong)dest.Length);var message=Encoding.UTF8.GetBytes("Experimental Ikarus compatibility for bundled SCADE 2026-11-15; exact fresh SCADE input only.");N((ulong)message.Length);s.Write(message);
 for(int i=0;i<dest.Length;){int start=i;bool equal=source[i]==dest[i];while(i<dest.Length&&(source[i]==dest[i])==equal)i++;int length=i-start;N(((ulong)(length-1)<<2)|(equal?0UL:1UL));if(!equal)s.Write(dest.AsSpan(start,length));}
 s.Write(BitConverter.GetBytes((uint)Patcher.CalcCRC32(source)));s.Write(BitConverter.GetBytes((uint)Patcher.CalcCRC32(dest)));var body=s.ToArray();s.Write(BitConverter.GetBytes((uint)Patcher.CalcCRC32(body)));return s.ToArray();}
var patch=MakeBps(scade,target);if(!BpsPatch.Apply(scade,patch).SequenceEqual(target))throw new Exception("Generated patch does not reproduce exact candidate.");
int rejected=0;void Reject(byte[] source,byte[] input){try{BpsPatch.Apply(source,input);}catch(InvalidDataException){rejected++;return;}throw new Exception("Invalid patch/base was accepted.");}
Reject(clean,patch);Reject(target,patch);var changed=scade.ToArray();changed[0]^=1;Reject(changed,patch);var broken=patch.ToArray();broken[broken.Length/2]^=1;Reject(scade,broken);
Directory.CreateDirectory(destination);File.WriteAllBytes(Path.Combine(destination,"Ikarus-SCADE-Compatibility.bps"),patch);File.WriteAllLines(Path.Combine(destination,"Ikarus-SCADE.toml"),lines);File.WriteAllLines(Path.Combine(folder,"candidate.toml"),lines);
string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
var report=new{status="EXPERIMENTAL_NOT_FOR_DISTRIBUTION",baseSha256=Hash(scade),targetSha256=Hash(target),patchSha256=Hash(patch),patchBytes=patch.Length,tomlSha256=Hash(File.ReadAllBytes(Path.Combine(destination,"Ikarus-SCADE.toml"))),originalDefinitions=original.NamedAnchors.Count,originalLists=original.Lists.Count,relocatedDefinitionsAdded=named.Length,originalMetadataLinesPreserved=true,originalDefinitionsAndListsRetained=true,metadataReloadVerified=true,patchRoundtripVerified=true,invalidInputsRejected=rejected,threeWayConflictingBytes=conflicts,scadeSpecificBytesPreserved=scadeSpecific,relocations,blockArraysUseExistingHmaRawConvention=true,allocatorProbes,blockSpans=blockSpans.Select(p=>new{p.start,p.end}).ToArray(),anchors};
File.WriteAllText(Path.Combine(destination,"build-verification.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"PACKAGE_READY:patch={patch.Length},new_definitions={named.Length},original_definitions={original.NamedAnchors.Count},rejected={rejected}");

record Relocation(int source,int old,int relocated);
