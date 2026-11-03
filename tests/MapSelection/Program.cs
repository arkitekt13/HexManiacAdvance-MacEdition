using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Performance;
using System.Diagnostics;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks=0;
void Check(bool ok,string message) { checks++;if(!ok)throw new InvalidOperationException(message); }
var fixture=new SyntheticMapFixture(24,20);var model=fixture.Model;
model.SetList(new NoDataChangeDeltaModel(),"MapAttributeBehaviors","Normal");
var token=new ModelDelta();
foreach(var at in new[]{0x104,0x108,0x10C})model.WritePointer(token,at,0x110);
var format=model.GetNextRun(0x100).FormatString;
var error=ArrayRun.TryParse(model,format[..^1]+"4",0x100,SortedSpan<int>.None,out var root);
Check(!error.HasError,"Synthetic map banks must parse.");
model.ObserveAnchorWritten(new NoDataChangeDeltaModel(),"data.maps.banks",root);
((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new("synthetic.layout","data.maps.banks/3/maps/0/map/0/layout/","Maps"));
// Distinct block/collision pairs establish that the brush preserves each cell, not just its art.
int[,] pattern={{0x400,0x801,0xC00},{0x1001,0x1400,0x1801}};
for(int x=0;x<2;x++)for(int y=0;y<3;y++)model.WriteMultiByteValue(SyntheticMapFixture.Cells+((y+2)*24+x+2)*2,2,token,pattern[x,y]);
var fs=new StubFileSystem{MetadataFor=_=>Array.Empty<string>()};
Application.Current!.Resources["FileSystem"]=fs;
var editor=new EditorViewModel(fs,InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
var viewport=new ViewPort("selection-fixture.bin",model,InstantDispatch.Instance,new Singletons(),tutorials:editor.MapTutorials,fs:fs);
viewport.InitializationWorkload.GetAwaiter().GetResult();
var mapper=viewport.MapEditor;
var window=new MainWindow{Width=1440,Height=1000,FileSystem=fs,Editor=editor};
window.Resources["FileSystem"]=fs;window.Show();
editor.Add(viewport);editor.Add(mapper);
void Layout(){
   Dispatcher.UIThread.RunJobs();window.Measure(new Size(1440,1000));window.Arrange(new Rect(0,0,1440,1000));
   // Refresh the headless hit-test scene after zooming, as the native render timer would.
   AvaloniaHeadlessPlatform.ForceRenderTimerTick();
}
Layout();
var view=window.GetVisualDescendants().OfType<MapTab>().Single();
var clock=Stopwatch.StartNew();
while(!view.GetVisualDescendants().OfType<PixelImage>().Any(i=>ReferenceEquals(i.DataContext,mapper.PrimaryMap)&&i.Source is WriteableBitmap)) {
   Layout();if(clock.Elapsed.TotalSeconds>10)throw new TimeoutException("Map frame timed out.");Thread.Sleep(1);
}
var image=view.GetVisualDescendants().OfType<PixelImage>().Single(i=>ReferenceEquals(i.DataContext,mapper.PrimaryMap));
Avalonia.Point Cell(int x,int y) {
   var border=mapper.PrimaryMap.GetBorderThickness();var scale=mapper.PrimaryMap.SpriteScale;
   return image.TranslatePoint(new Avalonia.Point((border.West+x+.5)*16*scale,(border.North+y+.5)*16*scale),window)!.Value;
}
var baseline=model.RawData.ToArray();
var metadata=model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
var gestures=new List<(MouseButton Button,RawInputModifiers Modifier,bool ReleaseControlFirst)>();
if(OperatingSystem.IsMacOS()) {
   gestures.Add((MouseButton.Left,RawInputModifiers.Control,false));
   gestures.Add((MouseButton.Left,RawInputModifiers.Control,true));
}
gestures.Add((MouseButton.Right,RawInputModifiers.None,false));
foreach(var (button,modifier,releaseControlFirst) in gestures)foreach(var scale in new[]{1d,2d}) {
   mapper.PrimaryMap.SpriteScale=scale;Layout();
   var start=Cell(2,2);var end=Cell(3,4);
   window.MouseDown(start,button,modifier);
   window.MouseMove(end,modifier|(button==MouseButton.Left?RawInputModifiers.LeftMouseButton:RawInputModifiers.RightMouseButton));
   Layout();
   window.MouseUp(end,button,releaseControlFirst?RawInputModifiers.None:modifier);
   Layout();
   Console.WriteLine($"GESTURE:{button}/{modifier} early_release={releaseControlFirst} scale={scale} multiple={mapper.DrawMultipleTiles} brush={mapper.MultiTileDrawRender?.PixelWidth}x{mapper.MultiTileDrawRender?.PixelHeight}");
   Check(mapper.DrawMultipleTiles && mapper.MultiTileDrawRender.PixelWidth==32 && mapper.MultiTileDrawRender.PixelHeight==48,
      "Secondary/Control drag must capture the exact placed 2x3 block rectangle.");
   Check(model.RawData.SequenceEqual(baseline) && !viewport.Undo.CanExecute(null),"Selecting must not modify the ROM or create undo history.");
   var expected=baseline.ToArray();
   foreach(var (left,top) in new[]{(10,8),(15,12)}) {
      var destination=Cell(left,top);
      window.MouseDown(destination,MouseButton.Left);window.MouseUp(destination,MouseButton.Left);Layout();
      for(int x=0;x<2;x++)for(int y=0;y<3;y++) {
         var at=SyntheticMapFixture.Cells+((top+y)*24+left+x)*2;
         expected[at]=(byte)pattern[x,y];expected[at+1]=(byte)(pattern[x,y]>>8);
         Check(model.ReadMultiByteValue(at,2)==pattern[x,y],"Stamping must preserve each selected block and movement permission.");
      }
      Check(model.RawData.SequenceEqual(expected),"Stamping must change only the intended destination cells.");
   }
   Check(metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Copy/stamp must preserve anchor and pointer metadata.");
   viewport.Undo.Execute(null);viewport.Undo.Execute(null);
   Check(model.RawData.SequenceEqual(baseline),"Undo must restore both stamped areas exactly.");
   viewport.Redo.Execute(null);viewport.Redo.Execute(null);
   Check(model.RawData.SequenceEqual(expected),"Redo must restore both stamped areas exactly.");
   viewport.Undo.Execute(null);viewport.Undo.Execute(null);
}
// Command-drag must remain rectangle painting, rather than becoming map selection.
mapper.AutoUpdateCollision=false;mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=0;Layout();
var command=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
window.MouseDown(Cell(10,8),MouseButton.Left,command);
window.MouseMove(Cell(12,10),command|RawInputModifiers.LeftMouseButton);Layout();
window.MouseUp(Cell(12,10),MouseButton.Left,command);Layout();
var rectangle=baseline.ToArray();
for(int y=8;y<=10;y++)for(int x=10;x<=12;x++){var at=SyntheticMapFixture.Cells+(y*24+x)*2;rectangle[at]=1;rectangle[at+1]=0;}
Check(model.RawData.SequenceEqual(rectangle),"Command-drag must still paint the intended rectangle with the selected block.");
viewport.Undo.Execute(null);Check(model.RawData.SequenceEqual(baseline),"Rectangle fill must remain undoable.");
window.Editor=null;window.Close();
Console.WriteLine("PASS: Control/secondary map selection, early modifier release, exact repeated stamps including collision bits, two scales, undo/redo, metadata, and Command rectangle fill.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
