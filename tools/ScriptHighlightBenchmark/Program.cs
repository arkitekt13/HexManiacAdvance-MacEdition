using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.Core.ViewModels;

// Measure shared-Core highlighting only, without a ROM, compiler, UI, or network access.
if (args.Length != 1) throw new ArgumentException("Expected a JSON output path.");
var rows = new List<object>();
foreach (var lines in new[] { 100, 500, 2000 }) {
   var vm = new TextEditorViewModel { LineCommentHeader = "#" };
   for (int i = 0; i < 256; i++) vm.Keywords.Add("command" + i);
   for (int i = 0; i < 4096; i++) vm.Constants.Add("FLAG_" + i);
   var source = string.Join("\n", Enumerable.Repeat("command255 FLAG_4095 0x4000 <section0> # dialogue", lines));
   for (int i = 0; i < 2; i++) vm.Content = source + "\n# warm " + i;
   var elapsed = new List<double>();
   var allocated = new List<long>();
   for (int i = 0; i < 7; i++) {
      var input = source + "\n# sample " + i;
      var bytes = GC.GetAllocatedBytesForCurrentThread();
      var start = Stopwatch.GetTimestamp();
      vm.Content = input;
      elapsed.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
      allocated.Add(GC.GetAllocatedBytesForCurrentThread() - bytes);
      if (vm.Content != input) throw new InvalidOperationException("Highlighting modified the source.");
   }
   // Exact hashes outside the timed region allow before/after equality checks for every layer.
   var layers = new[] { vm.PlainContent, vm.AccentContent, vm.ConstantContent,
      vm.NumericContent, vm.CommentContent, vm.TextContent };
   if (layers.Any(layer => layer.Length != vm.Content.Length)) throw new InvalidOperationException("Layer indices changed.");
   var hashes = layers.Select(layer => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(layer)))).ToArray();
   var medianMs = elapsed.Order().ElementAt(3);
   var medianBytes = allocated.Order().ElementAt(3);
   rows.Add(new { lines, characters = vm.Content.Length, keywords = vm.Keywords.Count,
      constants = vm.Constants.Count, medianMs, medianAllocatedBytes = medianBytes,
      samplesMs = elapsed, layerSha256 = hashes });
   Console.WriteLine($"{lines} lines: {medianMs:F3} ms median; {medianBytes:N0} allocated bytes");
}
var path = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(path)!);
File.WriteAllText(path, JsonSerializer.Serialize(new {
   runtime = RuntimeInformation.FrameworkDescription, platform = RuntimeInformation.OSDescription,
   architecture = RuntimeInformation.ProcessArchitecture.ToString(), samplesPerSize = 7,
   scope = "Core highlighting only; no compiler, ROM, UI layout, or end-to-end typing measurement", results = rows,
}, new JsonSerializerOptions { WriteIndented = true }));
