using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions {UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException(message);}
var fixture=new SyntheticMapFixture(7,5);
RenderBounds.Run(Check,fixture.ViewPort.Singletons);
var bytes=fixture.Model.RawData.ToArray();
var metadata=fixture.Model.ExportMetadata(null,fixture.ViewPort.Singletons.MetadataInfo).Serialize();
var view=new MapTab();
var window=new Window{Width=1000,Height=650,Content=view};window.Show();
void Pump(){Dispatcher.UIThread.RunJobs();window.Measure(new Size(window.Width,window.Height));window.Arrange(new Rect(0,0,window.Width,window.Height));Dispatcher.UIThread.RunJobs();}
var panel=view.FindControl<ScrollViewer>("HeaderPanel")!;panel.IsVisible=true;Pump();
var layer=view.FindControl<Canvas>("BlocksetPreviewLayer")!;
var box=view.FindControl<Viewbox>("BlocksetPreviewBox")!;
var image=view.FindControl<PixelImage>("BlocksetPreviewImage")!;
void Enter(Control row)=>row.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent,row,new Pointer(1,PointerType.Mouse,true),null,default,0,new PointerPointProperties(),KeyModifiers.None));
foreach(var name in new[]{"PrimaryBlocksetSelector","SecondaryBlocksetSelector"}) {
 var combo=view.FindControl<AngleComboBox>(name)!;
 var options=new[]{new BlocksetOption(fixture.Model,0x180),new BlocksetOption(fixture.Model,0x1A0)};
 combo.ItemsSource=options;combo.SelectedIndex=0;combo.IsDropDownOpen=true;Pump();
 Control Row(int index)=>combo.ContainerFromIndex(index)!.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.DataContext is BlocksetOption);
 var row=Row(0);
 Check(ToolTip.GetTip(row)==null,"Blockset rows must not open a native image tooltip.");
 Enter(row);Pump();
 Check(box.IsVisible&&ReferenceEquals(image.DataContext,options[0].Render),"Hover must show the current option's image.");
 Check(box.Bounds.Left>layer.PointToClient(row.PointToScreen(new Point(row.Bounds.Width,0))).X,"Preview must appear beyond the dropdown row, not underneath it.");
 Check(!layer.IsHitTestVisible&&ReferenceEquals(TopLevel.GetTopLevel(box),window),"Preview must be input-transparent and remain in the map window.");
 Check(box.Bounds.Height<=layer.Bounds.Height-8+.01&&box.Bounds.Width<=layer.Bounds.Width-8+.01,"Tall previews must fit within the map area.");
 for(int i=0;i<8;i++){Enter(Row(i%2));Pump();Check(ReferenceEquals(image.DataContext,options[i%2].Render),"Rapid row changes must never retain another option's preview.");}
 combo.IsDropDownOpen=false;Pump();Check(!box.IsVisible&&image.DataContext==null,"Closing the dropdown must release the preview.");
 combo.IsDropDownOpen=true;Pump();row=Row(0);Enter(row);Pump();
 combo.SelectedIndex=1;Pump();Check(!box.IsVisible,"Selection changes must dismiss the preview.");
 combo.IsDropDownOpen=true;Pump();row=Row(0);Enter(row);Pump();row.DataContext=options[1];Pump();
 Check(!box.IsVisible,"Recycling a row context must dismiss its old image.");
 combo.IsDropDownOpen=false;Pump();combo.IsDropDownOpen=true;Pump();Enter(Row(0));Pump();
 panel.IsVisible=false;Pump();Check(!box.IsVisible,"Closing the header must dismiss blockset previews.");
 panel.IsVisible=true;combo.IsDropDownOpen=false;Pump();
}
// Keep explicit tall-image coverage even after render-length correction makes short sets tiny.
var bounded=new BlocksetHoverPreview(layer,box,image);
bounded.Show(layer,new CanvasPixelViewModel(128,4096));Pump();
Check(box.IsVisible&&box.Bounds.Height<=layer.Bounds.Height-8+.01,"A very tall image must be uniformly fitted, not cropped by a popup.");
window.Width=700;window.Height=400;Pump();
Check(box.Bounds.Height<=layer.Bounds.Height-8+.01&&box.Bounds.Width<=layer.Bounds.Width-8+.01,"Visible previews must refit when the window shrinks.");
Check(image.Source!=null,"A fitted tall image must still have a rendered bitmap.");
bounded.Hide();Check(!box.IsVisible&&image.DataContext==null,"Dismissal releases the preview image.");
Check(bytes.SequenceEqual(fixture.Model.RawData)&&metadata.SequenceEqual(fixture.Model.ExportMetadata(null,fixture.ViewPort.Singletons.MetadataInfo).Serialize())&&!fixture.ViewPort.Undo.CanExecute(null),"Preview interactions must preserve ROM bytes, metadata and history.");
window.Content=null;Pump();Check(!box.IsVisible,"Detaching the map view must dismiss the preview.");window.Close();
Console.WriteLine($"CHECKS_PASSED:{checks}");
