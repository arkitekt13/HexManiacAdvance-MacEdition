using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Xml.Linq;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.ViewModels.Map;

// Test the real tutorial control's geometry without a window, ROM or native input device.
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem()
   .UseWindowingSubsystem(() => { }, "offscreen").UseSkia().WithInterFont().SetupWithoutStarting();

var checks = 0;
void Check(bool condition, string message) {
   checks++;
   if (!condition) throw new InvalidOperationException(message);
}
string ExpectedDescription(MapTutorialViewModel tutorial) => tutorial.Title == "Paint Blocks"
   ? "Select one block, then double-click the map to fill an area. Multiple-block selections cannot fill."
   : OperatingSystem.IsMacOS() ? tutorial.Content.Replace("Control", "Command (⌘)") : tutorial.Content;

foreach (var hostWidth in new[] { 800, 500 }) {
   var vm = new MapTutorialsViewModel();
   var tutorial = new TutorialProbe {
      DataContext = vm, FontSize = 12,
      HorizontalAlignment = HorizontalAlignment.Right,
      VerticalAlignment = VerticalAlignment.Center,
   };
   var root = new Grid { Width = hostWidth, Height = 600, ClipToBounds = true };
   root.Children.Add(tutorial);
   var cards = tutorial.FindControl<ItemsControl>("Tutorials");
   // A window normally applies the stock ItemsControl template; install its presenter
   // explicitly here, while retaining the production item template and Canvas panel.
   cards.Template = new FuncControlTemplate<ItemsControl>((control, scope) => {
      var presenter = new ItemsPresenter { Name = "PART_ItemsPresenter", ItemsPanel = control.ItemsPanel };
      scope.Register(presenter.Name, presenter);
      return presenter;
   });

   void Layout() {
      // Materialize templates and run layout explicitly because there is no window/layout root.
      for (int pass = 0; pass < 8; pass++) {
         foreach (var control in root.GetVisualDescendants().Prepend(root).OfType<Control>().ToArray()) {
            control.ApplyStyling();
            control.ApplyTemplate();
            if (control is ContentPresenter presenter) presenter.UpdateChild();
            control.InvalidateMeasure();
            control.InvalidateArrange();
         }
      }
      Dispatcher.UIThread.RunJobs();
      root.Measure(new Size(hostWidth, 600));
      root.Arrange(new Rect(0, 0, hostWidth, 600));
   }

   Layout();
   var viewport = tutorial.FindControl<Avalonia.Controls.Border>("CardsViewport");
   Check(tutorial.Bounds.Width == 72 && viewport.Bounds.Width == 72 && tutorial.Bounds.Height == 400,
      "Collapsed cards must occupy only the icon strip.");

   for (int cycle = 0; cycle < 3; cycle++) {
      tutorial.Hover();
      Layout();
      Check(Math.Abs(tutorial.Bounds.Right - hostWidth) < .01,
         "Expanded panel must stay anchored to the host's right edge.");
      Check(tutorial.Bounds.Width == 320 && viewport.Bounds.Width >= cards.Bounds.Width,
         "The full cards must fit within both the hover area and clipping viewport.");
      Check(Math.Abs(cards.Bounds.X) < .01, "Cards must not slide outside the viewport.");
      foreach (var item in vm.Tutorials.Take(MapTutorialsViewModel.VisibleCount)) {
         var container = cards.ContainerFromItem(item) as Control;
         Check(container != null, "Tutorial cards must be realized.");
         var description = container.GetVisualDescendants().OfType<TextBlock>()
            .Single(block => block.Text == ExpectedDescription(item));
         var position = description.TranslatePoint(new Point(), viewport);
         Check(position.HasValue && new Rect(viewport.Bounds.Size)
            .Contains(new Rect(position.Value, description.Bounds.Size)),
            "Description must remain fully inside the expanded viewport.");
      }
      foreach (var item in vm.Tutorials) {
         var container = (Control)cards.ContainerFromItem(item);
         var description = container.GetVisualDescendants().OfType<TextBlock>()
            .Single(block => block.Text == ExpectedDescription(item));
         Check(description.TextLayout.Height <= description.Bounds.Height + .01,
            $"Compact card must fit the full description: {item.Title}.");
         if (item.Icon == "CtrlKey") {
            var icon = container.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().First();
            var expected = HavenSoft.HexManiac.AvaloniaUI.Resources.IconExtension.GetIcon(
               OperatingSystem.IsMacOS() ? "CommandKey" : "CtrlKey");
            Check(icon.Data != null && expected != null && icon.Data.Bounds == expected.Bounds,
               "Modifier tutorial icons must match the platform's gesture.");
            Check(item.Content.Contains("Control"), "Display translation must not mutate shared Core tutorial records.");
         }
      }
      tutorial.Leave();
      Layout();
      Check(tutorial.Bounds.Width == 72 && viewport.Bounds.Width == 72,
         "Leaving the expanded hover area must collapse it again.");
   }
}
Console.WriteLine("PASS: repeated hover expansion/collapse stays right-anchored at two host widths, with all five visible descriptions inside the viewport.");

