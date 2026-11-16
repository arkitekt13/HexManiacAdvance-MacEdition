using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Performance;

internal static class IpsVerification {
   public static int Run() {
      int checks=0;
      void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
      void Rejected(Action action,string message){try{action();}catch(InvalidDataException){Check(true,message);return;}throw new Exception(message);}
      string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b));
      var folder=Path.Combine(Path.GetTempPath(),"hma-tutorial-ips-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
      try {
         var fixture=new SyntheticMapFixture(4,3);var source=fixture.Model.RawData.ToArray();
         Encoding.ASCII.GetBytes("BPRE").CopyTo(source,0xAC);source[0xBC]=1;
         var before=source.ToArray();
         var target=source.ToArray();Array.Fill(target,(byte)33,0x8000,64);target[0x9000]=42;
         var patch=Patcher.BuildIpsPatch(source,target);
         var toml=Encoding.UTF8.GetBytes(string.Join("\n",fixture.Model.ExportMetadata(null,fixture.ViewPort.Singletons.MetadataInfo).Serialize())+"\n");
         var manifest=new TutorialPatch.Manifest{Title="IPS lesson",BaseRom=TutorialPatch.BaseGame,BaseSha256=Hash(source),PatchSha256=Hash(patch),OutputSha256=Hash(target),TomlSha256=Hash(toml),PatchFile="tutorial.ips"};
         void Write(){File.WriteAllText(Path.Combine(folder,"tutorial.json"),JsonSerializer.Serialize(manifest));File.WriteAllBytes(Path.Combine(folder,"tutorial.ips"),patch);File.WriteAllBytes(Path.Combine(folder,"tutorial.toml"),toml);}
         TutorialPatch Load()=>TutorialPatch.LoadTestPackage(folder,out _,source);
         Write();var tutorial=Load();Check(tutorial!=null,"Explicit IPS package loads");
         Check(tutorial.Create(source).SequenceEqual(target),"IPS literal/RLE output matches the pinned target");
         File.WriteAllBytes(Path.Combine(folder,"tutorial.ups"),Patcher.BuildUpsPatch(source,source));
         Check(Load().Create(source).SequenceEqual(target),"Manifest selects IPS even if a stale UPS is present");
         var changed=source.ToArray();changed[0x9000]^=1;Rejected(()=>tutorial.Create(changed),"IPS cannot be applied to an edited base");
         Rejected(()=>tutorial.Create(target),"IPS cannot be reapplied to its output");
         var output=Path.Combine(folder,"My Lesson — test.gba");tutorial.SaveNewCopy(output,target);
         Check(File.ReadAllBytes(output).SequenceEqual(target),"IPS output publishes under the chosen name");
         Check(File.ReadAllBytes(Path.ChangeExtension(output,".toml")).SequenceEqual(toml),"IPS publication preserves supplied TOML bytes and matching name");
         manifest.OutputSha256=new string('0',64);Write();Rejected(()=>Load().Create(source),"IPS output hash is checked before publication");manifest.OutputSha256=Hash(target);
         var valid=patch;patch=patch[..^1];manifest.PatchSha256=Hash(patch);Write();Rejected(()=>Load().Create(source),"Missing IPS EOF is rejected even with a matching manifest hash");
         patch=Encoding.ASCII.GetBytes("PATCH\0\0\0\0\0\0\0\0EOF");manifest.PatchSha256=Hash(patch);Write();Rejected(()=>Load().Create(source),"Zero-length IPS RLE is rejected safely");
         patch=valid;manifest.PatchSha256=Hash(patch);Write();File.Delete(Path.Combine(folder,"tutorial.ips"));Check(Load()==null,"Missing manifest-selected IPS stays unavailable");Write();
         foreach(var name in new[]{"../tutorial.ips","lesson.bps",null}){manifest.PatchFile=name;Write();Check(Load()==null,"Unsupported or escaping patch filename is rejected");}
         manifest.PatchFile="tutorial.ips";patch=Encoding.ASCII.GetBytes("UPS1bad signature");manifest.PatchSha256=Hash(patch);Write();Check(Load()==null,"IPS filename requires IPS magic even with a matching hash");
         patch=valid;manifest.PatchSha256=new string('0',64);Write();Check(Load()==null,"Incorrect IPS asset hash disables the package");
         Check(source.SequenceEqual(before),"IPS operations leave the input bytes unchanged");
      }finally{Directory.Delete(folder,true);}
      return checks;
   }
}
