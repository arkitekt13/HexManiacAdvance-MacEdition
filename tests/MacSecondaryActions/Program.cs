using HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.Core.Models.Runs;

using Avalonia.LogicalTree;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Review;
using HavenSoft.HexManiac.Performance;
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message) {checks++;if(!ok)throw new InvalidOperationException(message);}
var gestures=new[]{(MouseButton.Right,RawInputModifiers.None,false),(MouseButton.Left,RawInputModifiers.Control,false),(MouseButton.Left,RawInputModifiers.Control,true)};
if(!args.Contains("--block-only")) {
   using var f=new SyntheticImageFixture(false);f.Window.Editor.SelectedIndex=0;f.ViewPort.Goto.Execute(0x400);f.Pump();
   var hex=f.Window.GetVisualDescendants().OfType<HexContent>().Single(h=>ReferenceEquals(h.ViewPort,f.ViewPort)&&h.IsEffectivelyVisible);
   var point=hex.TranslatePoint(new Avalonia.Point(hex.CellWidth*.5,hex.CellHeight*.5),f.Window)!.Value;
   var before=f.Model.RawData.ToArray();var metadata=f.Metadata();string[] labels=Array.Empty<string>();
   foreach(var(button,modifier,early)in gestures) {
      if(modifier==RawInputModifiers.Control&&!OperatingSystem.IsMacOS())continue;
      hex.ContextMenu?.Close();
      f.Window.MouseDown(point,button,modifier);f.Window.MouseUp(point,button,early?RawInputModifiers.None:modifier);f.Pump();
      Check(hex.ContextMenu?.IsOpen==true,$"Hex context actions must open for secondary/Control-click ({button}/{modifier}).");
      var current=hex.ContextMenu!.Items.OfType<MenuItem>().Select(i=>i.Header?.ToString()??"").ToArray();
      if(labels.Length==0)labels=current;
      Check(current.SequenceEqual(labels),"Control-click must retain the same hex actions and labels as physical secondary-click.");
      hex.ContextMenu.Close();
      Check(before.SequenceEqual(f.Model.RawData)&&metadata.SequenceEqual(f.Metadata()),"Hex context gestures cannot modify ROM bytes or metadata.");
   }
   f.ViewPort.SelectionEnd=new HavenSoft.HexManiac.Core.Models.Point(1,0);f.Pump();
   var tab=f.Window.GetVisualDescendants().OfType<TabView>().Single(t=>ReferenceEquals(t.DataContext,f.ViewPort));
   var summary=tab.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Text?.StartsWith("Selected Bytes:")==true);
   point=summary.TranslatePoint(new Avalonia.Point(summary.Bounds.Width/2,summary.Bounds.Height/2),f.Window)!.Value;
   foreach(var(button,modifier,early)in gestures) {
      if(modifier==RawInputModifiers.Control&&!OperatingSystem.IsMacOS())continue;
      f.Window.MouseDown(point,button,modifier);f.Window.MouseUp(point,button,early?RawInputModifiers.None:modifier);f.Pump();
      // This menu is created by the summary's release handler and opens as a separate popup.
      var menu=(ContextMenu)typeof(TabView).GetField("bytesMenu",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(tab);
      Check(menu?.IsOpen==true,"Selected-byte Copy Bytes must be accessible through secondary/Control-click.");
      var copy=menu!.Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Copy Bytes"));
      copy.Command!.Execute(copy.CommandParameter);menu.Close();f.Pump();
      Check(((IFileSystem)f.FileSystem).CopyText=="FF FF ",$"Copy Bytes copies exactly the selected ROM bytes through the real command (text={((IFileSystem)f.FileSystem).CopyText}).");
      Check(before.SequenceEqual(f.Model.RawData)&&metadata.SequenceEqual(f.Metadata()),"Summary menu operations preserve the model.");
   }
   var summaryMenu=(ContextMenu)typeof(TabView).GetField("bytesMenu",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(tab)!;
   f.Window.MouseDown(point,MouseButton.Left);f.Window.MouseUp(point,MouseButton.Left);f.Pump();
   Check(!summaryMenu.IsOpen,"Ordinary summary clicks must not reopen a stale secondary menu.");
   if(OperatingSystem.IsMacOS()) {
      IPointer pointer=null;hex.AddHandler(InputElement.PointerPressedEvent,(_,e)=>pointer=e.Pointer,Avalonia.Interactivity.RoutingStrategies.Tunnel,handledEventsToo:true);
      var hexPoint=hex.TranslatePoint(new Avalonia.Point(hex.CellWidth*1.5,hex.CellHeight*.5),f.Window)!.Value;
      f.Window.MouseDown(hexPoint,MouseButton.Left,RawInputModifiers.Control);f.Pump();
      Check(ReferenceEquals(pointer?.Captured,hex),"Control-click context gestures retain their physical release target.");
      pointer!.Capture(null);f.Window.MouseUp(hexPoint,MouseButton.Left);f.Pump();
      Check(hex.ContextMenu?.IsOpen!=true,"Losing capture cancels an abandoned hex context gesture.");
      f.Window.MouseDown(hexPoint,MouseButton.Left);f.Window.MouseUp(hexPoint,MouseButton.Left);f.Pump();
      Check(hex.ContextMenu?.IsOpen!=true,"A canceled context gesture cannot turn the next primary click into a menu.");
   }
}
if(!args.Contains("--block-only"))
using(var imageFixture=new SyntheticImageFixture(false)) {
   var palette=imageFixture.View.FindControl<PaletteControl>("PaletteControl")!;
   var colors=palette.FindControl<ItemsControl>("ItemsControl")!;
   var swatch=(Popup)typeof(PaletteControl).GetField("swatchPopup",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(palette)!;
   foreach(var(button,modifier,early)in gestures) {
      if(modifier==RawInputModifiers.Control&&!OperatingSystem.IsMacOS())continue;
      palette.ClosePopup();palette.ContextMenu!.Close();
      var before=imageFixture.Model.RawData.ToArray();var metadata=imageFixture.Metadata();imageFixture.ViewPort.ChangeHistory.ClearHistory(false);
      var point=colors.TranslatePoint(new Avalonia.Point(colors.Bounds.Width/4*1.5,colors.Bounds.Height/4*.5),imageFixture.Window)!.Value;
      imageFixture.Window.MouseMove(point);imageFixture.Window.MouseDown(point,button,modifier);imageFixture.Window.MouseUp(point,button,early?RawInputModifiers.None:modifier);imageFixture.Pump();
      Check(palette.ContextMenu.IsOpen,$"Palette secondary/Control-click must open its actions instead of the primary swatch editor ({button}/{modifier}).");
      Check(!swatch.IsOpen,"Secondary palette actions must not leave the primary color editor open.");
      Check(before.SequenceEqual(imageFixture.Model.RawData)&&metadata.SequenceEqual(imageFixture.Metadata())&&!imageFixture.Image.Undo.CanExecute(null),"Opening palette actions preserves bytes, metadata and history.");
      var copy=palette.ContextMenu.Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Copy"));copy.Command!.Execute(copy.CommandParameter);palette.ContextMenu.Close();
      Check(((IFileSystem)imageFixture.FileSystem).CopyText=="1:1:1","A Control-click palette action uses the clicked color and current filesystem.");
   }
}
var fixture=new SyntheticMapFixture(24,20);fixture.Model.SetList(new NoDataChangeDeltaModel(),"MapAttributeBehaviors","Normal");
fixture.Model.ObserveRunWritten(new NoDataChangeDeltaModel(),new BlockmapRun(fixture.Model,SyntheticMapFixture.Cells,SortedSpan.One(0x14C),24,20));
var block=fixture.Map.BlockEditor;block.BlockIndex=1;block.BlockIndex=0;block.ShowTiles=true;
var control=new SelectedBlockEditor {DataContext=block,Width=350};
var window=new Window {Width=500,Height=300,Content=control};window.Show();
void Pump(){Dispatcher.UIThread.RunJobs();window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
Pump();var tile=control.FindControl<PixelImage>("LeftTopBack")!;
foreach(var(button,modifier,early)in gestures) {
   if(modifier==RawInputModifiers.Control&&!OperatingSystem.IsMacOS())continue;
   block.TileSelectionX=24;block.ShowTiles=true;Pump();
   var before=fixture.Model.RawData.ToArray();fixture.ViewPort.ChangeHistory.ClearHistory(false);
   var point=tile.TranslatePoint(new Avalonia.Point(tile.Bounds.Width/2,tile.Bounds.Height/2),window)!.Value;
   window.MouseMove(point);window.MouseDown(point,button,modifier);window.MouseUp(point,button,early?RawInputModifiers.None:modifier);Pump();
   Check(block.DrawTileIndex==0,$"Secondary/Control-click on a block tile must sample the tile instead of painting the selected tile over it ({button}/{modifier}, tile={block.DrawTileIndex}, hit={window.InputHitTest(point)}, point={point}, bounds={tile.Bounds}).");
   Check(before.SequenceEqual(fixture.Model.RawData)&&!fixture.ViewPort.ChangeHistory.Undo.CanExecute(null),"Block sampling preserves ROM bytes and creates no edit history.");
}
block.TileSelectionX=24;block.ShowTiles=true;Pump();
var unchanged=fixture.Model.RawData.ToArray();var target=tile.TranslatePoint(new Avalonia.Point(tile.Bounds.Width/2,tile.Bounds.Height/2),window)!.Value;
window.MouseDown(target,MouseButton.Left);window.MouseUp(target,MouseButton.Left);Pump();fixture.ViewPort.ChangeHistory.ChangeCompleted();
Check(!unchanged.SequenceEqual(fixture.Model.RawData),$"Ordinary primary clicks must still paint selected block tiles (index={block.DrawTileIndex}, show={block.ShowTiles}, hit={window.InputHitTest(target)}, raw={fixture.Model[0x6000]}, pixel={block.LeftTopBack.PixelData[0]}).");
var painted=fixture.Model.RawData.ToArray();fixture.ViewPort.Undo.Execute(null);Pump();
Check(unchanged.SequenceEqual(fixture.Model.RawData),"Undo restores all bytes before block painting.");
fixture.ViewPort.Redo.Execute(null);Pump();Check(painted.SequenceEqual(fixture.Model.RawData),"Redo restores the same block edit.");
window.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
