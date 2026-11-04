using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media.Imaging;
using HavenSoft.HexManiac.AvaloniaUI.MacPlatform;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<Application>().UseStandardRuntimePlatformSubsystem().UseWindowingSubsystem(()=>{},"offscreen").UseSkia().SetupWithoutStarting();
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException(message);}
var root=Path.Combine(Path.GetTempPath(),"hma-map-export-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try {
   var rom=Path.Combine(root,"test.gba");File.WriteAllBytes(rom,new byte[]{1,2,3});
   var output=Path.Combine(root,"MapImages","Rolling Acres.43-0.png");
   int prompts=0,pickers=0,errors=0,choice=0;
   string fallback=null;
   var exporter=new MapImageExporter(_=>{prompts++;return choice;},_=>{pickers++;return fallback;},_=>errors++);
   void Encode(Stream stream)=>stream.Write(new byte[]{4,5,6});
   Check(exporter.Export(rom,"Rolling Acres.43-0",Encode)==output,"First export uses the ROM directory and map identity.");
   Check(File.ReadAllBytes(output).SequenceEqual(new byte[]{4,5,6})&&prompts==0&&pickers==0,"First export creates MapImages without prompting.");
   File.WriteAllText(output,"original");
   choice=2;Check(exporter.Export(rom,"Rolling Acres.43-0",Encode)==null&&File.ReadAllText(output)=="original","Cancel preserves the original.");
   choice=-1;Check(exporter.Export(rom,"Rolling Acres.43-0",Encode)==null&&File.ReadAllText(output)=="original","Closing the dialog cancels.");
   choice=1;var second=exporter.Export(rom,"Rolling Acres.43-0",Encode);
   Check(second==Path.Combine(root,"MapImages","Rolling Acres.43-0 (2).png")&&File.Exists(second),"Updated copy starts at (2).");
   Check(File.ReadAllText(output)=="original","Updated copy retains original bytes.");
   Check(exporter.Export(rom,"Rolling Acres.43-0",Encode)==Path.Combine(root,"MapImages","Rolling Acres.43-0 (3).png"),"Further copies skip existing files.");
   choice=0;Check(exporter.Export(rom,"Rolling Acres.43-0",Encode)==output&&File.ReadAllBytes(output).SequenceEqual(new byte[]{4,5,6}),"Explicit overwrite replaces the original.");
   File.WriteAllText(output,"keep");fallback=null;
   Check(exporter.Export(rom,"Rolling Acres.43-0",s=>{s.WriteByte(9);throw new IOException("injected encoding failure");})==null,"Failed encoding offers a cancellable fallback.");
   Check(File.ReadAllText(output)=="keep"&&errors==1&&pickers==1,"Partial encoding cannot truncate an existing PNG.");
   Check(!Directory.GetFiles(Path.GetDirectoryName(output)!,"*.tmp").Any(),"Failed exports remove temporary files.");
   // A file at MapImages simulates an unavailable output folder without relying on host permissions.
   var blocked=Path.Combine(root,"blocked");Directory.CreateDirectory(blocked);File.WriteAllText(Path.Combine(blocked,"MapImages"),"not a directory");
   fallback=Path.Combine(root,"fallback.png");
   Check(exporter.Export(Path.Combine(blocked,"test.gba"),"Test",Encode)==fallback&&File.Exists(fallback),"Unwritable default folder can use the save dialog destination.");
   fallback=null;Check(exporter.Export("untitled","Test",Encode)==null,"Unsaved ROM uses a cancellable save dialog.");
   fallback=Path.Combine(root,"unsaved.png");Check(exporter.Export(null,"Test",Encode)==fallback,"Unsaved ROM can export to a chosen path.");
   Check(MapImageExporter.FileName("../a:b/c\\d\n")!="../a:b/c\\d\n.png"&&!MapImageExporter.FileName("../a:b/c\\d\n").Contains('/'),"Map names cannot escape the output folder.");
   Check(MapImageExporter.FileName(" ")=="Map.png","Empty names receive a safe default.");
   // Another writer appearing after collision checks must not be silently overwritten.
   var race=Path.Combine(root,"MapImages","Race.png");
   fallback=null;
   Check(exporter.Export(rom,"Race",s=>{Encode(s);File.WriteAllText(race,"other writer");})==null&&File.ReadAllText(race)=="other writer","New-file races preserve the other writer's image.");
   Check(File.ReadAllBytes(rom).SequenceEqual(new byte[]{1,2,3}),"Export never changes the ROM.");
   // Exercise the production PNG encoder and Mac integration without a window or clipboard.
   using(var fs=new MacFileSystem(()=>null))fs.ExportMapImage(new short[]{0x7C00,0x03E0,0x001F,0},2,rom,"Colors.0-0");
   using(var bitmap=new Bitmap(Path.Combine(root,"MapImages","Colors.0-0.png")))
      Check(bitmap.PixelSize==new PixelSize(2,2),"The real Mac export writes a decodable PNG at original dimensions.");

   using(var fs=new MacFileSystem(()=>null)) {
      var decoded=fs.LoadImage(Path.Combine(root,"MapImages","Colors.0-0.png"));
      Check(decoded.width==2&&decoded.image.SequenceEqual(new short[]{0x7C00,0x03E0,0x001F,0}),"PNG export preserves each RGB555 pixel.");
   }

   // Isolate the real export method from unrelated map-editor startup requirements.
   var fixture=new SyntheticMapFixture(7,5);var map=fixture.Map;
   var editor=(MapEditorViewModel)RuntimeHelpers.GetUninitializedObject(typeof(MapEditorViewModel));
   void Set(string name,object value)=>typeof(MapEditorViewModel).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(editor,value);
   var capture=new CaptureExporter();Set("primaryMap",map);Set("fileSystem",capture);Set("viewPort",fixture.ViewPort);
   var raw=fixture.Model.RawData.ToArray();var metadata=fixture.Model.ExportMetadata(null,fixture.ViewPort.Singletons.MetadataInfo).Serialize();
   map.IsSelected=true;map.ShowEvents=MapDisplayOptions.AllEvents;
   foreach(var mode in new[]{MapDisplayOptions.NoEvents,MapDisplayOptions.ObjectEvents,MapDisplayOptions.AllEvents}) {
      editor.ExportMapImage(false,mode);
      Check(capture.Pixels?.Length==112*80&&capture.Width==112,"Map export retains native 16-pixel tile resolution.");
      Check(capture.MapName=="0-0"&&capture.RomPath==fixture.ViewPort.FullFileName,"Renderer forwards map identity and ROM location.");
      Check(map.IsSelected&&map.ShowEvents==MapDisplayOptions.AllEvents,"Export restores selection and event display.");
   }
   editor.ExportMapImage(true,MapDisplayOptions.NoEvents);
   Check(capture.MapName=="0-0-connected","Connected export has a separate stable filename.");
   capture.Fail=true;
   try{editor.ExportMapImage(false,MapDisplayOptions.NoEvents);throw new Exception("Expected failure");}catch(IOException){}
   Check(map.IsSelected&&map.ShowEvents==MapDisplayOptions.AllEvents,"Export failure also restores display state.");
   var clipboard=new StubFileSystem();short[] copied=null;
   clipboard.CopyImage.set=value=>copied=value.Item1;
   Set("fileSystem",clipboard);editor.ExportMapImage(false,MapDisplayOptions.NoEvents);
   Check(copied?.Length==112*80&&map.IsSelected&&map.ShowEvents==MapDisplayOptions.AllEvents,"Other filesystem implementations retain clipboard export and restore display state.");
   Check(raw.SequenceEqual(fixture.Model.RawData)&&metadata.SequenceEqual(fixture.Model.ExportMetadata(null,fixture.ViewPort.Singletons.MetadataInfo).Serialize())&&!fixture.ViewPort.Undo.CanExecute(null),"Export preserves ROM bytes, metadata and history.");
} finally {Directory.Delete(root,true);}
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class CaptureExporter:StubFileSystem,IMapImageExporter {
   public short[] Pixels;public int Width;public string RomPath,MapName;public bool Fail;
   public void ExportMapImage(short[] pixels,int width,string romPath,string mapName) {
      if(Fail)throw new IOException("injected platform error");
      (Pixels,Width,RomPath,MapName)=(pixels,width,romPath,mapName);
   }
}
