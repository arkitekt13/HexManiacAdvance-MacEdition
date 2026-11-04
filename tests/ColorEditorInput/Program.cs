using HavenSoft.HexManiac.Core.Models;

using Avalonia;
using Avalonia.Headless;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.Review;
using System.Globalization;
AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext()); Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message) { checks++;if(!ok) throw new InvalidOperationException(message); }
using var f=new SyntheticImageFixture(false);
foreach(var (input,word) in new[]{("7FFF",false),("0000",false),("001F",false),("03E0",false),("7C00",false),("1234",false),("white",true),("black",true),("red",true),("Lime",true),("Blue",true),("Magenta",true),("cyan",true),("orange",true)}) {
   var popup=new PalettePopup(f);Check(popup.Popup.IsOpen,"The actual color popup must open.");
   var before=f.Model.RawData.ToArray();var metadata=f.Metadata();
   popup.Type(word?popup.Color:popup.Code,input,f);
   int expected;
   if(word) {var c=Color.Parse(input);expected=((c.R>>3)<<10)|((c.G>>3)<<5)|(c.B>>3);}
   else {int code=int.Parse(input,NumberStyles.HexNumber);expected=((code&31)<<10)|(code&0x3E0)|((code>>10)&31);}
   int address=f.Model.ReadPointer(0x104)+2;
   int encoded=((expected&31)<<10)|(expected&0x3E0)|((expected>>10)&31);
   Check(f.Model.ReadMultiByteValue(address,2)==encoded,$"Color input {input} must update the selected ROM color with existing channel rules.");
   Check(f.Image.Palette.Elements[1].Color==expected,$"Color input {input} must update the palette view model.");
   Check(popup.Popup.IsOpen&&(word?popup.Color:popup.Code).IsFocused,"A color edit must retain its popup and focused input.");
   Check(before.Where((_,i)=>i!=address&&i!=address+1).SequenceEqual(f.Model.RawData.Where((_,i)=>i!=address&&i!=address+1)),"A color edit preserves every unrelated ROM byte.");
   Check(metadata.SequenceEqual(f.Metadata()),"A color edit preserves pointer/format metadata.");
   f.ViewPort.ChangeHistory.ChangeCompleted();
}
var swatch=new Swatch();int notifications=0;swatch.ResultChanged+=(_,_)=>notifications++;
foreach(var text in new[]{"112233","#112233","ABC","#ABC","FF112233","#FF112233","Red","White"}) {
   int previous=notifications;swatch.Result=text;
   Check(notifications==previous+1,"Swatches retain named, hash-prefixed and bare theme-hex input support.");
}
int accepted=notifications;swatch.Result="invalid color";
Check(notifications==accepted,"Invalid color drafts must not trigger color edits.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
