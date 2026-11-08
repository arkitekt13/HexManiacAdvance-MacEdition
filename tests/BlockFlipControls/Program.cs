using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using BlockEditor=HavenSoft.HexManiac.Core.ViewModels.Map.BlockEditor;
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var fs=new StubFileSystem();Application.Current.Resources["FileSystem"]=fs;var editor=new EditorViewModel(fs,InstantDispatch.Instance);App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
var model=new PokemonModel(new byte[512],singletons:new Singletons());
model.SetList(new NoDataChangeDeltaModel(),"MapAttributeBehaviors","Ocean_Water");model.SetList(new NoDataChangeDeltaModel(),"MapLayerOptions","Normal");model.SetList(new NoDataChangeDeltaModel(),"MapTerrainOptions","Water");model.SetList(new NoDataChangeDeltaModel(),"MapEncounterOptions","Water");
var palettes=Enumerable.Range(0,16).Select(_=>new short[]{0,31,992,31744,32767,31,992,31744,0,31,992,31744,32767,31,992,31744}).ToArray();
var tiles=Enumerable.Range(0,1024).Select(_=>{var t=new int[8,8];for(int x=0;x<8;x++)for(int y=0;y<8;y++)t[x,y]=1+(x<3?0:y<4?1:2);return t;}).ToArray();
var blocks=new[]{new byte[16],new byte[16]};var attrs=new[]{new byte[4],new byte[4]};
var block=new BlockEditor(new ChangeHistory<ModelDelta>(delta=>delta.Revert(model)),model,new MapTutorialsViewModel(),palettes,tiles,blocks,attrs);block.BlockIndex=1;block.BlockIndex=0;block.ShowTiles=true;
var control=new SelectedBlockEditor{DataContext=block,Width=300};var host=new Window{Width=500,Height=300,Content=control};host.Show();
void Pump(){Dispatcher.UIThread.RunJobs();host.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();}
int checks=0,changes=0;void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}block.BlocksChanged+=(_,_)=>changes++;
var names=new[]{"LeftTopBack","RightTopBack","LeftBottomBack","RightBottomBack","LeftTopFront","RightTopFront","LeftBottomFront","RightBottomFront"};
var canvas=control.FindControl<Canvas>("TileCanvas");
foreach(var width in new[]{280d,300d,350d})foreach(var font in new[]{14d,20d}){
 control.Width=width;host.FontSize=font;Pump();
 for(int index=0;index<names.Length;index++)foreach(bool horizontal in new[]{false,true}){
  var tile=control.FindControl<PixelImage>(names[index]);var origin=tile.TranslatePoint(new Avalonia.Point(12,12),host).Value;host.MouseMove(origin);Pump();
  var button=canvas.Children.OfType<Button>().Single(b=>horizontal?b.Width==24:b.Height==24);
  var target=button.TranslatePoint(new Avalonia.Point(button.Bounds.Width/2,button.Bounds.Height/2),host).Value;
  var before=blocks[0].ToArray();var state=LzTilemapRun.ReadTileData(blocks[0],index,2);var oldChanges=changes;
  // Continuous movement crosses the narrow gap which direct center-click tests miss.
  int steps=(int)Math.Ceiling(Math.Max(Math.Abs(target.X-origin.X),Math.Abs(target.Y-origin.Y)));
  for(int n=1;n<=steps;n++){
   var point=origin+(target-origin)*(n/(double)steps);host.MouseMove(point);Pump();
   Check(button.IsEffectivelyVisible,$"Flip arrow must remain visible across its approach path: {names[index]},horizontal={horizontal},width={width},font={font},point={point}.");
  }
  var hit=host.InputHitTest(target);Check(ReferenceEquals(hit,button)||hit is Visual v&&v.GetVisualAncestors().Contains(button),"The visible arrow must receive its positional click, not a dropdown or underlying tile.");
  host.MouseDown(target,MouseButton.Left);host.MouseUp(target,MouseButton.Left);Pump();
  var expected=before.ToArray();LzTilemapRun.WriteTileData(expected,index,state.Item1,horizontal?!state.Item2:state.Item2,horizontal?state.Item3:!state.Item3,state.Item4);
  Check(blocks[0].SequenceEqual(expected)&&changes==oldChanges+1,"Each click must flip only the intended quadrant/axis exactly once, preserving tile/palette/other bits.");
  host.MouseMove(new Avalonia.Point(1,1));Pump();Check(!block.FlipHVisible&&!block.FlipVVisible,"Leaving the complete interaction area must hide flip controls.");
 }
}
Check(model.RawData.All(b=>b==0),"The isolated control check must not touch ROM bytes.");
host.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
