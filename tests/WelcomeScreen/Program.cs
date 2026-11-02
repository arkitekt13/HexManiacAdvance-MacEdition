using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem()
   .UseWindowingSubsystem(() => { }, "offscreen").UseSkia().WithInterFont().SetupWithoutStarting();
void Check(bool condition, string message) {
   if (!condition) throw new InvalidOperationException(message);
}
var calls = 0;
var command = new StubCommand { CanExecute = _ => true, Execute = _ => calls++ };
var welcome = new StartScreen { DataContext = new WelcomeModel(command) };
var tabs = new ItemsControl { ItemsSource = new ObservableCollection<object>() };
var items = (ObservableCollection<object>)tabs.ItemsSource;
welcome.Bind(Visual.IsVisibleProperty, new Binding("ItemCount") {
   Source = tabs, Converter = new EqualityToBooleanConverter(), ConverterParameter = "0",
});
var root = new Grid();
root.Children.Add(welcome);
void Layout(int width, int height) {
   for (int pass = 0; pass < 6; pass++) {
      foreach (var control in root.GetVisualDescendants().Prepend(root).OfType<Control>().ToArray()) {
         control.ApplyStyling();
         control.ApplyTemplate();
         if (control is ContentPresenter presenter) presenter.UpdateChild();
         control.InvalidateMeasure();
         control.InvalidateArrange();
      }
   }
   Dispatcher.UIThread.RunJobs();
   root.Measure(new Size(width, height));
   root.Arrange(new Rect(0, 0, width, height));
}
foreach (var size in new[] { new Size(1200, 800), new Size(800, 600), new Size(500, 400) }) {
   Layout((int)size.Width, (int)size.Height);
   foreach (var name in new[] { "WelcomeTitle", "MacSubtitle", "OpenFileButton", "VersionInformation" }) {
      var control = welcome.FindControl<Control>(name)!;
      var bounds = new Rect(control.Bounds.Size).TransformToAABB(control.TransformToVisual(root)!.Value);
      Check(bounds.Width > 0 && bounds.Height > 0 && bounds.X >= 0 && bounds.Y >= 0 && bounds.Right <= size.Width + .01 && bounds.Bottom <= size.Height + .01,
         $"Welcome element clipped at {size}: {name} {bounds}");
   }
}
var open = welcome.FindControl<Button>("OpenFileButton")!;
Layout(1200, 800);
var surface = open.GetVisualDescendants().OfType<Avalonia.Controls.Border>().Single(b => b.Name == "WelcomeButtonSurface");
Check(surface.Bounds.Size == open.Bounds.Size && surface.CornerRadius.TopLeft == 9,
   "The rounded hover surface must fill the entire button.");
var states = (IPseudoClasses)open.Classes;
Color SurfaceColor() => ((ISolidColorBrush)surface.Background!).Color;
Check(SurfaceColor() == Color.Parse("#9970CE"), "Unexpected normal button background.");
states.Set(":pointerover", true);
Layout(1200, 800);
Check(SurfaceColor() == Color.Parse("#AE86E1"), "Hover must recolor the full button surface.");
Check(open.GetVisualDescendants().OfType<ContentPresenter>().All(p => p.Background == null || p.Background == Brushes.Transparent),
   "Hover must not add a smaller colored rectangle behind the label.");
states.Set(":pressed", true);
Layout(1200, 800);
Check(SurfaceColor() == Color.Parse("#805AB5"), "Pressed state must recolor the full button surface.");
states.Set(":pressed", false);
states.Set(":pointerover", false);
Layout(1200, 800);
Check(SurfaceColor() == Color.Parse("#9970CE"), "Pointer exit must restore the normal background.");
states.Set(":focus-visible", true);
Layout(1200, 800);
Check(((ISolidColorBrush)surface.BorderBrush!).Color == Color.Parse("#E2CAFF"), "Keyboard focus must outline the full button.");
states.Set(":focus-visible", false);
Console.WriteLine("PASS: full-size rounded button surface changes for hover and press, without a separate label highlight.");
Check(ReferenceEquals(open.Command, command), "Open File must use the existing editor command.");
open.Command!.Execute(null);
Check(calls == 1, "Open File must execute the bound command.");
Check(welcome.IsVisible, "Welcome must be shown with no tabs.");
items.Add(new object());
Check(!welcome.IsVisible, "Welcome must hide when a tab opens.");
items.Clear();
Check(welcome.IsVisible, "Welcome must return after the last tab closes.");
welcome.DataContext = new WelcomeModel(command, true, "Could not open the ROM.", command);
Layout(500, 400);
Check(welcome.FindControl<Avalonia.Controls.Border>("WelcomeError")!.IsVisible,
   "Open failures must remain visible while welcome covers the editor workspace.");
welcome.DataContext = new WelcomeModel(command);
var version = typeof(StartScreen).Assembly.GetName().Version!.ToString(3);
Check(welcome.FindControl<TextBlock>("VersionInformation")!.Text == StartScreen.VersionLabel && StartScreen.VersionLabel.Contains(version),
   "Version label must report the actual application assembly version.");
var shellPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/HexManiac.Avalonia/Windows/MainWindow.axaml"));
var shell = XDocument.Load(shellPath);
var mount = shell.Descendants().Single(e => (string)e.Attribute("Name") == "WelcomeScreen");
Check(mount.Name.LocalName == "StartScreen" && ((string)mount.Attribute("IsVisible"))!.Contains("ItemCount, ElementName=Tabs"),
   "The production shell must mount welcome with tab-count visibility.");
Console.WriteLine($"PASS: welcome layout fits three window sizes; Open command, zero-tab lifecycle, shell mounting, and version label ({version}) are correct.");

// Optional offscreen preview for visual inspection, using the actual compiled control.
if (args.Length == 1) {
   Layout(1200, 800);
   using var bitmap = new RenderTargetBitmap(new PixelSize(1200, 800));
   void Render(Visual visual, DrawingContext context) {
      if (!visual.IsVisible) return;
      using var opacity = context.PushOpacity(visual.Opacity);
      if (visual is Control control) control.Render(context);
      foreach (var child in visual.GetVisualChildren()) {
         var transform = child.TransformToVisual(visual);
         if (transform == null) continue;
         using var pushed = context.PushTransform(transform.Value);
         Render(child, context);
      }
   }
   using (var context = bitmap.CreateDrawingContext()) Render(root, context);
   bitmap.Save(args[0]);
}
public record WelcomeModel(ICommand Open, bool ShowError = false, string ErrorMessage = "", ICommand ClearError = null);
