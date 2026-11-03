using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using System;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class ScriptNewlineTests : BaseViewModelTestClass {
      [Theory]
      [InlineData("\n")]
      [InlineData("\r\n")]
      [InlineData("\r")]
      public void CompleteDialogueBlock_Normalization_IsIdempotentAndKeepsCaret(string newline) {
         var script = string.Join(newline, "loadpointer 0 <000100>", "{", "Hello", "}", "end");
         var original = script;
         var caret = script.IndexOf("Hello", StringComparison.Ordinal) + 2;

         Assert.Equal(0, ScriptParser.InsertMissingClosers(ref script, caret));
         Assert.Equal(original, script);
         Assert.Equal(0, ScriptParser.InsertMissingClosers(ref script, caret));
         Assert.Equal(original, script);
      }

      [Fact]
      public void CompleteMixedNewlineBlock_DoesNotInsertBlankDialogueLines() {
         var script = "loadpointer 0 <000100>\r\n{\nHello\r\n}\nend";
         var original = script;
         Assert.Equal(0, ScriptParser.InsertMissingClosers(ref script, 0));
         Assert.Equal(original, script);
      }

      [Theory]
      [InlineData("\n")]
      [InlineData("\r\n")]
      [InlineData("\r")]
      public void MissingCloser_BeforeEndCommand_PreservesCommand(string newline) {
         var script = "loadpointer 0 <000100>" + newline + "{" + newline + "end";
         ScriptParser.InsertMissingClosers(ref script, 0);
         var lines = script.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
         Assert.Equal(new[] { "loadpointer 0 <000100>", "{", "}", "end" }, lines);
      }

      [Theory]
      [InlineData("\n")]
      [InlineData("\r\n")]
      [InlineData("\r")]
      public void OpeningBraceAtEnd_CaretMovesIntoEmptyBody(string newline) {
         var script = "loadpointer 0 <000100>" + newline + "{";
         var caret = script.Length;
         var delta = ScriptParser.InsertMissingClosers(ref script, caret);
         Assert.Equal("loadpointer 0 <000100>" + newline + "{" + newline + newline + "}", script);
         Assert.Equal(script.IndexOf('{') + 1 + newline.Length, caret + delta);
         Assert.Equal(0, ScriptParser.InsertMissingClosers(ref script, caret + delta));
      }

      [Theory]
      [InlineData(0)]
      [InlineData(1)]
      public void LoneOpeningBrace_ProducesEditableBodyWithBoundedCaret(int caret) {
         var script = "{";
         var delta = ScriptParser.InsertMissingClosers(ref script, caret);
         Assert.Equal("{" + Environment.NewLine + Environment.NewLine + "}", script);
         Assert.Equal(caret == 0 ? 0 : 1 + Environment.NewLine.Length, caret + delta);
      }

      [Fact]
      public void InlineBraces_MoveToOwnLines_KeepCaretWithinDialogue() {
         var script = "loadpointer 0 <000100>{Hello}end";
         var caret = script.IndexOf("Hello", StringComparison.Ordinal) + 2;
         var delta = ScriptParser.InsertMissingClosers(ref script, caret);
         Assert.Equal(string.Join(Environment.NewLine, "loadpointer 0 <000100>", "{", "Hello", "}", "end"), script);
         Assert.Equal(script.IndexOf("Hello", StringComparison.Ordinal) + 2, caret + delta);
      }

      [Theory]
      [InlineData("\n")]
      [InlineData("\r\n")]
      [InlineData("\r")]
      public void MultilineDialogue_PreservesIntentionalBlankLineAndExactEncodedLength(string newline) {
         SetFullModel(0xFF);
         var tool = ViewPort.Tools.CodeTool;
         tool.IsSelected = true;
         tool.Mode = CodeMode.Script;
         tool.Contents[0].Content = string.Join(newline, "loadpointer 0 <000100>", "{", "Hello", "", "World", "}", "end");

         var expected = Model.TextConverter.Convert("Hello" + Environment.NewLine + Environment.NewLine + "World", out var _).ToArray();
         var run = (PCSRun)Model.GetNextRun(0x100);
         Assert.False(tool.Contents[0].HasError);
         Assert.Equal(expected.Length, run.Length);
         Assert.Equal(expected, Model.RawData.Skip(run.Start).Take(run.Length).ToArray());
      }

      [Theory]
      [InlineData("\n")]
      [InlineData("\r\n")]
      [InlineData("\r")]
      public void CompileInlineDialogue_KeepsExactTextPointerAndUndo(string newline) {
         SetFullModel(0xFF);
         var before = Model.RawData.ToArray();
         var tool = ViewPort.Tools.CodeTool;
         tool.IsSelected = true;
         tool.Mode = CodeMode.Script;
         tool.Contents[0].Content = string.Join(newline, "loadpointer 0 <000100>", "{", "Hello", "}", "end");

         Assert.Empty(Errors);
         Assert.False(tool.Contents[0].HasError);
         Assert.Equal(0x100, Model.ReadPointer(2));
         Assert.Equal("Hello", ((PCSRun)Model.GetNextRun(0x100)).SerializeRun());
         Assert.Equal(new byte[] { 0x0F, 0x00, 0x00, 0x01, 0x00, 0x08, 0x02 }, Model.RawData.Take(7).ToArray());
         ViewPort.ChangeHistory.ChangeCompleted();
         // Compilation may create separate stream/format/code history entries; check the complete round trip.
         var undoCount = 0;
         while (ViewPort.Undo.CanExecute(null) && undoCount < 10) {
            ViewPort.Undo.Execute();
            undoCount++;
         }
         Assert.InRange(undoCount, 1, 9);
         Assert.Equal(before, Model.RawData);
         for (var i = 0; i < undoCount; i++) {
            Assert.True(ViewPort.Redo.CanExecute(null));
            ViewPort.Redo.Execute();
         }
         Assert.Equal("Hello", ((PCSRun)Model.GetNextRun(Model.ReadPointer(2))).SerializeRun());
      }
   }
}
