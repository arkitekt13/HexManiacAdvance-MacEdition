using HavenSoft.HexManiac.Core.Models.Runs.Sprites;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Windows;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
namespace HavenSoft.HexManiac.Review;
public sealed class SyntheticImageFixture : IDisposable {
   public PokemonModel Model { get; }
   public ViewPort ViewPort { get; }
   public ImageEditorViewModel Image { get; }
   public StubFileSystem FileSystem { get; } = new();
   public MainWindow Window { get; }
   public ImageEditorView View => Window.GetVisualDescendants().OfType<ImageEditorView>().Single();
   public SyntheticImageFixture(bool compressed) {
      var data = Enumerable.Repeat((byte)0xFF, 0x10000).ToArray();
      BitConverter.GetBytes(0x08000800).CopyTo(data, 0x100); BitConverter.GetBytes(0x08000600).CopyTo(data, 0x104);
      for(int i=0;i<16;i++) BitConverter.GetBytes((short)(i | (i << 5) | (i << 10))).CopyTo(data, 0x600+i*2);
      var raw = Enumerable.Repeat((byte)0x11, 128).ToArray();
      if(compressed) LZRun.Compress(raw).ToArray().CopyTo(data, 0x800); else raw.CopyTo(data, 0x800);
      var singletons = new Singletons();
      Model = new PokemonModel(data, new StoredMetadata(anchors: new[] {
         new StoredAnchor(0x100,"assets",$"[sprite<`{(compressed ? "lzs" : "ucs")}4x2x2|assets.palette`> palette<`ucp4`>]1"),
         new StoredAnchor(0x600,"assets.palette","`ucp4`"),
      }), singletons); Model.FreeSpaceStart=0xD000;
      ViewPort = new ViewPort("synthetic-image.bin",Model,InstantDispatch.Instance,singletons,fs:FileSystem);
      ViewPort.InitializationWorkload.GetAwaiter().GetResult();
      Image = new ImageEditorViewModel(ViewPort.ChangeHistory,Model,0x800,ViewPort.Save);
      Application.Current!.Resources["FileSystem"] = FileSystem;
      var editor = new EditorViewModel(FileSystem,InstantDispatch.Instance);
      App.UpdateThemeDictionary(Application.Current.Resources,editor.Theme);
      Window = new MainWindow { Width=1200, Height=900, FileSystem=FileSystem, Editor=editor };
      Window.Resources["FileSystem"]=FileSystem; Window.Show(); editor.Add(ViewPort); editor.Add(Image);
      editor.GotoViewModel.ControlVisible=false; Pump(); ViewPort.ChangeHistory.ClearHistory(false);
   }
   public void Pump() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
   public Avalonia.Point Pixel(int x,int y) {
      var at=Image.FromSpriteSpace(new HavenSoft.HexManiac.Core.Models.Point(x,y));
      var canvas=View.FindControl<Grid>("ImageContainer")!;
      return canvas.TranslatePoint(new Avalonia.Point(canvas.Bounds.Width/2+at.X,canvas.Bounds.Height/2+at.Y),Window)!.Value;
   }
   public string[] Metadata()=>Model.ExportMetadata(null,ViewPort.Singletons.MetadataInfo).Serialize();
   public void Dispose() { Window.Editor=null; Window.Close(); }
}
