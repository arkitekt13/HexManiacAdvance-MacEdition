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
using System.Runtime.InteropServices;

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
   Dispatcher.UIThread.RunJobs();window.Measure(new Size(window.Width,window.Height));window.Arrange(new Rect(0,0,window.Width,window.Height));
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
var previewLayer=view.FindControl<Grid>("StampPreviewLayer")!;
var previewBounds=view.FindControl<Avalonia.Controls.Border>("StampPreviewBounds")!;
var previewImage=view.FindControl<PixelImage>("StampPreviewImage")!;
Check(previewLayer!=null && previewBounds!=null && previewImage!=null,"Map must provide the stamp preview overlay.");
Check(!previewLayer.IsHitTestVisible && previewLayer.ClipToBounds,"Preview must pass clicks through and clip to the map viewport.");
Check(previewLayer.ZIndex<view.FindControl<ItemsControl>("MapButtons")!.ZIndex &&
   previewLayer.ZIndex<view.FindControl<TutorialControl>("TutorialsControl")!.ZIndex,"Arrows and tutorials must remain above the preview.");
Check(previewBounds.Opacity>0 && previewBounds.Opacity<1,"Preview must be translucent.");
var previewUploads=0;previewImage.PropertyChanged+=(_,e)=>{if(e.Property==Image.SourceProperty)previewUploads++;};
byte[] BitmapBytes(PixelImage source) {
   var bitmap=(WriteableBitmap)source.Source;using var frame=bitmap.Lock();
   var bytes=new byte[bitmap.PixelSize.Width*bitmap.PixelSize.Height*4];
   for(int y=0;y<bitmap.PixelSize.Height;y++)Marshal.Copy(frame.Address+y*frame.RowBytes,bytes,y*bitmap.PixelSize.Width*4,bitmap.PixelSize.Width*4);
   return bytes;
}
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
   Check(!previewLayer.IsVisible,"Selecting a new rectangle must hide the previous brush image.");
   window.MouseMove(end,modifier|(button==MouseButton.Left?RawInputModifiers.LeftMouseButton:RawInputModifiers.RightMouseButton));
   Layout();
   window.MouseUp(end,button,releaseControlFirst?RawInputModifiers.None:modifier);
   Layout();
   Console.WriteLine($"GESTURE:{button}/{modifier} early_release={releaseControlFirst} scale={scale} multiple={mapper.DrawMultipleTiles} brush={mapper.MultiTileDrawRender?.PixelWidth}x{mapper.MultiTileDrawRender?.PixelHeight}");
   Check(mapper.DrawMultipleTiles && mapper.MultiTileDrawRender.PixelWidth==32 && mapper.MultiTileDrawRender.PixelHeight==48,
      "Secondary/Control drag must capture the exact placed 2x3 block rectangle.");
   Check(model.RawData.SequenceEqual(baseline) && !viewport.Undo.CanExecute(null),"Selecting must not modify the ROM or create undo history.");
   Check(previewLayer.IsVisible && ReferenceEquals(previewImage.DataContext,mapper.MultiTileDrawRender),"Ready preview must use the actual captured brush.");
   Check(previewImage.Source is WriteableBitmap brushBitmap && brushBitmap.PixelSize==new PixelSize(32,48),"Copied 2x3 brush must produce a real source bitmap.");
   var sidebar=view.GetVisualDescendants().OfType<PixelImage>().First(i=>!ReferenceEquals(i,previewImage)&&ReferenceEquals(i.DataContext,mapper.MultiTileDrawRender));
   Check(BitmapBytes(previewImage).SequenceEqual(BitmapBytes(sidebar)) && mapper.MultiTileDrawRender.PixelData.Distinct().Count()>1,
      "Cursor preview must contain the exact nonempty copied pixels shown in the sidebar.");
   var previewSource=previewImage.Source;
   var expected=baseline.ToArray();
   foreach(var (left,top) in new[]{(10,8),(15,12)}) {
      var destination=Cell(left,top);
      var oldUploads=previewUploads;var beforeHover=model.RawData.ToArray();
      window.MouseMove(destination);Layout();
      var topLeft=previewImage.TranslatePoint(new Avalonia.Point(),window)!.Value;
      var bottomRight=previewImage.TranslatePoint(new Avalonia.Point(32,48),window)!.Value;
      Check(Math.Abs(topLeft.X-(destination.X-8*scale))<.01 && Math.Abs(topLeft.Y-(destination.Y-8*scale))<.01,
         "Preview must align with the exact destination tile corner.");
      Check(Math.Abs(bottomRight.X-topLeft.X-32*scale)<.01 && Math.Abs(bottomRight.Y-topLeft.Y-48*scale)<.01,
         "Preview must match map zoom without applying the brush's sidebar scale twice.");
      Check(ReferenceEquals(previewSource,previewImage.Source) && previewUploads==oldUploads,"Pointer movement must reuse the preview bitmap without another upload.");
      Check(beforeHover.SequenceEqual(model.RawData),"Moving the ghost preview must not write map data.");
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
var cachedPreview=previewImage.Source;var cachedUploads=previewUploads;
foreach(var width in new[]{1440d,1441d})foreach(var scale in new[]{.5,1d,2d}) {
   window.Width=width;mapper.PrimaryMap.SpriteScale=scale;Layout();
   var destination=Cell(10,8);window.MouseMove(destination);Layout();
   var corner=previewImage.TranslatePoint(new Avalonia.Point(),window)!.Value;
   var farCorner=previewImage.TranslatePoint(new Avalonia.Point(32,48),window)!.Value;
   Check(Math.Abs(corner.X-(destination.X-8*scale))<.01 && Math.Abs(corner.Y-(destination.Y-8*scale))<.01 &&
      Math.Abs(farCorner.X-corner.X-32*scale)<.01 && Math.Abs(farCorner.Y-corner.Y-48*scale)<.01,
      "Preview must stay pixel-aligned through fractional zoom and odd/even viewport widths.");
   Check(ReferenceEquals(cachedPreview,previewImage.Source) && previewUploads==cachedUploads,"Zoom/resizing must reuse the copied preview bitmap.");
}
Check(model.RawData.SequenceEqual(baseline),"Preview zoom and window resizing must not change any map bytes.");
mapper.ShowBeneath=true;Layout();Check(!previewLayer.IsVisible,"Reveal-beneath mode must hide the brush overlay.");
mapper.ShowBeneath=false;window.MouseMove(new Avalonia.Point(5,5));Layout();
Check(!previewLayer.IsVisible,"Leaving the map must hide the preview.");
window.MouseMove(Cell(10,8));Layout();Check(previewLayer.IsVisible,"Returning to the map must restore the ready brush.");
mapper.Cancel();Layout();Check(!previewLayer.IsVisible,"Cancel must hide stale copied brush pixels.");
// Command-drag must remain rectangle painting, rather than becoming map selection.
mapper.AutoUpdateCollision=false;mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=0;Layout();
window.MouseMove(Cell(10,8));Layout();
Check(previewLayer.IsVisible && previewImage.Source is WriteableBitmap single && single.PixelSize==new PixelSize(16,16),"Single-block brushes must also show the current preview.");
var command=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
window.MouseDown(Cell(10,8),MouseButton.Left,command);
window.MouseMove(Cell(12,10),command|RawInputModifiers.LeftMouseButton);Layout();
window.MouseUp(Cell(12,10),MouseButton.Left,command);Layout();
var rectangle=baseline.ToArray();
for(int y=8;y<=10;y++)for(int x=10;x<=12;x++){var at=SyntheticMapFixture.Cells+(y*24+x)*2;rectangle[at]=1;rectangle[at+1]=0;}
Check(model.RawData.SequenceEqual(rectangle),"Command-drag must still paint the intended rectangle with the selected block.");
viewport.Undo.Execute(null);Check(model.RawData.SequenceEqual(baseline),"Rectangle fill must remain undoable.");
window.Editor=null;window.Close();
Console.WriteLine("PASS: Control/secondary selection, early release, exact repeated stamps, two scales, translucent aligned/cached previews, pointer pass-through, hiding/cancel, metadata, undo/redo, and Command rectangle fill.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
