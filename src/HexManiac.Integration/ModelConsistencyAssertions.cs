using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Integration {
   // Test-only counterpart of PokemonModel.ResolveConflicts using its public inspection APIs.
   // Keep these rules aligned with that diagnostic, including its allowed invalid-ROM pointers.
   // xUnit assertions execute in Debug and Release; no production validation behavior changes.
   internal static class ModelConsistencyAssertions {
      public static void AssertNoConflicts(PokemonModel model) {
         model.ThreadlockRuns(() => {
            // Public snapshots preserve model run order and the original checker's rules.
            var runs = model.All<IFormattedRun>().ToList();
            var anchorForAddress = model.Anchors.ToDictionary(
               name => model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, name), name => name);
            for (var i = 0; i < runs.Count; i++) {
               if (!anchorForAddress.TryGetValue(runs[i].Start, out var pointerSourceName)) pointerSourceName = string.Empty;
               else pointerSourceName = " (" + pointerSourceName + ")";

               // for every pointer run, make sure that the thing it points to knows about it
               if (runs[i] is PointerRun pointerRun) {
                  var destination = model.ReadPointer(pointerRun.Start);
                  var run = model.GetNextRun(destination);
                  if (destination < 0 || destination >= model.Count) {
                     // pointer points outside scope. Such a pointer is an error, but is not a metadata inconsistency.
                  } else if (run is ArrayRun arrayRun1 && arrayRun1.SupportsInnerPointers) {
                     var offsets = arrayRun1.ConvertByteOffsetToArrayOffset(destination);
                     if (offsets.SegmentOffset == 0 && offsets.SegmentIndex == 0) {
                        Assert.True(arrayRun1.PointerSourcesForInnerElements[offsets.ElementIndex].Contains(pointerRun.Start),
                           $"Expected array at {arrayRun1.Start.ToAddress()} element {offsets.ElementIndex} to know about the pointer at {pointerRun.Start.ToAddress()}.");
                     }
                     if (offsets.ElementIndex == 0) Assert.Contains(pointerRun.Start, run.PointerSources);
                  } else if (run.Start < destination) {
                     // pointer points into the middle of a run. Such a pointer is an error, but is not a metadata inconsistency.
                  } else if (run.Start > destination) {
                     Assert.True(false, $"Pointer at {pointerRun.Start:X6} expected a run at {destination:X6} but the next run was at {run.Start:X6}.");
                  } else if (run != NoInfoRun.NullRun) {
                     Assert.True(run.PointerSources != null && run.PointerSources.Contains(pointerRun.Start), $"Expected run at {run.Start:X6} to know about pointer at {pointerRun.Start:X6}, but it did not.");
                  }
               }

               // for every TPTRun, make sure something points to it
               if (runs[i] is TrainerPokemonTeamRun) Assert.True(runs[i].PointerSources.Count > 0, "TPTRuns must not exist with no sources long-term.");

               // for ever NoInfoRun, something points to it
               if ((runs[i] is NoInfoRun || runs[i] is PointerRun) && !anchorForAddress.ContainsKey(runs[i].Start)) {
                  Assert.True(runs[i].PointerSources == null || runs[i].PointerSources.Count > 0, $"{runs[i].Start:X6}: Unnamed NoInfoRuns must have something pointing to them!");
               }

               // for every run with sources, make sure the pointer at that source actually points to it
               if (runs[i].PointerSources != null) {
                  foreach (var source in runs[i].PointerSources) {
                     var run = model.GetNextRun(source);
                     if (run is PointerRun) {
                        Assert.True(run.Start == source, $"{runs[i].Start:X6}{pointerSourceName} expects a pointer at {source:X6}, but the next pointer was found at {run.Start:X6}.");
                        Assert.True(model.ReadPointer(source) == runs[i].Start, $"Expected {source:X6} to point to {runs[i].Start:X6}{pointerSourceName}");
                     } else if (run is ITableRun) {
                        Assert.True(run.Start <= source, $"The run at {runs[i].Start:X6} expects a pointer at {source:X6}, but found a table at {run.Start:X6}.");
                        var destination = model.ReadPointer(source);
                        Assert.True(destination == runs[i].Start, $"The run at {runs[i].Start:X6} expects a pointer at {source:X6}, but that source points to {destination:X6}.");
                     } else {
                        Assert.True(false, $"Pointer at {source:X6} must be a {nameof(PointerRun)} or live within an {nameof(ITableRun)} (pointing to {runs[i].Start:X6})");
                     }
                  }
               }
               if (runs[i] is ArrayRun arrayRun2 && arrayRun2.SupportsInnerPointers) {
                  for (var j = 0; j < arrayRun2.ElementCount; j++) {
                     foreach (var source in arrayRun2.PointerSourcesForInnerElements[j]) {
                        var run = model.GetNextRun(source);
                        if (run is PointerRun) {
                           Assert.True(run.Start == source, $"{runs[i].Start:X6}{pointerSourceName} index {j} expects a pointer at {source:X6}, but the next pointer was found at {run.Start:X6}.");
                           Assert.True(model.ReadPointer(source) == runs[i].Start + arrayRun2.ElementLength * j, $"Expected {source:X6} to point to {runs[i].Start:X6}{pointerSourceName} index {j}");
                        } else if (run is ITableRun) {
                           Assert.True(model.ReadPointer(source) == runs[i].Start + arrayRun2.ElementLength * j, $"Expected {source:X6} to point to {runs[i].Start:X6}{pointerSourceName} index {j}");
                        } else {
                           Assert.True(false, $"Pointer at {source:X6} must be a {nameof(PointerRun)} or live within an {nameof(ITableRun)}");
                        }
                     }
                  }
               }

               // for every table, make sure the things it points to know about the table
               if (runs[i] is ITableRun tableRun) {
                  var elementOffset = 0;
                  foreach (var segment in tableRun.ElementContent) {
                     if (segment.Type != ElementContentType.Pointer) { elementOffset += segment.Length; continue; }
                     for (var j = 0; j < tableRun.ElementCount; j++) {
                        var start = tableRun.Start + elementOffset + tableRun.ElementLength * j;
                        var destination = model.ReadPointer(start);
                        var run = model.GetNextRun(destination);
                        if (destination < 0 || destination >= model.Count) {
                           // pointer points outside scope. Such a pointer is an error, but is not a metadata inconsistency.
                        } else if (run is ArrayRun arrayRun1 && arrayRun1.SupportsInnerPointers) {
                           var offsets = arrayRun1.ConvertByteOffsetToArrayOffset(destination);
                           if (offsets.SegmentOffset == 0 && offsets.SegmentIndex == 0) {
                              Assert.True(arrayRun1.PointerSourcesForInnerElements[offsets.ElementIndex].Contains(start), $"Expected InnerPointer to {start:X6} (from {arrayRun1.Start:X6}[{offsets.ElementIndex}])");
                              if (offsets.ElementIndex == 0) Assert.Contains(start, run.PointerSources);
                           } else {
                              // pointer points into an element (not the beginning). This is an error, but is not a metadata inconsistency.
                           }
                        } else if (run is ITableRun && run.Start < destination) {
                           // exception: tables are allowed to have pointers that point randomly into other runs.
                           // such a thing is a data error in the ROM, but is not a metadata inconsistency.
                        } else if (run.Start != destination) {
                           // for tables, the invalidly point into a run. Such is an error in the data, but is allowed for the metadata.
                        } else {
                           if (run.PointerSources != null) {
                              Assert.True(run.PointerSources.Contains(start), $"Expected {run.Start:X6} to know about pointer {start:X6} (within table {tableRun.Start:X6}), but it did not.");
                           } else {
                              Assert.True(false, "This run is referenced by a table, but doesn't know about the table that points to it.");
                           }
                        }
                     }
                     elementOffset += segment.Length;
                  }
               }

               if (i == runs.Count - 1 || runs[i].Start + runs[i].Length <= runs[i + 1].Start) continue;
               var debugRunStart1 = runs[i].Start.ToString("X6");
               var debugRunStart2 = runs[i + 1].Start.ToString("X6");
               Assert.True(false, $"Conflict: there's a run that ends after the next run starts! {debugRunStart1}{pointerSourceName} and {debugRunStart2}");
            }

            // For every table with a matched-length, verify that the length is as expected.
            // (The child array length must still be at least 1.)
            var token = new NoDataChangeDeltaModel();
            foreach (var array in model.Arrays) {
               if (string.IsNullOrEmpty(array.LengthFromAnchor)) continue;
               var parentName = array.LengthFromAnchor;
               var childName = model.GetAnchorFromAddress(-1, array.Start);
               var set = model.GetMatchedWords(parentName);
               if (set.Count > 0) {
                  foreach (var wordAddress in set) {
                     if (model.GetNextRun(wordAddress) is WordRun word) {
                        var expectedElementCount = (model.ReadMultiByteValue(word.Start, word.Length) - word.ValueOffset) / word.MultOffset;
                        Assert.True(array.ElementCount == expectedElementCount, $"Expected {childName} to have {expectedElementCount} elements because of {parentName}, but it had {array.ElementCount} elements instead!");
                     } else {
                        Assert.True(false, "Expected a constant at " + wordAddress.ToAddress() + " but didn't find one!");
                     }
                  }
               }
               if (!(model.GetNextRun(model.GetAddressFromAnchor(token, -1, array.LengthFromAnchor)) is ITableRun parent)) continue;
               if (array.ParentOffset.BeginningMargin + array.ParentOffset.EndMargin + parent.ElementCount > 0) {
                  var expectedChildLength = parent.ElementCount + array.ParentOffset.BeginningMargin + array.ParentOffset.EndMargin;
                  Assert.True(expectedChildLength == array.ElementCount, $"Expected table {childName} to be {expectedChildLength} elements based on {parentName}, but it was {array.ElementCount} elements instead.");
               } else {
                  Assert.True(array.ElementCount == 1);
               }
            }
         });
      }
   }
}
