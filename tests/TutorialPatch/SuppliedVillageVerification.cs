using System.Security.Cryptography;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

internal static class SuppliedVillageVerification {
   public static int Run(string sourcePath,string independentPath) {
      int checks=0;
      void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
      var source=File.ReadAllBytes(sourcePath);var expected=File.ReadAllBytes(independentPath);
      var tutorial=TutorialPatch.Load(TutorialPatch.DefaultDirectory,out var reason);
      Check(tutorial!=null&&reason==null,"Shipped tutorial package loads through the production loader");
      var bytes=tutorial.Create(source);
      Check(bytes.SequenceEqual(expected),"HMA output equals independently decoded IPS output byte for byte");
      Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals("8e611cc0ebe03aca59418c2b55d728776327582975ed9fa96fdbb1b88f3b7c69",StringComparison.OrdinalIgnoreCase),"Supplied tutorial output has the pinned identity");
      var originalToml=File.ReadAllBytes(Path.Combine(TutorialPatch.DefaultDirectory,"tutorial.toml"));
      var suppliedMetadata=new StoredMetadata(File.ReadAllLines(Path.Combine(TutorialPatch.DefaultDirectory,"tutorial.toml")));
      var supplied=suppliedMetadata.NamedAnchors;
      var folder=Path.Combine(Path.GetTempPath(),"hma-village-review-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
      try {
         int romWrites=0,metadataWrites=0;
         var fs=new StubFileSystem{
            MetadataFor=name=>File.Exists(Path.ChangeExtension(name,".toml"))?File.ReadAllLines(Path.ChangeExtension(name,".toml")):Array.Empty<string>(),
            Save=file=>{File.WriteAllBytes(file.Name,file.Contents);romWrites++;return true;},
            SaveMetadata=(name,lines)=>{File.WriteAllLines(Path.ChangeExtension(name,".toml"),lines);metadataWrites++;return true;},
         };
         var editor=new EditorViewModel(fs,InstantDispatch.Instance);
         var path=Path.Combine(folder,"Tutorial Village — paired.gba");tutorial.SaveNewCopy(path,bytes);
         Check(File.ReadAllBytes(Path.ChangeExtension(path,".toml")).SequenceEqual(originalToml),"Initial chosen-name TOML is byte-identical to the supplied file");
         var view=editor.OpenFileAsync(new LoadedFile(path,File.ReadAllBytes(path))).GetAwaiter().GetResult();
         Check(view!=null,"Created tutorial opens through the normal editor");view.InitializationWorkload.GetAwaiter().GetResult();
         bool DefinitionsMatch(ViewPort v){var actual=v.Model.ExportMetadata(null,editor.Singletons.MetadataInfo).NamedAnchors;return supplied.All(a=>actual.Any(b=>b.Name==a.Name&&b.Address==a.Address&&b.Format==a.Format));}
         bool ListsMatch(ViewPort v){var actual=v.Model.ExportMetadata(null,editor.Singletons.MetadataInfo).Lists;return suppliedMetadata.Lists.All(a=>actual.Any(b=>b.Name==a.Name&&a.Contents.SequenceEqual(b.Contents)));}
         Check(DefinitionsMatch(view),"Every supplied anchor address and format survives opening");
         Check(ListsMatch(view),"Every supplied named list retains its values on opening");
         Check(metadataWrites==0&&File.ReadAllBytes(Path.ChangeExtension(path,".toml")).SequenceEqual(originalToml),"Opening does not regenerate or rewrite the supplied TOML");
         Check(view.Model.RawData.SequenceEqual(bytes),"Opening does not alter tutorial bytes");
         var map=ScriptMapSnapshot.Create(view,43001);
         Check(map.Image.PixelWidth==80*16&&map.Image.PixelHeight==56*16,"Tutorial Village renders at 80 by 56 tiles");
         Check(map.Image.PixelData.Length>0,"Tutorial map contains rendered pixels");
         Check(view.Model.RawData.SequenceEqual(bytes),"Map rendering leaves the tutorial unchanged");
         int address=view.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(),-1,"data.pokemon.stats");
         Check(address>=0,"Supplied Pokemon stats definition resolves");
         view.ChangeHistory.CurrentChange.ChangeData(view.Model,address,(byte)(view.Model[address]^1));view.ChangeHistory.ChangeCompleted();view.Save.Execute(fs);
         Check(romWrites==1&&metadataWrites==1&&view.ChangeHistory.IsSaved,"A real edit and Save writes the disposable ROM/TOML pair");
         var saved=view.Model.RawData.ToArray();view.Undo.Execute(null);Check(view.Model.RawData.SequenceEqual(bytes),"Undo restores exact tutorial bytes");view.Redo.Execute(null);Check(view.Model.RawData.SequenceEqual(saved),"Redo restores the disposable edit");
         var reopened=editor.OpenFileAsync(new LoadedFile(path,File.ReadAllBytes(path))).GetAwaiter().GetResult();reopened.InitializationWorkload.GetAwaiter().GetResult();
         Check(DefinitionsMatch(reopened)&&reopened.Model.RawData.SequenceEqual(saved),"Save/reopen retains all supplied definitions and the saved edit");
         Check(ListsMatch(reopened),"Every supplied named list survives actual Save/reopen");
         Check(ScriptMapSnapshot.Create(reopened,43001).Image.PixelData.Length>0,"Tutorial map renders after Save/reopen");
         Check(source.SequenceEqual(File.ReadAllBytes(sourcePath)),"Original clean ROM remains unchanged on disk");
         Console.WriteLine($"SUPPLIED_VILLAGE_PASS:anchors={supplied.Count}:checks={checks}");
      }finally{Directory.Delete(folder,true);}
      return checks;
   }
}
