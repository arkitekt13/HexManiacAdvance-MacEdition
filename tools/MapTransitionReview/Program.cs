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

var scenario = args.Single();
Console.WriteLine("SCENARIO:"+scenario);
var issues = 0;
void Observe(string name, bool ok, string detail) {
   if (!ok) issues++;
   Console.WriteLine($"OBSERVATION:{name}:{(ok ? "PASS" : "FAIL")}:{detail}");
}
mapper.AutoUpdateCollision=false;mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=-1;Layout();
viewport.ChangeHistory.ClearHistory(false);
var baseline=model.RawData.ToArray();
var metadata=model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
IPointer pointer=null;int clickCount=0;
window.AddHandler(InputElement.PointerPressedEvent, (_,e)=>{pointer=e.Pointer;clickCount=e.ClickCount;},RoutingStrategies.Tunnel,true);
Avalonia.Point Cell(int x,int y) {
   var current=view.GetVisualDescendants().OfType<PixelImage>().Single(i=>ReferenceEquals(i.DataContext,mapper.PrimaryMap));
   var border=mapper.PrimaryMap.GetBorderThickness();var scale=mapper.PrimaryMap.SpriteScale;
   return current.TranslatePoint(new Avalonia.Point((border.West+x+.5)*16*scale,(border.North+y+.5)*16*scale),window)!.Value;
}
int Value(int x,int y)=>model.ReadMultiByteValue(SyntheticMapFixture.Cells+(y*24+x)*2,2);
void Down(Avalonia.Point p,RawInputModifiers modifier=RawInputModifiers.None) {
   Thread.Sleep(600); // Keep independent presses out of the headless double-click interval.
   window.MouseMove(p);window.MouseDown(p,MouseButton.Left,modifier);Layout();
   Check(clickCount==1,"Audit must use independent single clicks.");
}
void Up(Avalonia.Point p) {window.MouseUp(p,MouseButton.Left);Layout();}
void Click(Avalonia.Point p) {Down(p);Up(p);}
void LoseCapture() {
   var owner=(pointer?.Captured as Control)?.Name;
   var expected=scenario=="picker-capture"?"BlockRenderImage":scenario=="background-capture"?"MapBackground":"MapView";
   Check(owner==expected,$"Capture must belong to {expected}, actual={owner}.");
   Console.WriteLine("CAPTURE_OWNER:"+owner);pointer.Capture(null);Layout();
   Check(pointer.Captured==null,"Capture must actually be released.");
}
var offMap=new Avalonia.Point(5,5);
if(scenario=="undo" || scenario=="redo") {
   Click(Cell(6,6));Check(Value(6,6)==0x4001,"Initial stroke must paint.");
   mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData),"Undo must restore initial bytes.");
   if(scenario=="redo") {mapper.Redo.Execute(null);Layout();mapper.CollisionIndex=1;}
   Click(Cell(6,6));
   Observe("same-cell-"+scenario,Value(6,6)==(scenario=="undo"?0x4001:0x0401),$"value={Value(6,6):X4}");
} else if(scenario=="paint-capture" || scenario=="pan-capture" || scenario=="select-capture" || scenario=="background-capture") {
   var modifier=scenario=="pan-capture"?RawInputModifiers.Alt:scenario=="select-capture"?RawInputModifiers.Control:RawInputModifiers.None;
   var start=scenario=="background-capture" ? view.FindControl<Control>("MapBackground")!.TranslatePoint(new Avalonia.Point(view.Bounds.Width-20,view.Bounds.Height-20),window)!.Value : Cell(6,6);
   Down(start,modifier);LoseCapture();Up(offMap);
   var before=model.RawData.ToArray();var left=mapper.PrimaryMap.LeftEdge;var top=mapper.PrimaryMap.TopEdge;
   var multiple=mapper.DrawMultipleTiles;
   window.MouseMove(scenario=="background-capture" ? start-new Avalonia.Vector(16,16) : Cell(10,8));Layout();
   Observe(scenario+"-no-ghost",before.SequenceEqual(model.RawData)&&left==mapper.PrimaryMap.LeftEdge&&top==mapper.PrimaryMap.TopEdge,
      $"bytesChanged={!before.SequenceEqual(model.RawData)},panChanged={left!=mapper.PrimaryMap.LeftEdge||top!=mapper.PrimaryMap.TopEdge},multiple={mapper.DrawMultipleTiles}");
   mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=-1;Layout();
   Click(Cell(12,8));
   Observe(scenario+"-next-click",Value(12,8)==1&&!mapper.DrawMultipleTiles,$"value={Value(12,8):X4},brush={mapper.DrawBlockIndex},multiple={mapper.DrawMultipleTiles}");
   if(scenario=="paint-capture") {mapper.Undo.Execute(null);Layout();Observe("paint-capture-history-boundary",Value(6,6)==0x4001,$"firstCellAfterUndo={Value(6,6):X4}");}
} else if(scenario=="picker-capture") {
   var blocks=view.FindControl<PixelImage>("BlockRenderImage")!;
   Avalonia.Point Block(int id) {var scale=((IPixelViewModel)blocks.DataContext!).SpriteScale;return blocks.TranslatePoint(new Avalonia.Point((id*16+8)*scale,8*scale),window)!.Value;}
   Down(Block(1));LoseCapture();Up(offMap);Click(Block(0));
   Observe("picker-capture-next-click",mapper.DrawBlockIndex==0&&!mapper.DrawMultipleTiles,$"brush={mapper.DrawBlockIndex},multiple={mapper.DrawMultipleTiles}");
} else if(scenario=="space-focus" || scenario=="space-tab") {
   view.Focus();window.KeyPressQwerty(PhysicalKey.Space,RawInputModifiers.None);Layout();
   Check(mapper.ShowBeneath,"Space must enter reveal mode.");
   if(scenario=="space-tab") {editor.SelectedIndex=0;Layout();}
   else {
      var target=window.GetVisualDescendants().OfType<TextBox>().First(t=>t.IsEffectivelyVisible&&!t.GetVisualAncestors().Contains(view));
      Check(target.Focus(),"Focus must actually move outside MapTab.");Layout();
      Check(!view.IsKeyboardFocusWithin,"Reveal interruption must leave map focus.");
   }
   window.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None);Layout();
   if(scenario=="space-tab") {editor.SelectedIndex=1;Layout();}
   Observe(scenario+"-reveal-cleared",!mapper.ShowBeneath,$"showBeneath={mapper.ShowBeneath}");
} else if(scenario=="tab-stroke" || scenario=="map-stroke") {
   Down(Cell(6,6));
   if(scenario=="tab-stroke") {editor.SelectedIndex=0;Layout();Up(offMap);editor.SelectedIndex=1;Layout();view=window.GetVisualDescendants().OfType<MapTab>().Single();}
   else {mapper.NavigateTo(0,0,6,6);Layout();Up(offMap);}
   var before=model.RawData.ToArray();window.MouseMove(Cell(10,8));Layout();
   Observe(scenario+"-no-ghost",before.SequenceEqual(model.RawData),$"bytesChanged={!before.SequenceEqual(model.RawData)}");
   Click(Cell(12,8));Observe(scenario+"-next-click",Value(12,8)==0x0001,$"value={Value(12,8):X4}");
   mapper.Undo.Execute(null);Layout();Observe(scenario+"-history-boundary",Value(6,6)==0x4001,$"firstCellAfterUndo={Value(6,6):X4}");
} else if(scenario=="escape-stroke") {
   Down(Cell(6,6));var before=model.RawData.ToArray();
   window.KeyPressQwerty(PhysicalKey.Escape,RawInputModifiers.LeftMouseButton);window.KeyReleaseQwerty(PhysicalKey.Escape,RawInputModifiers.LeftMouseButton);Layout();
   Check(mapper.DrawBlockIndex==-1&&mapper.CollisionIndex==-1,"Escape must actually clear the brush before testing continued motion.");
   window.MouseMove(Cell(10,8),RawInputModifiers.LeftMouseButton);Layout();
   Observe("escape-stops-active-stroke",before.SequenceEqual(model.RawData),$"bytesChanged={!before.SequenceEqual(model.RawData)}");
   Up(Cell(10,8));
} else if(scenario=="completed-pan" || scenario=="completed-select") {
   var p=Cell(6,6);Down(p,scenario=="completed-pan"?RawInputModifiers.Alt:RawInputModifiers.Control);
   window.MouseMove(p+new Avalonia.Vector(16,16),RawInputModifiers.LeftMouseButton);Up(p+new Avalonia.Vector(16,16));
   Observe(scenario+"-no-write",baseline.SequenceEqual(model.RawData),"completed gesture with modifier released before pointer");
   mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=-1;Layout();Click(Cell(12,8));
   Observe(scenario+"-paint",Value(12,8)==1,$"value={Value(12,8):X4}");
} else throw new ArgumentException("Unknown scenario");
if(scenario!="map-stroke") Check(metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Audit must preserve metadata.");
else Console.WriteLine("NAVIGATION_METADATA_EQUAL_TO_START:"+metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()));
Console.WriteLine($"POSITIVE_CONTROLS:{checks}");Console.WriteLine($"FAILED_OBSERVATIONS:{issues}");
window.Editor=null;window.Close();
