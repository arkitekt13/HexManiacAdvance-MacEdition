using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();
var fs=new StubFileSystem();Application.Current.Resources["FileSystem"]=fs;var app=new EditorViewModel(fs,InstantDispatch.Instance);App.UpdateThemeDictionary(Application.Current.Resources,app.Theme);
int checks=0;void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
var view=new MapTab();var panel=view.FindControl<DockPanel>("BlockPanel");var header=panel.Children.OfType<Grid>().ElementAt(1);panel.Children.Remove(header);var model=new HeaderModel();header.DataContext=model;
var host=new Window{Width=800,Height=600,Content=header};host.Show();
void Pump(){Dispatcher.UIThread.RunJobs();host.UpdateLayout();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();}
Rect Area(Control c)=>new(c.TranslatePoint(default,header).Value,c.Bounds.Size);
foreach(double width in new[]{256d,320d,512d})foreach(double font in new[]{14d,20d}){
 header.Width=width;host.FontSize=font;Pump();Pump();
 var automatic=header.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.IsVisible&&c.IsChecked==model.AutoUpdateCollision);
 var id=header.GetVisualDescendants().OfType<AngleBorder>().Single(c=>c.Content?.ToString()=="Block ID");
 var movement=header.GetVisualDescendants().OfType<AngleComboBox>().Single();var edit=header.GetVisualDescendants().OfType<AngleButton>().Single(b=>b.Name=="EditBlockButton");
 var movementLabel=header.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Text=="Movement:");
 Check(Math.Abs(Area(movementLabel).Center.Y-Area(movement).Center.Y)<=1,"Movement label and selector must share a vertical center.");
 Check(Area(movementLabel).Top>=Area(movement).Top-1&&Area(movementLabel).Bottom<=Area(movement).Bottom+1,"Movement label must fit the selector row without protruding above its border.");
 Check(!Area(automatic).Intersects(Area(id)),$"Block ID must not overlap automatic movement permission: width={width}, font={font}, permission={Area(automatic)}, id={Area(id)}.");
 Check(!Area(movement).Intersects(Area(id)),"Block ID must not overlap the movement selector.");
 Check(!Area(edit).Intersects(Area(automatic))&&!Area(edit).Intersects(Area(movement)),"Edit Block must have its own clear hit area.");
 foreach(var control in new Control[]{automatic,id,movement,edit})Check(Area(control).Left>=-1&&Area(control).Right<=width+1,"Every block-panel control must fit the available width.");
 var at=automatic.TranslatePoint(new Avalonia.Point(12,automatic.Bounds.Height/2),host).Value;
 var before=model.AutoUpdateCollision;host.MouseMove(at);host.MouseDown(at,MouseButton.Left);host.MouseUp(at,MouseButton.Left);Pump();
 Check(model.AutoUpdateCollision!=before,"Automatic movement permission stays clickable and bound.");
}
if(args.Length==1){host.Width=512;Pump();using var preview=new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(512,(int)Math.Ceiling(header.DesiredSize.Height)));preview.Render(header);preview.Save(args[0]);}
host.Content=null;host.Close();Console.WriteLine($"CHECKS_PASSED:{checks}");
class HeaderModel{
 public object Blocks{get;}=new{ScaledWidth=256d};public bool AutoUpdateCollision{get;set;}=true;public bool BlockEditorVisible=>true;public int DrawBlockIndex=>17;public bool BlockBagVisible=>false;public bool IsSingleBlock=>false;public bool DrawMultipleTiles=>false;public bool IsValid9GridSelection=>false;public bool Use9Grid{get;set;}public int CollisionIndex{get;set;}
 public object PrimaryMap{get;}=new{BlockEditor=new BlockOptions()};
}
class BlockOptions{public string[] MovementPermissions{get;}=new[]{"0C - Elevation 3 Passable"};public void ToggleShowTiles(){} }
