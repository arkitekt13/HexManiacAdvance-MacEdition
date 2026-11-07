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
var fixture=new SyntheticMapFixture(24,20,withScriptEvent:true,withNpcText:true,withSignpost:true);var model=fixture.Model;
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
model.WritePointer(new ModelDelta(),0x1B008,0x18000);model.ObserveRunWritten(new NoDataChangeDeltaModel(),new XSERun(0x18000,SortedSpan<int>.None));
var sign=mapper.PrimaryMap.EventGroup.Signposts.Single();mapper.SelectedEvent=sign;Layout();
var arrow=view.GetVisualDescendants().OfType<AngleButton>().Single(b=>ReferenceEquals(b.DataContext,sign)&&b.Name=="SignpostScriptArrow");
Check(arrow.IsEffectivelyVisible,"The real signpost script arrow is visible.");
var bytes=model.RawData.ToArray();string[] Metadata()=>model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();var metadata=Metadata();
void Click(){var at=arrow.TranslatePoint(new Avalonia.Point(arrow.Bounds.Width/2,arrow.Bounds.Height/2),window).Value;window.MouseMove(at);window.MouseDown(at,MouseButton.Left);window.MouseUp(at,MouseButton.Left);for(int i=0;i<5;i++)Layout();}
Click();
Check(editor.SelectedTab is ViewPort,"Signpost arrow opens a script tab.");var selected=(ViewPort)editor.SelectedTab;
var tab=window.GetVisualDescendants().OfType<TabView>().Single(t=>ReferenceEquals(t.DataContext,selected));
var input=tab.GetVisualDescendants().OfType<TextEditor>().First(e=>ReferenceEquals(e.DataContext,selected.Tools.CodeTool.Contents[0].Editor)).EditBox;
Check(selected.Tools.CodeTool.Contents[0].Address==0x18000&&input.IsFocused,$"The script address arrow puts typing focus directly into its script source: address={selected.Tools.CodeTool.Contents[0].Address:X}, focused={input.IsFocused}, mode={selected.Tools.CodeTool.Mode}, tool={selected.Tools.SelectedIndex}, goto={editor.GotoViewModel.ControlVisible}.");
Check(input.SelectedText==input.Text,"First arrow entry selects all source for replacement.");
Check(bytes.SequenceEqual(model.RawData)&&metadata.SequenceEqual(Metadata()),"Opening and focusing sign source cannot edit ROM bytes or metadata.");
input.CaretIndex=3;input.SelectionStart=2;input.SelectionEnd=3;
editor.SelectedIndex=Enumerable.Range(0,editor.Count).Single(i=>ReferenceEquals(editor[i],mapper));Layout();view=window.GetVisualDescendants().OfType<MapTab>().Single();mapper.SelectedEvent=mapper.PrimaryMap.EventGroup.Signposts.Single();Layout();sign=(SignpostEventViewModel)mapper.SelectedEvent;
arrow=view.GetVisualDescendants().OfType<AngleButton>().Single(b=>ReferenceEquals(b.DataContext,sign)&&b.Name=="SignpostScriptArrow");
Click();tab=window.GetVisualDescendants().OfType<TabView>().Single(t=>ReferenceEquals(t.DataContext,editor.SelectedTab));input=tab.GetVisualDescendants().OfType<TextEditor>().First(e=>ReferenceEquals(e.DataContext,selected.Tools.CodeTool.Contents[0].Editor)).EditBox;
Check(ReferenceEquals(editor.SelectedTab,selected)&&input.IsFocused,"Opening the same sign again reuses and focuses its existing script tab.");
Check(input.SelectedText==input.Text,"Entering by the signpost arrow selects all current script source for replacement.");
var point=input.TranslatePoint(new Avalonia.Point(30,12),window).Value;window.MouseMove(point);window.MouseDown(point,MouseButton.Left);window.MouseUp(point,MouseButton.Left);Layout();
Check(input.IsFocused&&input.SelectionStart==input.SelectionEnd,"Clicking inside the source clears select-all and places the caret normally.");
window.Editor=null;window.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
