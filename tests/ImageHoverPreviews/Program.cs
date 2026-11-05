using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using System.Collections.ObjectModel;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException(message);}
var model=new PokemonModel(new byte[0x30000]);var setup=new ModelDelta();
model.WritePointer(setup,0x100,0x2000);model.ObserveRunWritten(setup,new PointerRun(0x100));
model.WritePointer(setup,0x104,0x1800);model.ObserveRunWritten(setup,new PointerRun(0x104));
model.ObserveAnchorWritten(new NoDataChangeDeltaModel(),"audit.sprite",new SpriteRun(model,0x2000,new SpriteFormat(4,16,256,null),new SortedSpan<int>(0x100)));
var fs=new StubFileSystem{MetadataFor=_=>Array.Empty<string>()};Application.Current!.Resources["FileSystem"]=fs;
var editor=new EditorViewModel(fs,InstantDispatch.Instance);App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
var viewport=new ViewPort("synthetic.bin",model,InstantDispatch.Instance,editor.Singletons,fs:fs);viewport.InitializationWorkload.GetAwaiter().GetResult();
var window=new MainWindow{Width=900,Height=600,Editor=editor,FileSystem=fs};window.Show();editor.Add(viewport);viewport.Goto.Execute(0x100);
void Pump(){Dispatcher.UIThread.RunJobs();window.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
Pump();viewport.ChangeHistory.ClearHistory(false);var bytes=model.RawData.ToArray();var metadata=model.ExportMetadata(null,editor.Singletons.MetadataInfo).Serialize();
Control Layer()=>window.FindControl<Control>("ImagePreviewLayer")!;
Viewbox Box()=>Layer().GetVisualDescendants().OfType<Viewbox>().Single();
bool Visible()=>Layer()!=null&&Box().IsVisible;
void Fits(){
 var layer=Layer();var box=Box();var origin=box.TranslatePoint(default,layer)!.Value;
 Check(box.Bounds.Width>0&&box.Bounds.Height>0&&origin.X>=0&&origin.Y>=0&&origin.X+box.Bounds.Width<=layer.Bounds.Width+.1&&origin.Y+box.Bounds.Height<=layer.Bounds.Height+.1,$"Complete preview must fit the window at every edge: origin={origin}, box={box.Bounds}, layer={layer.Bounds}.");
 Check(!layer.IsHitTestVisible&&!box.IsHitTestVisible,"Image previews must never intercept pointer input.");
 foreach(var image in box.GetVisualDescendants().OfType<PixelImage>()) {
   var corner=image.TranslatePoint(new Avalonia.Point(image.Bounds.Width,image.Bounds.Height),layer)!.Value;
   var start=image.TranslatePoint(default,layer)!.Value;
   Check(start.X>=0&&start.Y>=0&&corner.X<=layer.Bounds.Width+.1&&corner.Y<=layer.Bounds.Height+.1,"Fit the entire image, including wide/tall source edges, rather than cropping it inside a small frame.");
 }
}
if(args.FirstOrDefault()!="goto") {
 var hex=window.GetVisualDescendants().OfType<HexContent>().Single(h=>ReferenceEquals(h.ViewPort,viewport));
 Avalonia.Point Cell(int x,int y)=>hex.TranslatePoint(new Avalonia.Point((x+.5)*hex.CellWidth,(y+.5)*hex.CellHeight),window)!.Value;
 int exits=0;hex.PointerExited+=(_,_)=>exits++;
 window.MouseMove(Cell(1,0));Pump();
 Check(Visible()&&!ToolTip.GetIsOpen(hex),"Oversized hex image must stay visible without opening a native tooltip.");
 Fits();Check(exits==0,"Showing the image must not eject the pointer from the hex editor.");
 var image=Box().GetVisualDescendants().OfType<PixelImage>().Single();
 Check(image.Source!=null&&((IPixelViewModel)image.DataContext!).PixelHeight==2048,"Keep all source pixels while fitting the preview.");
 window.MouseMove(new Avalonia.Point(-10,-10));Pump();Check(!Visible(),"Real pointer exit dismisses a hex image.");
 window.MouseMove(Cell(1,0));Pump();Check(Visible(),"Returning to the identical cell must reopen its image.");
 window.MouseMove(Cell(2,0));Pump();Check(Visible(),"Moving within one pointer preserves its preview.");
 window.MouseWheel(Cell(2,0),new Vector(0,-1));Pump();Check(!Visible(),"Scrolling dismisses the old hex image.");
 viewport.Goto.Execute(0x100);Pump();window.MouseMove(Cell(1,0));Pump();Check(Visible(),"The same source remains previewable after scrolling.");
 window.MouseDown(Cell(1,0),MouseButton.Left);window.MouseUp(Cell(1,0),MouseButton.Left);Pump();Check(!Visible(),"Clicking a hex cell dismisses the preview and reaches selection.");
 Check(viewport.IsSelected(new HavenSoft.HexManiac.Core.Models.Point(1,0)),"The image cannot intercept hex selection.");
}
editor.GotoViewModel.ControlVisible=true;Pump();
var section=new GotoLabelSection(Array.Empty<string>(),Array.Empty<string>());
var token=new GotoToken{Content="oversized",IsGoto=true};section.Tokens.Add(token);
editor.GotoViewModel.PrefixSelections.Clear();editor.GotoViewModel.PrefixSelections.Add(section);Pump();
ToggleButton Button()=>window.GetVisualDescendants().OfType<ToggleButton>().Single(b=>ReferenceEquals(b.DataContext,token));
foreach(var (width,height) in new[]{(64,64),(128,2048),(2048,64)}) {
 token.IsSelected=false;token.HoverTip=new ObservableCollection<object>{"Image preview",new ReadonlyPixelViewModel(width,height,new short[width*height])};Pump();
 var button=Button();var point=button.TranslatePoint(new Avalonia.Point(button.Bounds.Width/2,button.Bounds.Height/2),window)!.Value;
 window.MouseMove(new Avalonia.Point(-10,-10));window.MouseMove(point);Pump();
 Check(Visible()&&ToolTip.GetTip(button)==null,"Compiled Go To image tokens use the fitted overlay instead of a native tooltip.");Fits();
 Check(ReferenceEquals(window.InputHitTest(point),button)||window.InputHitTest(point) is Visual hit&&hit.GetVisualAncestors().Contains(button),"The source token must remain the pointer hit target.");
 window.MouseDown(point,MouseButton.Left);window.MouseUp(point,MouseButton.Left);Pump();
 Check(token.IsSelected&&!Visible(),"Mouse selection must pass through small, tall and wide previews.");
 window.MouseMove(new Avalonia.Point(-10,-10));window.MouseMove(point);Pump();Check(Visible(),"Re-entry can reopen the same Go To preview.");
 token.HoverTip=null;Pump();Check(!Visible()&&ToolTip.GetTip(button)==null,"Empty/recycled tokens dismiss old image content.");
}

void HoverToken() {
 var button=Button();var point=button.TranslatePoint(new Avalonia.Point(button.Bounds.Width/2,button.Bounds.Height/2),window)!.Value;
 window.MouseMove(new Avalonia.Point(-10,-10));window.MouseMove(point);Pump();
}
token.HoverTip=new ObservableCollection<object>{"Web Link"};Pump();HoverToken();
Check(!Visible()&&ToolTip.GetTip(Button()) is HexContentToolTip,"Ordinary text tips retain their native tooltip presentation.");
ToolTip.SetIsOpen(Button(),false);
token.HoverTip=new ObservableCollection<object>{new ReadonlyPixelViewModel(128,2048,new short[128*2048])};Pump();HoverToken();Check(Visible(),"Image preview can follow an ordinary text tooltip.");
window.Width=700;window.Height=420;Pump();if(Visible())Fits();else{HoverToken();Fits();}
Check(Visible(),"Image hints remain available and fitted after window resizing.");
window.Width=900;window.Height=600;Pump();HoverToken();
token.HoverTip.Clear();Pump();Check(!Visible(),"Mutating an existing hover collection dismisses its old image.");
token.HoverTip=new ObservableCollection<object>{new ReadonlyPixelViewModel(64,64,new short[4096])};Pump();HoverToken();
var recycled=Button();recycled.DataContext=new GotoToken{Content="replacement"};Pump();Check(!Visible(),"Recycling a token's data context clears its old image.");
recycled.DataContext=token;Pump();HoverToken();Check(Visible(),"Recycled controls can display their current source again.");
editor.GotoViewModel.ControlVisible=false;Pump();Check(!Visible(),"Closing Go To dismisses token images immediately.");
editor.GotoViewModel.ControlVisible=true;Pump();HoverToken();Check(Visible(),"Reopening Go To supports the same token again.");
token.IsSelected=false;Button().Focus();window.KeyPressQwerty(PhysicalKey.Space,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None);Pump();
Check(token.IsSelected&&!Visible(),"Keyboard token selection still works and dismisses the preview.");
HoverToken();var root=window.Content;window.Content=null;Pump();Check(!Visible(),"Detaching window content dismisses all retained hints.");
window.Content=root;Pump();HoverToken();Check(Visible(),"Reattached token controls regain current image previews.");

editor.GotoViewModel.ControlVisible=false;viewport.Goto.Execute(0x100);Pump();
var currentHex=window.GetVisualDescendants().OfType<HexContent>().Single(h=>ReferenceEquals(h.ViewPort,viewport));
Avalonia.Point HexCell(int x)=>currentHex.TranslatePoint(new Avalonia.Point((x+.5)*currentHex.CellWidth,currentHex.CellHeight*.5),window)!.Value;
window.MouseMove(HexCell(5));Pump();Check(!Visible()&&ToolTip.GetIsOpen(currentHex),"Text-only pointer addresses retain their native hex tooltip.");
window.MouseMove(HexCell(1));Pump();Check(Visible()&&!ToolTip.GetIsOpen(currentHex),"Switching from text-only to image content replaces the native hint.");
var otherView=new ViewPort("other.bin",new PokemonModel(new byte[512]),InstantDispatch.Instance,editor.Singletons,fs:fs);
otherView.InitializationWorkload.GetAwaiter().GetResult();editor.Add(otherView);Pump();Check(!Visible(),"Selecting another document dismisses the former document's image.");
editor.SelectedIndex=0;Pump();
window.Close();Check(!Visible(),"Closing the window releases its image preview.");
Check(bytes.SequenceEqual(model.RawData)&&metadata.SequenceEqual(model.ExportMetadata(null,editor.Singletons.MetadataInfo).Serialize())&&!viewport.Undo.CanExecute(null),"All preview and navigation interactions preserve bytes, metadata and edit history.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
