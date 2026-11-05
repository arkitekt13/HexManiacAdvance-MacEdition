using Avalonia;
using Avalonia.Threading;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<Application>().UseStandardRuntimePlatformSubsystem().UseWindowingSubsystem(()=>{},"offscreen").UseSkia().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException(message);}
foreach(var (width,height) in new[]{(2,2),(3,4)}) {
   var fixture=new SyntheticMapFixture(7,5);var singletons=fixture.ViewPort.Singletons;
   var raw=fixture.Model.RawData.ToArray();
   var anchors=fixture.Model.ExportMetadata(null,singletons.MetadataInfo).NamedAnchors;
   if(width!=2) {
      var original=anchors.Single(a=>a.Name=="synthetic.layout").Format;
      var custom=original[..^2]+" borderwidth. borderheight.]1";
      anchors=anchors.Select(a=>a with {Format=a.Format.Replace(original,custom)}).ToArray();
      raw[0x158]=(byte)width;raw[0x159]=(byte)height;
   }
   for(int i=0;i<width*height;i++)BitConverter.GetBytes((ushort)0xC000).CopyTo(raw,0x900+i*2);
   BitConverter.GetBytes((ushort)0x5AA5).CopyTo(raw,0x8FE);BitConverter.GetBytes((ushort)0x6BB6).CopyTo(raw,0x900+width*height*2);
   var model=new PokemonModel(raw,new StoredMetadata(anchors:anchors),singletons);
   var viewport=new ViewPort("border-bounds.bin",model,InstantDispatch.Instance,singletons,fs:new StubFileSystem());
   var map=new BlockMapViewModel(new StubFileSystem(),new MapTutorialsViewModel(),viewport,new Format(model),null,0,0){IncludeBorders=false,ShowEvents=MapDisplayOptions.NoEvents};
   _=map.PixelData;var border=map.BorderEditor;
   Check(border.Width==width&&border.Height==height&&border.HasBorderDimensions==(width!=2),"Fixture must expose the intended real border dimensions.");
   viewport.ChangeHistory.ClearHistory(false);var baseline=model.RawData.ToArray();var metadata=model.ExportMetadata(null,singletons.MetadataInfo).Serialize();
   int[,] pattern={{0x4000,0xFC01},{0x0001,0x0400}};
   border.Draw(1,8,8);viewport.ChangeHistory.ChangeCompleted();viewport.Undo.Execute(null);
   Check(viewport.Redo.CanExecute(null)&&baseline.SequenceEqual(model.RawData),"Invalid-input probes start with a real retained Redo entry.");
   var invalid=new[]{(-.001,8d),(-1d,8d),(-17d,8d),(width*16d,8d),(width*16+.01,8d),(8d,-.001),(8d,-1d),(8d,-17d),(8d,height*16d),(8d,height*16+.01),(double.NaN,8d),(8d,double.NaN),(double.NegativeInfinity,8d),(8d,double.PositiveInfinity),(double.MaxValue,8d)};
   foreach(var (x,y) in invalid) {
      border.Draw(1,x,y);border.Draw(pattern,x,y);viewport.ChangeHistory.ChangeCompleted();
      Check(baseline.SequenceEqual(model.RawData),$"Invalid border input ({x},{y}) must write no ROM bytes.");
      Check(border.GetBlock(x,y)==-1,"Invalid border sampling must not read adjacent ROM data.");
      Check(viewport.Redo.CanExecute(null)&&!viewport.Undo.CanExecute(null)&&metadata.SequenceEqual(model.ExportMetadata(null,singletons.MetadataInfo).Serialize()),"Rejected operations preserve metadata and existing history.");
   }
   viewport.ChangeHistory.ClearHistory(false);
   foreach(var multi in new[]{false,true})foreach(var (x,y) in new[]{(0d,0d),(width*16-.01,height*16-.01),(16d,0d),(0d,16d)}) {
      var expected=baseline.ToArray();int left=(int)(x/16),top=(int)(y/16);
      for(int dy=0;dy<(multi?2:1);dy++)for(int dx=0;dx<(multi?2:1);dx++) {
         if(left+dx>=width||top+dy>=height)continue;
         BitConverter.GetBytes((ushort)(multi?pattern[dx,dy]:1)).CopyTo(expected,0x900+((top+dy)*width+left+dx)*2);
      }
      if(multi)border.Draw(pattern,x,y);else border.Draw(1,x,y);viewport.ChangeHistory.ChangeCompleted();
      Check(expected.SequenceEqual(model.RawData),"Valid edge placement changes only intersecting border cells with original packed values.");
      Check(border.GetBlock(x,y)==(multi?pattern[0,0]:1),"Valid border sampling retains its full original word semantics.");
      byte[] saved=null;string[] stored=null;var fs=new StubFileSystem {Save=f=>{saved=f.Contents.ToArray();return true;},SaveMetadata=(_,lines)=>{stored=lines.ToArray();return true;}};
      viewport.Save.Execute(fs);Check(viewport.ChangeHistory.IsSaved&&expected.SequenceEqual(saved),"Save writes the exact valid border edit.");
      var loaded=new PokemonModel(saved,new StoredMetadata(stored),singletons);
      Check(expected.SequenceEqual(loaded.RawData)&&metadata.SequenceEqual(loaded.ExportMetadata(null,singletons.MetadataInfo).Serialize()),"Border edits reload with identical bytes and metadata.");
      viewport.Undo.Execute(null);Check(baseline.SequenceEqual(model.RawData)&&!viewport.ChangeHistory.IsSaved&&!viewport.Undo.CanExecute(null),"One Undo restores all original bytes including both canaries.");
      viewport.Redo.Execute(null);Check(expected.SequenceEqual(model.RawData)&&viewport.ChangeHistory.IsSaved,"Redo restores the exact saved border values.");
      viewport.Undo.Execute(null);
   }
   if(border.HasBorderDimensions) {
      viewport.ChangeHistory.ClearHistory(false);var original=model.RawData.ToArray();
      border.ShowBorderPanel=true;border.Width=2;border.Height=3;viewport.ChangeHistory.ChangeCompleted();map.ClearCaches();
      var resized=model.RawData.ToArray();
      Check(map.BorderEditor.ShowBorderPanel&&map.BorderEditor.Width==2&&map.BorderEditor.Height==3,"Cache refresh retains open state and reloads changed dimensions.");
      Check(map.BorderEditor.BorderRender.PixelWidth==32&&map.BorderEditor.BorderRender.PixelHeight==48,"Changed border dimensions update the displayed image.");
      viewport.Undo.Execute(null);map.ClearCaches();
      Check(original.SequenceEqual(model.RawData)&&map.BorderEditor.Width==width&&map.BorderEditor.Height==height,"Undo refresh restores original dimensions and exact bytes.");
      Check(map.BorderEditor.BorderRender.PixelWidth==width*16&&map.BorderEditor.BorderRender.PixelHeight==height*16&&map.BorderEditor.ShowBorderPanel,"Undo refresh retains open state with the correct image dimensions.");
      viewport.Redo.Execute(null);map.ClearCaches();
      Check(resized.SequenceEqual(model.RawData)&&map.BorderEditor.Width==2&&map.BorderEditor.Height==3&&map.BorderEditor.ShowBorderPanel,"Redo refresh restores resized data without closing the border editor.");
   }
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
