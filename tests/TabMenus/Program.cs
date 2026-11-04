using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var fs=new RevealFileSystem { MetadataFor=_=>Array.Empty<string>(), Exists=_=>true,
   LaunchProcess=(_,_)=>throw new InvalidOperationException("The Mac tab action must not launch explorer.exe.") };
Application.Current!.Resources["FileSystem"]=fs;
var editor=new EditorViewModel(fs,InstantDispatch.Instance);App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
var first=new ViewPort(new LoadedFile("first with spaces.gba",new byte[512]));
var second=new ViewPort(new LoadedFile("second.gba",new byte[512]));
editor.Add(first);editor.Add(second);
var window=new MainWindow { Width=1400, Height=900, FileSystem=fs, Editor=editor };
window.Resources["FileSystem"]=fs;window.Show();
int checks=0;
void Check(bool value,string message){checks++;if(!value)throw new InvalidOperationException(message);}
void Pump(){Dispatcher.UIThread.RunJobs();window.Measure(new Size(1400,900));window.Arrange(new Rect(0,0,1400,900));AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
TextBlock Header(ITabContent tab)=>window.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Name=="TabTextBlock" && ReferenceEquals(t.DataContext,tab));
ContextMenu Menu(ITabContent tab)=>Header(tab).GetVisualAncestors().OfType<Control>().First(c=>c.ContextMenu!=null).ContextMenu!;
Avalonia.Point Point(ITabContent tab){var header=Header(tab);return header.TranslatePoint(new Avalonia.Point(header.Bounds.Width/2,header.Bounds.Height/2),window)!.Value;}
void OpenMenu(ITabContent tab){var p=Point(tab);window.MouseDown(p,MouseButton.Right);window.MouseUp(p,MouseButton.Right);Pump();}
void Click(MenuItem item){var root=TopLevel.GetTopLevel(item)!;var p=item.TranslatePoint(new Avalonia.Point(item.Bounds.Width/2,item.Bounds.Height/2),root)!.Value;root.MouseMove(p);root.MouseDown(p,MouseButton.Left);Pump();root.MouseUp(p,MouseButton.Left);Pump();}
Pump();
// Optional integrations remain compiled but have no normal UI entry point for this release.
IEnumerable<NativeMenuItem> NativeItems(NativeMenu menu) => menu.Items.OfType<NativeMenuItem>()
   .SelectMany(item => new[] { item }.Concat(item.Menu == null ? Enumerable.Empty<NativeMenuItem>() : NativeItems(item.Menu)));
