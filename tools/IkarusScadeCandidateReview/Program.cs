using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.AvaloniaUI.Project;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
if(args.Length!=3||!args.All(Path.IsPathFullyQualified))throw new ArgumentException("Pass absolute repository root, compatibility build folder and clean FireRed 1.0 ROM.");
var repo=args[0];var folder=args[1];if(!File.Exists(Path.Combine(folder,"files/build-verification.json")))throw new ArgumentException("Use a generated disposable compatibility build folder.");var baseData=File.ReadAllBytes(args[2]);var package=ScadePackage.Load(Path.Combine(repo,"OptionalAssets/SCADE"),out _)!;var scadeData=package.Create(baseData);
var candidateData=File.ReadAllBytes(Path.Combine(folder,"candidate.gba"));var donorData=File.ReadAllBytes(Path.Combine(folder,"donor.gba"));var meta=File.ReadAllLines(Path.Combine(repo,"OptionalAssets/SCADE/SCADE.toml"));
var failures=new List<string>();int checks=0;void Check(bool ok,string message){checks++;if(!ok){failures.Add(message);Console.WriteLine("CHECK_FAILED:"+message);}}
string Hash(byte[] data)=>Convert.ToHexString(SHA256.HashData(data));
var snapshots=new List<object>();var mismatches=new List<object>();var differences=new List<object>();
(ViewPort view,EditorViewModel editor,StubFileSystem fs) Open(string name,byte[] bytes,string[] definitions,bool write=false){
 var path=Path.Combine(folder,name+".gba");var fs=new StubFileSystem {MetadataFor=f=>f==path?definitions:Array.Empty<string>(),Save=file=>{if(!write)throw new Exception("Unexpected write");File.WriteAllBytes(file.Name,file.Contents);return true;},SaveMetadata=(file,lines)=>{if(!write)throw new Exception("Unexpected metadata write");File.WriteAllLines(Path.ChangeExtension(file,".toml"),lines);return true;}};
 var editor=new EditorViewModel(fs,InstantDispatch.Instance);var v=editor.OpenFileAsync(new LoadedFile(path,bytes.ToArray())).GetAwaiter().GetResult();v.Model.InitializationWorkload.GetAwaiter().GetResult();v.InitializationWorkload.GetAwaiter().GetResult();return(v,editor,fs);
}
var (source,se,sfs)=Open("scade-source",scadeData,meta);var (donor,de,dfs)=Open("donor",donorData,Array.Empty<string>());var (candidate,ce,cfs)=Open("candidate",candidateData,File.ReadAllLines(Path.Combine(folder,"files/Ikarus-SCADE.toml")));
Check(candidate.Model.RawData.SequenceEqual(candidateData),"Candidate loads without rewriting bytes");
int specific=0;for(int i=0;i<scadeData.Length;i++){byte original=i<baseData.Length?baseData[i]:(byte)255;if(scadeData[i]!=original){specific++;if(scadeData[i]!=candidateData[i])throw new Exception($"SCADE-specific byte altered at {i:X6}");}}
Check(specific>0,"Every SCADE-specific byte is retained");
var arena=0x1F80000;var arenaEnd=arena+0x54400;
var occupied=source.Model.All<IFormattedRun>().Where(r=>r.Start<arenaEnd&&r.Start+r.Length>arena).Select(r=>new{r.Start,r.Length,type=r.GetType().Name}).ToArray();Check(occupied.Length==0,"Chosen FF tail has no recognized formatted ownership");
var typedArenaPointers=new List<object>();foreach(var table in source.Model.All<ITableRun>())for(int row=0;row<table.ElementCount;row++){int off=table.Start+row*table.ElementLength;foreach(var seg in table.ElementContent){if(seg.Type==ElementContentType.Pointer){int target=source.Model.ReadPointer(off);if(target>=arena&&target<arenaEnd)typedArenaPointers.Add(new{source=off,target});}off+=seg.Length;}}
Check(typedArenaPointers.Count==0,"No recognized typed pointer targets the reserved arena");
var maps=AllMapsModel.Create(donor.Model,null).SelectMany(b=>b).Where(m=>m!=null).ToArray();
var donorPairs=new Dictionary<(int,int),(int group,int map)>();
foreach(var map in maps){try{
 var dm=new BlockMapViewModel(dfs,de.MapTutorials,donor,donor.MapEditor.Format,donor.MapEditor.Templates,map.Group,map.Map);
 var cm=new BlockMapViewModel(cfs,ce.MapTutorials,candidate,candidate.MapEditor.Format,candidate.MapEditor.Templates,map.Group,map.Map);
 dm.ShowEvents=MapDisplayOptions.NoEvents;cm.ShowEvents=MapDisplayOptions.NoEvents;
 var d=dm.PixelData;var c=cm.PixelData;
 bool equal=dm.PixelWidth==cm.PixelWidth&&dm.PixelHeight==cm.PixelHeight&&d.SequenceEqual(c);
 if(!equal)mismatches.Add(new{map.Group,map.Map,kind="map_pixels",dwidth=dm.PixelWidth,dheight=dm.PixelHeight,cwidth=cm.PixelWidth,cheight=cm.PixelHeight});
 var pair=(map.Layout.Element.GetAddress(Format.PrimaryBlockset),map.Layout.Element.GetAddress(Format.SecondaryBlockset));donorPairs.TryAdd(pair,(map.Group,map.Map));
 if(snapshots.Count<5||map.Map==3&&map.Group==3)snapshots.Add(new{map.Group,map.Map,pixels=c.Length,blocks=cm.BlockRenders.Count,match=equal});
 if(map.Map%30==0)Console.WriteLine($"MAP_PROGRESS:{map.Group}-{map.Map}");
 }catch(Exception ex){mismatches.Add(new{map.Group,map.Map,kind="exception",error=ex.ToString()});}}
