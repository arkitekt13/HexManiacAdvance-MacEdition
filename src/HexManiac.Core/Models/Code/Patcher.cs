using HavenSoft.HexManiac.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HavenSoft.HexManiac.Core {
   public static class Patcher {
      // Bound untrusted patch allocations while allowing expanded GBA ROMs up to 64 MiB.
      public const int MaximumPatchedLength = 64 * 1024 * 1024;
      #region IPS Patching

      public static byte[] BuildIpsPatch(byte[] source, byte[] destination) {
         if (destination.Length == 0x1000000 && source.Length > destination.Length) throw new ArgumentOutOfRangeException(nameof(destination), "IPS cannot encode this truncation length; use UPS.");
         if (destination.Length > 0x1000000) throw new ArgumentOutOfRangeException(nameof(destination), "IPS addresses are limited to 24 bits; use UPS for larger ROMs.");
         var patch = new List<byte>();
         var originalLength = source.Length;
         if (source.Length < destination.Length) {
            source = source.Concat(new byte[destination.Length - source.Length]).ToArray();
         }

         // header
         patch.AddRange("PATCH".Select(c => (byte)c));

         // body
         for (int i = 0; i < destination.Length; i++) {
            if (source[i] == destination[i]) continue;
            // The reserved EOF offset must be encoded by a record beginning one byte earlier.
            if (i == 0x454F46) i--;
            int j = i + 1;
            while (j < destination.Length && source[j] != destination[j] && j - i < 0xFFFF) j++;
            patch.AddRange(BuildChunk(destination, i, j - i));
            if (j < destination.Length && source[j] != destination[j]) j -= 1; // need to re-check this byte, we exited due to the length limit
            i = j;
         }

         // footer
         // Force expansion even when the newly added tail consists entirely of zero bytes.
         if (destination.Length > originalLength && destination.Length > 0) {
            var last = destination.Length - 1;
            patch.AddRange(BuildChunk(destination, last == 0x454F46 ? last - 1 : last, last == 0x454F46 ? 2 : 1));
         }
         patch.AddRange("EOF".Select(c => (byte)c));
         if (destination.Length < originalLength) {
            patch.Add((byte)(destination.Length >> 16));
            patch.Add((byte)(destination.Length >> 8));
            patch.Add((byte)destination.Length);
         }
         return patch.ToArray();
      }

      public static byte[] BuildChunk(byte[] data, int start, int length) {
         var chunk = new List<byte>();
         bool compressed = length > 4 && length.Range().All(i => data[start + i] == data[start]);

         // offset
         chunk.Add((byte)(start >> 16));
         chunk.Add((byte)(start >> 8));
         chunk.Add((byte)start);

         // compressed content
         if (compressed) {
            chunk.AddRange(new byte[] { 0, 0, (byte)(length >> 8), (byte)length, data[start] });
            return chunk.ToArray();
         }

         // normal content
         chunk.AddRange(new byte[] { (byte)(length >> 8), (byte)length });
         chunk.AddRange(length.Range().Select(i => data[start + i]));

         return chunk.ToArray();
      }

      /// <returns>The first edited offset, or -1 for malformed input without changing the model.</returns>
      public static int ApplyIPSPatch(IDataModel model, byte[] patch, ModelDelta token) {
         if (patch == null || patch.Length < 8 || !patch.Take(5).Select(v => (char)v).SequenceEqual("PATCH")) return -1;
         if (model.Count > MaximumPatchedLength) return -1;
         var candidate = model.RawData.ToArray();
         var index = 5;
         var first = -1;
         while (index <= patch.Length - 3) {
            if (patch[index] == 'E' && patch[index + 1] == 'O' && patch[index + 2] == 'F') {
               index += 3;
               if (patch.Length - index == 3) {
                  var size = (patch[index] << 16) | (patch[index + 1] << 8) | patch[index + 2];
                  Array.Resize(ref candidate, size);
               } else if (index != patch.Length) return -1;
               CommitCandidate(model, candidate, token);
               return first < 0 ? 0 : first;
            }
            if (patch.Length - index < 5) return -1;
            var offset = (patch[index] << 16) | (patch[index + 1] << 8) | patch[index + 2];
            var length = (patch[index + 3] << 8) | patch[index + 4];
            index += 5;
            var repeated = length == 0;
            if (repeated) {
               if (patch.Length - index < 3) return -1;
               length = (patch[index] << 8) | patch[index + 1];
               index += 2;
               if (length == 0) return -1;
            }
            if (patch.Length - index < (repeated ? 1 : length) || offset > MaximumPatchedLength - length) return -1;
            if (candidate.Length < offset + length) Array.Resize(ref candidate, offset + length);
            if (repeated) { Array.Fill(candidate, patch[index++], offset, length); }
            else { Array.Copy(patch, index, candidate, offset, length); index += length; }
            if (first < 0) first = offset;
         }
         return -1;
      }

      // Only validated candidates reach the model; record changed spans rather than every byte.
      private static void CommitCandidate(IDataModel model, byte[] candidate, ModelDelta token) {
         if (candidate.Length > model.Count) model.ExpandData(token, candidate.Length - 1);
         int index = 0;
         while (index < candidate.Length) {
            if (model[index] == candidate[index]) { index++; continue; }
            var start = index++;
            while (index < candidate.Length && model[index] != candidate[index]) index++;
            token.ChangeData(model, start, candidate.Skip(start).Take(index - start).ToArray());
         }
         if (candidate.Length < model.Count) model.ContractData(token, candidate.Length - 1);
      }

      #endregion

      #region UPS Patching

      public enum UpsPatchDirection { Fail, SourceToDestination, DestinationToSource }

      public static byte[] BuildUpsPatch(byte[] source, byte[] destination) {
         var patch = new List<byte>();

         // header
         patch.AddRange("UPS1".Select(c => (byte)c));
         patch.AddRange(WriteVariableWidthInteger(source.Length));
         patch.AddRange(WriteVariableWidthInteger(destination.Length));

         // body
         int offset = 0;
         bool writing = false;
         // Include a truncated source tail in the XOR stream so reverse application is lossless.
         for (int i = 0; i < Math.Max(source.Length, destination.Length); i++) {
            var sourceVal = source.Length > i ? source[i] : (byte)0;
            var destinationVal = destination.Length > i ? destination[i] : (byte)0;
            if (sourceVal == destinationVal) {
               if (writing) {
                  writing = false;
                  offset = i + 1;
                  patch.Add(0);
               }
               continue;
            }
            if (!writing) {
               patch.AddRange(WriteVariableWidthInteger(i - offset));
               writing = true;
            }
            patch.Add((byte)(sourceVal ^ destinationVal));
         }
         if (writing) patch.Add(0);

         // footer
         patch.AddRange(BitConverter.GetBytes(CalcCRC32(source)));
         patch.AddRange(BitConverter.GetBytes(CalcCRC32(destination)));
         patch.AddRange(BitConverter.GetBytes(CalcCRC32(patch.ToArray())));

         return patch.ToArray();
      }

      // return -1 if the header is wrong (UPS1)
      // return -2 if the source file CRC doesn't match
      // return -3 if the patch file CRC doesn't match
      // return -4 if the source file size doesn't match
      // return -5 if the result file CRC doesn't match
      // return -6 if the UPS content didn't finish the last chunk with exactly 12 bytes left
      // return -7 if trying to write past the end of the destination file
      // returns a positive integer, the address of the first change, if everything worked correctly
      public static int ApplyUPSPatch(IDataModel model, byte[] patch, Func<ModelDelta> tokenFactory, bool ignoreChecksums, out UpsPatchDirection direction) {
         direction = UpsPatchDirection.Fail;
         if (patch == null || patch.Length < 18 || !patch.Take(4).Select(v => (char)v).SequenceEqual("UPS1")) return -1;
         var footer = patch.Length - 12;
         if (CalcCRC32(patch.Take(patch.Length - 4).ToArray()) != ReadChecksum(patch, patch.Length - 4)) return -3;
         var original = model.RawData;
         var sourceCrc = ReadChecksum(patch, footer);
         var destinationCrc = ReadChecksum(patch, footer + 4);
         var currentCrc = CalcCRC32(original);
         if (currentCrc == sourceCrc) direction = UpsPatchDirection.SourceToDestination;
         else if (currentCrc == destinationCrc) direction = UpsPatchDirection.DestinationToSource;
         else if (!ignoreChecksums) return -2;
         else direction = UpsPatchDirection.SourceToDestination;
         int index = 4;
         try {
            var sourceSize = ReadVariableWidthInteger(patch, ref index, footer);
            var destinationSize = ReadVariableWidthInteger(patch, ref index, footer);
            if (direction == UpsPatchDirection.DestinationToSource) (sourceSize, destinationSize) = (destinationSize, sourceSize);
            if (!ignoreChecksums && sourceSize != model.Count) return -4;
            var workLength = Math.Max(model.Count, Math.Max(sourceSize, destinationSize));
            if (workLength > MaximumPatchedLength) return -7;
            var candidate = new byte[workLength];
            Array.Copy(original, candidate, original.Length);
            int position = 0, first = -1;
            while (index < footer) {
               var skip = ReadVariableWidthInteger(patch, ref index, footer);
               if (position > workLength || skip > workLength - position) return -7;
               position += skip;
               var terminated = false;
               while (index < footer) {
                  var value = patch[index++];
                  if (value == 0) { position++; terminated = true; break; }
                  if (position >= workLength) return -7;
                  if (first < 0) first = position;
                  candidate[position++] ^= value;
               }
               if (!terminated) return -6;
            }
            Array.Resize(ref candidate, destinationSize);
            var expected = direction == UpsPatchDirection.SourceToDestination ? destinationCrc : sourceCrc;
            if (!ignoreChecksums && CalcCRC32(candidate) != expected) return -5;
            CommitCandidate(model, candidate, tokenFactory());
            return first < 0 ? 0 : first;
         } catch (FormatException) { return -6; }
      }

      private static int ReadChecksum(byte[] bytes, int index) =>
         bytes[index] | (bytes[index + 1] << 8) | (bytes[index + 2] << 16) | (bytes[index + 3] << 24);

      public static int CalcCRC32(byte[] array) => (int)Force.Crc32.Crc32Algorithm.Compute(array);

      public static int ReadVariableWidthInteger(byte[] data, ref int index) => ReadVariableWidthInteger(data, ref index, data.Length);

      private static int ReadVariableWidthInteger(byte[] data, ref int index, int limit) {
         long result = 0, shift = 1;
         for (int count = 0; count < 5 && index < limit; count++) {
            var value = data[index++];
            result += (value & 0x7F) * shift;
            if (result > int.MaxValue) throw new FormatException("Patch integer exceeds supported bounds.");
            if ((value & 0x80) != 0) return (int)result;
            shift <<= 7;
            result += shift;
         }
         throw new FormatException("Truncated or oversized patch integer.");
      }

      public static IEnumerable<byte> WriteVariableWidthInteger(int value) {
         do {
            var payload = value & 0x7F;
            value >>= 7;
            if (value == 0) payload |= 0x80;
            value -= 1;
            yield return (byte)payload;
         } while (value >= 0);
      }


      #endregion
   }
}
