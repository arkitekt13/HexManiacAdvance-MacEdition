using System;
using System.Buffers.Binary;
using System.IO;
using HavenSoft.HexManiac.Core;

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

/// <summary>Detached, bounded BPS1 decoding for the bundled SCADE installer, not a file-import route.</summary>
public static class BpsPatch {
   public static byte[] Apply(byte[] source, byte[] patch) {
      if (source == null || patch == null || source.Length > Patcher.MaximumPatchedLength ||
          patch.Length > Patcher.MaximumPatchedLength || patch.Length < 19 ||
          !patch.AsSpan(0, 4).SequenceEqual("BPS1"u8)) throw Invalid("Invalid BPS header or size.");
      int end = patch.Length - 12;
      uint Checksum(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(patch.AsSpan(offset, 4));
      if (Force.Crc32.Crc32Algorithm.Compute(patch, 0, patch.Length - 4) != Checksum(patch.Length - 4))
         throw Invalid("The BPS patch checksum does not match.");
      var reader = new Reader(patch, end);
      ulong sourceLength = reader.Number(), targetLength = reader.Number(), metadataLength = reader.Number();
      if (sourceLength != (ulong)source.Length || (uint)Patcher.CalcCRC32(source) != Checksum(end))
         throw Invalid("The BPS patch requires a different unmodified base ROM.");
      if (targetLength > Patcher.MaximumPatchedLength) throw Invalid("The BPS output exceeds the supported ROM size.");
      reader.Skip(metadataLength);
      var target = new byte[(int)targetLength];
      int output = 0;
      long sourceRelative = 0, targetRelative = 0;
      while (reader.Position < end) {
         ulong action = reader.Number(), length = (action >> 2) + 1;
         if (length > (ulong)(target.Length - output)) throw Invalid("BPS command exceeds the output size.");
         int count = (int)length;
         switch (action & 3) {
            case 0: // SourceRead at the current output offset.
               if ((long)output + count > source.Length) throw Invalid("BPS source read is out of bounds.");
               source.AsSpan(output, count).CopyTo(target.AsSpan(output));
               break;
            case 1:
               reader.CopyTo(target.AsSpan(output, count));
               break;
            case 2:
               sourceRelative = Move(sourceRelative, reader.Number());
               if (sourceRelative < 0 || sourceRelative > source.Length - (long)count)
                  throw Invalid("BPS source copy is out of bounds.");
               source.AsSpan((int)sourceRelative, count).CopyTo(target.AsSpan(output));
               sourceRelative += count;
               break;
            case 3:
               targetRelative = Move(targetRelative, reader.Number());
               if (targetRelative < 0 || targetRelative >= output) throw Invalid("BPS target copy refers to unwritten data.");
               // Intentionally sequential: overlapping copies repeat newly written bytes.
               for (int i = 0; i < count; i++) target[output + i] = target[(int)targetRelative++];
               break;
         }
         output += count;
      }
      if (output != target.Length || reader.Position != end) throw Invalid("BPS output or command stream is incomplete.");
      if ((uint)Patcher.CalcCRC32(target) != Checksum(end + 4)) throw Invalid("The patched ROM checksum does not match.");
      return target;
   }

   private static long Move(long position, ulong encoded) {
      var distance = encoded >> 1; // At most Int64.MaxValue, even for UInt64.MaxValue.
      try { return checked(position + ((encoded & 1) == 0 ? (long)distance : -(long)distance)); }
      catch (OverflowException) { throw Invalid("BPS relative offset overflow."); }
   }
   private static InvalidDataException Invalid(string message) => new(message);

   private sealed class Reader(byte[] bytes, int end) {
      public int Position { get; private set; } = 4;
      public ulong Number() {
         ulong value = 0, shift = 1;
         try {
            while (true) {
               if (Position >= end) throw Invalid("Truncated BPS integer.");
               byte next = bytes[Position++];
               value = checked(value + (ulong)(next & 127) * shift);
               if ((next & 128) != 0) return value;
               shift = checked(shift * 128);
               value = checked(value + shift);
            }
         } catch (OverflowException) { throw Invalid("BPS integer overflow."); }
      }
      public void Skip(ulong count) {
         if (count > (ulong)(end - Position)) throw Invalid("Truncated BPS metadata or literal data.");
         Position += (int)count;
      }
      public void CopyTo(Span<byte> destination) {
         int start = Position;
         Skip((ulong)destination.Length);
         bytes.AsSpan(start, destination.Length).CopyTo(destination);
      }
   }
}
