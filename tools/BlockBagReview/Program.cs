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
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Map;
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
model.WriteMultiByteValue(SyntheticMapFixture.Cells+(6*24+6)*2,2,token,0x4000);
model.WriteMultiByteValue(SyntheticMapFixture.Cells+(6*24+7)*2,2,token,0x4000);
model.WriteMultiByteValue(SyntheticMapFixture.Cells+(6*24+8)*2,2,token,0x4000);
var fs=new StubFileSystem{MetadataFor=_=>Array.Empty<string>()};
Application.Current!.Resources["FileSystem"]=fs;
var editor=new EditorViewModel(fs,InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
var viewport=new ViewPort("bag-review.bin",model,InstantDispatch.Instance,new Singletons(),tutorials:editor.MapTutorials,fs:fs);
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
mapper.AutoUpdateCollision=false;mapper.SelectBlock(0,0);mapper.ReleaseBlock(0,0);Layout();
viewport.ChangeHistory.ClearHistory(false);
var before=model.RawData.ToArray();var metadata=model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
var blocks=view.FindControl<PixelImage>("BlockRenderImage")!;
var bag=view.FindControl<StackPanel>("BlockBag")!;
var command=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
void ClickBlock(int id,RawInputModifiers modifiers) {
   double scale=((IPixelViewModel)blocks.DataContext!).SpriteScale;
   var point=blocks.TranslatePoint(new Avalonia.Point((id%8*16+8)*scale,(id/8*16+8)*scale),window)!.Value;
   window.MouseMove(point);window.MouseDown(point,MouseButton.Left,modifiers);window.MouseUp(point,MouseButton.Left,modifiers);Layout();
}
void ClickBag(RawInputModifiers modifiers) {
   var point=bag.TranslatePoint(new Avalonia.Point(8,8),window)!.Value;
   window.MouseMove(point);window.MouseDown(point,MouseButton.Left,modifiers);window.MouseUp(point,MouseButton.Left,modifiers);Layout();
}
Check(mapper.BlockBag.PixelWidth==0&&bag.IsVisible,"The initial empty mixing bag is visible for a single-block brush.");
if(OperatingSystem.IsMacOS()) {
   ClickBlock(1,RawInputModifiers.Control);
   Check(mapper.BlockBag.PixelWidth==0,"Mac Control-click does not add to the bag.");
}
ClickBlock(1,command);Check(mapper.BlockBag.PixelWidth==16&&mapper.DrawBlockIndex==1,"Command-click adds and selects the clicked block.");
ClickBlock(0,command);Check(mapper.BlockBag.PixelWidth==32,"Command-click can add a second block.");
ClickBlock(0,command);Check(mapper.BlockBag.PixelWidth==16,"Command-click toggles an existing member back out.");
if(OperatingSystem.IsMacOS()) {ClickBag(RawInputModifiers.Control);Check(mapper.BlockBag.PixelWidth==16,"Mac Control-click does not clear the bag.");}
Check(before.SequenceEqual(model.RawData)&&metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize())&&!viewport.Undo.CanExecute(null),"Bag membership is brush state and preserves ROM bytes, metadata and clean history.");
ClickBag(command);Check(mapper.BlockBag.PixelWidth==0,"Command-click on the bag icon clears it.");
ClickBlock(1,command);Check(mapper.BlockBag.PixelWidth==16,"A fresh one-block bag can be rebuilt.");
mapper.CollisionIndex=-1;Layout();
var borders=mapper.PrimaryMap.GetBorderThickness();double mapScale=mapper.PrimaryMap.SpriteScale;
var paint=image.TranslatePoint(new Avalonia.Point((borders.West+6.5)*16*mapScale,(borders.North+6.5)*16*mapScale),window)!.Value;
window.MouseMove(paint);window.MouseDown(paint,MouseButton.Left);window.MouseUp(paint,MouseButton.Left);Layout();
var painted=before.ToArray();BitConverter.GetBytes((ushort)0x4001).CopyTo(painted,SyntheticMapFixture.Cells+(6*24+6)*2);
Check(painted.SequenceEqual(model.RawData),"Painting with the deterministic one-member random bag updates only the intended block and preserves its movement permission.");
Check(viewport.Undo.CanExecute(null),"Bag painting is undoable.");
mapper.Undo.Execute(null);Layout();Check(before.SequenceEqual(model.RawData),"Undo restores every map byte.");
Check(metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize())&&viewport.Redo.CanExecute(null),"Undo restores metadata and preserves Redo.");
mapper.Redo.Execute(null);Layout();Check(painted.SequenceEqual(model.RawData),"Redo restores bag painting.");
// Verify the public Core fill operation separately from pointer/shortcut checks.
// Explicit press kinds avoid relying on the headless clock's double-click counting.
mapper.Undo.Execute(null);Layout();
mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=-1;Layout();
var surface=view.FindControl<ItemsControl>("MapView")!;
var currentImage=view.GetVisualDescendants().OfType<PixelImage>().Single(i=>ReferenceEquals(i.DataContext,mapper.PrimaryMap));
var inSurface=currentImage.TranslatePoint(new Avalonia.Point((borders.West+6.5)*16*mapScale,(borders.North+6.5)*16*mapScale),surface)!.Value;
var local=(x:inSurface.X-surface.Bounds.Width/2,y:inSurface.Y-surface.Bounds.Height/2);
_ = mapper.PrimaryMap.GetBlock(local.x,local.y); // start a fresh Core stroke, independent of the previous history probe
mapper.PrimaryDown(local.x,local.y,PrimaryInteractionStart.Click);mapper.PrimaryUp(local.x,local.y);Layout();
Check(model.ReadMultiByteValue(SyntheticMapFixture.Cells+(6*24+6)*2,2)==0x4001,$"Fill characterization starts with a valid first stamp; brush={mapper.DrawBlockIndex}, bagWidth={mapper.BlockBag.PixelWidth}, value={model.ReadMultiByteValue(SyntheticMapFixture.Cells+(6*24+6)*2,2):X4}.");
mapper.PrimaryDown(local.x,local.y,PrimaryInteractionStart.DoubleClick);mapper.PrimaryUp(local.x,local.y);Layout();
var fill=Enumerable.Range(6,3).Select(x=>model.ReadMultiByteValue(SyntheticMapFixture.Cells+(6*24+x)*2,2)).ToArray();
Check(fill.All(v=>v==0x4001), "Bag flood fill must preserve existing movement permissions.");
Console.WriteLine("FLOOD_FILL_PRESERVES_MOVEMENT:"+fill.All(v=>v==0x4001));
Console.WriteLine("FLOOD_FILL_VALUES:"+string.Join(",",fill.Select(v=>v.ToString("X4"))));
window.Editor=null;window.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
