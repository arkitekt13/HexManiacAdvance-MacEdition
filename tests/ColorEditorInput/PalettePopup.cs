using Avalonia;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using System.Reflection;
namespace HavenSoft.HexManiac.Review;
public sealed class PalettePopup {
   public PaletteControl Control { get; }
   public Popup Popup { get; }
   public TextBox Code => Field("Color Code (0000 to 7FFF)");
   public TextBox Color => Field("Color Code (000000-FFFFFF or a color word)");
   public TextBox Field(string tip) => Popup.Child!.GetVisualDescendants().OfType<TextBox>().Single(t => Equals(ToolTip.GetTip(t),tip));
   public PalettePopup(SyntheticImageFixture f,int index=1) {
      Control=f.View.FindControl<PaletteControl>("PaletteControl")!;Control.ClosePopup();
      var cells=Control.FindControl<ItemsControl>("ItemsControl")!;
      var point=cells.TranslatePoint(new Avalonia.Point(cells.Bounds.Width/f.Image.Palette.ColorWidth*(index%f.Image.Palette.ColorWidth+.5),cells.Bounds.Height/f.Image.Palette.ColorHeight*(index/f.Image.Palette.ColorWidth+.5)),f.Window)!.Value;
      f.Window.MouseMove(point);f.Window.MouseDown(point,MouseButton.Left);f.Window.MouseUp(point,MouseButton.Left);f.Pump();
      // This code-created popup has its own input root and is not in the XAML name scope.
      Popup=(Popup)typeof(PaletteControl).GetField("swatchPopup",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Control)!;
   }
   public void Type(TextBox box,string text,SyntheticImageFixture f) { box.Focus();box.SelectAll();TopLevel.GetTopLevel(box)!.KeyTextInput(text);f.Pump(); }
}
