using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using Avalonia.Controls.Primitives;
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
var fixture=new SyntheticMapFixture(24,20,withScriptEvent:true);var model=fixture.Model;
model.SetList(new NoDataChangeDeltaModel(),"MapAttributeBehaviors","Normal");
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
model.FreeSpaceStart = 0x18000;
model.SetList(viewport.CurrentChange, "FacingOptions", "NoMovement");
var objectFormat = new HavenSoft.HexManiac.Core.Models.Map.Format(model).ObjectsFormat;
objectFormat = objectFormat[..(objectFormat.LastIndexOf(']') + 1)] + "1";
var objectError = ArrayRun.TryParse(model, objectFormat, 0x17000, SortedSpan<int>.None, out var objectRun);
Check(!objectError.HasError, "Synthetic object table must parse.");
model.ObserveAnchorWritten(viewport.CurrentChange, "synthetic.object", objectRun);
var sprites = Enumerable.Range(0, 152).Select(i => {
   int w = i == 108 ? 64 : i == 150 ? 96 : i is 106 or 107 or 109 ? 32 : 16;
   int h = i == 108 ? 64 : i == 150 ? 128 : 32;
   return new ReadonlyPixelViewModel(w, h, Enumerable.Repeat((short)(i + 1), w * h).ToArray(), -1);
}).ToArray();
ObjectEventViewModel Object() => new(mapper.PrimaryMap, _ => {}, model.GetTableModel("synthetic.object", () => viewport.CurrentChange)[0], mapper.Templates, sprites, sprites[0], null);
var obj = Object(); mapper.SelectedEvent = obj; Layout();
AngleComboBox Combo() => view.GetVisualDescendants().OfType<AngleComboBox>().Single(c => ReferenceEquals(c.ItemsSource, obj.Options));
var combo = Combo();
viewport.ChangeHistory.ClearHistory(false);
var initial = model.RawData.ToArray();
var metadata = model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize();
var position = (mapper.PrimaryMap.LeftEdge, mapper.PrimaryMap.TopEdge);
double previewHeight = combo.Bounds.Height;
var field = view.GetVisualDescendants().OfType<AngleTextBox>().First(t => ReferenceEquals(t.DataContext, obj));
double fieldY = field.TranslatePoint(default, view)!.Value.Y;
string BitmapHash(PixelImage image) {
   var bitmap = (WriteableBitmap)image.Source!; using var frame = bitmap.Lock();
   var bytes = new byte[frame.RowBytes * bitmap.PixelSize.Height]; Marshal.Copy(frame.Address, bytes, 0, bytes.Length);
   return Convert.ToHexString(SHA256.HashData(bytes));
}
void Selected(int index) {
   Check(combo.SelectedIndex == index && obj.Graphics == index, $"Selection must reach sprite {index} without resetting.");
   Check(combo.Bounds.Height == previewHeight && field.TranslatePoint(default, view)!.Value.Y == fieldY, $"Sprite {index} must not resize the selector or shift event fields.");
   var image = combo.GetVisualDescendants().OfType<PixelImage>().Single();
   Check(image.Source is WriteableBitmap bitmap && bitmap.PixelSize == new PixelSize(sprites[index].PixelWidth, sprites[index].PixelHeight), "The preview retains the entire source bitmap, including large sprites.");
   var thumbnail = image.GetVisualAncestors().OfType<Viewbox>().Single();
   Check(thumbnail.Bounds.Width <= 64 && thumbnail.Bounds.Height <= 64, "The complete image fits inside a bounded thumbnail.");
}
string previous = "";
foreach (var index in new[] { 0, 106, 107, 108, 109, 110, 108, 107, 150, 151, 0 }) {
   combo.SelectedIndex = index; Layout(); Selected(index);
   string hash = BitmapHash(combo.GetVisualDescendants().OfType<PixelImage>().Single());
   Check(hash != previous, "Changing graphics must publish fresh sprite pixels, including transitions into and out of large images."); previous = hash;
}
combo.SelectedIndex = 105; combo.Focus(); Layout();
for (int index = 106; index <= 111; index++) {
   window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None); window.KeyReleaseQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None); Layout(); Selected(index);
   Check(position == (mapper.PrimaryMap.LeftEdge, mapper.PrimaryMap.TopEdge), "Arrow keys in the selector must change graphics without panning the map.");
}
combo.IsDropDownOpen = true; Layout();
var popup = combo.GetVisualDescendants().OfType<Popup>().Single();
var popupRoot = popup.Child!;
var scroller = popupRoot.GetVisualDescendants().OfType<ScrollViewer>().First();
var rowHeight = popupRoot.GetVisualDescendants().OfType<ComboBoxItem>().First().Bounds.Height;
scroller.Offset = new Vector(0, 107 * rowHeight); Layout();
var rows = popupRoot.GetVisualDescendants().OfType<ComboBoxItem>().ToArray();
var largeRow = rows.Single(r => r.DataContext is VisualComboOption { Index: 108 });
var neighbor = rows.Single(r => r.DataContext is VisualComboOption { Index: 107 });
Check(largeRow.Bounds.Height == neighbor.Bounds.Height && largeRow.Bounds.Height > 0, "Large and ordinary sprites have stable popup row heights.");
scroller.Offset = new Vector(0, 107 * rowHeight); Layout();
var popupWindow = TopLevel.GetTopLevel(scroller)!;
var wheelPoint = scroller.TranslatePoint(new Avalonia.Point(scroller.Bounds.Width / 2, scroller.Bounds.Height / 2), popupWindow)!.Value;
foreach (var direction in new[] { -1, 1 }) for (int step = 0; step < 12; step++) {
   var offset = scroller.Offset.Y;
   popupWindow.MouseWheel(wheelPoint, new Vector(0, direction), RawInputModifiers.None); Layout();
   Check(combo.IsDropDownOpen && combo.SelectedIndex == 111 && obj.Graphics == 111, "Wheel scrolling around sprite 108 preserves the open list and committed selection.");
   Check(scroller.Offset.Y != offset, "Actual wheel events scroll the sprite list.");
   Check(popupRoot.GetVisualDescendants().OfType<ComboBoxItem>().Any(r => r.GetVisualDescendants().OfType<PixelImage>().Any(i => i.Source != null)), "Scrolled sprite rows remain populated.");
}
// Choose the large sprite with an actual popup pointer click, then reopen at the same ID.
scroller.Offset = new Vector(0, 107 * rowHeight); Layout();
largeRow = popupRoot.GetVisualDescendants().OfType<ComboBoxItem>().Single(r => r.DataContext is VisualComboOption { Index: 108 });
var click = largeRow.TranslatePoint(new Avalonia.Point(largeRow.Bounds.Width / 2, largeRow.Bounds.Height / 2), popupWindow)!.Value;
popupWindow.MouseMove(click); popupWindow.MouseDown(click, MouseButton.Left); popupWindow.MouseUp(click, MouseButton.Left); Layout();
Selected(108); Check(!combo.IsDropDownOpen, "Clicking a sprite commits its graphics ID and closes the list.");
combo.IsDropDownOpen = true; Layout(); combo.IsDropDownOpen = false; Layout(); Selected(108);
Check(ReferenceEquals(mapper.SelectedEvent, obj), "Selector interactions retain the object being edited.");
Check(model.RawData.Where((b, i) => i != 0x17001).SequenceEqual(initial.Where((b, i) => i != 0x17001)), "Sprite selection preserves every unrelated ROM byte.");
Check(metadata.SequenceEqual(model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()), "Sprite selection preserves formatting and pointer metadata.");
viewport.ChangeHistory.ChangeCompleted(); var selectedBytes = model.RawData.ToArray();
mapper.Undo.Execute(null); Layout(); Check(model.RawData.SequenceEqual(initial), "Undo restores the original graphics byte and complete ROM.");
mapper.Redo.Execute(null); Layout(); Check(model.RawData.SequenceEqual(selectedBytes), "Redo restores the selected large sprite exactly.");
obj = Object(); mapper.SelectedEvent = obj; Layout(); combo = Combo();
Check(combo.SelectedIndex == 108 && combo.GetVisualDescendants().OfType<PixelImage>().Single().Source is WriteableBitmap b && b.PixelSize == new PixelSize(64, 64), $"Reopening the object after history restores the selected large image (UI={combo.SelectedIndex}, ROM={obj.Graphics}, source={((WriteableBitmap)combo.GetVisualDescendants().OfType<PixelImage>().Single().Source)?.PixelSize}).");
Check(metadata.SequenceEqual(model.ExportMetadata(null, viewport.Singletons.MetadataInfo).Serialize()), "History preserves sprite and pointer metadata.");
// Recycled controls must follow the newly selected object, including queued replacements.
model.WriteMultiByteValue(0x17021, 1, viewport.CurrentChange, 7);
var otherError = ArrayRun.TryParse(model, objectFormat, 0x17020, SortedSpan<int>.None, out var otherRun);
Check(!otherError.HasError, "The second object fixture must parse.");
model.ObserveAnchorWritten(viewport.CurrentChange, "synthetic.other", otherRun);
var other = new ObjectEventViewModel(mapper.PrimaryMap, _ => {}, model.GetTableModel("synthetic.other", () => viewport.CurrentChange)[0], mapper.Templates, sprites, sprites[0], null);
var switchBytes = model.RawData.ToArray();
foreach (var target in new[] { other, obj, other, obj }) mapper.SelectedEvent = target;
Layout(); combo = Combo();
Check(combo.SelectedIndex == 108 && obj.Graphics == 108 && other.Graphics == 7, "Coalesced context changes must restore the current object's ID, never a stale selection.");
mapper.SelectedEvent = other; Layout();
var otherCombo = view.GetVisualDescendants().OfType<AngleComboBox>().Single(c => ReferenceEquals(c.ItemsSource, other.Options));
Check(otherCombo.SelectedIndex == 7 && other.Graphics == 7, "Switching to a different object's option instances restores its own graphics ID.");
Check(switchBytes.SequenceEqual(model.RawData), "Queued selection restoration cannot rewrite either object's graphics byte.");
window.Editor = null; window.Close(); Console.WriteLine($"CHECKS_PASSED:{checks}");
