using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>Moves and sizes the shell's attention outline on a single animation clock.</summary>
public static class FocusOutlineAnimation {
   public static async Task RunAsync(Control outline, Size container, Rect target, TimeSpan duration) {
      var endX = -container.Width + target.Right;
      outline.RenderTransform = new TranslateTransform();
      outline.IsVisible = true;
      var animation = new Animation {
         Duration = duration,
         Easing = new CubicEaseOut(),
         Children = {
            new KeyFrame { Cue = new Cue(0d), Setters = {
               new Setter(TranslateTransform.XProperty, endX / 2),
               new Setter(TranslateTransform.YProperty, target.Y / 2),
               new Setter(Control.WidthProperty, container.Width / 2),
               new Setter(Control.HeightProperty, container.Height / 2),
            } },
            new KeyFrame { Cue = new Cue(1d), Setters = {
               new Setter(TranslateTransform.XProperty, endX),
               new Setter(TranslateTransform.YProperty, target.Y),
               new Setter(Control.WidthProperty, target.Width),
               new Setter(Control.HeightProperty, target.Height),
            } },
         },
      };
      try {
         // Avalonia's TransformAnimator expects the Visual that owns RenderTransform,
         // whereas WPF's BeginAnimation accepted the TranslateTransform directly.
         await animation.RunAsync(outline);
      } finally {
         outline.IsVisible = false;
         outline.RenderTransform = null;
      }
   }
}
