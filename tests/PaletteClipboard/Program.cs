using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Review;
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message) { checks++;if(!ok)throw new InvalidOperationException(message); }
foreach(bool native in new[]{false,true}) {
   using var f=new SyntheticImageFixture(false);
   var palette=f.View.FindControl<PaletteControl>("PaletteControl")!;
   var fs=(IFileSystem)f.FileSystem;
   var cmd=OperatingSystem.IsMacOS()?RawInputModifiers.Meta:RawInputModifiers.Control;
   void Key(PhysicalKey key,RawInputModifiers extra=RawInputModifiers.None) {f.Window.KeyPressQwerty(key,cmd|extra);f.Window.KeyReleaseQwerty(key,RawInputModifiers.None);f.Pump();}
   void Edit(string name) {var menu=NativeMenu.GetMenu(f.Window)!.Items.OfType<NativeMenuItem>().Single(i=>Equals(i.Header,"Edit")).Menu!;var item=menu.Items.OfType<NativeMenuItem>().Single(i=>Equals(i.Header,name));item.Command!.Execute(item.CommandParameter);f.Pump();}
   f.Image.SelectAll.Execute(null);f.Image.Palette.SelectionStart=1;palette.Focus();f.Pump();
   fs.CopyText="sentinel";var before=f.Model.RawData.ToArray();var metadata=f.Metadata();
   if(native)Edit("Copy");else Key(PhysicalKey.C);
   Check(fs.CopyText=="1:1:1"&&fs.CopyImage.Item1==null,"Keyboard and native Copy must target the focused palette, preserving its established color format.");
   Check(before.SequenceEqual(f.Model.RawData)&&metadata.SequenceEqual(f.Metadata()),"Palette copy preserves every ROM byte and metadata.");
   fs.CopyText="31:0:0";
   if(native)Edit("Paste / Replace");else Key(PhysicalKey.V);
   f.ViewPort.ChangeHistory.ChangeCompleted();
   Check(f.Model.ReadMultiByteValue(0x602,2)==31,"Focused palette paste uses the existing color parser and raw channel ordering.");
   Check(before.Where((_,i)=>i!=0x602&&i!=0x603).SequenceEqual(f.Model.RawData.Where((_,i)=>i!=0x602&&i!=0x603)),"Palette paste cannot alter sprite pixels, pointers or unrelated ROM bytes.");
   Check(metadata.SequenceEqual(f.Metadata()),"Palette paste preserves pointer/format metadata.");
   var pasted=f.Model.RawData.ToArray();f.Image.Undo.Execute(null);f.Pump();
   Check(before.SequenceEqual(f.Model.RawData),"Undo restores the palette paste exactly.");
   f.Image.Redo.Execute(null);f.Pump();Check(pasted.SequenceEqual(f.Model.RawData),"Redo restores the palette paste exactly.");
   palette.Focus();fs.CopyImage=(Enumerable.Repeat((short)0x7FFF,256).ToArray(),16);fs.CopyText="not a palette";
   if(native)Edit("Paste / Replace");else Key(PhysicalKey.V);
   Check(pasted.SequenceEqual(f.Model.RawData),"Disabled palette paste must never fall through to a valid image clipboard payload.");
   fs.CopyText=null;
   if(native)Edit("Paste / Replace");else Key(PhysicalKey.V);
   Check(pasted.SequenceEqual(f.Model.RawData),"A clipboard without text must remain a harmless disabled palette paste.");
   f.Image.Palette.SelectionStart=1;
   palette.ContextMenu!.Open(palette);f.Pump();
   var copyItem=palette.ContextMenu.Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Copy"));
   var pasteItem=palette.ContextMenu.Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Paste"));
   Check(copyItem.IsEnabled&&!pasteItem.Command!.CanExecute(null),"Palette context-menu commands resolve the attached filesystem and disabled paste state.");
   fs.CopyText="sentinel";copyItem.Command!.Execute(copyItem.CommandParameter);f.Pump();
   Check(fs.CopyText=="31:0:0",$"The palette context-menu Copy executes without a null filesystem (text={fs.CopyText}, selected={f.Image.Palette.SelectionStart}/{f.Image.Palette.SelectionEnd}).");
   palette.ContextMenu.Close();palette.Focus();fs.CopyText="0:31:0";palette.ContextMenu.Open(palette);f.Pump();
   pasteItem=palette.ContextMenu.Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Paste"));
   Check(pasteItem.IsEnabled,"Opening the menu refreshes paste availability for changed clipboard contents.");
   pasteItem.Command!.Execute(pasteItem.CommandParameter);palette.ContextMenu.Close();f.Pump();
   Check(f.Model.ReadMultiByteValue(0x602,2)==0x3E0,"Context-menu Paste uses the current palette and live filesystem.");
   palette.Focus();var stable=f.Model.RawData.ToArray();
   Key(PhysicalKey.G);Check(!f.Window.Editor.GotoViewModel.ControlVisible&&stable.SequenceEqual(f.Model.RawData),"An unavailable palette gradient shortcut must not open global Goto or edit data.");
   if(native)Edit("Select All");else Key(PhysicalKey.A);
   Check(f.Image.Palette.Elements.All(c=>c.Selected),"Select All applies to focused palette colors.");
   Check(stable.SequenceEqual(f.Model.RawData),"Palette selection itself creates no ROM edits.");
   if(native)Edit("Cut");else Key(PhysicalKey.X);
   Check(stable.SequenceEqual(f.Model.RawData),"Unsupported palette Cut must not fall through to image or hex deletion.");
   // Text inside the popup keeps native text clipboard behavior, ahead of palette commands.
   var popup=new PalettePopup(f);popup.Code.Focus();popup.Code.SelectAll();
   fs.CopyText="sentinel";HavenSoft.HexManiac.AvaloniaUI.MacPlatform.UiSync.Run(()=>f.Window.Clipboard!.SetTextAsync("invalid"));
   if(native)Edit("Copy");else Key(PhysicalKey.C);
   Check(fs.CopyText=="sentinel"&&stable.SequenceEqual(f.Model.RawData),"Popup text copy must not invoke palette/image/binary copy.");
   HavenSoft.HexManiac.AvaloniaUI.MacPlatform.UiSync.Run(()=>f.Window.Clipboard!.SetTextAsync("invalid"));
   if(native)Edit("Paste / Replace");else Key(PhysicalKey.V);
   for(int i=0;i<100&&popup.Code.Text!="invalid";i++){f.Pump();Thread.Sleep(1);}
   Check(popup.Code.Text=="invalid"&&stable.SequenceEqual(f.Model.RawData),$"Popup text paste edits its draft without falling through to palette or ROM edits (text={popup.Code.Text}, bytes={stable.SequenceEqual(f.Model.RawData)}, root={TopLevel.GetTopLevel(popup.Code)?.GetType().Name}, focus={f.Window.FocusManager?.GetFocusedElement()?.GetType().Name}, sameRoot={ReferenceEquals(TopLevel.GetTopLevel(popup.Code),f.Window)}).");
   popup.Control.ClosePopup();f.View.Focus();f.Image.SelectAll.Execute(null);fs.CopyText="image sentinel";fs.CopyImage=(null,0);f.Pump();
   Key(PhysicalKey.C);
   Check(fs.CopyImage.Item1!=null&&fs.CopyText=="image sentinel","Image copy still works when focus returns to the image canvas.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
