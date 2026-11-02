using HavenSoft.HexManiac.Core.ViewModels;
using System;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class SyntaxHighlightingTests {
      private static TextEditorViewModel Create(string content) {
         var vm = new TextEditorViewModel {
            LineCommentHeader = "#", MultiLineCommentHeader = "/*", MultiLineCommentFooter = "*/",
         };
         foreach (var keyword in new[] { "lock", "sound", "flag.compare", "se_pin" }) vm.Keywords.Add(keyword);
         foreach (var constant in new[] { "se_pin", "FLAG_READY", "\"NICE NAME\"", "2to5Hits" }) vm.Constants.Add(constant);
         vm.Content = content;
         return vm;
      }

      [Theory]
      [InlineData("lock\n", "lock", TextFormatting.Keyword)]
      [InlineData("LOCK\n", "LOCK", TextFormatting.None)]
      [InlineData("lock_extra\n", "lock_extra", TextFormatting.None)]
      [InlineData("flag.compare\n", "flag.compare", TextFormatting.Keyword)]
      [InlineData("flag.compare.extra\n", "flag.compare.extra", TextFormatting.None)]
      [InlineData("sound FLAG_READY\n", "FLAG_READY", TextFormatting.Constant)]
      [InlineData("sound flag_ready\n", "flag_ready", TextFormatting.None)]
      [InlineData("sound 2to5Hits\n", "2to5Hits", TextFormatting.Constant)]
      [InlineData("sound \"NICE NAME\"\n", "\"NICE NAME\"", TextFormatting.Constant)]
      [InlineData("sound se_pin\n", "se_pin", TextFormatting.Keyword)]
      [InlineData("# lock FLAG_READY 123\n", "lock FLAG_READY 123", TextFormatting.Comment)]
      [InlineData("/* lock\nFLAG_READY */\n", "FLAG_READY", TextFormatting.Comment)]
      [InlineData("lock 0x4000\n", "0x4000", TextFormatting.Numeric)]
      [InlineData("sound 123\n", "123", TextFormatting.Numeric)]
      public void TokensKeepTheirExistingClassificationAndIndices(string content, string token, TextFormatting expected) {
         var vm = Create(content);
         var layers = new[] { vm.PlainContent, vm.AccentContent, vm.ConstantContent,
            vm.NumericContent, vm.CommentContent, vm.TextContent };
         var formatting = new[] { TextFormatting.None, TextFormatting.Keyword, TextFormatting.Constant,
            TextFormatting.Numeric, TextFormatting.Comment, TextFormatting.Text };
         var start = content.IndexOf(token, StringComparison.Ordinal);
         Assert.True(start >= 0);
         for (int layer = 0; layer < layers.Length; layer++) {
            Assert.Equal(content.Length, layers[layer].Length);
            for (int i = start; i < start + token.Length; i++) {
               if (char.IsWhiteSpace(content[i])) continue;
               if (formatting[layer] == expected) Assert.Equal(content[i], layers[layer][i]);
               else Assert.True(char.IsWhiteSpace(layers[layer][i]) || layers[layer][i] == '\0');
            }
         }
         Assert.Equal(content, vm.Content);
      }

      [Fact]
      public void DictionaryEditsReclassifyWithoutChangingSourceCaretOrContentNotifications() {
         var vm = new TextEditorViewModel { Content = "CUSTOM VALUE\n" };
         vm.CaretIndex = 4;
         var contentNotifications = 0;
         vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.Content)) contentNotifications++; };

         vm.Constants.Add("VALUE");
         Assert.Equal("VALUE", vm.ConstantContent.Substring(7, 5));
         vm.Keywords.Add("CUSTOM");
         Assert.Equal("CUSTOM", vm.AccentContent.Substring(0, 6));
         vm.Keywords[0] = "VALUE";
         Assert.Equal("CUSTOM", vm.PlainContent.Substring(0, 6));
         Assert.Equal("VALUE", vm.AccentContent.Substring(7, 5));
         vm.Keywords.Clear();
         Assert.Equal("VALUE", vm.ConstantContent.Substring(7, 5));
         vm.Constants.Remove("VALUE");
         Assert.Equal("VALUE", vm.PlainContent.Substring(7, 5));
         Assert.Equal("CUSTOM VALUE\n", vm.Content);
         Assert.Equal(4, vm.CaretIndex);
         Assert.Equal(0, contentNotifications);
      }

      [Fact]
      public void DuplicateKeywordsAndConstantsKeepKeywordPrecedence() {
         var vm = Create("se_pin\n");
         vm.Keywords.Add("se_pin");
         vm.Constants.Add("se_pin");
         vm.Keywords.Remove("se_pin");
         Assert.StartsWith("se_pin", vm.AccentContent);
         vm.Keywords.Clear();
         Assert.StartsWith("se_pin", vm.ConstantContent);
      }

      [Theory]
      [InlineData("\n")]
      [InlineData("\r\n")]
      [InlineData("\r")]
      public void LargeScriptKeepsAllLineEndingsAndClassification(string newline) {
         var content = string.Join(newline, Enumerable.Repeat("lock\tsound FLAG_READY 0x4000", 300));
         var vm = Create(content);
         var layers = new[] { vm.PlainContent, vm.AccentContent, vm.ConstantContent,
            vm.NumericContent, vm.CommentContent, vm.TextContent };
         foreach (var layer in layers) {
            Assert.Equal(content.Length, layer.Length);
            for (int i = 0; i < content.Length; i++)
               if (content[i] == '\r' || content[i] == '\n' || content[i] == '\t') Assert.Equal(content[i], layer[i]);
         }
         Assert.Equal(300, vm.AccentContent.Split("lock").Length - 1);
         Assert.Equal(300, vm.ConstantContent.Split("FLAG_READY").Length - 1);
         Assert.Equal(content, vm.Content);
      }

      [Fact]
      public void NumericLookingConstantsStillTakePrecedenceOverNumericColoring() {
         var vm = new TextEditorViewModel();
         vm.Constants.Add("123");
         vm.Content = "123\n";
         Assert.StartsWith("123", vm.ConstantContent);
         Assert.DoesNotContain("123", vm.NumericContent);
      }

      [Theory]
      [InlineData("\"quoted\"")]
      [InlineData("\"\"")]
      [InlineData("before \"last\"")]
      [InlineData("\"two words\"")]
      public void ClosingQuoteAtEndOfDocumentKeepsTheOriginalPlainText(string content) {
         var vm = new TextEditorViewModel();
         vm.Keywords.Add("quoted");
         vm.Constants.Add("different");
         vm.Content = content;
         Assert.Equal(content, vm.PlainContent);
         Assert.Equal(content, vm.Content);
         Assert.Equal(content.Length, vm.AccentContent.Length);
         Assert.Equal(content.Length, vm.ConstantContent.Length);
      }
   }
}
