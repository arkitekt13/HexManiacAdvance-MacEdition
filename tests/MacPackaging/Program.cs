using System.Diagnostics;
using System.Xml.Linq;

int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException(message);}
var repo=new DirectoryInfo(AppContext.BaseDirectory);while(repo!=null&&!File.Exists(Path.Combine(repo.FullName,"publish-mac.sh")))repo=repo.Parent;
if(repo==null)throw new Exception("Repository not found.");
var root=Path.Combine(Path.GetTempPath(),"hma packaging "+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try {
 var source=Path.Combine(root,"source tree");var bin=Path.Combine(root,"fake tools");var cwd=Path.Combine(root,"unrelated directory");
 Directory.CreateDirectory(source);Directory.CreateDirectory(bin);Directory.CreateDirectory(cwd);
 File.Copy(Path.Combine(repo.FullName,"publish-mac.sh"),Path.Combine(source,"publish-mac.sh"));
 Directory.CreateDirectory(Path.Combine(source,"src/HexManiac.Avalonia"));
 File.WriteAllText(Path.Combine(source,"src/SharedAssemblyInfo.cs"),"[assembly: AssemblyFileVersion(\"9.8.7\")]\n");
 File.WriteAllText(Path.Combine(source,"src/HexManiac.Avalonia/HexManiac.Avalonia.csproj"),"<Project />");
 void Tool(string name,string body) {var path=Path.Combine(bin,name);File.WriteAllText(path,"#!/bin/bash\nset -eu\n"+body);if(!OperatingSystem.IsWindows())File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);}
 Tool("dotnet","""
[[ "${HMA_TEST_MODE:-}" != publish-failure ]] || exit 31
out=""
while [[ $# -gt 0 ]]; do if [[ "$1" == -o ]]; then out="$2"; shift 2; else shift; fi; done
mkdir -p "$out/resources"
for file in HexManiacAdvance HexManiacAdvance.dll HexManiacAdvance.runtimeconfig.json libhostfxr.dylib libcoreclr.dylib; do echo synthetic > "$out/$file"; done
for file in hma.py armReference.txt scriptReference.txt pcsReference.txt tableReference.txt default.toml; do echo synthetic > "$out/resources/$file"; done
if [[ "${HMA_TEST_MODE:-}" == missing-resource ]]; then rm "$out/resources/hma.py"; fi
if [[ "${HMA_TEST_MODE:-}" == missing-runtime ]]; then rm "$out/libcoreclr.dylib"; fi
""");
 Tool("codesign","""
echo "$*" >> "$HMA_TEST_LOG"
if [[ "${HMA_TEST_MODE:-}" == sign-failure && "$1" == --force ]]; then exit 32; fi
if [[ "${HMA_TEST_MODE:-}" == verify-failure && "$1" == --verify ]]; then exit 33; fi
""");
 var log=Path.Combine(root,"codesign.log");
 int Run(string mode,params string[] args) {
  var start=new ProcessStartInfo("/bin/bash"){WorkingDirectory=cwd,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
  start.ArgumentList.Add(Path.Combine(source,"publish-mac.sh"));foreach(var arg in args)start.ArgumentList.Add(arg);
  start.Environment["PATH"]=bin+Path.PathSeparator+Environment.GetEnvironmentVariable("PATH");
  start.Environment["HMA_TEST_MODE"]=mode;start.Environment["HMA_TEST_LOG"]=log;
  using var process=Process.Start(start)!;var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
  if(!process.WaitForExit(20000)){process.Kill(true);throw new TimeoutException("Packaging probe timed out.");}
  Task.WaitAll(output,error);Console.WriteLine($"PACKAGING:{mode}:{string.Join(',',args)}:exit={process.ExitCode}");return process.ExitCode;
 }
 string Output(string rid)=>Path.Combine(source,"artifacts/mac",rid,"HexManiacAdvance.app");
 var arm=Output("osx-arm64");Directory.CreateDirectory(arm);File.WriteAllText(Path.Combine(arm,"old-build"),"keep me");
 foreach(var bad in new[]{new[]{"invalid-rid"},new[]{"osx-arm64","extra"}}) {
  Check(Run("normal",bad)==2,"Invalid arguments must fail before publishing.");
  Check(File.ReadAllText(Path.Combine(arm,"old-build"))=="keep me","Invalid arguments preserve previous output.");
 }
 foreach(var mode in new[]{"publish-failure","missing-resource","missing-runtime","sign-failure","verify-failure"}) {
  Check(Run(mode,"osx-arm64")!=0,"A failed build/validation/signature cannot publish a bundle.");
  Check(File.ReadAllText(Path.Combine(arm,"old-build"))=="keep me","A failed package preserves the previous bundle exactly.");
  Check(!Directory.GetDirectories(Path.GetDirectoryName(arm)!,".stage.*").Any(),"Failure cleans only its own staging folder.");
 }
 Check(Run("normal")==0,"Default arm64 packaging works from an unrelated directory.");
 Check(!File.Exists(Path.Combine(arm,"old-build"))&&File.Exists(Path.Combine(arm,"Contents/MacOS/resources/hma.py")),"Successful packaging replaces the old bundle with its complete resources.");
 var dict=XDocument.Load(Path.Combine(arm,"Contents/Info.plist")).Root!.Element("dict")!.Elements().ToArray();
 string Value(string key){var i=Array.FindIndex(dict,e=>e.Name=="key"&&e.Value==key);return dict[i+1].Value;}
 Check(Value("CFBundleShortVersionString")=="9.8.7"&&Value("CFBundleVersion")=="9.8.7","Bundle versions come from the application rather than a repository tag.");
 Check(Value("LSMinimumSystemVersion")=="12.0"&&Value("CFBundleDisplayName")=="HexManiacAdvance Mac Edition","Bundle prerequisites and display identity are explicit.");
 Check(Value("CFBundleIdentifier")=="io.github.hexmaniacadvance.mac"&&Value("CFBundleExecutable")=="HexManiacAdvance","Existing bundle identity and executable remain compatible.");
 Check(!Directory.Exists(Path.Combine(cwd,"artifacts")),"Packaging never writes under the caller's unrelated working directory.");
 Check(File.ReadAllLines(log).Any(l=>l.StartsWith("--verify --deep --strict")),"Publication requires explicit signature verification.");
 Check(Run("normal","osx-x64")==0,"Intel packaging uses its own output directory.");
 Check(Directory.Exists(arm)&&Directory.Exists(Output("osx-x64")),"Building one architecture preserves the other.");
 File.WriteAllText(Path.Combine(arm,"keep-new-build"),"saved");
 Check(Run("publish-failure","osx-arm64")!=0&&File.Exists(Path.Combine(arm,"keep-new-build")),"A subsequent failed rebuild preserves the last successful bundle.");
 Check(!Directory.EnumerateDirectories(Path.GetDirectoryName(arm)!).Any(p=>Path.GetFileName(p).StartsWith('.')),"Successful and failed runs leave no staging/backup directories.");
} finally {Directory.Delete(root,true);}
Console.WriteLine($"CHECKS_PASSED:{checks}");
