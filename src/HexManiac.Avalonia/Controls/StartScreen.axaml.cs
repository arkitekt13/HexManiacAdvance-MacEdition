using System.Diagnostics;
using System;
using System.IO;
using System.Buffers.Binary;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>The empty editor's welcome screen; opening a ROM uses the normal editor command.</summary>
public partial class StartScreen : Grid {
   private Bitmap background;
   private readonly string backgroundPath;
   public static string VersionLabel => AppIdentity.VersionLabel;

   public StartScreen() : this(Path.Combine(AppContext.BaseDirectory, "resources", "Optional", "Welcome", "background.png")) { }

   public StartScreen(string backgroundPath) {
      this.backgroundPath = backgroundPath;
      InitializeComponent();
      VersionInformation.Text = VersionLabel;
   }

   protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
      base.OnAttachedToVisualTree(e);
      background = LoadBackground(backgroundPath);
      WelcomeBackgroundImage.Source = background;
   }

   protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
      WelcomeBackgroundImage.Source = null;
      background?.Dispose();
      background = null;
      base.OnDetachedFromVisualTree(e);
   }

   public static Bitmap LoadBackground(string path) {
      try {
         if (!File.Exists(path) || new FileInfo(path).Length > 32 * 1024 * 1024) return null;
         using var stream = File.OpenRead(path);
         Span<byte> header = stackalloc byte[24];
         stream.ReadExactly(header);
         if (!header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return null;
         var width = BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
         var height = BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
         if (width <= 0 || height <= 0 || (long)width * height > 32_000_000) return null;
         stream.Position = 0;
         return Bitmap.DecodeToWidth(stream, Math.Min(width, 1600));
      } catch (Exception ex) when (ex is not OutOfMemoryException) {
         // Decoration must never prevent opening the editor.
         return null;
      }
   }

   private void Navigate(object sender, RoutedEventArgs e) {
      if (sender is Control { Tag: string url }) {
         var start = new ProcessStartInfo("open") { UseShellExecute = false };
         start.ArgumentList.Add(url);
         Process.Start(start);
      }
      e.Handled = true;
   }
}

/// <summary>Measures its child without reserving any space for it.</summary>
public class MeasureEmptyDecorator : Decorator {
   protected override Avalonia.Size MeasureOverride(Avalonia.Size constraint) {
      Child?.Measure(default);
      return default;
   }
}