Check(mismatches.Count==0,"All map pixels must match donor: "+JsonSerializer.Serialize(mismatches.Take(5)));
var sets=new HashSet<int>();var previewFailures=new List<object>();var paletteCounts=new List<object>();
var rawAssetChecks=new List<object>();
foreach(var address in donorPairs.Keys.SelectMany(p=>new[]{p.Item1,p.Item2}).Distinct()){
 var db=new BlocksetModel(donor.Model,address);var cb=new BlocksetModel(candidate.Model,address);
 var dblocks=db.ReadBlocks(-1);var cblocks=cb.ReadBlocks(-1);
 var dattrs=db.ReadBlockAttributes(-1);var cattrs=cb.ReadBlockAttributes(-1);
 var dtiles=db.ReadTiles();var ctiles=cb.ReadTiles();var dpal=db.ReadPalettes();var cpal=cb.ReadPalettes();
 bool blocksExact=dblocks.Length==cblocks.Length&&dblocks.Zip(cblocks).All(p=>p.First.SequenceEqual(p.Second));
 bool attrsExact=dattrs.Length==cattrs.Length&&dattrs.Zip(cattrs).All(p=>p.First.SequenceEqual(p.Second));
 bool tilesExact=dtiles.Length==ctiles.Length&&dtiles.Zip(ctiles).All(p=>p.First.GetLength(0)==p.Second.GetLength(0)&&p.First.GetLength(1)==p.Second.GetLength(1)&&p.First.Cast<int>().SequenceEqual(p.Second.Cast<int>()));
 bool palettesExact=dpal.Length==cpal.Length&&dpal.Zip(cpal).All(p=>p.First.SequenceEqual(p.Second));
 bool animationReferenceExact=donor.Model.ReadPointer(address+16)==candidate.Model.ReadPointer(address+16);
 Check(blocksExact,$"Raw block definitions match at {address:X6}");Check(attrsExact,$"Raw behavior/layer attributes match at {address:X6}");Check(tilesExact,$"Decompressed tiles match at {address:X6}");Check(palettesExact,$"Palette data match at {address:X6}");Check(animationReferenceExact,$"Animation reference matches donor at {address:X6}");
 rawAssetChecks.Add(new{address,blocks=dblocks.Length,tiles=dtiles.Length,blocksExact,attrsExact,tilesExact,palettesExact,animationReferenceExact});
}
foreach(var (pair,where) in donorPairs){
 donor.MapEditor.NavigateTo(where.group,where.map,0,0);candidate.MapEditor.NavigateTo(where.group,where.map,0,0);
 var dm=donor.MapEditor.PrimaryMap;var cm=candidate.MapEditor.PrimaryMap;
 var d=dm.BlockRenders;var c=cm.BlockRenders;
 if(d.Count!=c.Count||!d.Zip(c).All(p=>p.First.PixelData.SequenceEqual(p.Second.PixelData)))previewFailures.Add(new{where.group,where.map,kind="palette",donor=d.Count,candidate=c.Count});
 paletteCounts.Add(new{where.group,where.map,donor=d.Count,candidate=c.Count});
 foreach(var address in new[]{pair.Item1,pair.Item2})if(sets.Add(address)){
  var dr=new BlocksetModel(donor.Model,address).RenderBlockset(.5);var cr=new BlocksetModel(candidate.Model,address).RenderBlockset(.5);
  if(dr.PixelWidth!=cr.PixelWidth||dr.PixelHeight!=cr.PixelHeight||!dr.PixelData.SequenceEqual(cr.PixelData))previewFailures.Add(new{address,kind="preview",dw=dr.PixelWidth,dh=dr.PixelHeight,cw=cr.PixelWidth,ch=cr.PixelHeight});
 }
}
Console.WriteLine("PALETTE_RESULTS:"+JsonSerializer.Serialize(previewFailures));Check(previewFailures.Count==0,"All palettes and previews match donor");
var frameChanges=new List<object>();var unique=new HashSet<(int,int)>();
foreach(var list in source.Model.All<OverworldSpriteListRun>())for(int i=0;i<list.ElementCount;i++){
 int addr=source.Model.ReadPointer(list.Start+i*8),len=source.Model.ReadMultiByteValue(list.Start+i*8+4,2);if(addr<0||len<=0||addr>scadeData.Length-len||!unique.Add((addr,len)))continue;
 if(!scadeData.AsSpan(addr,len).SequenceEqual(candidateData.AsSpan(addr,len))){bool matchesDonor=addr<=donorData.Length-len&&candidateData.AsSpan(addr,len).SequenceEqual(donorData.AsSpan(addr,len));bool preexistingVanilla=addr<=baseData.Length-len&&scadeData.AsSpan(addr,len).SequenceEqual(baseData.AsSpan(addr,len));frameChanges.Add(new{addr,len,matchesDonor,preexistingVanilla});Check(matchesDonor&&preexistingVanilla,"Only Ikarus's unchanged-vanilla legacy frame edits are permitted");}}
