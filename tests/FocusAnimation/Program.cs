using System.Reflection;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem()
   .UseWindowingSubsystem(() => { }, "offscreen").UseSkia().WithInterFont().SetupWithoutStarting();
void Check(bool condition, string message) {
   if (!condition) throw new InvalidOperationException(message);
}
var oldAnimation = new Animation {
   Duration = TimeSpan.FromMilliseconds(30),
   Children = {
      new KeyFrame { Cue = new Cue(0), Setters = { new Setter(TranslateTransform.XProperty, 0d) } },
      new KeyFrame { Cue = new Cue(1), Setters = { new Setter(TranslateTransform.XProperty, 50d) } },
   },
};
var reproduced = false;
try { oldAnimation.RunAsync(new TranslateTransform()).GetAwaiter().GetResult(); }
catch (InvalidCastException) { reproduced = true; }
Check(reproduced, "Expected the reported Transform-to-Visual cast failure.");
// No native window means no compositor ticks; drive Avalonia 11.2.3's real clock explicitly.
// Reflection is confined to this check because MediaContext and IClock are internal APIs.
const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
var contextType = typeof(Animation).Assembly.GetType("Avalonia.Media.MediaContext")!;
var context = contextType.GetProperty("Instance", flags)!.GetValue(null);
var clock = contextType.GetProperty("Clock", flags)!.GetValue(context)!;
var pulse = clock.GetType().GetMethod("Pulse", flags)!;
var pulseNew = clock.GetType().GetMethod("PulseNewSubscriptions", flags)!;
for (int repeat = 0; repeat < 3; repeat++) {
   pulse.Invoke(clock, new object[] { TimeSpan.FromMilliseconds(repeat * 200) });
   var outline = new Border();
   var task = FocusOutlineAnimation.RunAsync(outline, new Size(800, 600),
      new Rect(500, 200, 100, 80), TimeSpan.FromMilliseconds(100));
   Check(outline.IsVisible && outline.RenderTransform is TranslateTransform, "Outline should begin visible with a transform.");
   var sawMovement = false;
   pulseNew.Invoke(clock, null);
   for (int tick = 0; tick <= 8; tick++) {
      pulse.Invoke(clock, new object[] { TimeSpan.FromMilliseconds(repeat * 200 + tick * 25) });
      Dispatcher.UIThread.RunJobs();
      if (outline.RenderTransform is TranslateTransform transform && transform.X != 0 && outline.Width > 0)
         sawMovement = true;
   }
   Check(task.IsCompleted, "Focus animation did not complete.");
   task.GetAwaiter().GetResult();
   Check(sawMovement, "Focus animation must actually change position and size.");
   Check(!outline.IsVisible && outline.RenderTransform == null, "Outline must be hidden and its transform cleared on completion.");
}
Console.WriteLine("PASS: reproduced the reported crash; three real focus animations moved, resized, completed, and cleaned up without a cast failure.");
