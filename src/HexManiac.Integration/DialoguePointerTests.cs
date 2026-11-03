using HavenSoft.HexManiac.Core.Models;
using System;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Integration {
   public class DialoguePointerTests {
      [Theory]
      [InlineData(0x0F, 0x10, 0x80)]
      [InlineData(0x0F, 0x30, 0x180)]
      [InlineData(0x67, 0x10, 0x80)]
      [InlineData(0x67, 0x30, 0x180)]
      [InlineData(0x0F, 0x10, -0x08000000)] // null GBA pointer, handled by the caller's existing range guard
      [InlineData(0x67, 0x10, -0x08000000)]
      public void DialogueCommand_ReadsItsOwnPointerWithoutMutatingTheModel(int opcode, int command, int destination) {
         var model = new PokemonModel(new byte[0x200]);
         model.RawData[command] = (byte)opcode;
         if (opcode == 0x0F) model.RawData[command + 1] = 3; // a nonzero memory bank
         var pointer = command + (opcode == 0x0F ? 2 : 1);
         model.WritePointer(new ModelDelta(), pointer, destination);
         var before = model.RawData.ToArray();

         Assert.Equal(destination, ScriptTests.ReadDialogueTextStart(model, command));

         Assert.Equal(before, model.RawData);
      }

      [Fact]
      public void UnsupportedCommand_IsRejectedInsteadOfGuessingAPointer() {
         var model = new PokemonModel(new byte[0x200]);
         Assert.Throws<NotImplementedException>(() => ScriptTests.ReadDialogueTextStart(model, 0x10));
      }
   }
}
