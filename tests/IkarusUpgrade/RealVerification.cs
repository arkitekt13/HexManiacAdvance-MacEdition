using System.Security.Cryptography;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using Upgrade = HavenSoft.HexManiac.AvaloniaUI.Project.IkarusUpgrade;

internal static class RealVerification {
 public static int ReviewProject(string sourcePath) {
  var bytes=File.ReadAllBytes(sourcePath);
  var metadataPath=Path.ChangeExtension(sourcePath,".toml");
  var metadata=File.ReadAllBytes(metadataPath);
  var folder=Path.Combine(Path.GetTempPath(),"hma-cerulean-project-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(folder);
  try {
   var path=Path.Combine(folder,"disposable.gba");File.WriteAllBytes(path,bytes);
   File.WriteAllBytes(Path.ChangeExtension(path,".toml"),metadata);
   var fs=new StubFileSystem { MetadataFor=file=>File.Exists(Path.ChangeExtension(file,".toml"))?File.ReadAllLines(Path.ChangeExtension(file,".toml")):Array.Empty<string>() };
   var editor=new EditorViewModel(fs,InstantDispatch.Instance);
   var view=editor.OpenFileAsync(new LoadedFile(path,bytes.ToArray())).GetAwaiter().GetResult();
   view.Model.InitializationWorkload.GetAwaiter().GetResult();view.InitializationWorkload.GetAwaiter().GetResult();
   var table=view.Model.GetTableModel(HardcodeTablesModel.LevelMovesTableName);
   Console.WriteLine("UNREADABLE_LEARNSET_ROWS:"+string.Join(',',table.Where(row=>row.GetSubTable("movesFromLevel")==null).Select(row=>row.ArrayIndex)));
   var learnsets=HavenSoft.HexManiac.Core.ViewModels.Map.Flags.GetPokemonLevelupMoves(view.Model);
   if(learnsets.Count==0)throw new Exception("Expected readable learnsets.");
   view.Goto.Execute("maps.bank3.CERULEAN CITY.3-3");
   var map=view.MapEditor.PrimaryMap;
   if(map.MapID!=3003 || map.PixelData.Length==0 || map.EventGroup==null)throw new Exception("Cerulean failed to load.");
   foreach(var obj in map.EventGroup.Objects) { _=obj.ShowNoContent; _=obj.ShowRematchTrainerContent; }
   view.MapEditor.SelectBlock(1,0);view.MapEditor.ReleaseBlock(1,0);
   if(map.BlockEditor.BlockIndex!=1)throw new Exception("Block palette selection failed.");
   var x=map.LeftEdge+map.PixelWidth*map.SpriteScale/2;
   var y=map.TopEdge+map.PixelHeight*map.SpriteScale/2;
   view.MapEditor.SelectDown(x,y);view.MapEditor.SelectUp(x,y);
   if(map.PixelData.Length==0)throw new Exception("Map tile selection failed.");
   if(!bytes.SequenceEqual(view.Model.RawData)||!bytes.SequenceEqual(File.ReadAllBytes(sourcePath))||!metadata.SequenceEqual(File.ReadAllBytes(metadataPath)))throw new Exception("Read-only project review changed bytes.");
   Console.WriteLine("CERULEAN_PROJECT_PASS:learnsets="+learnsets.Count);
   return 7;
  } finally { Directory.Delete(folder,true); }
 }
 public static int Run(string sourcePath, Upgrade upgrade) {
  int checks=0;
  void Check(bool value,string message) { checks++;if(!value)throw new Exception(message); }
  string Hash(byte[] data)=>Convert.ToHexString(SHA256.HashData(data));
  var source=File.ReadAllBytes(sourcePath);var sourceHash=Hash(source);
  Check(sourceHash=="3D0C79F1627022E18765766F6CB5EA067F6B5BF7DCA115552189AD65A5C3A8AC","exact clean supplied base");
  var root=new DirectoryInfo(AppContext.BaseDirectory);
  while(root!=null && !File.Exists(Path.Combine(root.FullName,"src/HexManiac.Core/ViewModels/QuickEditItems/hubol_dpe_cfru.ups")))root=root.Parent;
  var hubol=new BasicModel(source.ToArray());
  Check(Patcher.ApplyUPSPatch(hubol,File.ReadAllBytes(Path.Combine(root.FullName,"src/HexManiac.Core/ViewModels/QuickEditItems/hubol_dpe_cfru.ups")),()=>new NoTrackChange(),false,out var direction)>=0 && direction==Patcher.UpsPatchDirection.SourceToDestination,"HUBOL checksums enforced");
  var scade=ScadePackage.Load(ScadePackage.DefaultDirectory,out _);
  var folder=Path.Combine(Path.GetTempPath(),"hma-ikarus-real-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  try {
   foreach(var (name,data,metadata) in new[] {
    ("vanilla",source,(string[])null), ("hubol",hubol.RawData,(string[])null),
    ("scade",scade.Create(source),File.ReadAllLines(Path.Combine(ScadePackage.DefaultDirectory,"SCADE.toml"))) }) {
    Console.WriteLine("IKARUS_REAL_START:"+name);
    var path=Path.Combine(folder,name+".gba");File.WriteAllBytes(path,data);
    if(metadata!=null)File.WriteAllLines(Path.ChangeExtension(path,".toml"),metadata);
    int writes=0;
    var fs=new StubFileSystem {
     MetadataFor=file=>File.Exists(Path.ChangeExtension(file,".toml"))?File.ReadAllLines(Path.ChangeExtension(file,".toml")):Array.Empty<string>(),
     Save=file=>{File.WriteAllBytes(file.Name,file.Contents);writes++;return true;},
     SaveMetadata=(file,lines)=>{File.WriteAllLines(Path.ChangeExtension(file,".toml"),lines);writes++;return true;},
    };
    var editor=new EditorViewModel(fs,InstantDispatch.Instance);
    var view=editor.OpenFileAsync(new LoadedFile(path,data.ToArray())).GetAwaiter().GetResult();
    view.Model.InitializationWorkload.GetAwaiter().GetResult();view.InitializationWorkload.GetAwaiter().GetResult();
    if (name != "vanilla") {
     Check(Upgrade.DisabledReason(view)!=null,name+" expanded base rejected by vanilla path");
     Check(view.Model.RawData.SequenceEqual(data),name+" blocked input stays unchanged");
     editor.CloseAll.Execute(fs);
     continue;
    }
    Check(Upgrade.DisabledReason(view)==null,name+" normal loader eligible");
    writes=0;
    var diskBefore=File.ReadAllBytes(path);
    var metadataPath=Path.ChangeExtension(path,".toml");
    var sidecarBefore=File.Exists(metadataPath)?File.ReadAllBytes(metadataPath):null;
    var mapper=view.MapEditor;
    Check(mapper!=null,name+" live map editor exists");
    var liveBefore=mapper.PrimaryMap.PixelData.ToArray();
    view.ChangeHistory.ChangeCompleted();
    view.ChangeHistory.CurrentChange.ChangeData(view.Model,0x700000,(byte)(view.Model[0x700000]^1));view.ChangeHistory.ChangeCompleted();
    var before=view.Model.RawData.ToArray();var names=view.Model.ExportMetadata(null,editor.Singletons.MetadataInfo).NamedAnchors;
    var beforePixels=ScriptMapSnapshot.Create(view,3000).Image.PixelData.ToArray();
    var prepared=upgrade.Prepare(view);var backup=upgrade.WriteBackup(prepared,Path.Combine(folder,name+" backup.gba"));upgrade.Apply(prepared,backup);MainWindow.WatchIkarusMaps(editor,view);view.Refresh();MainWindow.RefreshIkarusMaps(editor,view);
    var after=view.Model.RawData.ToArray();
    var expected=new BasicModel(before.ToArray());Patcher.ApplyIPSPatch(expected,File.ReadAllBytes(Path.Combine(Upgrade.DefaultDirectory,"Ikarus.ips")),new NoTrackChange());
    Check(after.SequenceEqual(expected.RawData),name+" live bytes match independently applied reviewed patch");
    Check(writes==0 && File.ReadAllBytes(path).SequenceEqual(diskBefore) && (sidecarBefore==null ? !File.Exists(metadataPath) : File.ReadAllBytes(metadataPath).SequenceEqual(sidecarBefore)),name+" live patch does not save original files");
    Check(backup.Verify() && File.ReadAllBytes(backup.Path).SequenceEqual(before),name+" backup captures unsaved edit");
    var afterPixels=ScriptMapSnapshot.Create(view,3000).Image.PixelData.ToArray();
    Check(!beforePixels.SequenceEqual(afterPixels),name+" patched Pallet image refreshes");
    var liveAfter=mapper.PrimaryMap.PixelData.ToArray();
    Check(!liveBefore.SequenceEqual(liveAfter),name+" existing live map cache refreshes after patch");
    view.Undo.Execute(null);
    Check(view.Model.RawData.SequenceEqual(before),name+" UI Undo restores bytes");
    Check(ScriptMapSnapshot.Create(view,3000).Image.PixelData.SequenceEqual(beforePixels),name+" Undo rebuilds old map image");
    Check(mapper.PrimaryMap.PixelData.SequenceEqual(liveBefore),name+" existing map cache refreshes after data-tab Undo");
    view.Undo.Execute(null);Check(view.Model.RawData.SequenceEqual(data),name+" prior edit Undo retained");
    view.Redo.Execute(null);view.Redo.Execute(null);Check(view.Model.RawData.SequenceEqual(after),name+" UI Redo exact");
    Check(ScriptMapSnapshot.Create(view,3000).Image.PixelData.SequenceEqual(afterPixels),name+" Redo rebuilds patched map image");
    var current=view.Model.ExportMetadata(null,editor.Singletons.MetadataInfo).NamedAnchors;
    Check(names.All(a=>current.Any(b=>a.Name==b.Name && a.Address==b.Address && a.Format==b.Format)),name+" every named definition survives");
    view.Goto.Execute("maps.bank3.CERULEAN CITY.3-3");
    var liveCerulean=view.MapEditor.PrimaryMap;
    Check(liveCerulean.MapID==3003 && liveCerulean.PixelData.Length>0 && liveCerulean.EventGroup!=null,name+" Go To Cerulean renders live patched trainer events before Save");
    Check(view.Model.RawData.SequenceEqual(after),name+" live Cerulean loading preserves patched bytes");
    view.Save.Execute(fs);Check(writes==2 && view.ChangeHistory.IsSaved,name+" actual normal Save writes ROM and TOML");
    var reopened=editor.OpenFileAsync(new LoadedFile(path,File.ReadAllBytes(path))).GetAwaiter().GetResult();
    reopened.Model.InitializationWorkload.GetAwaiter().GetResult();reopened.InitializationWorkload.GetAwaiter().GetResult();
    Check(reopened.Model.RawData.SequenceEqual(after),name+" saved bytes survive reopen");
    var saved=reopened.Model.ExportMetadata(null,editor.Singletons.MetadataInfo).NamedAnchors;
    Check(names.All(a=>saved.Any(b=>a.Name==b.Name && a.Address==b.Address && a.Format==b.Format)),name+" every named definition survives save/reopen");
    Check(ScriptMapSnapshot.Create(reopened,3000).Image.PixelData.SequenceEqual(afterPixels),name+" reopened map image exact");
    var beforeCerulean=reopened.Model.RawData.ToArray();
    reopened.MapEditor.NavigateTo(3,3,0,0);
    var cerulean=reopened.MapEditor.PrimaryMap;
    Check(cerulean.PixelData.Length>0 && cerulean.EventGroup!=null,name+" Cerulean renders with trainer events after patch and reopen");
    Check(reopened.Model.RawData.SequenceEqual(beforeCerulean),name+" Cerulean event loading preserves ROM bytes");
    Console.WriteLine($"IKARUS_REAL_PASS:{name}:anchors={names.Count}:sha256={Hash(after)}");
   }
   Check(Hash(File.ReadAllBytes(sourcePath))==sourceHash,"uploaded original remains byte identical");
  } finally {Directory.Delete(folder,true);}
  return checks;
 }
}
