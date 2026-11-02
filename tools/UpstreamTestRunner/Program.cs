using System.Xml.Linq;
using Xunit;

// Use xUnit's original discovery/execution engine in one process when VSTest's socket is unavailable.
if (args.Length < 1 || args.Length > 2 || (args.Length == 2 && args[1] != "HexManiac.Integration")) {
   Console.Error.WriteLine("Expected a result directory and optional HexManiac.Integration selector."); return 2;
}
var results = Path.GetFullPath(args[0]);
Directory.CreateDirectory(results);
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var assemblyPath = Path.Combine(AppContext.BaseDirectory, (args.Length == 2 ? args[1] : "HexManiac.Tests") + ".dll");
using var controller = new XunitFrontController(AppDomainSupport.Denied, assemblyPath, shadowCopy:false);
using var discovery = new TestDiscoverySink();
controller.Find(false, discovery, TestFrameworkOptions.ForDiscovery());
if (!discovery.Finished.WaitOne(TimeSpan.FromSeconds(60))) {
   Console.Error.WriteLine("Discovery timed out."); return 2;
}
File.WriteAllLines(Path.Combine(results,"discovery.log"), discovery.TestCases.Select(t => "    " + t.DisplayName));
if (discovery.TestCases.Count == 0) { Console.Error.WriteLine("No upstream tests discovered."); return 2; }
Console.WriteLine($"Discovered {discovery.TestCases.Count} upstream entries with xUnit's in-process engine.");
var assembly = new XElement("assembly");
using var summary = new DelegatingExecutionSummarySink(new NullMessageSink());
using var xml = new DelegatingXmlCreationSink(summary, assembly);
controller.RunTests(discovery.TestCases, xml, TestFrameworkOptions.ForExecution());
if (!xml.Finished.WaitOne(TimeSpan.FromMinutes(5))) {
   Console.Error.WriteLine("Execution timed out; partial results are not a baseline."); return 2;
}
new XDocument(new XElement("assemblies", assembly)).Save(Path.Combine(results,"results.xunit.xml"));
var counts = xml.ExecutionSummary;
Console.WriteLine($"xUnit completed: {counts.Total} cases; {counts.Failed} failed; {counts.Skipped} skipped; {counts.Errors} engine errors.");
return counts.Errors != 0 ? 2 : counts.Failed != 0 ? 1 : 0;
