using System.Text.Json;
using HavenSoft.HexManiac.Core.ViewModels;

int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
string[] Layers(TextEditorViewModel vm) => new[] { vm.PlainContent, vm.AccentContent, vm.ConstantContent, vm.NumericContent, vm.CommentContent, vm.TextContent };
TextEditorViewModel Model(string source, string line, string start, string end, bool syntax, bool pre = false) {
   var vm = new TextEditorViewModel(syntax) { LineCommentHeader = line, MultiLineCommentHeader = start, MultiLineCommentFooter = end };
   foreach (var keyword in new[] { "lock", "end", "alpha", "move.player" }) vm.Keywords.Add(keyword);
   foreach (var constant in new[] { "CONST", "temp1", "éclair" }) vm.Constants.Add(constant);
   if (pre) vm.PreFormatter = new Preformatter();
   vm.Content = source; return vm;
}
// Independent legacy oracle: ordinal string searches restart at zero over a mutable character
// array; reproduce comment precedence/blanking, then let the unchanged tokenizer color residue.
(string Plain, string Comments) LegacyComments(string source, string line, string start, string end) {
   var basic = source.ToCharArray(); var comments = new char[basic.Length]; int iterations = 0;
   for (int i = 0; i < basic.Length; i++) if (basic[i] is '\r' or '\n' or '\t') comments[i] = basic[i];
   void CopyAndClear(int first, int last) {
      if (++iterations > source.Length * 2 + 1) throw new InvalidOperationException("Nonterminating legacy marker fixture.");
      for (int i = first; i < last; i++) { comments[i] = basic[i]; if (basic[i] is not ('\r' or '\n' or '\t')) basic[i] = ' '; }
   }
   while (!string.IsNullOrEmpty(line)) {
      var text = new string(basic); int at = text.IndexOf(line, StringComparison.Ordinal); if (at < 0) break;
      int last = at; while (last < basic.Length && basic[last] is not ('\r' or '\n')) last++;
      CopyAndClear(at, last);
   }
   while (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end)) {
      var text = new string(basic); int at = text.IndexOf(start, StringComparison.Ordinal); if (at < 0) break;
      int last = text.IndexOf(end, at + start.Length, StringComparison.Ordinal);
      CopyAndClear(at, last < 0 ? basic.Length : last + end.Length);
   }
   return (new string(basic), new string(comments));
}
var samples = new[] { "", "# EOF", "lock # tail\nend", "\talpha CONST 0x42 # comment\r\nend", "#first\r#second\n#third\r\nend", "\"# inside quotes\" CONST", "/* a */alpha/* b */42", "/* outer /* nested */ end */", "lock /* missing close", "/*a*/#last", "//a\n/* b */end", "éclair #😀 comment\nTEXT", "/*/* tail", "aaaXaaaYaaa", "\t#tab\r\n", "lock\n\nend" };
var markers = new[] { ("#", "/*", "*/"), ("//", "/*", "*/"), ("", "/*", "*/"), ("#", "", "*/"), ("#", "/*", ""), ("", "aaa", "aaa"), ("/* ", "", ""), ("", "/* ", "*/"), ("\t#", "{", "}"), ("", "", "") };
var random = new Random(7619);
var tokens = new[] { "alpha", "CONST", "temp1", "42", "0x20", "# text", "// text", "/*", "*/", "\n", "\r\n", "\t", "\"text\"", "éclair", "😀", " ", "{", "}" };
var corpus = samples.Concat(Enumerable.Range(0, 100).Select(_ => string.Concat(Enumerable.Range(0, random.Next(1, 35)).Select(_ => tokens[random.Next(tokens.Length)])))).ToArray();
foreach (var source in corpus) foreach (var (line, start, end) in markers) foreach (bool syntax in new[] { false, true }) {
   var expected = LegacyComments(source, line, start, end);
   var residue = Model(expected.Plain, "", "", "", syntax);
   var actual = Model(source, line, start, end, syntax);
   var wanted = Layers(residue); wanted[4] = expected.Comments;
   Check(Layers(actual).SequenceEqual(wanted), "All six layers must match the legacy comment precedence and tokenizer output.");
   Check(actual.Content == source && Layers(actual).All(layer => layer.Length == source.Length), "Source and layer indices must remain unchanged.");
}
// Frozen before optimization at 769825a9: preformatting can erase part of a marker or color
// content inside a comment, so compare complete layer strings rather than guessing precedence.
var preSources = new[] { "lock # CONST 42 TEXT\nend", "/* lock CONST 42 TEXT */end", "# already\nlock", "lock /* missing CONST", "\tlock\r\n# CONST\rTEXT", "éclair /* TEXT */ 42" };
var pre = preSources.Select(source => Layers(Model(source, "#", "/*", "*/", true, true))).ToArray();
if (args.Length == 2 && args[0] == "--record-preformatter") { File.WriteAllText(args[1], JsonSerializer.Serialize(pre, new JsonSerializerOptions { WriteIndented = true })); return; }
var baseline = JsonSerializer.Deserialize<string[][]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "PreformatterBaseline.json")))!;
for (int i = 0; i < pre.Length; i++) {
   Check(pre[i].SequenceEqual(baseline[i]), "Preformatted layers must match the frozen before-change baseline.");
   Check(Model(preSources[i], "#", "/*", "*/", true, true).ErrorLocations.Count == 1, "Preformatter diagnostics must be retained.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class Preformatter : ITextPreProcessor {
   public TextFormatting[] Format(string content) {
      var result = new TextFormatting[content.Length];
      foreach (var (word, kind) in new[] { ("lock", TextFormatting.Keyword), ("CONST", TextFormatting.Constant), ("42", TextFormatting.Numeric), ("TEXT", TextFormatting.Text), ("# already", TextFormatting.Comment) }) {
         int start = 0, at; while ((at = content.IndexOf(word, start, StringComparison.Ordinal)) >= 0) {
            Array.Fill(result, kind, at, word.Length); start = at + word.Length;
         }
      }
      return result;
   }
   public IEnumerable<TextSegment> FindErrors(string content) { yield return new TextSegment(0, 0, 1, SegmentType.Warning); }
}
