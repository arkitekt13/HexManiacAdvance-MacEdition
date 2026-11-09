using System.Text;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Review;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;
void Check(bool value,string message) {checks++;if(!value)throw new Exception(message);}
using var f=new WorkflowFixture();
Check(AppIdentity.VersionNumber=="1.0.0"&&f.Services.MetadataInfo.VersionNumber=="0.6.1","Mac release and Core metadata versions are independent");
ArrayRun.TryParse(f.Model,"[value.]4",0xA000,SortedSpan<int>.None,out var custom);
f.Model.ObserveAnchorWritten(new NoDataChangeDeltaModel(),"tutorial.custom.definition",custom);
var metadata=f.Model.ExportMetadata(null,f.Services.MetadataInfo);
Check(metadata.Version=="0.6.1","new metadata still uses Core's existing format version");
Check(!StoredMetadata.NeedVersionUpdate(metadata.Version,f.Services.MetadataInfo.VersionNumber),"legacy metadata does not trigger a version migration");
Check(StoredMetadata.NeedVersionUpdate(metadata.Version,AppIdentity.VersionNumber),"test would detect incorrectly using the app version for metadata upgrades");
var bytes=f.Model.RawData.ToArray();
var originalToml=Encoding.UTF8.GetBytes(string.Join("\n",metadata.Serialize())+"\n# existing project definitions\n");
File.WriteAllBytes(f.FileName,bytes);File.WriteAllBytes(f.MetadataFile,originalToml);
int writes=0,metadataWrites=0;
var fs=new StubFileSystem {
 MetadataFor=name=>File.Exists(Path.ChangeExtension(name,".toml"))?File.ReadAllLines(Path.ChangeExtension(name,".toml")):Array.Empty<string>(),
 Save=file=>{File.WriteAllBytes(file.Name,file.Contents);writes++;return true;},
 SaveMetadata=(name,lines)=>{File.WriteAllLines(Path.ChangeExtension(name,".toml"),lines);metadataWrites++;return true;},
};
var owner=new EditorViewModel(fs,InstantDispatch.Instance);
var opened=owner.OpenFileAsync(new LoadedFile(f.FileName,bytes.ToArray())).GetAwaiter().GetResult();
opened.InitializationWorkload.GetAwaiter().GetResult();
bool Definitions(IDataModel model) {
 var actual=model.ExportMetadata(null,f.Services.MetadataInfo);
 return metadata.NamedAnchors.All(a=>actual.NamedAnchors.Any(b=>a.Name==b.Name&&a.Address==b.Address&&a.Format==b.Format)) &&
 metadata.Lists.All(a=>actual.Lists.Any(b=>a.Name==b.Name&&a.Contents.SequenceEqual(b.Contents)));
}
Check(opened.Model.RawData.SequenceEqual(bytes),"opening an existing project under Mac 1.0 does not alter ROM bytes");
Check(metadataWrites==0&&File.ReadAllBytes(f.MetadataFile).SequenceEqual(originalToml),"opening does not regenerate or rewrite existing TOML");
Check(Definitions(opened.Model),"all existing custom anchors, formats and lists load unchanged");
Check(opened.ChangeHistory.IsSaved&&!opened.ChangeHistory.Undo.CanExecute(null),"opening creates no spurious dirty state or Undo entry");
int address=opened.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(),-1,"tutorial.custom.definition");
Check(address==0xA000,"custom definition retains its exact address");
opened.ChangeHistory.CurrentChange.ChangeData(opened.Model,address,(byte)(opened.Model[address]^1));opened.ChangeHistory.ChangeCompleted();
opened.Save.Execute(fs);
Check(writes==1&&metadataWrites==1,"an actual edit saves one ROM and one metadata file");
Check(new StoredMetadata(File.ReadAllLines(f.MetadataFile)).Version=="0.6.1","saving under Mac 1.0 keeps the existing Core metadata version");
var saved=opened.Model.RawData.ToArray();
opened.Undo.Execute(null);Check(opened.Model.RawData.SequenceEqual(bytes),"Undo restores the exact pre-edit bytes");
opened.Redo.Execute(null);Check(opened.Model.RawData.SequenceEqual(saved),"Redo restores the saved edit");
var reopened=owner.OpenFileAsync(new LoadedFile(f.FileName,File.ReadAllBytes(f.FileName))).GetAwaiter().GetResult();
reopened.InitializationWorkload.GetAwaiter().GetResult();
Check(reopened.Model.RawData.SequenceEqual(saved)&&Definitions(reopened.Model),"save/reopen retains bytes and all custom definitions");
reopened.Goto.Execute(WorkflowFixture.Root);reopened.Tools.CodeTool.Mode=HavenSoft.HexManiac.Core.ViewModels.Tools.CodeMode.Script;
reopened.Tools.CodeTool.IsSelected=true;
Check(reopened.Tools.CodeTool.Contents.Any(body=>body.Content.Contains("setvar")&&!body.HasError),"existing scripts still decode without diagnostics after reopening");
if(args.Length==1) {
 var contents=Path.Combine(Path.GetFullPath(args[0]),"Contents");var binaries=Path.Combine(contents,"MacOS");
 Check(System.Reflection.AssemblyName.GetAssemblyName(Path.Combine(binaries,"HexManiacAdvance.dll")).Version==new Version(1,0,0,0),"actual published Mac assembly reports 1.0.0");
 Check(System.Reflection.AssemblyName.GetAssemblyName(Path.Combine(binaries,"HexManiac.Core.dll")).Version==new Version(0,6,1,0),"actual bundled Core assembly retains 0.6.1");
 var elements=System.Xml.Linq.XDocument.Load(Path.Combine(contents,"Info.plist")).Root.Element("dict").Elements().ToArray();
 string Value(string key)=>elements[Array.FindIndex(elements,e=>e.Name=="key"&&e.Value==key)+1].Value;
 Check(Value("CFBundleShortVersionString")=="1.0.0"&&Value("CFBundleVersion")=="1.0.0","actual Finder bundle versions match the Mac app identity");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
