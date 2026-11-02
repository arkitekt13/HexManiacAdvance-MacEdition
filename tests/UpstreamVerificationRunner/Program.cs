using System.Diagnostics;
using System.Xml.Linq;
if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("These checks exercise the Mac shell runner.");
var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
var root = Path.Combine(Path.GetTempPath(), "hma-upstream-runner-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(root,"scripts"));
Directory.CreateDirectory(Path.Combine(root,"mock-bin"));
foreach (var name in new[] { "verify-upstream.sh", "summarize-upstream.py", "run-upstream-command.py" }) File.Copy(Path.Combine(repository,"scripts",name),Path.Combine(root,"scripts",name));
var dotnet = Path.Combine(root,"mock-bin/dotnet");
File.WriteAllText(dotnet,"""
#!/bin/bash
case "$1" in
*UpstreamTestRunner.dll)
  printf '%s\n' "$MOCK_DISCOVERY" > "$2/discovery.log"
  if [[ "${MOCK_NO_TRX:-0}" == 0 ]]; then printf '%s' "$MOCK_TRX" > "$2/results.xunit.xml"; fi
  exit "${MOCK_TEST_EXIT:-0}" ;;
restore) exit "${MOCK_RESTORE_EXIT:-0}" ;;
build) exit "${MOCK_BUILD_EXIT:-0}" ;;
test)
  for arg in "$@"; do
    if [[ "$arg" == --list-tests ]]; then
      printf '%s\n' "$MOCK_DISCOVERY"
      exit "${MOCK_DISCOVERY_EXIT:-0}"
    fi
  done
  while [[ $# -gt 0 ]]; do
    if [[ "$1" == --results-directory ]]; then result="$2"; break; fi
    shift
  done
  if [[ "${MOCK_NO_TRX:-0}" == 0 ]]; then printf '%s' "$MOCK_TRX" > "$result/results.trx"; fi
  exit "${MOCK_TEST_EXIT:-0}" ;;
*) exit 9 ;;
esac
""");
File.SetUnixFileMode(dotnet,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
const string discovered = "The following Tests are available:\n    HavenSoft.HexManiac.Tests.Example.Pass\n    HavenSoft.HexManiac.Tests.Example.Optional";
string Trx(int passed = 1, int failed = 0, int skipped = 1, string outcome = "Completed") {
   XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
   var results = new XElement(ns+"Results");
   foreach (var (count,label) in new[] { (passed,"Passed"), (failed,"Failed"), (skipped,"NotExecuted") })
      for (var i=0;i<count;i++) results.Add(new XElement(ns+"UnitTestResult",new XAttribute("testName",label+i),new XAttribute("outcome",label),
         new XElement(ns+"Output",new XElement(ns+"ErrorInfo",new XElement(ns+"Message","fixture failure")))));
   return new XElement(ns+"TestRun",results,new XElement(ns+"ResultSummary",new XAttribute("outcome",outcome),
      new XElement(ns+"Counters",new XAttribute("total",passed+failed+skipped),new XAttribute("executed",passed+failed),
         new XAttribute("passed",passed),new XAttribute("failed",failed),new XAttribute("notExecuted",skipped)))).ToString();
}
(int Exit,string Output) Run(string trx, string discovery = discovered, int restore=0,int build=0,int list=0,int test=0,bool noTrx=false, params string[] args) {
   var info = new ProcessStartInfo("/bin/bash") { RedirectStandardOutput=true,RedirectStandardError=true };
   info.ArgumentList.Add(Path.Combine(root,"scripts/verify-upstream.sh"));
   foreach (var arg in args) info.ArgumentList.Add(arg);
   info.Environment["PATH"] = Path.Combine(root,"mock-bin")+":"+Environment.GetEnvironmentVariable("PATH");
   info.Environment["MOCK_TRX"]=trx;info.Environment["MOCK_DISCOVERY"]=discovery;
   foreach (var (name,value) in new[] { ("RESTORE",restore),("BUILD",build),("DISCOVERY",list),("TEST",test) }) info.Environment["MOCK_"+name+"_EXIT"]=value.ToString();
   info.Environment["MOCK_NO_TRX"]=noTrx?"1":"0";
   using var process = Process.Start(info)!;
   var stdout = process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
   if (!process.WaitForExit(15000)) { process.Kill(true);throw new Exception("Upstream runner hung."); }
   return (process.ExitCode,stdout.GetAwaiter().GetResult()+stderr.GetAwaiter().GetResult());
}
try {
   var valid=Run(Trx());
   Check(valid.Exit==0 && valid.Output.Contains("1 passed, 0 failed, 1 skipped"),"Valid mixed pass/skip evidence passes. " + valid.Output);
   Check(Directory.GetFiles(Path.Combine(root,"artifacts"),"summary.json",SearchOption.AllDirectories).Length==1,"Keep structured summary evidence.");
   Check(Run(Trx(),discovery:"").Exit!=0,"Empty discovery fails.");
   Check(Run(Trx(),discovery:"No test is available.").Exit!=0,"Generic discovery output cannot pass.");
   Check(Run(Trx(),restore:3).Exit!=0,"Restore failure fails verification.");
   Check(Run(Trx(),build:3).Exit!=0,"Build failure fails verification.");
   Check(Run(Trx(),list:3).Exit!=0,"Discovery process failure fails verification.");
   Check(Run(Trx(),test:3).Exit!=0,"Nonzero execution exit cannot be hidden by successful TRX.");
   Check(Run(Trx(),noTrx:true).Exit!=0,"Missing TRX fails verification.");
   Check(Run("invalid xml").Exit!=0,"Malformed TRX fails verification.");
   Check(Run(Trx(passed:0,skipped:0)).Exit!=0,"Zero cases cannot pass.");
   Check(Run(Trx(passed:0,skipped:2)).Exit!=0,"An entirely skipped run cannot pass.");
   Check(Run(Trx().Replace("total=\"2\"","total=\"3\"")).Exit!=0,"Incomplete TRX totals fail.");
   Check(Run(Trx().Replace("executed=\"1\"","executed=\"2\"")).Exit!=0,"Inconsistent executed counts fail.");
   Check(Run(Trx().Replace("outcome=\"Passed\"","outcome=\"Unknown\"")).Exit!=0,"Unknown case outcomes fail.");
   Check(Run(Trx(outcome:"Aborted")).Exit!=0,"Aborted test host cannot pass.");
   Check(Run(Trx(outcome:"Failed")).Exit!=0,"Failure outside individual cases cannot pass.");
   var failure = Run(Trx(passed:1,failed:1,outcome:"Failed"),test:1);
   Check(failure.Exit!=0 && failure.Output.Contains("1 failed") && failure.Output.Contains("fixture failure"),"Report real failed cases while returning failure.");
   Check(Run(Trx(),restore:3,args:new[] { "--no-restore","--configuration","Release" }).Exit==0,"No-restore and Release options are supported.");
   Check(Run(Trx(),args:new[] { "--configuration","invalid" }).Exit!=0,"Reject invalid configurations.");
   string XunitXml(int passed=1,int failed=0,int skipped=1,int errors=0) {
      var collection = new XElement("collection");
      foreach (var (count,result) in new[] { (passed,"Pass"),(failed,"Fail"),(skipped,"Skip") })
         for (var i=0;i<count;i++) collection.Add(new XElement("test",new XAttribute("name",result+i),new XAttribute("result",result),
            new XElement("failure",new XElement("message","fixture failure"))));
      return new XElement("assemblies",new XElement("assembly",new XAttribute("total",passed+failed+skipped),new XAttribute("passed",passed),
         new XAttribute("failed",failed),new XAttribute("skipped",skipped),new XAttribute("errors",errors),collection)).ToString();
   }
   Check(Run(XunitXml(),args:new[] { "--in-process" }).Exit==0,"In-process xUnit evidence is supported.");
   Check(Run(XunitXml(failed:1),test:1,args:new[] { "--in-process" }).Exit!=0,"In-process failures remain failures.");
   Check(Run(XunitXml(errors:1),args:new[] { "--in-process" }).Exit!=0,"In-process engine errors cannot pass.");
   Check(Run(XunitXml(passed:0,skipped:0),args:new[] { "--in-process" }).Exit!=0,"In-process empty results cannot pass.");
   Check(Run(XunitXml(passed:0),args:new[] { "--in-process" }).Exit!=0,"In-process entirely skipped results cannot pass.");
   Check(Run(XunitXml(),discovery:"",args:new[] { "--in-process" }).Exit!=0,"In-process missing discovery cannot pass.");
   Check(Run(XunitXml(),noTrx:true,args:new[] { "--in-process" }).Exit!=0,"In-process missing results cannot pass.");
   Check(Run(XunitXml(),test:3,args:new[] { "--in-process" }).Exit!=0,"In-process process failure cannot be masked by XML.");
   var guard = new ProcessStartInfo("python3") { RedirectStandardOutput=true,RedirectStandardError=true };
   foreach (var arg in new[] { Path.Combine(root,"scripts/run-upstream-command.py"),"0.1","python3","-c","import time; time.sleep(30)" }) guard.ArgumentList.Add(arg);
   using var guardProcess = Process.Start(guard)!;
   var guardOutput=guardProcess.StandardError.ReadToEndAsync();
   Check(guardProcess.WaitForExit(10000) && guardProcess.ExitCode==124 && guardOutput.GetAwaiter().GetResult().Contains("timed out"),
      "Watchdog terminates a hung command and returns failure.");

} finally { Directory.Delete(root,true); }
Console.WriteLine("PASS: upstream runner rejects empty, skipped-only, inconsistent, aborted and failed evidence; valid runs retain structured counts.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
