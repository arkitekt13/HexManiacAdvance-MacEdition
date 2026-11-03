using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Performance;
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory); Application.Current!.Resources["FileSystem"] = new StubFileSystem();
// Fabricated data only: four banks alias one map to guard distinct per-bank/event contexts.
var f = new SyntheticMapFixture(16,16); var m = f.Model; var token = new ModelDelta();
void Word(int at, int value, int length=4) => m.WriteMultiByteValue(at,length,token,value);
void Pointer(int at,int target) => m.WritePointer(token,at,target);
void Anchor(int at,string name,string format) { var error=ArrayRun.TryParse(m,format,at,SortedSpan<int>.None,out var run); if(error.HasError)throw new Exception(error.ErrorMessage);m.ObserveAnchorWritten(new NoDataChangeDeltaModel(),name,run); }
Pointer(0x104,0x110);Pointer(0x108,0x110);Pointer(0x10C,0x110);
var bankFormat=m.GetNextRun(0x100).FormatString;Anchor(0x100,"data.maps.banks",bankFormat[..^1]+"4");
Pointer(0x128,0x200); Pointer(0x200,0x300);Pointer(0x204,0x400);Pointer(0x208,0x500);
for(int i=0;i<3;i++){Word(0x300+i*10,1+i,2);Word(0x302+i*10,2+i,2);Pointer(0x304+i*10,0x700);}
Word(0x400,5,2);Word(0x402,6,2);Pointer(0x404,0x700);
Word(0x500,7,2);Word(0x502,8,2);Word(0x505,5,1);Word(0x508,13,2);
// XSE additem 13, quantity 1, end; operands are edited through the real model/history below.
new ModelDelta().ChangeData(m,0x700,new byte[]{0x44,13,0,1,0,2});
Anchor(0x200,"synthetic.events","[objects<[x: y: script<> graphics. unused.]3> scripts<[x: y: script<>]1> signposts<[x: y: elevation. kind. unused: arg::]1>]1");
var viewport = new ViewPort("usage",m,InstantDispatch.Instance,new Singletons(),fs:new StubFileSystem());
viewport.InitializationWorkload.GetAwaiter().GetResult();
var rootFormat=m.GetNextRun(0x100).FormatString.Replace("events<>","events<[objects<[x: y: script<> graphics. unused.]3> scripts<[x: y: script<>]1> signposts<[x: y: elevation. kind. unused: arg::]1>]1>");
Anchor(0x100,"data.maps.banks",rootFormat);
var mapper = viewport.MapEditor;
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
Check(mapper?.IsValidState == true, "Synthetic map editor must be valid.");
var bankField = typeof(GotoMapButton).GetField("bank", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
var eventField = typeof(GotoMapButton).GetField("eventModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
string[] Keys(MapOptionsArrayElementViewModel owner) => owner.MapPreviews.Select(button => $"{bankField.GetValue(button)}:{((HavenSoft.HexManiac.Core.Models.Map.IEventModel)eventField.GetValue(button)).Element.Start:X}").ToArray();
MapOptionsArrayElementViewModel Query(int item, string tableName = HardcodeTablesModel.ItemsTableName) {
   var timer = new ManualWorkTimer();
   var owner = new MapOptionsArrayElementViewModel(InstantDispatch.Instance, timer, mapper, tableName, item);
   Check(owner.MapPreviews.Count == 0, "Search must remain deferred until its timer executes.");
   timer.RunWork(); return owner;
}
string[] Expected(params int[] addresses) => Enumerable.Range(0,4).SelectMany(bank => addresses.Select(at => $"{bank}:{at:X}")).ToArray();
var all = Expected(0x300,0x30A,0x314,0x400,0x500);
var baseline = m.RawData.ToArray();
var initial = Query(13);
Check(Keys(initial).SequenceEqual(all), "Shared scripts must preserve each bank/event and exact ordering, including hidden items.");
var pixels = initial.MapPreviews.Select(b => b.Image.PixelData.ToArray()).ToArray();
Check(Query(50).MapPreviews.Count == 0, "Negative shared-root matches must remain negative.");
Check(Query(13, HardcodeTablesModel.MoveNamesTable).MapPreviews.Count == 0, "A different enum query must not reuse item outcomes.");
Check(baseline.SequenceEqual(m.RawData), "Read-only searches changed bytes.");
m.WriteMultiByteValue(0x140,4,new ModelDelta(),0);
Check(Query(13).MapPreviews.Count == 0, "Matching scripts with invalid preview dimensions must not publish unusable buttons.");
m.WriteMultiByteValue(0x140,4,new ModelDelta(),16);
Check(Keys(Query(13)).SequenceEqual(all), "Valid per-event previews must recover on a fresh query.");


void ChangeAndHistory(Action change, Action verify) {
   var before = m.RawData.ToArray(); change(); viewport.ChangeHistory.ChangeCompleted(); verify();
   var after = m.RawData.ToArray(); Check(!before.SequenceEqual(after), "Mutation scenario did not edit bytes.");
   viewport.Undo.Execute(null); Check(before.SequenceEqual(m.RawData), "Undo did not restore exact source bytes/pointers.");
   Check(Keys(Query(13)).SequenceEqual(all), "New query after undo must see original script operands/pointers.");
   viewport.Redo.Execute(null); Check(after.SequenceEqual(m.RawData), "Redo did not restore exact edited bytes/pointers."); verify();
   viewport.Undo.Execute(null); Check(before.SequenceEqual(m.RawData), "Cleanup undo failed.");
}
ChangeAndHistory(() => m.WriteMultiByteValue(0x701,2,viewport.CurrentChange,14), () => {
   Check(Keys(Query(13)).SequenceEqual(Expected(0x500)), "Script edits must invalidate old positive outcomes on the next query.");
   Check(Keys(Query(14)).SequenceEqual(Expected(0x300,0x30A,0x314,0x400)), "Script edits must invalidate old negative outcomes on the next query.");
});
ChangeAndHistory(() => {
   viewport.CurrentChange.ChangeData(m,0x760,new byte[]{0x44,15,0,1,0,2});
   var field = new FieldArrayElementViewModel(viewport,"script",0x304,4,AddressFieldStrategy.Instance);
   field.Content = "<000760>";
}, () => {
   Check(Keys(Query(13)).SequenceEqual(Expected(0x30A,0x314,0x400,0x500)), "Repointed event must leave its old shared root's results.");
   Check(Keys(Query(15)).SequenceEqual(Expected(0x300)), "Repointed event must use its new script while preserving other events.");
});
ChangeAndHistory(() => {
   viewport.CurrentChange.ChangeData(m,0x780,new byte[]{0x04,0,7,0,8,0x05,0x80,7,0,8});
   m.WritePointer(viewport.CurrentChange,0x404,0x780);
}, () => {
   Check(Keys(Query(13)).SequenceEqual(all), "Branched/cyclic scripts must preserve the original bounded traversal results.");
   Check(Query(50).MapPreviews.Count == 0, "A cyclic graph without a matching value must terminate without false results.");
});
ChangeAndHistory(() => {
   viewport.CurrentChange.ChangeData(m,0x7C0,0xFF); m.WritePointer(viewport.CurrentChange,0x304,0x7C0);
}, () => Check(Keys(Query(13)).SequenceEqual(Expected(0x30A,0x314,0x400,0x500)), "Malformed roots must not gain false matches."));
ChangeAndHistory(() => m.WritePointer(viewport.CurrentChange,0x304,m.Count+0x100),
   () => Check(Keys(Query(13)).SequenceEqual(Expected(0x30A,0x314,0x400,0x500)), "Out-of-range roots must not gain false matches."));
ChangeAndHistory(() => m.WriteMultiByteValue(0x508,2,viewport.CurrentChange,14), () => {
   Check(Keys(Query(13)).SequenceEqual(Expected(0x300,0x30A,0x314,0x400)), "Hidden-item edits must not reuse script outcomes.");
   Check(Keys(Query(14)).SequenceEqual(Expected(0x500)), "Updated hidden-item value was not found.");
});

var cancelTimer = new ManualWorkTimer();
var cancelled = new MapOptionsArrayElementViewModel(InstantDispatch.Instance,cancelTimer,mapper,HardcodeTablesModel.ItemsTableName,13);
cancelled.TryCopy(null); cancelTimer.RunWork();
Check(cancelled.MapPreviews.Count == 0, "Cancelled queued query must not publish previews.");
var duringTimer = new ManualWorkTimer();
var during = new MapOptionsArrayElementViewModel(InstantDispatch.Instance,duringTimer,mapper,HardcodeTablesModel.ItemsTableName,13);
during.MapPreviews.CollectionChanged += (_,_) => during.TryCopy(null);
duringTimer.RunWork();
Check(during.MapPreviews.Count == 1, "Cancellation during publication must stop even when the next event shares a cached positive root.");
Check(Keys(Query(13)).SequenceEqual(all), "Cancellation must not contaminate another query.");

var metadata = m.ExportMetadata(null,viewport.Singletons.MetadataInfo);
var loaded = baseline.ToArray(); loaded[0x701] = 16;
m.Load(loaded,metadata);
Check(Keys(Query(16)).SequenceEqual(Expected(0x300,0x30A,0x314,0x400)), "Reload must see new script bytes.");
Check(Keys(Query(13)).SequenceEqual(Expected(0x500)), "Reload must discard outcomes from the old model contents.");
m.Load(baseline.ToArray(),metadata);
var restored = Query(13);
Check(Keys(restored).SequenceEqual(all), "Restored model must recover exact ordered event results.");
Check(pixels.Zip(restored.MapPreviews,(expected,button) => expected.SequenceEqual(button.Image.PixelData)).All(equal => equal), "Restored query thumbnail pixels differ.");
Check(baseline.SequenceEqual(m.RawData), "Final model bytes differ from the baseline.");
// A long shared nonmatching script isolates repeated parsing without thumbnail costs.
var longScript = Enumerable.Range(0,200).SelectMany(_ => new byte[]{0x44,13,0,1,0}).Concat(new byte[]{2}).ToArray();
new ModelDelta().ChangeData(m,0x12000,longScript);
foreach(var pointer in new[]{0x304,0x30E,0x318,0x404}) m.WritePointer(new ModelDelta(),pointer,0x12000);
Query(50); Query(50); Query(50);
var allocationStart = GC.GetAllocatedBytesForCurrentThread();
var negative = Query(50);
var allocated = GC.GetAllocatedBytesForCurrentThread()-allocationStart;
Console.WriteLine($"NEGATIVE_QUERY_ALLOCATED:{allocated}");
Check(negative.MapPreviews.Count == 0, "Long shared script must retain negative results.");
Check(allocated < 2000000, $"Long shared negative query allocated {allocated:N0} bytes; budget is 2,000,000.");
Console.WriteLine("PASS: shared scripts across maps, positive/negative/hidden matches, enum isolation, branches/cycles/malformed roots, script/pointer edits, reload, cancellation, and exact-byte undo/redo.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