Check(candidateData.AsSpan(0x900000,0x54371).SequenceEqual(scadeData.AsSpan(0x900000,0x54371)),"Original SCADE graphics region stays exact");
var originalNames=new StoredMetadata(meta).NamedAnchors;
var preCreateNames=candidate.Model.ExportMetadata(null,ce.Singletons.MetadataInfo).NamedAnchors;
var baselineNames=source.Model.ExportMetadata(null,se.Singletons.MetadataInfo).NamedAnchors;
var missingBefore=originalNames.Where(a=>!preCreateNames.Any(b=>a.Name==b.Name&&a.Address==b.Address&&a.Format==b.Format)).Select(a=>new{a.Name,a.Address,a.Format,baselineMatches=baselineNames.Any(b=>a.Name==b.Name&&a.Address==b.Address&&a.Format==b.Format),actual=preCreateNames.FirstOrDefault(b=>b.Name==a.Name)?.Format}).ToArray();
Console.WriteLine("METADATA_BEFORE_CREATE:"+JsonSerializer.Serialize(missingBefore));
Check(missingBefore.Length==0,"All supplied definitions survive before creating a map");
candidate.MapEditor.NavigateTo(3,0,0,0);var pallet=candidate.MapEditor.PrimaryMap;var palletBlocks=pallet.BlockRenders.Select(r=>r.PixelData.ToArray()).ToArray();
var create=typeof(BlockMapViewModel).GetMethod("CreateNewMap",BindingFlags.Instance|BindingFlags.NonPublic)!;var created=(BlockMapViewModel)create.Invoke(pallet,new object[]{candidate.ChangeHistory.CurrentChange,3,9,9})!;
var newBlocks=created.BlockRenders;Check(newBlocks.Count==palletBlocks.Length&&newBlocks.Select((r,i)=>r.PixelData.SequenceEqual(palletBlocks[i])).All(x=>x),"New Pallet map has complete identical block palette");
var names=candidate.Model.ExportMetadata(null,ce.Singletons.MetadataInfo).NamedAnchors;
var changedDefinitions=originalNames.Where(a=>!names.Any(b=>a.Name==b.Name&&a.Address==b.Address&&a.Format==b.Format)).Select(a=>a.Name).ToArray();
Console.WriteLine("METADATA_AFTER_CREATE:"+JsonSerializer.Serialize(changedDefinitions));
candidate.ChangeHistory.ChangeCompleted();var savedBytes=candidate.Model.RawData.ToArray();var savedDefinitions=names.ToArray();int saves=0;
var roundRom=Path.Combine(folder,"roundtrip.gba");var roundToml=Path.Combine(folder,"roundtrip.toml");
cfs.Save=file=>{File.WriteAllBytes(roundRom,file.Contents);saves++;return true;};cfs.SaveMetadata=(file,lines)=>{File.WriteAllLines(roundToml,lines);saves++;return true;};
candidate.Save.Execute(cfs);Check(saves==2&&candidate.ChangeHistory.IsSaved,"Normal Save writes both disposable files and marks saved");
candidate.Undo.Execute(null);Check(candidate.Model.RawData.SequenceEqual(candidateData),"Undo new-map creation restores pristine candidate bytes");
candidate.Redo.Execute(null);Check(candidate.Model.RawData.SequenceEqual(savedBytes),"Redo restores exactly the saved new map");
var (reopened,re,rfs)=Open("roundtrip",File.ReadAllBytes(roundRom),File.ReadAllLines(roundToml));
Check(reopened.Model.RawData.SequenceEqual(savedBytes),"Saved bytes reopen unchanged");var reNames=reopened.Model.ExportMetadata(null,re.Singletons.MetadataInfo).NamedAnchors;
Check(savedDefinitions.All(a=>reNames.Any(b=>a.Name==b.Name&&a.Address==b.Address&&a.Format==b.Format)),"Saved definitions survive reopen");
reopened.MapEditor.NavigateTo(created.MapID/1000,created.MapID%1000,0,0);Check(reopened.MapEditor.PrimaryMap.BlockRenders.Count==newBlocks.Count,"New map palette survives save/reopen");
Console.WriteLine($"REVIEW_COMPLETE:checks={checks},failures={failures.Count}");
var report=new{status="EXPERIMENTAL_NOT_FOR_DISTRIBUTION",checks,failures,specificScadeBytesPreserved=specific,mapsCompared=maps.Length,blocksetPairs=donorPairs.Count,blocksetsCompared=sets.Count,rawAssetChecks,mismatches,previewFailures,paletteCounts,legacyFrameChanges=frameChanges,frameRangesChecked=unique.Count,arenaFormattedOwners=occupied,typedArenaPointers,suppliedNamedDefinitions=originalNames.Count,missingBeforeCreate=missingBefore,changedDefinitionsAfterMapCreation=changedDefinitions,normalSaveReopenChecks=true,newPalletPaletteCount=newBlocks.Count,snapshots,candidateSha256=Hash(candidateData)};
File.WriteAllText(Path.Combine(folder,"editor-verification.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));

if(failures.Count>0)throw new InvalidOperationException("Review failed; see the complete editor-verification.json report.");
