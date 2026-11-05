using System.Reflection;
using System.Runtime.Loader;
using System.Collections;
using System.Text.Json;

if(args.Length!=1)throw new ArgumentException("Pass the absolute path to HexManiacAdvance.app.");
var binaries=Path.Combine(Path.GetFullPath(args[0]),"Contents/MacOS");
AssemblyLoadContext.Default.Resolving+=(context,name)=>{
 var candidate=Path.Combine(binaries,name.Name+".dll");
 return File.Exists(candidate)?context.LoadFromAssemblyPath(candidate):null;
};
var temporary=Path.Combine(Path.GetTempPath(),"hma-published-review-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temporary);
int checks=0;
void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
var initialDirectory=Directory.GetCurrentDirectory();
try {
 Directory.SetCurrentDirectory(temporary);
 var assembly=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(binaries,"HexManiacAdvance.dll"));
 var fsType=assembly.GetType("HavenSoft.HexManiac.AvaloniaUI.MacPlatform.MacFileSystem")!;
 var startup=assembly.GetType("HavenSoft.HexManiac.AvaloniaUI.App")!.GetMethod("GetViewModel",BindingFlags.Static|BindingFlags.NonPublic)!;
 var profile=Path.Combine(temporary,"isolated profile");
 object FileSystem()=>Activator.CreateInstance(fsType,new object?[]{null,null,profile})!;
 object Editor(object fs)=>startup.Invoke(null,new object[]{"",-1,fs,new[]{true,false}})!;
 using var fs=(IDisposable)FileSystem();var editor=Editor(fs);var editorType=editor.GetType();
 Check((int)editorType.GetProperty("Count")!.GetValue(editor)! == 0,"Published startup has zero tabs.");
 Check(string.IsNullOrEmpty((string)editorType.GetProperty("ErrorMessage")!.GetValue(editor)!),"Published startup has no error.");
 Check(Directory.GetCurrentDirectory()==binaries,"Startup finds resources beside its published assembly from an unrelated working directory.");
 var singletons=editorType.GetProperty("Singletons")!.GetValue(editor)!;
 var lines=(IEnumerable)singletons.GetType().GetProperty("ScriptLines")!.GetValue(singletons)!;
 Check(lines.Cast<object>().Count()>100,"Published startup loads its script reference data.");
 var enabled=editorType.GetProperty("InsertAutoActive")!;enabled.SetValue(editor,false);
 editorType.GetMethod("WriteAppLevelMetadata")!.Invoke(editor,null);
 Check(File.Exists(Path.Combine(profile,"HexManiacAdvance.toml")),"Published settings are written only under the injected profile.");
 using var second=(IDisposable)FileSystem();var reloaded=Editor(second);
 Check(!(bool)enabled.GetValue(reloaded)!,"Published preferences survive a new filesystem/editor instance.");
 Check(File.Exists(Path.Combine(binaries,"libhostfxr.dylib"))&&File.Exists(Path.Combine(binaries,"libcoreclr.dylib")),"The bundle contains a self-contained runtime.");
 Console.WriteLine(JsonSerializer.Serialize(new{checks,version=assembly.GetName().Version!.ToString(),resourceFiles=Directory.GetFiles(Path.Combine(binaries,"resources"),"*",SearchOption.AllDirectories).Length,isolatedSettings=true,nativeWindowLaunched=false},new JsonSerializerOptions{WriteIndented=true}));
} finally {Directory.SetCurrentDirectory(initialDirectory);Directory.Delete(temporary,true);}
