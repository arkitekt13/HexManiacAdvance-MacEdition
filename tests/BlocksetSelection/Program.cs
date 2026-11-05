using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Performance;
using HexManiac.Core.Models.Runs.Sprites;
using System.Collections.Specialized;
using System.Diagnostics;


AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
   .WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
void PumpUntil(Func<bool> ready) {
   var clock = Stopwatch.StartNew();
   while (!ready()) {
      Dispatcher.UIThread.RunJobs();
      if (clock.Elapsed > TimeSpan.FromSeconds(20)) throw new TimeoutException("Map startup/frame did not finish.");
      Thread.Sleep(1);
   }
}

   var window = new MainWindow { Width = 1440, Height = 1000 };
   var dispatcher = InstantDispatch.Instance;
   // Never read/write user settings, ROM sidecars, or file watchers in a test.
   var fileSystem = new StubFileSystem { MetadataFor = _ => Array.Empty<string>() };
   Application.Current!.Resources["FileSystem"] = fileSystem;
   var editor = new EditorViewModel(fileSystem, dispatcher);
   App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
   window.Resources["FileSystem"] = fileSystem;
   window.FileSystem = fileSystem;
   window.Editor = editor;
   window.Show();
   ViewPort viewport;
   {
      var fixture = new SyntheticMapFixture(24, 20);
      var seed=fixture.Model;var data=seed.RawData.ToArray();
      Array.Copy(data,0x120,data,0x240,16);Array.Copy(data,0x140,data,0x260,24);
      Array.Copy(data,0x180,data,0x280,24);Array.Copy(data,0x1A0,data,0x2A0,24);
      Array.Copy(data,0x120,data,0x340,16);Array.Copy(data,0x140,data,0x360,24);
      void Ptr(int source,int target)=>BitConverter.GetBytes(0x08000000+target).CopyTo(data,source);
      Ptr(0x118,0x340);Ptr(0x340,0x360);Ptr(0x114,0x240);Ptr(0x240,0x260);Ptr(0x270,0x280);Ptr(0x274,0x2A0);
      var anchors=seed.ExportMetadata(null,fixture.ViewPort.Singletons.MetadataInfo).NamedAnchors;
      var oldBank=anchors.Single(a=>a.Name=="synthetic.bank").Format;
      var newBank=oldBank[..^1]+"3";
      anchors=anchors.Select(a=>a with{Format=a.Format.Replace(oldBank,newBank)}).ToArray();
      var model=new PokemonModel(data,new StoredMetadata(anchors:anchors),fixture.ViewPort.Singletons);

      var token = new ModelDelta();
      foreach (var address in new[] { 0x104, 0x108, 0x10C }) model.WritePointer(token, address, 0x110);
      var format = model.GetNextRun(0x100).FormatString;
      var error = ArrayRun.TryParse(model, format[..^1] + "4", 0x100, SortedSpan<int>.None, out var root);
      Check(!error.HasError, "Four fabricated banks must parse.");
      model.ObserveAnchorWritten(new NoDataChangeDeltaModel(), "data.maps.banks", root);
      ((IList<GotoShortcutModel>)model.GotoShortcuts).Add(new GotoShortcutModel(
         "synthetic.layout", "data.maps.banks/3/maps/0/map/0/layout/", "Maps"));
      viewport = new ViewPort("zoom-fixture.bin", model, dispatcher, new Singletons(dispatcher),
         tutorials: editor.MapTutorials, fs: fileSystem);
      editor.Add(viewport);
   }
   PumpUntil(() => viewport.MapEditor != null);
   var mapper = viewport.MapEditor;
   Check(mapper.IsValidState, "Map editor must initialize successfully.");
   mapper.NavigateTo(3, 0, 10, 10);
   editor.Add(mapper);