var nativeItems=NativeItems(NativeMenu.GetMenu(window)!).ToArray();
foreach(var label in new[]{"Test in Emulator","Claude Assistant (MCP Server)","Show MCP Setup Command","Toggle Claude Chat"}) {
   var item=nativeItems.Single(i=>Equals(i.Header,label));
   Check(!item.IsVisible && !item.IsEnabled && item.Gesture==null,"Optional integration menus must be hidden, disabled and lack accelerators: "+label);
}
var toolbarButtons=window.GetVisualDescendants().OfType<Button>().ToArray();
foreach(var tooltip in new[]{"Ask Claude about this ROM (Cmd+Shift+C)","Test in Emulator (F6) - runs the ROM as it is right now, unsaved edits included"}) {
   var button=toolbarButtons.Single(b=>Equals(ToolTip.GetTip(b),tooltip));
   Check(!button.IsVisible && !button.IsEnabled,"Optional toolbar controls must collapse and be disabled.");
}
Check(!window.KeyBindings.Any(b=>b.Gesture?.Key==Key.F6),"Hidden built-in emulation must not retain the F6 binding.");
window.KeyPressQwerty(PhysicalKey.F6,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.F6,RawInputModifiers.None);Pump();
Check(!window.IsEmulatorOpen,"F6 cannot open the hidden built-in emulator.");
var external=nativeItems.Single(i=>Equals(i.Header,"Launch in External Emulator"));
Check(external.IsVisible && external.IsEnabled && external.Gesture?.Key==Key.F5 && window.KeyBindings.Any(b=>b.Gesture?.Key==Key.F5),"External emulator menu and F5 stay available.");
Check(toolbarButtons.Single(b=>Equals(ToolTip.GetTip(b),"Launch in External Emulator (F5)")).IsVisible,"External emulator toolbar action stays visible.");
var revealInfo=MacFileReveal.CreateStartInfo("/tmp/Tab Tests/rom \"quoted\".gba");
Check(revealInfo.FileName=="/usr/bin/open" && !revealInfo.UseShellExecute && revealInfo.ArgumentList.SequenceEqual(new[]{"-R","/tmp/Tab Tests/rom \"quoted\".gba"}),"Finder reveal uses separate -R/path arguments and preserves spaces/quotes.");
var bytes=first.Model.RawData.ToArray();
var point=Point(first);window.MouseDown(point,MouseButton.Right);window.MouseUp(point,MouseButton.Right);Pump();
Check(Menu(first).IsOpen && ReferenceEquals(Menu(first).DataContext,first),"Secondary click must open the clicked tab's menu.");
Check(Menu(first).Items.OfType<MenuItem>().Select(i=>i.Header).SequenceEqual(new[]{"Show in Finder","Duplicate Tab","Diff with Left","Diff with Right","Make IPS Patch, this -> Right","Make UPS Patch, this -> Right"}),"Retain all existing tab actions.");
Menu(first).Close();Pump();editor.SelectedIndex=1;Pump();
if(OperatingSystem.IsMacOS()) foreach(bool releaseControlFirst in new[]{false,true}) {
   var order=editor.ToArray();point=Point(first);window.MouseDown(point,MouseButton.Left,RawInputModifiers.Control);Pump();
   Check(Menu(first).IsOpen,"Control-click opens on press without tab drag capture.");
   window.MouseMove(Point(second),RawInputModifiers.LeftMouseButton|RawInputModifiers.Control);
   window.MouseUp(Point(second),MouseButton.Left,releaseControlFirst?RawInputModifiers.None:RawInputModifiers.Control);Pump();
   Check(Menu(first).IsOpen && ReferenceEquals(Menu(first).DataContext,first),"Control-click must open the clicked tab's menu rather than start dragging.");
   Check(order.SequenceEqual(editor),"Moving during Control-click must not reorder tabs.");
   Menu(first).Close();Pump();
}
OpenMenu(first);
Click(Menu(first).Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Show in Finder")));
Check(fs.Revealed.SequenceEqual(new[]{first.FullFileName}),"Finder action uses the clicked tab's path even when another ROM was selected.");
fs.Exists=_=>false;OpenMenu(first);
Click(Menu(first).Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Show in Finder")));
Check(fs.Revealed.Count==1 && editor.ErrorMessage.Contains("existing saved file"),"Missing files show an error without a launch request.");
fs.Exists=_=>true;fs.Failure="Finder launch rejected";OpenMenu(first);
Click(Menu(first).Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Show in Finder")));
Check(editor.ErrorMessage.Contains("Finder launch rejected"),"Finder launch errors are visible.");
fs.Failure=null;
var unsaved=new ViewPort(new LoadedFile(string.Empty,new byte[512]));editor.Add(unsaved);Pump();
var priorReveals=fs.Revealed.Count;OpenMenu(unsaved);
Click(Menu(unsaved).Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Show in Finder")));
Check(fs.Revealed.Count==priorReveals && editor.ErrorMessage.Contains("existing saved file"),"Untitled tabs do not launch Finder with an invalid path.");
unsaved.Close.Execute(fs);Pump();OpenMenu(first);
var duplicate=Menu(first).Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Duplicate Tab"));
Check(duplicate.Command!.CanExecute(null),"Duplicate must remain available.");
duplicate.Command.Execute(null);Menu(first).Close();Pump();
Check(editor.Count==3 && editor.OfType<ViewPort>().Count(v=>ReferenceEquals(v.Model,first.Model))==2,"Duplicate acts on the clicked ROM, not the previously selected ROM.");
Check(first.Model.RawData.SequenceEqual(bytes) && !first.Undo.CanExecute(null),"Opening menus and duplicating must not edit ROM data/history.");
var duplicateTab=editor.OfType<ViewPort>().Single(v=>!ReferenceEquals(v,first) && ReferenceEquals(v.Model,first.Model));
point=Point(duplicateTab);window.MouseDown(point,MouseButton.Middle);window.MouseUp(point,MouseButton.Middle);Pump();
Check(editor.Count==2 && editor.Contains(first) && editor.Contains(second),"Middle-click closes only the clicked duplicate tab.");
editor.SelectedIndex=0;Pump();var moving=editor[0];var otherTab=editor[1];
point=Point(moving);window.MouseDown(point,MouseButton.Left);
window.MouseMove(point+new Avalonia.Vector(Header(moving).Bounds.Width+Header(otherTab).Bounds.Width,0),RawInputModifiers.LeftMouseButton);
window.MouseUp(point+new Avalonia.Vector(400,0),MouseButton.Left);Pump();
Check(ReferenceEquals(editor[1],moving),"Ordinary primary drag still reorders tabs.");
Check(first.Model.RawData.SequenceEqual(bytes),"Tab dragging must not modify ROM data.");

var fixture=new SyntheticMapFixture(24,20);var model=fixture.Model;var token=new ModelDelta();
foreach(var address in new[]{0x104,0x108,0x10C})model.WritePointer(token,address,0x110);
var format=model.GetNextRun(0x100).FormatString;
var parse=ArrayRun.TryParse(model,format[..^1]+"4",0x100,SortedSpan<int>.None,out var rootTable);
Check(!parse.HasError,"The fabricated map banks parse.");
model.ObserveAnchorWritten(new NoDataChangeDeltaModel(),"data.maps.banks",rootTable);
((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new("synthetic.layout","data.maps.banks/3/maps/0/map/0/layout/","Maps"));
var owner=new ViewPort("map owner.gba",model,InstantDispatch.Instance,new Singletons(),tutorials:editor.MapTutorials,fs:fs);
owner.InitializationWorkload.GetAwaiter().GetResult();editor.Add(owner);var map=owner.MapEditor;editor.Add(map);Pump();
var mapBytes=model.RawData.ToArray();OpenMenu(map);
Check(ReferenceEquals(Menu(map).DataContext,map),"Map tabs retain their own context menu target.");
Click(Menu(map).Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Show in Finder")));
Check(fs.Revealed.Last()==owner.FullFileName,"A map tab reveals its owning ROM file.");
Check(mapBytes.SequenceEqual(model.RawData),"Revealing a map's ROM must not modify it.");
window.Editor=null;window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class RevealFileSystem : StubFileSystem, IFileRevealer {
   public List<string> Revealed { get; }=new();
   public string Failure { get; set; }
   public Task<string> RevealFileAsync(string path){Revealed.Add(path);return Task.FromResult(Failure);}
}
