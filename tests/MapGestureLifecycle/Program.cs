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
   foreach (var name in new[] { "paint-capture", "pan-capture", "select-capture", "picker-capture", "background-capture", "tab-stroke", "completed-pan", "completed-select", "rectangle-capture", "bag-capture", "context-stroke", "release-endpoint", "unrelated-release", "event-capture", "pending-template", "navigation-stroke", "pending-wave" }) {
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
var fixture=new SyntheticMapFixture(24,20,withScriptEvent: args[0]=="event-capture");var model=fixture.Model;
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
} else throw new ArgumentException("Unknown scenario");
if(scenario!="navigation-stroke") Check(metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Audit must preserve metadata.");
else Console.WriteLine("NAVIGATION_METADATA_EQUAL_TO_START:"+metadata.SequenceEqual(model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()));
Console.WriteLine($"CHILD_CHECKS:{checks}");
window.Editor=null;window.Close();
