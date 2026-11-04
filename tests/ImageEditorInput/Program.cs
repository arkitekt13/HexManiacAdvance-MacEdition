using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using System.Runtime.InteropServices;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Review;
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext()); Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message) { checks++;if(!ok) throw new InvalidOperationException(message); }
foreach(bool compressed in new[]{false,true}) {
   using var f=new SyntheticImageFixture(compressed);
   foreach(var gesture in new[]{(MouseButton.Right,RawInputModifiers.None,false),(MouseButton.Left,RawInputModifiers.Control,false),(MouseButton.Left,RawInputModifiers.Control,true)}) {
      if(gesture.Item2==RawInputModifiers.Control && !OperatingSystem.IsMacOS())continue;
      f.Image.SelectedTool=ImageEditorTools.Draw;f.Image.Palette.SelectionStart=7;f.Pump();
      var before=f.Model.RawData.ToArray();var metadata=f.Metadata();var at=f.Pixel(8,8);
      f.Window.MouseMove(at);f.Window.MouseDown(at,gesture.Item1,gesture.Item2);f.Pump();
      f.Window.MouseUp(at,gesture.Item1,gesture.Item3?RawInputModifiers.None:gesture.Item2);f.Pump();
      Check(f.Image.Palette.SelectionStart==1,$"Secondary/Control-click must sample the sprite color rather than paint with the selected tool ({gesture}, palette={f.Image.Palette.SelectionStart}, pixel={f.Image.ReadRawPixel(8,8)}, point={at}).");
      Check(before.SequenceEqual(f.Model.RawData),"Eyedropper gestures must preserve every ROM byte.");
      Check(metadata.SequenceEqual(f.Metadata())&&!f.ViewPort.ChangeHistory.Undo.CanExecute(null),"Sampling must preserve metadata and create no ROM history.");
      f.Window.MouseMove(f.Pixel(10,10));f.Pump();
      Check(before.SequenceEqual(f.Model.RawData),"Moving after sample release cannot leave a painting gesture active.");
   }
   f.Image.SelectedTool=ImageEditorTools.Draw;f.Image.Palette.SelectionStart=7;f.Pump();
   var original=f.Model.RawData.ToArray();var originalMetadata=f.Metadata();
   string Hash() {
      var image=f.View.FindControl<PixelImage>("EditImage")!;
      var b=(WriteableBitmap)image.Source!;using var frame=b.Lock();
      var bytes=new byte[frame.RowBytes*b.PixelSize.Height];Marshal.Copy(frame.Address,bytes,0,bytes.Length);
      return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
   }
   var originalHash=Hash();var draw=f.Pixel(8,8);
   f.Window.MouseDown(draw,MouseButton.Left);f.Window.MouseUp(draw,MouseButton.Left);f.Pump();
   Check(f.Image.ReadRawPixel(8,8)==7&&!original.SequenceEqual(f.Model.RawData),"A normal primary gesture must still draw the selected color.");
   Check(Hash()!=originalHash,"The live image must display the committed pixel edit.");
   var edited=f.Model.RawData.ToArray();var editedMetadata=f.Metadata();var editedHash=Hash();
   f.Window.KeyPressQwerty(PhysicalKey.Z,OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control);f.Window.KeyReleaseQwerty(PhysicalKey.Z,RawInputModifiers.None);f.Pump();
   Check(original.SequenceEqual(f.Model.RawData)&&originalMetadata.SequenceEqual(f.Metadata()),"Cmd+Z restores complete bytes, caller pointers, and metadata.");
   Check(Hash()==originalHash,"Undo refreshes the actual bound image bitmap.");
   f.Window.KeyPressQwerty(PhysicalKey.Z,(OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control)|RawInputModifiers.Shift);f.Window.KeyReleaseQwerty(PhysicalKey.Z,RawInputModifiers.None);f.Pump();
   Check(edited.SequenceEqual(f.Model.RawData)&&editedMetadata.SequenceEqual(f.Metadata()),"Cmd+Shift+Z restores the exact sprite edit and metadata.");
   Check(Hash()==editedHash,"Redo refreshes the actual bound image bitmap.");
   // Once released, another sample uses the new pixel and leaves the redo/drawing state intact.
   f.Image.Palette.SelectionStart=2;
   var sampleButton=OperatingSystem.IsMacOS()?MouseButton.Left:MouseButton.Right;
   var sampleModifiers=OperatingSystem.IsMacOS()?RawInputModifiers.Control:RawInputModifiers.None;
   f.Window.MouseDown(draw,sampleButton,sampleModifiers);f.Window.MouseUp(draw,sampleButton);f.Pump();
   Check(f.Image.Palette.SelectionStart==7&&edited.SequenceEqual(f.Model.RawData),"Sampling a newly edited pixel uses current sprite data.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