// Verify the production stacking configuration without constructing a native map window.
var mapMarkupPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
   "../../../../../src/HexManiac.Avalonia/Controls/MapTab.axaml"));
var markup = XDocument.Load(mapMarkupPath);
XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
var tutorialMarkup = markup.Descendants().Single(node => (string)node.Attribute(xaml + "Name") == "TutorialsControl");
var arrowsMarkup = markup.Descendants().Single(node => (string)node.Attribute("Name") == "MapButtons");
Check(int.Parse(tutorialMarkup.Attribute("ZIndex").Value) > int.Parse(arrowsMarkup.Attribute("ZIndex").Value),
   "Tutorials must draw and receive input above map arrows.");

var mapSurface = new Grid();
var mapImage = new Avalonia.Controls.Border();
mapSurface.Children.Add(mapImage);
var overlay = new TutorialControl();
var previewController = new MapHoverPreview(mapSurface);
Check(previewController.OwnsSource(mapSurface) && previewController.OwnsSource(mapImage)
   && !previewController.OwnsSource(overlay), "Only the map surface and its children may trigger previews.");
var preview = new object[] { "NPC dialogue preview" };
previewController.Update(preview);
Check(ToolTip.GetTip(mapSurface) is HexContentToolTip, "NPC preview must belong to the map surface.");
previewController.Hide();
Check(!ToolTip.GetIsOpen(mapSurface) && ToolTip.GetTip(mapSurface) == null,
   "Leaving the map must close and remove the NPC tooltip.");
previewController.Update(null);
Check(ToolTip.GetTip(mapSurface) is HexContentToolTip tip && ReferenceEquals(tip.DataContext, preview),
   "Re-entering the same NPC cell must recover the cached preview.");
previewController.Update(Array.Empty<object>());
previewController.Update(null);
Check(ToolTip.GetTip(mapSurface) == null, "An empty map cell must clear cached NPC content.");
Console.WriteLine("PASS: tutorial stacking configuration, map-only preview sources, dismissal, same-NPC re-entry, and empty-cell clearing.");

Console.WriteLine($"CHECKS_PASSED:{checks}");
class TutorialProbe : TutorialControl {
   private PointerEventArgs Args(Avalonia.Interactivity.RoutedEvent routedEvent) =>
      new(routedEvent, this, new Pointer(1, PointerType.Mouse, true), null,
         new Point(), 0, new PointerPointProperties(), KeyModifiers.None);

   // Call the same handlers as native pointer entry/exit, without a desktop input root.
   public void Hover() => OnPointerEntered(Args(PointerEnteredEvent));
   public void Leave() => OnPointerExited(Args(PointerExitedEvent));
}

