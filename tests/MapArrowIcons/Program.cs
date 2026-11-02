using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using HavenSoft.HexManiac.Core.ViewModels;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Resources;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using ShapePath = Avalonia.Controls.Shapes.Path;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem()
   .UseWindowingSubsystem(() => { }, "offscreen").UseSkia().WithInterFont().SetupWithoutStarting();

void Check(bool condition, string message) {
   if (!condition) throw new InvalidOperationException(message);
}

var icons = new Dictionary<MapSliderIcons, string> {
   [MapSliderIcons.LeftRight] = Icons.ArrowsLeftRight,
   [MapSliderIcons.UpDown] = Icons.ArrowsUpDown,
   [MapSliderIcons.ExtendLeft] = Icons.LineArrowLeft,
   [MapSliderIcons.ExtendRight] = Icons.LineArrowRight,
   [MapSliderIcons.ExtendUp] = Icons.LineArrowUp,
   [MapSliderIcons.ExtendDown] = Icons.LineArrowDown,
};

// Native geometry must already exist to reproduce the failure in rotated-resource Clone().
var reproduced = 0;
foreach (var (kind, name) in icons) {
   Check(Application.Current.TryGetResource(IconExtension.ResourcePrefix + name,
      Application.Current.ActualThemeVariant, out var resource) && resource is Geometry,
      $"Missing arrow resource: {name}");
   var original = (Geometry)resource;
   var originalBounds = original.Bounds;
   var originalTransform = original.Transform?.Value;
   if (kind is MapSliderIcons.ExtendLeft or MapSliderIcons.ExtendUp or MapSliderIcons.ExtendDown) {
      try { original.Clone(); } catch (InvalidCastException) { reproduced++; }
   }

   for (int copy = 0; copy < 3; copy++) {
      var geometry = (Geometry)MapSliderIconConverter.Instance.Convert(kind,
         typeof(Geometry), null, CultureInfo.InvariantCulture);
      Check(geometry != null && !ReferenceEquals(original, geometry), "Never hand out a mutable shared icon.");
      Check(geometry.Bounds.Width > 0 && geometry.Bounds.Height > 0, $"Empty geometry: {kind}");
      var path = new ShapePath { Data = geometry, Fill = Brushes.White, Stretch = Stretch.Uniform, Width = 20, Height = 20 };
      path.Measure(new Size(20, 20));
      path.Arrange(new Rect(0, 0, 20, 20));
      using var bitmap = new RenderTargetBitmap(new PixelSize(20, 20));
      using (var context = bitmap.CreateDrawingContext()) path.Render(context);
      var pixels = new byte[20 * 20 * 4];
      var pinned = GCHandle.Alloc(pixels, GCHandleType.Pinned);
      try { bitmap.CopyPixels(new PixelRect(0, 0, 20, 20), pinned.AddrOfPinnedObject(), pixels.Length, 20 * 4); }
      finally { pinned.Free(); }
      Check(Enumerable.Range(0, 400).Count(pixel => pixels[pixel * 4 + 3] > 0) > 20,
         $"Arrow renders blank: {kind}");
      Check(original.Bounds == originalBounds && original.Transform?.Value == originalTransform,
         $"Rendering mutated shared resource: {kind}");
   }
}
Check(reproduced == 3, "Expected the three rotated arrow resources to reproduce the original cloning failure.");
Console.WriteLine("PASS: reproduced all three rotated-icon cloning failures; all six map arrow types render visible pixels across repeated instances without mutating shared resources.");

// Validate the actual map template uses the same theme brush for every arrow direction.
var mapXaml = XDocument.Load(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
   "../../../../../src/HexManiac.Avalonia/Controls/MapTab.axaml")));
