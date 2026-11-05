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
var checks = 0;
void Check(bool condition, string message) {
   checks++;
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
// The control owns an independent clock: dispatcher wall-clock pulses cannot finish
// the animation between our explicit ticks. Reflection stays in this pinned test adapter.
const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
var clockType = typeof(Animation).Assembly.GetType("Avalonia.Animation.ClockBase")!;
var pulse = clockType.GetMethod("Pulse", flags)!;
var clockProperty = (AvaloniaProperty)typeof(Animatable).GetField("ClockProperty", flags)!.GetValue(null)!;
foreach (int delay in new[] { 0, 35 }) {
   for (int repeat = 0; repeat < 3; repeat++) {
      var clock = Activator.CreateInstance(clockType, nonPublic: true)!;
      pulse.Invoke(clock, new object[] { TimeSpan.Zero });
      var outline = new Border();
      outline.SetValue(clockProperty, clock);
      var task = FocusOutlineAnimation.RunAsync(outline, new Size(800, 600),
         new Rect(500, 200, 100, 80), TimeSpan.FromMilliseconds(100));
      Check(outline.IsVisible && outline.RenderTransform is TranslateTransform, "Outline should begin visible with a transform.");
      var sawMovement = false;
      double? firstX = null, firstWidth = null;
      for (int tick = 0; tick <= 8; tick++) {
         pulse.Invoke(clock, new object[] { TimeSpan.FromMilliseconds(tick * 25) });
         if (delay != 0) Thread.Sleep(delay); // stress dispatcher timing, not animation time
         Dispatcher.UIThread.RunJobs();
         if (outline.RenderTransform is TranslateTransform transform && outline.Width > 0) {
            firstX ??= transform.X;
            firstWidth ??= outline.Width;
            if (transform.X != firstX && outline.Width != firstWidth) sawMovement = true;
         }
      }
      Check(task.IsCompleted, "Focus animation did not complete.");
      task.GetAwaiter().GetResult();
      Check(sawMovement, "Focus animation must actually change position and size.");
      Check(!outline.IsVisible && outline.RenderTransform == null, "Outline must be hidden and its transform cleared on completion.");
   }
}
Console.WriteLine("PASS: reproduced the reported crash; six real animations moved, resized, completed and cleaned up under normal and deliberately delayed dispatcher timing.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
