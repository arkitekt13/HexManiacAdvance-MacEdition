using Avalonia;
using Pointer = HavenSoft.HexManiac.Core.ViewModels.DataFormats.Pointer;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks=0;
void Check(bool ok,string message) { checks++;if(!ok)throw new InvalidOperationException(message); }
var fixture=new SyntheticMapFixture(24,20,withScriptEvent:true,withNpcText:true,withSignpost:true,withMapScripts:true);var model=fixture.Model;
model.SetList(new NoDataChangeDeltaModel(),"MapAttributeBehaviors","Normal");
model.SetList(new NoDataChangeDeltaModel(),"FacingOptions","NoMovement");
var token=new ModelDelta();
foreach(var at in new[]{0x104,0x108,0x10C})model.WritePointer(token,at,0x110);
var format=model.GetNextRun(0x100).FormatString;
var error=ArrayRun.TryParse(model,format[..^1]+"4",0x100,SortedSpan<int>.None,out var root);
Check(!error.HasError,"Synthetic map banks must parse.");
model.ObserveAnchorWritten(new NoDataChangeDeltaModel(),"data.maps.banks",root);
((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new("synthetic.layout","data.maps.banks/3/maps/0/map/0/layout/","Maps"));
var fs=new StubFileSystem{MetadataFor=_=>Array.Empty<string>()};
Application.Current!.Resources["FileSystem"]=fs;
var editor=new EditorViewModel(fs,InstantDispatch.Instance);
App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
var viewport=new ViewPort("sprite-selection-fixture.bin",model,InstantDispatch.Instance,new Singletons(),tutorials:editor.MapTutorials,fs:fs);
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
editor.GotoViewModel.ControlVisible=false;
var view=window.GetVisualDescendants().OfType<MapTab>().Single();
model[0x18800]=2;model.WritePointer(new ModelDelta(),0x1600C,0x18800);model.ObserveRunWritten(new NoDataChangeDeltaModel(),new XSERun(0x18800,SortedSpan<int>.None));
bool Command(AngleButton b,string name)=>b.Command is HavenSoft.HexManiac.Core.MethodCommand c&&c.ExecuteName==name;
void Open(AngleButton button,int address,string label){
 var bytes=model.RawData.ToArray();var at=button.TranslatePoint(new Avalonia.Point(button.Bounds.Width/2,button.Bounds.Height/2),window).Value;
 window.MouseMove(at);window.MouseDown(at,MouseButton.Left);window.MouseUp(at,MouseButton.Left);for(int i=0;i<5;i++)Layout();
 Check(editor.SelectedTab is ViewPort,"A "+label+" arrow opens a data tab.");var source=(ViewPort)editor.SelectedTab;
 Check(source.Tools.CodeTool.Mode==CodeMode.Script&&source.Tools.CodeTool.Contents.Any(body=>body.Address==address),label+" opens the intended script address.");
 var tab=window.GetVisualDescendants().OfType<TabView>().Single(t=>ReferenceEquals(t.DataContext,source));var input=tab.GetVisualDescendants().OfType<TextEditor>().First(e=>e.Tag is CodeBody body&&body.Address==address).EditBox;
 Check(input.IsFocused,label+" puts typing focus in source rather than hex/address controls.");
 Check(input.SelectionStart==input.SelectionEnd,label+" does not select all source automatically.");
 Check(bytes.SequenceEqual(model.RawData),label+" navigation cannot modify ROM bytes.");
}
void Map(){editor.SelectedIndex=Enumerable.Range(0,editor.Count).Single(i=>ReferenceEquals(editor[i],mapper));for(int i=0;i<3;i++)Layout();view=window.GetVisualDescendants().OfType<MapTab>().Single();}
mapper.SelectedEvent=mapper.PrimaryMap.EventGroup.Objects.Single();Layout();
Open(view.GetVisualDescendants().OfType<AngleButton>().First(b=>ReferenceEquals(b.DataContext,mapper.SelectedEvent)&&Command(b,"GotoScript")&&Grid.GetRow(b)==8),0x18000,"Object");
Map();mapper.SelectedEvent=mapper.PrimaryMap.EventGroup.Scripts.Single();Layout();
var green=view.GetVisualDescendants().OfType<AngleButton>().Single(b=>ReferenceEquals(b.DataContext,mapper.SelectedEvent)&&Command(b,"GotoScript"));
Open(green,0x18800,"Green event");
Map();var eventModel=mapper.PrimaryMap.EventGroup.Scripts.Single();mapper.SelectedEvent=eventModel;Layout();
green=view.GetVisualDescendants().OfType<AngleButton>().Single(b=>ReferenceEquals(b.DataContext,eventModel)&&Command(b,"GotoScript"));
eventModel.ScriptAddress=HavenSoft.HexManiac.Core.ViewModels.DataFormats.Pointer.NULL;Layout();Check(!green.IsEffectivelyEnabled,"A null green-event script disables its navigation arrow.");
eventModel.ScriptAddress=0x18800;Layout();Check(green.IsEffectivelyEnabled,"Assigning a valid green script re-enables the same arrow.");
eventModel.ScriptAddressText="<F00000>";Layout();Check(!green.IsEffectivelyEnabled,"An out-of-ROM green-event address disables navigation.");eventModel.ScriptAddressText="<018800>";Layout();Check(green.IsEffectivelyEnabled,"Correcting a green address restores navigation.");
Map();mapper.ShowHeaderPanel=true;for(int i=0;i<3;i++)Layout();var parents=mapper.PrimaryMap.MapScriptCollection.Scripts;
Open(view.GetVisualDescendants().OfType<AngleButton>().Single(b=>ReferenceEquals(b.DataContext,parents[0])&&Command(b,"Goto")),0x18A00,"Regular map-header");
Map();mapper.ShowHeaderPanel=true;for(int i=0;i<3;i++)Layout();parents=mapper.PrimaryMap.MapScriptCollection.Scripts;
var parent=view.GetVisualDescendants().OfType<AngleButton>().Single(b=>ReferenceEquals(b.DataContext,parents[1])&&Command(b,"Goto"));var p=parent.TranslatePoint(new Avalonia.Point(parent.Bounds.Width/2,parent.Bounds.Height/2),window).Value;
window.MouseMove(p);window.MouseDown(p,MouseButton.Left);window.MouseUp(p,MouseButton.Left);for(int i=0;i<4;i++)Layout();
Check(editor.SelectedTab is ViewPort table&&table.Tools.SelectedTool==table.Tools.TableTool,"A conditional parent arrow still opens its condition table, not a script editor.");
Map();mapper.ShowHeaderPanel=true;for(int i=0;i<3;i++)Layout();var child=mapper.PrimaryMap.MapScriptCollection.Scripts[1].SubScripts[0];
Open(view.GetVisualDescendants().OfType<AngleButton>().Single(b=>ReferenceEquals(b.DataContext,child)&&Command(b,"Goto")),0x18C00,"Conditional child");
window.Editor=null;window.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
