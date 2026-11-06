using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core;
using System.Diagnostics;
using System.Text.Json;
using HavenSoft.HexManiac.Performance;
using HavenSoft.HexManiac.Core.ViewModels.Map;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
foreach(var withNpc in new[]{false,true}) {
 var f=new SyntheticMapFixture(80,50,withNpcText:withNpc);
 f.Model.SetList(new HavenSoft.HexManiac.Core.Models.NoDataChangeDeltaModel(), "FacingOptions", "NoMovement");
 f.Model.SetList(new HavenSoft.HexManiac.Core.Models.NoDataChangeDeltaModel(), "MapAttributeBehaviors", "Normal");
 f.Map.ShowEvents=MapDisplayOptions.AllEvents;
 var before=f.Model.RawData.ToArray();
 var metadata=f.Model.ExportMetadata(null,f.ViewPort.Singletons.MetadataInfo).Serialize();
 var saved=f.ViewPort.ChangeHistory.IsSaved; var undo=f.ViewPort.Undo.CanExecute(null);
 var watch=Stopwatch.StartNew();
 try {
  var pixels=f.Map.PixelData;watch.Stop();
  var after=f.Model.ExportMetadata(null,f.ViewPort.Singletons.MetadataInfo).Serialize();
  var cold=watch.Elapsed.TotalMilliseconds;watch.Restart();var again=f.Map.PixelData;watch.Stop();
  Console.WriteLine(JsonSerializer.Serialize(new{withNpc,pixels=pixels.Length,coldMs=cold,cachedMs=watch.Elapsed.TotalMilliseconds,cacheReused=ReferenceEquals(pixels,again),bytesUnchanged=before.SequenceEqual(f.Model.RawData),metadataUnchanged=metadata.SequenceEqual(after),savedBefore=saved,savedAfter=f.ViewPort.ChangeHistory.IsSaved,undoBefore=undo,undoAfter=f.ViewPort.Undo.CanExecute(null),addedMetadata=after.Except(metadata).Take(6)}));
 } catch(Exception e) {Console.WriteLine(JsonSerializer.Serialize(new{withNpc,error=e.GetType().Name,message=e.ToString()}));}
}
