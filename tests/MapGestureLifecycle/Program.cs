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

if (args.Length == 0) {
   var total = 0;
   foreach (var name in new[] { "paint-capture", "pan-capture", "select-capture", "picker-capture", "background-capture", "tab-stroke", "completed-pan", "completed-select", "rectangle-capture", "bag-capture", "context-stroke", "release-endpoint", "unrelated-release", "event-capture", "pending-template", "navigation-stroke", "pending-wave", "escape-stroke", "escape-rectangle", "escape-event", "escape-picker", "escape-pan", "escape-select", "escape-template", "escape-wave", "cancel-api", "rectangle-permissions", "same-cell-history", "reveal-focus", "reveal-tab", "reveal-context", "reveal-navigation", "reveal-normal" }) {
      var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
      start.ArgumentList.Add(System.Reflection.Assembly.GetExecutingAssembly().Location);
      start.ArgumentList.Add(name);
      using var child = Process.Start(start)!;
      var output = child.StandardOutput.ReadToEnd(); var errors = child.StandardError.ReadToEnd();child.WaitForExit();
      if (child.ExitCode != 0) throw new InvalidOperationException(name + " failed: " + output + errors);
      total += int.Parse(output.Split('\n').Single(line => line.StartsWith("CHILD_CHECKS:")).Split(':')[1]);
      Console.WriteLine("PASS: " + name);
   }
   Console.WriteLine("CHECKS_PASSED:" + total);return;
}
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks=0;
void Check(bool ok,string message) { checks++;if(!ok)throw new InvalidOperationException(message); }
var fixture=new SyntheticMapFixture(24,20,withScriptEvent: args[0]=="event-capture"||args[0]=="escape-event");var model=fixture.Model;
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
if(args[0]=="rectangle-permissions") {
   var permissions=new[]{0,1,16,63};
   for(int y=2;y<=9;y++)for(int x=2;x<=9;x++)
      model.WriteMultiByteValue(SyntheticMapFixture.Cells+(y*24+x)*2,2,token,(permissions[(x+y)%4]<<10)|((x+y)%2));
}
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
void Observe(string name, bool ok, string detail) {
   Check(ok, name + ": " + detail);
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
if(scenario=="paint-capture" || scenario=="pan-capture" || scenario=="select-capture" || scenario=="background-capture") {
   var modifier=scenario=="pan-capture"?RawInputModifiers.Alt:scenario=="select-capture"?RawInputModifiers.Control:RawInputModifiers.None;
   var start=scenario=="background-capture" ? view.FindControl<Control>("MapBackground")!.TranslatePoint(new Avalonia.Point(view.Bounds.Width-20,view.Bounds.Height-20),window)!.Value : Cell(6,6);
   Down(start,modifier);LoseCapture();Up(offMap);
   var before=model.RawData.ToArray();var left=mapper.PrimaryMap.LeftEdge;var top=mapper.PrimaryMap.TopEdge;
   var multiple=mapper.DrawMultipleTiles;
   window.MouseMove(scenario=="background-capture" ? start-new Avalonia.Vector(16,16) : Cell(10,8));Layout();
   Observe(scenario+"-no-ghost",before.SequenceEqual(model.RawData)&&left==mapper.PrimaryMap.LeftEdge&&top==mapper.PrimaryMap.TopEdge,
      $"bytesChanged={!before.SequenceEqual(model.RawData)},panChanged={left!=mapper.PrimaryMap.LeftEdge||top!=mapper.PrimaryMap.TopEdge},multiple={mapper.DrawMultipleTiles}");
   if(scenario=="paint-capture") {
      byte[] saved=null;string[] stored=null;
      fs.Save=file=>{saved=file.Contents.ToArray();return true;};
      fs.SaveMetadata=(_,lines)=>{stored=lines.ToArray();return true;};
      Check(viewport.Save.CanExecute(fs),"Interrupted stroke must be saveable.");viewport.Save.Execute(fs);
      Check(viewport.ChangeHistory.IsSaved&&before.SequenceEqual(saved),"Save must record the finished stroke and its history boundary.");
      var loaded=new PokemonModel(saved,new StoredMetadata(stored),viewport.Singletons);
      Check(before.SequenceEqual(loaded.RawData)&&metadata.SequenceEqual(loaded.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Independent reload preserves every byte and metadata.");
   }
   mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=-1;Layout();
   Click(Cell(12,8));
   Observe(scenario+"-next-click",Value(12,8)==1&&!mapper.DrawMultipleTiles,$"value={Value(12,8):X4},brush={mapper.DrawBlockIndex},multiple={mapper.DrawMultipleTiles}");
   if(scenario=="paint-capture") {
      var second=model.RawData.ToArray();mapper.Undo.Execute(null);Layout();
      Check(before.SequenceEqual(model.RawData)&&viewport.ChangeHistory.IsSaved,"Undo the next stroke restores the exact saved interrupted stroke.");
      mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData)&&!viewport.ChangeHistory.IsSaved&&!mapper.Undo.CanExecute(null),"One further Undo restores original bytes, without empty history entries.");
      mapper.Redo.Execute(null);Layout();Check(before.SequenceEqual(model.RawData)&&viewport.ChangeHistory.IsSaved,"Redo returns to the saved checkpoint.");
      mapper.Redo.Execute(null);Layout();Check(second.SequenceEqual(model.RawData)&&!viewport.ChangeHistory.IsSaved,"Redo the second stroke restores exact subsequent bytes.");
   }
} else if(scenario=="picker-capture") {
   var blocks=view.FindControl<PixelImage>("BlockRenderImage")!;
   Avalonia.Point Block(int id) {var scale=((IPixelViewModel)blocks.DataContext!).SpriteScale;return blocks.TranslatePoint(new Avalonia.Point((id*16+8)*scale,8*scale),window)!.Value;}
   Down(Block(1));LoseCapture();Up(offMap);Click(Block(0));
   Observe("picker-capture-next-click",mapper.DrawBlockIndex==0&&!mapper.DrawMultipleTiles,$"brush={mapper.DrawBlockIndex},multiple={mapper.DrawMultipleTiles}");
} else if(scenario=="tab-stroke") {
   Down(Cell(6,6));
   editor.SelectedIndex=0;Layout();Up(offMap);editor.SelectedIndex=1;Layout();view=window.GetVisualDescendants().OfType<MapTab>().Single();
   var before=model.RawData.ToArray();window.MouseMove(Cell(10,8));Layout();
   Observe(scenario+"-no-ghost",before.SequenceEqual(model.RawData),$"bytesChanged={!before.SequenceEqual(model.RawData)}");
   Click(Cell(12,8));Observe(scenario+"-next-click",Value(12,8)==0x0001,$"value={Value(12,8):X4}");
   mapper.Undo.Execute(null);Layout();Observe(scenario+"-history-boundary",Value(6,6)==0x4001,$"firstCellAfterUndo={Value(6,6):X4}");
} else if(scenario=="completed-pan" || scenario=="completed-select") {
   var p=Cell(6,6);Down(p,scenario=="completed-pan"?RawInputModifiers.Alt:RawInputModifiers.Control);
   window.MouseMove(p+new Avalonia.Vector(16,16),RawInputModifiers.LeftMouseButton);Up(p+new Avalonia.Vector(16,16));
   Observe(scenario+"-no-write",baseline.SequenceEqual(model.RawData),"completed gesture with modifier released before pointer");
   mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=-1;Layout();Click(Cell(12,8));
   Observe(scenario+"-paint",Value(12,8)==1,$"value={Value(12,8):X4}");
 } else if(scenario=="rectangle-capture" || scenario=="bag-capture") {
   var command=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
   if(scenario=="rectangle-capture") mapper.CollisionIndex=0; // Keep packed-brush behavior explicit; preserve-mode rectangle packing is a separate finding.
   if(scenario=="bag-capture") {mapper.ToggleBlockInBag(0,0);mapper.ToggleBlockInBag(1,0);mapper.CollisionIndex=1;}
   Down(Cell(6,6),scenario=="rectangle-capture"?command:RawInputModifiers.None);
   window.MouseMove(Cell(8,8),RawInputModifiers.LeftMouseButton|(scenario=="rectangle-capture"?command:RawInputModifiers.None));Layout();
   var applied=model.RawData.ToArray();
   if(scenario=="rectangle-capture") {
      var expected=baseline.ToArray();
      for(int y=6;y<=8;y++)for(int x=6;x<=8;x++) {
         var at=SyntheticMapFixture.Cells+(y*24+x)*2;
         BitConverter.GetBytes((ushort)1).CopyTo(expected,at);
      }
      Check(expected.SequenceEqual(applied),"Rectangle preview must contain exactly the selected packed brush in its region: " + string.Join(",",Enumerable.Range(6,3).SelectMany(y=>Enumerable.Range(6,3).Select(x=>Value(x,y).ToString("X4")))) + " mismatches=" + string.Join(",",Enumerable.Range(0,expected.Length).Where(i=>expected[i]!=applied[i]).Take(20).Select(i=>$"{i:X}:{expected[i]:X2}/{applied[i]:X2}")));
   } else {
      Check(Value(6,6)>>10==1 && Value(8,8)>>10==1,"Bag stamping must set explicit permissions independently of random art.");
      Check(new[]{0,1}.Contains(Value(6,6)&0x3FF)&&new[]{0,1}.Contains(Value(8,8)&0x3FF),"Random art must belong to the bag.");
   }
   LoseCapture();Up(offMap);window.MouseMove(Cell(10,10));Layout();
   Check(applied.SequenceEqual(model.RawData),"Loss must preserve the last applied edit and prevent ghost drawing.");
   mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData),"One Undo restores all bytes before the interrupted edit.");
   Check(!mapper.Undo.CanExecute(null),"Repeated completion must not add empty Undo steps.");
   mapper.Redo.Execute(null);Layout();Check(applied.SequenceEqual(model.RawData),"Redo restores exact rectangle/random bytes without rerolling.");
   Click(Cell(12,8));mapper.Undo.Execute(null);Layout();
   Check(applied.SequenceEqual(model.RawData),"Next independent stroke has its own history entry.");
} else if(scenario=="context-stroke") {
   Down(Cell(6,6));var applied=model.RawData.ToArray();
   var otherModel=new PokemonModel(baseline.ToArray(),new StoredMetadata(metadata),viewport.Singletons);
   var other=new ViewPort("other-context.bin",otherModel,InstantDispatch.Instance,viewport.Singletons,tutorials:editor.MapTutorials,fs:fs);
   other.InitializationWorkload.GetAwaiter().GetResult();
   var replacement=other.MapEditor;_ = replacement.PrimaryMap.PixelData;
   var otherBytes=otherModel.RawData.ToArray();
   view.DataContext=replacement;Layout();Up(offMap);
   Check(applied.SequenceEqual(model.RawData)&&otherBytes.SequenceEqual(otherModel.RawData),"Changing context must not apply an old pointer edit to either document.");
   Check(mapper.Undo.CanExecute(null)&&!other.Undo.CanExecute(null),"Only the old owning model receives the finished Undo entry.");
   view.DataContext=mapper;Layout();Click(Cell(12,8));mapper.Undo.Execute(null);Layout();
   Check(applied.SequenceEqual(model.RawData),"Return to old context starts an independent edit.");
} else if(scenario=="release-endpoint") {
   Down(Cell(6,6),RawInputModifiers.Control);Up(Cell(8,8));
   Check(mapper.DrawMultipleTiles&&mapper.MultiTileDrawRender.PixelWidth==48&&mapper.MultiTileDrawRender.PixelHeight==48,"Normal selection must use the release endpoint even without a final Move event.");
   Check(baseline.SequenceEqual(model.RawData)&&!mapper.Undo.CanExecute(null),"Selection only changes brush state.");
   Click(Cell(12,8));var expected=baseline.ToArray();
   for(int y=0;y<3;y++)for(int x=0;x<3;x++)Array.Copy(baseline,SyntheticMapFixture.Cells+((y+6)*24+x+6)*2,expected,SyntheticMapFixture.Cells+((y+8)*24+x+12)*2,2);
   Check(expected.SequenceEqual(model.RawData),"Release-based selection stamps the exact packed source cells.");
} else if(scenario=="unrelated-release") {
   Down(Cell(6,6));window.MouseDown(Cell(6,6),MouseButton.Right,RawInputModifiers.LeftMouseButton);window.MouseUp(Cell(6,6),MouseButton.Right,RawInputModifiers.LeftMouseButton);Layout();
   Check((pointer.Captured as Control)?.Name=="MapView","Releasing another button must not end the left stroke.");
   window.MouseMove(Cell(7,6),RawInputModifiers.LeftMouseButton);Up(Cell(7,6));
   Check(Value(6,6)==0x4001&&Value(7,6)==0x4001,"The original physical button continues drawing.");
   mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData),"The uninterrupted stroke remains one Undo entry.");
} else if(scenario=="event-capture") {
   Down(Cell(0,0));window.MouseMove(Cell(2,3),RawInputModifiers.LeftMouseButton);Layout();
   Check(model.ReadMultiByteValue(0x16000,2)==2&&model.ReadMultiByteValue(0x16002,2)==3,"The actual Script Event must move before interruption.");
   var applied=model.RawData.ToArray();LoseCapture();Up(offMap);window.MouseMove(Cell(10,10));Layout();
   Check(applied.SequenceEqual(model.RawData),"Event must remain at its last valid position after capture loss.");
   mapper.SelectedEvent=null;mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=-1;Layout();
   Click(Cell(12,8));Check(Value(12,8)==1,$"A fresh brush must paint after event interruption: value={Value(12,8):X4},brush={mapper.DrawBlockIndex}, changed="+string.Join(",",Enumerable.Range(0,applied.Length).Where(i=>applied[i]!=model.RawData[i]).Take(20).Select(i=>$"{i:X}:{applied[i]:X2}/{model.RawData[i]:X2}")));mapper.Undo.Execute(null);Layout();Check(applied.SequenceEqual(model.RawData),"Later painting must not merge with the event move.");
   mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData),"Undo the interrupted event move restores all original bytes.");
 } else if(scenario=="navigation-stroke") {
   Down(Cell(6,6));var applied=model.RawData.ToArray();mapper.NavigateTo(0,0,6,6);Layout();
   Check(pointer.Captured==null,"Navigation must release the old gesture before installing the new map.");
   window.MouseMove(Cell(10,8));Layout();Up(offMap);
   Check(applied.SequenceEqual(model.RawData),"Navigation must prevent continuation of an old stroke on the new map.");
   var navigationMetadata=model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
   Click(Cell(12,8));mapper.Undo.Execute(null);Layout();Check(applied.SequenceEqual(model.RawData),"Paint after navigation has an independent Undo entry.");
   Check(navigationMetadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"The subsequent stroke/undo preserves the post-navigation metadata baseline.");
} else if(scenario=="pending-template" || scenario=="pending-wave") {
   var icon=view.FindControl<Control>(scenario=="pending-wave"?"WaveFunction":"ObjectEventTemplate")!;
   Check(!mapper.ShowTemplateSettings,"Pending template starts without a settings popup.");
   Down(icon.TranslatePoint(new Avalonia.Point(12,12),window)!.Value);LoseCapture();Up(offMap);
   window.MouseMove(Cell(10,8));Layout();
   Check(baseline.SequenceEqual(model.RawData)&&!mapper.Undo.CanExecute(null),"Abandoning a pending template creates no event or history.");
   Check(!mapper.ShowTemplateSettings,"Interruption must not behave like clicking to open template settings.");
   Click(Cell(12,8));Check(Value(12,8)==1,"Painting resumes after abandoning a template.");
 } else if(scenario.StartsWith("escape-") || scenario=="cancel-api") {
   var command=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
   if(scenario=="escape-picker") {
      var blocks=view.FindControl<PixelImage>("BlockRenderImage")!;var scale=((IPixelViewModel)blocks.DataContext!).SpriteScale;
      Down(blocks.TranslatePoint(new Avalonia.Point(24*scale,8*scale),window)!.Value);
   } else if(scenario=="escape-template" || scenario=="escape-wave") {
      var icon=view.FindControl<Control>(scenario=="escape-wave"?"WaveFunction":"ObjectEventTemplate")!;
      Down(icon.TranslatePoint(new Avalonia.Point(12,12),window)!.Value);
   } else if(scenario=="escape-event") {
      Down(Cell(0,0));window.MouseMove(Cell(2,3),RawInputModifiers.LeftMouseButton);Layout();
      Check(model.ReadMultiByteValue(0x16000,2)==2,"Event must move before Escape.");
   } else {
      var modifier=scenario=="escape-pan"?RawInputModifiers.Alt:scenario=="escape-select"?RawInputModifiers.Control:scenario=="escape-rectangle"?command:RawInputModifiers.None;
      if(scenario=="escape-rectangle") mapper.CollisionIndex=0;
      Down(Cell(6,6),modifier);
      if(scenario=="escape-rectangle") {window.MouseMove(Cell(8,8),command|RawInputModifiers.LeftMouseButton);Layout();}
   }
   Check(pointer.Captured!=null,"Cancellation starts with a captured gesture.");
   var applied=model.RawData.ToArray();
   if(scenario=="cancel-api") mapper.Cancel();
   else {
      Check(view.Focus(),"Escape must be routed through the map's real key binding.");
      window.KeyPressQwerty(PhysicalKey.Escape,RawInputModifiers.LeftMouseButton);window.KeyReleaseQwerty(PhysicalKey.Escape,RawInputModifiers.LeftMouseButton);
   }
   Layout();
   Check(mapper.DrawBlockIndex==-1&&mapper.CollisionIndex==-1,"Cancel clears the brush after completing the operation.");
   Check(pointer.Captured==null,"Cancel releases the owning pointer and UI gesture.");
   window.MouseMove(Cell(10,8),RawInputModifiers.LeftMouseButton);Layout();Up(Cell(10,8));
   Check(applied.SequenceEqual(model.RawData),"Held movement and release after Escape must neither paint, swap nor roll back applied edits.");
   Check(!mapper.ShowTemplateSettings,"Cancel does not open pending template settings.");
   mapper.CompleteInteraction();mapper.CompleteInteraction();Layout();
   if(!applied.SequenceEqual(baseline)) {
      Check(mapper.Undo.CanExecute(null),"Valid edits before Escape remain undoable.");mapper.Undo.Execute(null);Layout();
      Check(baseline.SequenceEqual(model.RawData)&&!mapper.Undo.CanExecute(null),"Exactly one Undo restores the entire pre-cancel model.");
      mapper.Redo.Execute(null);Layout();Check(applied.SequenceEqual(model.RawData),"Redo restores the exact valid canceled-stroke result.");
   } else Check(!mapper.Undo.CanExecute(null),"Cancel of a non-writing gesture creates no history.");
   mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=0;Layout();
   Down(Cell(12,8),scenario=="escape-rectangle"?command:RawInputModifiers.None);
   if(scenario=="escape-rectangle") {window.MouseMove(Cell(14,10),command|RawInputModifiers.LeftMouseButton);Layout();}
   Up(scenario=="escape-rectangle"?Cell(14,10):Cell(12,8));
   Check(Value(12,8)==1,"A new stroke works after Cancel.");mapper.Undo.Execute(null);Layout();
   Check(applied.SequenceEqual(model.RawData),"The next edit has an independent history boundary and no stale rectangle backup.");
 } else if(scenario=="rectangle-permissions") {
   var command=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
   foreach(var bagSize in new[]{0,1,2})foreach(var permission in new[]{-1,0,1,62}) {
      mapper.ClearBlockBag();
      if(bagSize>0)mapper.ToggleBlockInBag(1,0);
      if(bagSize>1)mapper.ToggleBlockInBag(0,0);
      mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=permission;Layout();
      Down(Cell(6,6),command);
      window.MouseMove(Cell(9,9),command|RawInputModifiers.LeftMouseButton);Layout();
      window.MouseMove(Cell(8,8),command|RawInputModifiers.LeftMouseButton);Layout();Up(Cell(8,8));
      var painted=model.RawData.ToArray();var expected=baseline.ToArray();
      for(int y=6;y<=8;y++)for(int x=6;x<=8;x++) {
         var at=SyntheticMapFixture.Cells+(y*24+x)*2;var actual=Value(x,y);var old=BitConverter.ToUInt16(baseline,at);
         Check((actual>>10)==(permission<0?old>>10:permission),$"Rectangle bag={bagSize}, permission={permission} at {x},{y}: actual={actual:X4}, original={old:X4}.");
         Check(bagSize<2?(actual&0x3FF)==1:new[]{0,1}.Contains(actual&0x3FF),"Rectangle art must match the brush or belong to the random bag.");
         BitConverter.GetBytes((ushort)actual).CopyTo(expected,at);
      }
      Check(expected.SequenceEqual(painted),"Shrinking a rectangle restores all extra cells and preserves every unrelated byte/pointer.");
      if(bagSize==0&&permission==-1) {
         byte[] saved=null;string[] stored=null;
         fs.Save=file=>{saved=file.Contents.ToArray();return true;};fs.SaveMetadata=(_,lines)=>{stored=lines.ToArray();return true;};
         viewport.Save.Execute(fs);Check(viewport.ChangeHistory.IsSaved&&painted.SequenceEqual(saved),"Save records the exact preserved-permission rectangle.");
         var loaded=new PokemonModel(saved,new StoredMetadata(stored),viewport.Singletons);
         Check(painted.SequenceEqual(loaded.RawData)&&metadata.SequenceEqual(loaded.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Saved rectangle reloads with identical bytes/metadata.");
      }
      mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData),"One Undo restores all original rectangle bytes.");
      mapper.Redo.Execute(null);Layout();Check(painted.SequenceEqual(model.RawData),"Redo restores the exact rectangle/random outcome.");
      mapper.Undo.Execute(null);Layout();Check(!mapper.Undo.CanExecute(null),"Each rectangle is one independent history entry.");
   }
   mapper.ClearBlockBag();
   // Copied multi-cell patterns retain their packed source permissions when repeated.
   Down(Cell(2,2),RawInputModifiers.Control);window.MouseMove(Cell(3,4),RawInputModifiers.Control|RawInputModifiers.LeftMouseButton);Up(Cell(3,4));
   Check(mapper.DrawMultipleTiles,"Copied multi-cell brush must be active.");
   Down(Cell(12,8),command);window.MouseMove(Cell(15,13),command|RawInputModifiers.LeftMouseButton);Up(Cell(15,13));
   var copied=baseline.ToArray();
   for(int y=0;y<6;y++)for(int x=0;x<4;x++)Array.Copy(baseline,SyntheticMapFixture.Cells+(((y%3)+2)*24+(x%2)+2)*2,copied,SyntheticMapFixture.Cells+((y+8)*24+x+12)*2,2);
   Check(copied.SequenceEqual(model.RawData),"Copied rectangles retain exact packed source patterns.");
   mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData),"Copied rectangle is independently undoable.");
   mapper.Cancel();mapper.CollisionIndex=1;Layout();
   Down(Cell(6,6),command);window.MouseMove(Cell(8,8),command|RawInputModifiers.LeftMouseButton);Up(Cell(8,8));
   var permissionsOnly=baseline.ToArray();
   for(int y=6;y<=8;y++)for(int x=6;x<=8;x++) {
      var at=SyntheticMapFixture.Cells+(y*24+x)*2;
      BitConverter.GetBytes((ushort)((BitConverter.ToUInt16(baseline,at)&0x3FF)|0x400)).CopyTo(permissionsOnly,at);
   }
   Check(permissionsOnly.SequenceEqual(model.RawData),"Permission-only rectangle preserves every complete ten-bit block ID.");
   mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData),"Permission-only rectangle restores exactly.");
 } else if(scenario=="same-cell-history") {
   Click(Cell(6,6));Check(Value(6,6)==0x4001,"Initial same-cell control must paint.");
   mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData),"Undo must restore all original bytes.");
   Click(Cell(6,6));Check(Value(6,6)==0x4001,"A fresh click at the same position after Undo must paint again.");
   var first=model.RawData.ToArray();
   mapper.Undo.Execute(null);Layout();Check(baseline.SequenceEqual(model.RawData)&&!mapper.Undo.CanExecute(null),"Fresh repaint is one independent Undo step.");
   mapper.Redo.Execute(null);Layout();Check(first.SequenceEqual(model.RawData),"Redo restores the exact same-cell repaint.");
   byte[] saved=null;string[] stored=null;
   fs.Save=file=>{saved=file.Contents.ToArray();return true;};fs.SaveMetadata=(_,lines)=>{stored=lines.ToArray();return true;};
   viewport.Save.Execute(fs);Check(viewport.ChangeHistory.IsSaved&&first.SequenceEqual(saved),"Save records the repainted checkpoint.");
   mapper.CollisionIndex=1;Click(Cell(6,6));Check(Value(6,6)==0x0401,"Same-cell permission changes after Redo must apply.");
   var second=model.RawData.ToArray();
   mapper.Undo.Execute(null);Layout();Check(first.SequenceEqual(model.RawData)&&viewport.ChangeHistory.IsSaved,"Undo the permission change returns to the exact saved checkpoint.");
   mapper.Redo.Execute(null);Layout();Check(second.SequenceEqual(model.RawData)&&!viewport.ChangeHistory.IsSaved,"Redo retains exact permissions and dirty state.");
   mapper.SelectBlock(0,0);mapper.ReleaseBlock(0,0);mapper.CollisionIndex=-1;Layout();Click(Cell(6,6));
   Check(Value(6,6)==0x0400,"A new brush can replace art at the same position while preserving permissions.");
   mapper.Undo.Execute(null);Layout();Check(second.SequenceEqual(model.RawData),"Brush replacement has a separate exact Undo entry.");
   mapper.ToggleBlockInBag(0,0);mapper.ToggleBlockInBag(1,0);mapper.SelectBlock(1,0);mapper.ReleaseBlock(1,0);mapper.CollisionIndex=2;Layout();
   Down(Cell(7,6));var once=model.RawData.ToArray();
   for(int i=0;i<10;i++) {window.MouseMove(Cell(7,6),RawInputModifiers.LeftMouseButton);Layout();}
   Check(once.SequenceEqual(model.RawData),"Repeated movement at one position within the same stroke must not reroll the random bag.");
   Up(Cell(7,6));mapper.Undo.Execute(null);Layout();Check(second.SequenceEqual(model.RawData),"Bag stroke remains a separate history entry.");
   mapper.Redo.Execute(null);Layout();Check(once.SequenceEqual(model.RawData),"Bag Redo restores the exact original random result.");
   var reloaded=new PokemonModel(saved,new StoredMetadata(stored),viewport.Singletons);
   Check(first.SequenceEqual(reloaded.RawData)&&metadata.SequenceEqual(reloaded.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Same-cell edits preserve independent saved bytes and metadata.");
   // Direct callers must also be able to draw the very first cell at (0,0).
   var direct=new SyntheticMapFixture(7,5);_ = direct.Map.PixelData;
   direct.Map.DrawBlock(direct.ViewPort.ChangeHistory.CurrentChange,1,-1,0,0);
   Check(direct.Model.ReadMultiByteValue(SyntheticMapFixture.Cells,2)==1,"A map's initial coordinate cache must not suppress its first top-left draw.");
 } else if(scenario.StartsWith("reveal-")) {
   Check(view.Focus(),"Reveal must start with map keyboard focus.");
   window.KeyPressQwerty(PhysicalKey.Space,RawInputModifiers.None);Layout();
   Check(mapper.ShowBeneath&&mapper.PrimaryMap.ShowBeneath,"Space reveals the current map.");
   if(scenario=="reveal-focus") {
      var field=window.GetVisualDescendants().OfType<TextBox>().First(t=>t.IsEffectivelyVisible&&!t.GetVisualAncestors().Contains(view));
      Check(field.Focus()&&!view.IsKeyboardFocusWithin,"Focus must actually move to a toolbar field.");Layout();
      Check(!mapper.ShowBeneath,"Map reveal must clear as soon as the field takes focus.");
      window.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None);Layout();
      var original=field.Text??"";field.CaretIndex=original.Length;field.SelectionStart=field.SelectionEnd=original.Length;
      window.KeyPressQwerty(PhysicalKey.Space,RawInputModifiers.None);window.KeyTextInput(" ");window.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None);Layout();
      Check(field.Text==original+" "&&!mapper.ShowBeneath,"Space remains native text input and does not reveal the map while a field owns focus.");
   } else if(scenario=="reveal-tab") {
      editor.SelectedIndex=0;Layout();Check(!mapper.ShowBeneath,"Detaching the map tab clears reveal before key release.");
      window.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None);editor.SelectedIndex=1;Layout();view=window.GetVisualDescendants().OfType<MapTab>().Single();
   } else if(scenario=="reveal-context") {
      var otherModel=new PokemonModel(baseline.ToArray(),new StoredMetadata(metadata),viewport.Singletons);
      var other=new ViewPort("reveal-context.bin",otherModel,InstantDispatch.Instance,viewport.Singletons,tutorials:editor.MapTutorials,fs:fs);
      other.InitializationWorkload.GetAwaiter().GetResult();var replacement=other.MapEditor;_ = replacement.PrimaryMap.PixelData;
      var otherBytes=otherModel.RawData.ToArray();view.DataContext=replacement;Layout();
      Check(!mapper.ShowBeneath&&!replacement.ShowBeneath,"Context replacement clears the old reveal state without inheriting it.");
      window.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None);view.DataContext=mapper;Layout();
      Check(otherBytes.SequenceEqual(otherModel.RawData)&&!other.Undo.CanExecute(null),"Reveal context changes preserve the independent document.");
   } else if(scenario=="reveal-navigation") {
      mapper.NavigateTo(0,0,6,6);Layout();
      Check(!mapper.ShowBeneath&&!mapper.PrimaryMap.ShowBeneath,"Explicit navigation must not carry a held reveal onto the replacement map.");
      window.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None);
   } else {window.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None);Layout();}
   Check(!mapper.ShowBeneath&&!mapper.PrimaryMap.ShowBeneath,"Both view model and rendered map end reveal mode.");
   Check(view.Focus(),"The map must accept focus again after interruption.");
   window.KeyPressQwerty(PhysicalKey.Space,RawInputModifiers.None);Layout();Check(mapper.ShowBeneath,"Reveal can be used again after cleanup.");
   window.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None);Layout();Check(!mapper.ShowBeneath,"Normal key release still clears reveal.");
   var modifier=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
   var modifierKey=OperatingSystem.IsMacOS()?PhysicalKey.MetaLeft:PhysicalKey.ControlLeft;
   window.KeyPressQwerty(modifierKey,modifier);Layout();Check(mapper.HideEvents,"Command/control still hides events.");
   window.KeyReleaseQwerty(modifierKey,RawInputModifiers.None);Layout();Check(!mapper.HideEvents,"Modifier release still restores event visibility.");
   Check(baseline.SequenceEqual(model.RawData),"Reveal and focus cleanup must not write ROM bytes.");
} else throw new ArgumentException("Unknown scenario");
if(scenario!="navigation-stroke"&&scenario!="reveal-navigation") Check(metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Audit must preserve metadata.");
else Console.WriteLine("NAVIGATION_METADATA_EQUAL_TO_START:"+metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()));
Console.WriteLine($"CHILD_CHECKS:{checks}");
window.Editor=null;window.Close();
