using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext()); Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
var errors = new List<Exception>();
Dispatcher.UIThread.UnhandledException += (_, e) => { errors.Add(e.Exception); e.Handled = true; };
var fs = new StubFileSystem { MetadataFor = _ => Array.Empty<string>() };
Application.Current!.Resources["FileSystem"] = fs;
var editor = new EditorViewModel(fs, InstantDispatch.Instance); App.UpdateThemeDictionary(Application.Current.Resources, editor.Theme);
var fixture = new SyntheticMapFixture(36, 36, withNpcText: true);
var view = fixture.ViewPort; var model = fixture.Model;
model.SetList(new NoDataChangeDeltaModel(), "FacingOptions", "NoMovement");
view.InitializationWorkload.GetAwaiter().GetResult(); editor.Add(view);
// A real bound Save button reproduces the crash report's command-notification subscriber.
var save = new Button { Command = editor.Save };
var image = new PixelImage(); var panel = new StackPanel(); panel.Children.Add(save); panel.Children.Add(image);
var window = new Window { Width = 700, Height = 700, Content = panel }; window.Show();
void Pump() { Dispatcher.UIThread.RunJobs(); window.Measure(new Size(700, 700)); window.Arrange(new Rect(0, 0, 700, 700)); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
void Until(Func<bool> ready) {
   var timer = Stopwatch.StartNew();
   while (!ready()) { Pump(); if (timer.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Queued map work did not settle."); Thread.Yield(); }
   Pump();
}
Pump();
view.ChangeHistory.ClearHistory(false); var bytes = model.RawData.ToArray();
Check(model.GetNextRun(0x19000) is not PCSRun, "NPC text starts unformatted, forcing the lazy discovery path from the report.");
int offThreadNotifications = 0, notifications = 0;
int offThreadHistoryChanges = 0;
view.ChangeHistory.PropertyChanged += (_, _) => { if (!Dispatcher.UIThread.CheckAccess()) offThreadHistoryChanges++; };
editor.Save.CanExecuteChanged += (_, _) => { notifications++; if (!Dispatcher.UIThread.CheckAccess()) offThreadNotifications++; };
fixture.Map.ShowEvents = MapDisplayOptions.AllEvents;
var clock = Stopwatch.StartNew(); image.DataContext = fixture.Map;
while (image.Source is not WriteableBitmap && errors.Count == 0) {
   Pump(); if (clock.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Map rendering did not settle.");
   Thread.Yield();
}
Check(errors.Count == 0, "Lazy NPC discovery must not raise the invalid-thread UI exception: " + string.Join(" | ", errors.Select(e => e.ToString())));
Check(image.Source is WriteableBitmap, "The event-bearing map must finish rendering.");
Check(model.GetNextRun(0x19000) is PCSRun, "The real event-discovery path annotates NPC text.");
Check(notifications > 0 && offThreadNotifications == 0 && offThreadHistoryChanges == 0,
   "Metadata/history changes and Save-state notifications stay on the UI thread.");
Check(bytes.SequenceEqual(model.RawData), "Discovery/rendering cannot change ROM bytes.");
Console.WriteLine($"INITIAL_RENDER_MS:{clock.Elapsed.TotalMilliseconds:F1}");
Check(view.IsMetadataOnlyChange, "Lazy format discovery retains its existing metadata-only history semantics.");
var annotatedMetadata = model.ExportMetadata(null, view.Singletons.MetadataInfo).Serialize();
view.ChangeHistory.ChangeCompleted();
int uploads = 0, offThreadUploads = 0;
image.PropertyChanged += (_, e) => {
   if (e.Property != Image.SourceProperty) return;
   uploads++; if (!Dispatcher.UIThread.CheckAccess()) offThreadUploads++;
};
for (int i = 0; i < 20; i++) image.UpdateSource();
Pump();
Check(uploads == 1, "A burst of map refresh requests coalesces to one bitmap publication.");
Check(annotatedMetadata.SequenceEqual(model.ExportMetadata(null, view.Singletons.MetadataInfo).Serialize()), "Repeated rendering cannot add duplicate metadata changes.");
var source = image.Source;
fixture.Map.DrawBlock(view.CurrentChange, 0, -1, 2, 1); view.ChangeHistory.ChangeCompleted();
Until(() => !ReferenceEquals(source, image.Source));
var painted = model.RawData.ToArray();
Check(!bytes.SequenceEqual(painted), "Painting still edits the intended map data.");
view.Undo.Execute(null); fixture.Map.ClearCaches(); Pump();
Check(bytes.SequenceEqual(model.RawData), "Undo after discovery restores all original map bytes.");
Check(annotatedMetadata.SequenceEqual(model.ExportMetadata(null, view.Singletons.MetadataInfo).Serialize()), "Undo of painting retains discovered text metadata.");
view.Redo.Execute(null); fixture.Map.ClearCaches(); Pump();
Check(painted.SequenceEqual(model.RawData), "Redo restores the painted map.");
Task.Run(image.UpdateSource).GetAwaiter().GetResult(); Pump();
Check(errors.Count == 0 && offThreadUploads == 0 && offThreadNotifications == 0, "Worker refresh requests publish model/UI results only on the UI thread.");

var abandoned = new SyntheticMapFixture(36, 36, withNpcText: true);
abandoned.Model.SetList(new NoDataChangeDeltaModel(), "FacingOptions", "NoMovement");
abandoned.Map.ShowEvents = MapDisplayOptions.AllEvents;
image.DataContext = abandoned.Map;
image.DataContext = new HavenSoft.HexManiac.Core.ViewModels.Images.ReadonlyPixelViewModel(1, 1, new short[] { 1 }, -1);
Pump();
Check(image.Source is WriteableBitmap small && small.PixelSize == new PixelSize(1, 1), "A newer data context wins over an older queued map render.");
Check(abandoned.Model.GetNextRun(0x19000) is not PCSRun && abandoned.ViewPort.ChangeHistory.IsSaved,
   "A discarded map request cannot lazily mutate the abandoned ROM model.");
image.DataContext = abandoned.Map; image.DataContext = null; Pump();
Check(abandoned.Model.GetNextRun(0x19000) is not PCSRun, "Clearing the image cancels queued model preparation.");

// Cold, larger maps still use a deferred UI pass rather than blocking the context setter.
var large = new SyntheticMapFixture(96, 64, withNpcText: true);
large.Model.SetList(new NoDataChangeDeltaModel(), "FacingOptions", "NoMovement"); large.Map.ShowEvents = MapDisplayOptions.AllEvents;
bool heartbeat = false, heartbeatBeforePublish = false;
image.PropertyChanged += (_, e) => { if (e.Property == Image.SourceProperty) heartbeatBeforePublish = heartbeat; };
Dispatcher.UIThread.Post(() => heartbeat = true, DispatcherPriority.Input);
clock.Restart(); source = image.Source; image.DataContext = large.Map;
Check(large.Model.GetNextRun(0x19000) is not PCSRun, "Assigning a map queues, rather than immediately performing, lazy preparation.");
Until(() => !ReferenceEquals(source, image.Source));
Check(heartbeatBeforePublish && large.Model.GetNextRun(0x19000) is PCSRun, "Higher-priority UI work runs before the deferred cold map render.");
Check(errors.Count == 0 && offThreadUploads == 0, "Large-map preparation also avoids invalid-thread failures.");
Console.WriteLine($"LARGE_COLD_RENDER_MS:{clock.Elapsed.TotalMilliseconds:F1}");
var closing = new SyntheticMapFixture(36, 36, withNpcText: true);
closing.Model.SetList(new NoDataChangeDeltaModel(), "FacingOptions", "NoMovement"); closing.Map.ShowEvents = MapDisplayOptions.AllEvents;
image.DataContext = closing.Map;
window.Close();
Dispatcher.UIThread.RunJobs();
Check(closing.Model.GetNextRun(0x19000) is not PCSRun, "Detaching a closing map cancels pending model preparation.");
panel.Children.Remove(image); window = new Window { Width = 700, Height = 700, Content = image }; window.Show();
Until(() => closing.Model.GetNextRun(0x19000) is PCSRun);
Check(errors.Count == 0 && image.Source is WriteableBitmap, "Reattaching resumes a current safe render.");
window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
