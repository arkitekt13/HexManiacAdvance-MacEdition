using Avalonia.Interactivity;
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
int At(int x,int y)=>SyntheticMapFixture.Cells+(y*24+x)*2;
var destPermissions=new[]{4,12,32,60};
for(int y=0;y<20;y++)for(int x=0;x<24;x++)model.WriteMultiByteValue(At(x,y),2,token,(destPermissions[(x+2*y)%4]<<10)|((x+y)%2));
var sourcePermissions=args[0]=="nine"?new[]{16,12,3,63,1,48,5,9,62}:new[]{16,0,3,63,1,48,5,9,62};
for(int y=0;y<3;y++)for(int x=0;x<3;x++)model.WriteMultiByteValue(At(x+2,y+2),2,token,(sourcePermissions[y*3+x]<<10)|((x+y)%2));
// Distinct canaries immediately outside the fabricated 2x2 border.
model.WriteMultiByteValue(0x8FE,2,token,0x5AA5);
model.WriteMultiByteValue(0x908,2,token,0x6BB6);
var fs=new StubFileSystem{MetadataFor=_=>Array.Empty<string>()};
Application.Current!.Resources["FileSystem"]=fs;
var editor=new EditorViewModel(fs,InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
var viewport=new ViewPort("copied-brush-review.bin",model,InstantDispatch.Instance,new Singletons(),tutorials:editor.MapTutorials,fs:fs);
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

var operation=args[0];var mode=args[1];var observations=0;var failures=0;
Console.WriteLine($"CASE:{operation}/{mode}");
void Observe(string name,bool ok,string detail) {observations++;if(!ok)failures++;Console.WriteLine($"OBSERVATION:{name}:{(ok?"PASS":"FAIL")}:{detail}");}
mapper.AutoUpdateCollision=false;mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);Layout();
viewport.ChangeHistory.ClearHistory(false);
var baseline=model.RawData.ToArray();var metadata=model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
int Read(int x,int y)=>model.ReadMultiByteValue(At(x,y),2);
Avalonia.Point Cell(int x,int y) {
   var current=view.GetVisualDescendants().OfType<PixelImage>().Single(i=>ReferenceEquals(i.DataContext,mapper.PrimaryMap));
   var border=mapper.PrimaryMap.GetBorderThickness();var scale=mapper.PrimaryMap.SpriteScale;
   return current.TranslatePoint(new Avalonia.Point((border.West+x+.5)*16*scale,(border.North+y+.5)*16*scale),window)!.Value;
}
IPointer pointer=null;int clickCount=0;
window.AddHandler(InputElement.PointerPressedEvent,(_,e)=>{pointer=e.Pointer;clickCount=e.ClickCount;},RoutingStrategies.Tunnel,true);
void Down(Avalonia.Point point,RawInputModifiers modifiers=RawInputModifiers.None) {
   Thread.Sleep(600);window.MouseMove(point);window.MouseDown(point,MouseButton.Left,modifiers);Layout();Check(clickCount==1,"Review requires independent single presses.");
}
void Up(Avalonia.Point point) {window.MouseUp(point,MouseButton.Left);Layout();}
void Copy(int size) {Down(Cell(2,2),RawInputModifiers.Control);window.MouseMove(Cell(2+size-1,2+size-1),RawInputModifiers.Control|RawInputModifiers.LeftMouseButton);Layout();Up(Cell(2+size-1,2+size-1));Check(mapper.DrawMultipleTiles,"Actual Control selection must create a copied multi-cell brush.");}
void ExpectRegion(int width,int height,Func<int,int,int> expected,string name) {
   var ok=true;var words=new List<string>();
   for(int y=0;y<height;y++)for(int x=0;x<width;x++) {var value=Read(x+12,y+8);ok&=value==expected(x,y);words.Add(value.ToString("X4"));}
   Observe(name,ok,string.Join(",",words));
}
if(operation.StartsWith("border-drag")) {
   if(mode=="multi")Copy(2);
   var border=view.FindControl<PixelImage>("BorderImage")!;
   border.GetVisualAncestors().OfType<Avalonia.Controls.Border>().First(b=>b.Classes.Contains("slidePanel")).Transitions=null;
   mapper.PrimaryMap.BorderEditor.ShowBorderPanel=true;Layout();var scale=((IPixelViewModel)border.DataContext!).SpriteScale;
   Avalonia.Point BorderPoint(double x,double y)=>border.TranslatePoint(new Avalonia.Point(x*scale,y*scale),window)!.Value;
   Check(ReferenceEquals(window.InputHitTest(BorderPoint(8,8)),border),"The fully opened border image must be the actual hit target.");
   Down(BorderPoint(8,8));
   Console.WriteLine("BORDER_CAPTURE_OWNER:"+((pointer.Captured as Control)?.Name??"none"));
   if(operation!="border-drag-before") Check((pointer.Captured as Control)?.Name=="BorderImage","Border must own capture for the current pointer route.");
   var valid=model.RawData.ToArray();
   var outside=operation.EndsWith("right")?BorderPoint(72,8):BorderPoint(-17,8);
   window.MouseMove(outside,RawInputModifiers.LeftMouseButton);Layout();Up(outside);
   var after=model.RawData.ToArray();
   Observe("outside-border-does-not-write",valid.SequenceEqual(after),$"canary8FE={model.ReadMultiByteValue(0x8FE,2):X4}, canary908={model.ReadMultiByteValue(0x908,2):X4}, changed="+string.Join(",",Enumerable.Range(0,after.Length).Where(i=>valid[i]!=after[i]).Select(i=>$"{i:X}:{valid[i]:X2}>{after[i]:X2}")));
} else {
   Copy(operation=="nine"?3:2);
   var movement=view.GetVisualDescendants().OfType<AngleComboBox>().Single(c=>ReferenceEquals(c.ItemsSource,mapper.PrimaryMap.BlockEditor.MovementPermissions));
   Check(movement.ItemCount==64,"Movement selector must contain its actual 64 numeric permission entries.");
   Console.WriteLine("MOVEMENT_OPTIONS:"+movement.ItemCount+":"+string.Join("|",movement.Items.Cast<object>().Take(2))+":"+movement.Items.Cast<object>().Last());
   if(mode=="explicit" || mode=="toggle") {movement.SetCurrentValue(ComboBox.SelectedIndexProperty,1);Layout();Check(mapper.CollisionIndex==1,"Actual movement binding must apply explicit permission 1.");}
   if(mode=="preserve" || mode=="toggle") {mapper.CollisionIndex=-1;Layout();Check(mapper.DrawMultipleTiles,"Setting Core preserve must leave the copied pattern active.");}
   if(mode=="ui-clear") {movement.SetCurrentValue(ComboBox.SelectedIndexProperty,-1);Layout();Observe("empty-combo-does-not-enable-preserve",mapper.CollisionIndex==16,$"Core permission={mapper.CollisionIndex}");}
   if(mode=="recopy") {mapper.CollisionIndex=-1;Layout();Copy(operation=="nine"?3:2);}
   Check(baseline.SequenceEqual(model.RawData)&&!viewport.Undo.CanExecute(null),"Selecting/altering a brush must not write ROM bytes or history.");
   if(operation=="stamp") {
      Down(Cell(12,8));Up(Cell(12,8));
      ExpectRegion(2,2,(x,y)=> {var source=BitConverter.ToUInt16(baseline,At(x+2,y+2));return mode=="preserve"||mode=="toggle"?(source&0x3FF)|(BitConverter.ToUInt16(baseline,At(x+12,y+8))&0xFC00):mode=="explicit"?(source&0x3FF)|0x400:source;},"stamp-permission-semantics");
   } else if(operation=="rectangle") {
      var command=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
      Down(Cell(12,8),command);window.MouseMove(Cell(15,11),command|RawInputModifiers.LeftMouseButton);Layout();Up(Cell(15,11));
      ExpectRegion(4,4,(x,y)=> {var source=BitConverter.ToUInt16(baseline,At(x%2+2,y%2+2));return mode=="preserve"||mode=="toggle"?(source&0x3FF)|(BitConverter.ToUInt16(baseline,At(x+12,y+8))&0xFC00):mode=="explicit"?(source&0x3FF)|0x400:source;},"rectangle-permission-semantics");
   } else if(operation=="nine") {
      mapper.Use9Grid=true;Layout();Check(mapper.IsValid9GridSelection,"Copied 3x3 pattern must activate nine-grid mode.");
      if(mode=="late-explicit") {movement.SetCurrentValue(ComboBox.SelectedIndexProperty,1);Layout();Check(mapper.CollisionIndex==1,"Late explicit permission must reach Core after nine-grid discovery.");}
      Down(Cell(12,8));window.MouseMove(Cell(13,8),RawInputModifiers.LeftMouseButton);Layout();window.MouseMove(Cell(12,9),RawInputModifiers.LeftMouseButton);Layout();Up(Cell(12,9));
      Console.WriteLine("NINE_WORDS:"+string.Join(",",Enumerable.Range(8,3).SelectMany(y=>Enumerable.Range(12,3).Select(x=>Read(x,y).ToString("X4")))));
      if(mode=="explicit"||mode=="late-explicit") Observe("nine-inner-corner-keeps-explicit-permission",Read(13,9)>>10==1,$"inner corner={Read(13,9):X4}");
      if(mode=="preserve"||mode=="toggle") Observe("nine-keeps-destination-permissions",Enumerable.Range(8,3).SelectMany(y=>Enumerable.Range(12,3).Where(x=>x!=14||y!=10).Select(x=>(x,y))).All(p=>(Read(p.x,p.y)&0xFC00)==(BitConverter.ToUInt16(baseline,At(p.x,p.y))&0xFC00)),"See NINE_WORDS; final bottom-right cell is outside the L region.");
      Console.WriteLine($"NINE_CORNER_PERMISSION:{Read(13,9)>>10}; derived-corner permissions are recorded without assuming copied-mode policy.");
   } else if(operation=="border") {
      var border=view.FindControl<PixelImage>("BorderImage")!;
      border.GetVisualAncestors().OfType<Avalonia.Controls.Border>().First(b=>b.Classes.Contains("slidePanel")).Transitions=null;
      mapper.PrimaryMap.BorderEditor.ShowBorderPanel=true;Layout();var scale=((IPixelViewModel)border.DataContext!).SpriteScale;
      var point=border.TranslatePoint(new Avalonia.Point(8*scale,8*scale),window)!.Value;Down(point);Up(point);
      var words=Enumerable.Range(0,4).Select(i=>model.ReadMultiByteValue(0x900+i*2,2)).ToArray();
      Console.WriteLine("BORDER_WORDS:"+string.Join(",",words.Select(w=>w.ToString("X4"))));
      // Border movement semantics need an engine-specific policy; record raw values without assuming one.
      Check(model.ReadMultiByteValue(0x8FE,2)==0x5AA5&&model.ReadMultiByteValue(0x908,2)==0x6BB6,"In-bounds copied border stamp must preserve outside canaries.");
   } else throw new ArgumentException("Unknown operation");
}
var edited=model.RawData.ToArray();
if(!operation.StartsWith("border-drag")) {
   var allowed=baseline.ToArray();
   if(operation=="border")Array.Copy(edited,0x900,allowed,0x900,8);
   else {
      var size=operation=="stamp"?2:operation=="rectangle"?4:3;
      for(int y=0;y<size;y++)for(int x=0;x<size;x++)Array.Copy(edited,At(12+x,8+y),allowed,At(12+x,8+y),2);
   }
   Check(allowed.SequenceEqual(edited),"All bytes outside the intended paint region, including source and pointer data, must be unchanged.");
}
Check(!baseline.SequenceEqual(edited),"The probe must have applied a real edit.");
Check(metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"All metadata must remain unchanged.");
byte[] saved=null;string[] stored=null;
fs.Save=file=>{saved=file.Contents.ToArray();return true;};fs.SaveMetadata=(_,lines)=>{stored=lines.ToArray();return true;};
viewport.Save.Execute(fs);Check(viewport.ChangeHistory.IsSaved&&edited.SequenceEqual(saved),"Save records the exact observed bytes, including any bad edit.");
var reloaded=new PokemonModel(saved,new StoredMetadata(stored),viewport.Singletons);
Check(edited.SequenceEqual(reloaded.RawData)&&metadata.SequenceEqual(reloaded.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Independent reload retains exact bytes and metadata.");
mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData)&&!viewport.ChangeHistory.IsSaved&&!mapper.Undo.CanExecute(null),"One Undo restores all bytes with no extra history entry.");
mapper.Redo.Execute(null);Layout();Check(edited.SequenceEqual(model.RawData)&&viewport.ChangeHistory.IsSaved,"Redo restores the exact observed edit and saved state.");
Console.WriteLine($"POSITIVE_CONTROLS:{checks}");Console.WriteLine($"OBSERVATIONS:{observations}");Console.WriteLine($"FAILED_OBSERVATIONS:{failures}");
window.Editor=null;window.Close();