var mapButtons = mapXaml.Descendants().Single(e => (string)e.Attribute("Name") == "MapButtons");
var arrow = mapButtons.Descendants().Single(e => e.Name.LocalName == "Path");
Check((string)arrow.Parent!.Attribute("Background") == "{DynamicResource Backlight}", "All arrow buttons must use the same theme background.");
Check((string)arrow.Attribute("Fill") == "{DynamicResource Primary}", "Map arrows must share the main theme foreground.");
ThemeDictionary.Fill(Application.Current.Resources, new Theme(Array.Empty<string>()));
var foreground = ThemeDictionary.Brush("Primary");
var background = ThemeDictionary.Brush("Backlight");
byte[] RenderThemed(MapSliderIcons kind) {
   var geometry = (Geometry)MapSliderIconConverter.Instance.Convert(kind, typeof(Geometry), null, CultureInfo.InvariantCulture);
   var path = new ShapePath { Data = geometry, Fill = foreground, Stretch = Stretch.Uniform, Width = 20, Height = 20 };
   var button = new Avalonia.Controls.Border { Background = background, Child = path, Width = 20, Height = 20 };
   button.Measure(new Size(20, 20));
   button.Arrange(new Rect(0, 0, 20, 20));
   using var bitmap = new RenderTargetBitmap(new PixelSize(20, 20));
   using (var context = bitmap.CreateDrawingContext()) {
      button.Render(context);
      path.Render(context);
   }
   var pixels = new byte[1600];
   var pinned = GCHandle.Alloc(pixels, GCHandleType.Pinned);
   try { bitmap.CopyPixels(new PixelRect(0, 0, 20, 20), pinned.AddrOfPinnedObject(), pixels.Length, 80); }
   finally { pinned.Free(); }
   return pixels;
}
var themed = icons.Keys.ToDictionary(kind => kind, RenderThemed);
var right = themed[MapSliderIcons.ExtendRight];
foreach (var (kind, turns) in new[] { (MapSliderIcons.ExtendDown, 1), (MapSliderIcons.ExtendLeft, 2), (MapSliderIcons.ExtendUp, 3) }) {
   var actual = themed[kind];
   var differences = new List<int>();
   for (int y = 0; y < 20; y++) for (int x = 0; x < 20; x++) {
      var (rx, ry) = (x, y);
      for (int turn = 0; turn < turns; turn++) (rx, ry) = (19 - ry, rx);
      for (int channel = 0; channel < 4; channel++)
         differences.Add(Math.Abs(right[(y * 20 + x) * 4 + channel] - actual[(ry * 20 + rx) * 4 + channel]));
   }
   // Skia rasterizes rotated diagonal edges slightly differently (up to 14 edge pixels here).
   // Require matching colors over 95% of channels and less than 2/255 average error overall.
   Check(differences.Count(d => d > 2) <= differences.Count * .05 && differences.Average() < 2,
      $"Arrow shape or color differs from the rotated right arrow: {kind}");

}
var color = ((ISolidColorBrush)foreground).Color;
Check(color.R == color.G && color.G == color.B, "Default theme test expects a neutral foreground.");
foreach (var (kind, pixels) in themed)
   Check(Enumerable.Range(0, 400).Count(i => pixels[i * 4] == color.R && pixels[i * 4 + 1] == color.G && pixels[i * 4 + 2] == color.B && pixels[i * 4 + 3] == 255) > 20,
      $"Arrow does not render the full theme foreground color: {kind}");
Console.WriteLine("PASS: all six arrows use the theme foreground; rotated arrow shapes and colors match the right arrow within edge antialiasing tolerance.");

var backgroundColor = ((ISolidColorBrush)background).Color;
Check(backgroundColor.A == 255 && backgroundColor.R == backgroundColor.G && backgroundColor.G == backgroundColor.B,
   "Default arrow background must be solid neutral gray.");
foreach (var (kind, pixels) in themed)
   Check(Enumerable.Range(0, 400).Count(i => pixels[i * 4] == backgroundColor.R && pixels[i * 4 + 1] == backgroundColor.G && pixels[i * 4 + 2] == backgroundColor.B && pixels[i * 4 + 3] == 255) > 80,
      $"Arrow button does not render the shared solid background color: {kind}");
Console.WriteLine($"PASS: all six arrow buttons render the same opaque background: {backgroundColor}.");
