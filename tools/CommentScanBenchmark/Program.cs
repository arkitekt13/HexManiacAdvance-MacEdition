using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.Core.ViewModels;

if (args.Length != 1) throw new ArgumentException("Expected output JSON path.");
var rows = new List<object>(); int checks = 0;
(string, string) Scan(string source, bool advance, bool blocks) {
   var basic = new MutableString(source); var comments = new MutableString(source.Length); comments.CopyWhitespace(basic);
   int from = 0;
   while (true) {
      int at = basic.IndexOf(blocks ? "/*" : "#", from); if (at < 0) break;
      int end = blocks ? basic.IndexOf("*/", at + 2) : basic.IndexOfCharacter(at, '\r', '\n');
      end = end < 0 ? basic.Length : blocks ? end + 2 : end;
      comments.Replace(at, basic, at, end - at); basic.Clear(at, end - at);
      from = advance ? end : 0;
   }
   return (basic.ToString(), comments.ToString());
}
foreach (bool blocks in new[] { false, true }) foreach (int lines in new[] { 100, 500, 2000 }) {
   var source = string.Join("\n", Enumerable.Repeat(blocks ? "alpha CONST /* comment */ 42" : "alpha CONST 42 # comment", lines));
   var old = Scan(source, false, blocks); var next = Scan(source, true, blocks);
   if (old != next) throw new InvalidOperationException("Comment scan output differs."); checks++;
   foreach (bool advance in new[] { false, true }) {
      for (int i = 0; i < 3; i++) _ = Scan(source, advance, blocks);
      var samples = new List<object>();
      for (int i = 0; i < 7; i++) {
         long bytes = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp(); _ = Scan(source, advance, blocks);
         samples.Add(new { ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds, bytes = GC.GetAllocatedBytesForCurrentThread() - bytes });
      }
      string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
      rows.Add(new { blocks, lines, advance, samples, hashes = new[] { Hash(old.Item1), Hash(old.Item2) } });
   }
}
File.WriteAllText(args[0], JsonSerializer.Serialize(new { checks, scope = "Isolated legacy scan versus cursor prototype; excludes tokenizer/compiler/UI", rows }, new JsonSerializerOptions { WriteIndented = true }));
