using HavenSoft.HexManiac.Core.Models;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Review;
using System.Runtime.InteropServices;
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext()); Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message) { checks++;if(!ok) throw new InvalidOperationException(message); }
foreach(bool compressed in new[]{false,true}) {
   using var f=new SyntheticImageFixture(compressed);
   string Hash() {
      var b=(WriteableBitmap)f.View.FindControl<PixelImage>("EditImage")!.Source!;using var frame=b.Lock();
      var bytes=new byte[frame.RowBytes*b.PixelSize.Height];Marshal.Copy(frame.Address,bytes,0,bytes.Length);
      return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
   }
   var popup=new PalettePopup(f);Check(popup.Popup.IsOpen,"Clicking a swatch opens its real color editor.");
   var before=f.Model.RawData.ToArray();var metadata=f.Metadata();var stored=f.Model.ExportMetadata(null,f.ViewPort.Singletons.MetadataInfo);var original=popup.Code.Text;var originalHash=Hash();
   popup.Type(popup.Code,"1234",f);f.ViewPort.ChangeHistory.ChangeCompleted();
   var edited=f.Model.RawData.ToArray();var editedHash=Hash();
   Check(!before.SequenceEqual(edited)&&originalHash!=editedHash,"The actual color field edits ROM data and refreshes the bound sprite image.");
   Check(popup.Popup.IsOpen&&popup.Code.IsFocused,"The popup remains focused during its own palette refresh.");
   var modifiers=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
   f.Window.KeyPressQwerty(PhysicalKey.Z,modifiers);f.Window.KeyReleaseQwerty(PhysicalKey.Z,RawInputModifiers.None);f.Pump();
   Check(before.SequenceEqual(f.Model.RawData)&&metadata.SequenceEqual(f.Metadata()),"Undo restores all original ROM bytes and metadata.");
   Check(!popup.Popup.IsOpen,"Undo must close the stale color editor instead of leaving undone values writable.");
   Check(Hash()==originalHash&&f.Image.Redo.CanExecute(null),"Undo refreshes the displayed sprite and preserves Redo.");
   popup=new PalettePopup(f);
   Check(popup.Popup.IsOpen&&popup.Code.Text==original,"Reopening after Undo displays the restored color.");
   Check(f.Image.Redo.CanExecute(null),"Opening a restored color for inspection cannot destroy Redo.");
   f.Image.Redo.Execute(null);f.Pump();
   Check(edited.SequenceEqual(f.Model.RawData)&&Hash()==editedHash,"Redo restores the exact color edit and bound bitmap.");
   Check(!popup.Popup.IsOpen,"Redo closes an editor based on the old palette snapshot.");
   popup=new PalettePopup(f);Check(popup.Code.Text=="1234","Reopening after Redo uses the current model color.");
   // A new edit after Undo must use the restored baseline, not the abandoned popup state.
   f.Image.Undo.Execute(null);f.Pump();popup=new PalettePopup(f);
   popup.Type(popup.Code,"2345",f);f.ViewPort.ChangeHistory.ChangeCompleted();
   Check(f.Model.ReadMultiByteValue(0x602,2)==0x2345&&!f.Image.Redo.CanExecute(null),"Correction after Undo uses the current palette and replaces the old redo branch.");
   f.Image.Undo.Execute(null);f.Pump();
   Check(before.SequenceEqual(f.Model.RawData)&&metadata.SequenceEqual(f.Metadata()),"Undoing correction returns to the exact original model.");
   // Replacement-file refresh must not leave fields bound to the previous palette.
   f.Image.Redo.Execute(null);f.Pump();popup=new PalettePopup(f);
   f.Model.Load(before,stored);f.Image.Refresh();f.Pump();
   Check(!popup.Popup.IsOpen&&Hash()==originalHash&&before.SequenceEqual(f.Model.RawData),"Model replacement closes stale fields and refreshes current palette pixels.");
   popup=new PalettePopup(f);
   var other=new PaletteCollection(f.Image,f.Model,f.ViewPort.ChangeHistory);
   other.SetContents(f.Image.Palette.Elements.Select(c=>c.Color).ToArray());
   popup.Control.DataContext=other;f.Pump();
   Check(!popup.Popup.IsOpen,"Switching palette contexts closes the old editor.");
   popup.Control.DataContext=f.Image.Palette;f.Pump();popup=new PalettePopup(f);
   other.SetContents(other.Elements.Select(c=>c.Color).ToArray());f.Pump();
   Check(popup.Popup.IsOpen,"Notifications from a detached palette must not close the current editor.");
   var parent=(DockPanel)popup.Control.Parent!;int index=parent.Children.IndexOf(popup.Control);
   parent.Children.RemoveAt(index);f.Pump();Check(!popup.Popup.IsOpen,"Detaching a palette closes its popup and releases subscriptions.");
   parent.Children.Insert(index,popup.Control);f.Pump();popup=new PalettePopup(f);
   popup.Type(popup.Code,"3456",f);
   Check(popup.Popup.IsOpen&&f.Model.ReadMultiByteValue(0x602,2)==0x3456,"Reattachment restores editing without duplicate or stale subscriptions.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