mapper.ShowHeaderPanel=true;
void Layout(){Dispatcher.UIThread.RunJobs();window.Measure(new Size(1440,1000));window.Arrange(new Rect(0,0,1440,1000));AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
Layout();var view=window.GetVisualDescendants().OfType<MapTab>().Single();
var header=mapper.PrimaryMap.Header;
viewport.ChangeHistory.ClearHistory(false);
int resets=0;
header.PrimaryOptions.CollectionChanged+=(_,e)=>{if(e.Action==NotifyCollectionChangedAction.Reset)resets++;};
header.SecondaryOptions.CollectionChanged+=(_,e)=>{if(e.Action==NotifyCollectionChangedAction.Reset)resets++;};
void StaleCache() {
 var otherMap=new BlockMapViewModel(fileSystem,editor.MapTutorials,viewport,mapper.Format,mapper.Templates,3,1);
 _=otherMap.Header;
 Check(!header.PrimaryOptions.SequenceEqual(mapper.Format.BlocksetCache.Primary),"Another real header must replace shared option instances.");
 Check(header.PrimaryOptions.Select(o=>o.Address).SequenceEqual(mapper.Format.BlocksetCache.Primary.Select(o=>o.Address)),"Reproduced stale cache retains exactly the same ROM addresses.");
}
void Pick(AngleComboBox combo,int index) {
 combo.IsDropDownOpen=true;Layout();
 var item=combo.ContainerFromIndex(index)!;
 var pointer=new Pointer(77,PointerType.Mouse,true);
 item.RaiseEvent(new PointerPressedEventArgs(item,pointer,TopLevel.GetTopLevel(item),new Avalonia.Point(4,4),0,new PointerPointProperties(RawInputModifiers.LeftMouseButton,PointerUpdateKind.LeftButtonPressed),KeyModifiers.None,1));
 item.RaiseEvent(new PointerReleasedEventArgs(item,pointer,TopLevel.GetTopLevel(item),new Avalonia.Point(4,4),1,new PointerPointProperties(RawInputModifiers.None,PointerUpdateKind.LeftButtonReleased),KeyModifiers.None,MouseButton.Left));
}
int cases=0;
foreach(var name in new[]{"PrimaryBlocksetSelector","SecondaryBlocksetSelector"})
foreach(var mode in new[]{"normal","stale","reorder","shrink","missing","keyboard"}) {
 var beforeBytes=viewport.Model.RawData.ToArray();
 var beforeMetadata=viewport.Model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize();
 viewport.ChangeHistory.ClearHistory(false);
 var combo=view.FindControl<AngleComboBox>(name)!;
 var choices=name=="PrimaryBlocksetSelector"?header.PrimaryOptions:header.SecondaryOptions;
 var beforeObjects=choices.ToArray();
 if(mode!="normal")StaleCache();
 var cache=name=="PrimaryBlocksetSelector"?mapper.Format.BlocksetCache.Primary:mapper.Format.BlocksetCache.Secondary;
 if(mode=="reorder")cache.Move(1,0);
 if(mode=="shrink")cache.RemoveAt(0);
 if(mode=="missing")cache.RemoveAt(1);
 int before=resets;
 if(mode=="keyboard") {
   combo.Focus();combo.IsDropDownOpen=true;Layout();window.KeyPressQwerty(PhysicalKey.End,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.End,RawInputModifiers.None);window.KeyPressQwerty(PhysicalKey.Enter,RawInputModifiers.None);window.KeyReleaseQwerty(PhysicalKey.Enter,RawInputModifiers.None);
 } else Pick(combo,1);
 Check(resets==before,"Committing a selection must never clear either option list.");
 var layout=mapper.PrimaryMap.GetLayout();
 var field=name=="PrimaryBlocksetSelector"?"blockdata1":"blockdata2";
 var target=field=="blockdata1"?0x280:0x2A0;
 Check(layout.GetAddress(field)==target,"Chosen pointer must update synchronously before dispatcher jobs run.");
 var expected=beforeBytes.ToArray();
 BitConverter.GetBytes(target+0x08000000).CopyTo(expected,field=="blockdata1"?0x150:0x154);
 Check(expected.SequenceEqual(viewport.Model.RawData),"Selection may change only its own four pointer bytes.");
 byte[] saved=null;string[] savedMetadata=null;
 fileSystem.Save=f=>{saved=f.Contents.ToArray();return true;};
 fileSystem.SaveMetadata=(_,lines)=>{savedMetadata=lines.ToArray();return true;};
 viewport.Save.Execute(fileSystem);
 Check(saved!=null&&saved.SequenceEqual(expected)&&savedMetadata!=null,"Save before queued presentation work must include the chosen pointer.");
 var reloaded=new PokemonModel(saved,new StoredMetadata(savedMetadata),viewport.Singletons);
 Check(reloaded.RawData.SequenceEqual(expected),"Saved selection must reload with the exact chosen pointer and unrelated ROM bytes.");
 Layout();
 Check(combo.SelectedItem is BlocksetOption selected&&selected.Address==layout.GetAddress(field),"After presentation refresh, displayed selection must match the ROM address, not an old index.");
 if(mode is "normal" or "stale" or "keyboard") {
   Check(resets==before,"Same-address cache refresh must preserve both collections.");
   Check(beforeObjects.SequenceEqual(choices),"Equivalent choices keep their bound object identities.");
 }
 Check(view.FindControl<Viewbox>("BlocksetPreviewBox")!.IsVisible==false,"Selection must dismiss blockset hover previews.");
 mapper.Undo.Execute(null);Layout();
 Check(beforeBytes.SequenceEqual(viewport.Model.RawData),"One Undo restores every original byte.");
 Check(beforeMetadata.SequenceEqual(viewport.Model.ExportMetadata(null,viewport.Singletons.MetadataInfo).Serialize()),"Undo restores pointer and anchor metadata.");
 Check(!viewport.ChangeHistory.IsSaved,"Undo differs from the saved tileset selection.");
 mapper.Redo.Execute(null);Layout();
 Check(expected.SequenceEqual(viewport.Model.RawData)&&viewport.ChangeHistory.IsSaved,"Redo restores exactly the saved selection.");
 mapper.Undo.Execute(null);Layout();
 Check(beforeBytes.SequenceEqual(viewport.Model.RawData),"Return to the baseline for the next independent case.");
 Console.WriteLine($"PASS:{name}:{mode}");cases++;
}
Check(cases==12,"Both selectors must cover normal, stale-cache, reordered, shrinking, missing-choice and keyboard selections.");
// A queued presentation refresh must not reapply an edit after Undo or navigation.
foreach(var name in new[]{"PrimaryBlocksetSelector","SecondaryBlocksetSelector"}) {
 var baseline=viewport.Model.RawData.ToArray();viewport.ChangeHistory.ClearHistory(false);
 StaleCache();Pick(view.FindControl<AngleComboBox>(name)!,1);
 var changed=viewport.Model.RawData.ToArray();
 mapper.Undo.Execute(null);Layout();
 Check(baseline.SequenceEqual(viewport.Model.RawData),"Queued selection presentation must not reapply undone pointer writes.");
 Check(mapper.Redo.CanExecute(null),"Queued presentation must retain Redo.");
 mapper.Redo.Execute(null);Layout();Check(changed.SequenceEqual(viewport.Model.RawData),"Redo must restore the original committed selection.");
 mapper.Undo.Execute(null);Layout();
}
var beforeNavigation=viewport.Model.RawData.ToArray();viewport.ChangeHistory.ClearHistory(false);
StaleCache();Pick(view.FindControl<AngleComboBox>("PrimaryBlocksetSelector")!,1);
var committed=viewport.Model.RawData.ToArray();
mapper.NavigateTo(3,2,10,10);Layout();
Check(committed.SequenceEqual(viewport.Model.RawData),"Navigation must not cause a queued callback to write another map's pointers.");
Check(view.FindControl<AngleComboBox>("PrimaryBlocksetSelector")!.SelectedItem is BlocksetOption visible&&visible.Address==mapper.PrimaryMap.GetLayout().GetAddress("blockdata1"),"Visible selection after navigation belongs to the new map.");
mapper.NavigateTo(3,0,10,10);Layout();mapper.Undo.Execute(null);Layout();
Check(beforeNavigation.SequenceEqual(viewport.Model.RawData),"Navigation scenario must undo back to its baseline.");
header=mapper.PrimaryMap.Header;
var beforeInvalid=viewport.Model.RawData.ToArray();
header.PrimaryIndex=-1;header.PrimaryIndex=int.MaxValue;header.SecondaryIndex=-1;header.SecondaryIndex=int.MaxValue;Layout();
Check(beforeInvalid.SequenceEqual(viewport.Model.RawData),"Transient or out-of-range indices must not change pointers.");
var retained=header.PrimaryOptions[0];var oldRender=retained.Render;
header.Refresh();Layout();
Check(ReferenceEquals(retained,header.PrimaryOptions[0]),"A content refresh must preserve equivalent option identities.");
Check(!ReferenceEquals(oldRender,retained.Render)&&oldRender.PixelData.SequenceEqual(retained.Render.PixelData),"Stable identities must still refresh preview content without changing its pixels.");
// Explicit refresh is also used by map data operations to invalidate rendered graphics.
var graphicsBaseline=viewport.Model.RawData.ToArray();
viewport.ChangeHistory.ClearHistory(false);
var oldMapPixels=mapper.PrimaryMap.PixelData.ToArray();
for(int i=0;i<32;i+=4)viewport.Model.WriteMultiByteValue(0x1000+i,4,viewport.ChangeHistory.CurrentChange,0x22222222);
viewport.ChangeHistory.ChangeCompleted();header.Refresh();Layout();
Check(!oldMapPixels.SequenceEqual(mapper.PrimaryMap.PixelData),"Explicit header refresh must invalidate map graphics even when the blockset addresses are unchanged.");
mapper.Undo.Execute(null);Layout();
Check(graphicsBaseline.SequenceEqual(viewport.Model.RawData)&&oldMapPixels.SequenceEqual(mapper.PrimaryMap.PixelData),"Undo restores both graphics bytes and their rendered map.");
window.Editor=null;window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
