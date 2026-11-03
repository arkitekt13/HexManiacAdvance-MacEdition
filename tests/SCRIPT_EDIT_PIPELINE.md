# Script editing investigation — 2026-11-03

Baseline: `8aaa6755`, macOS ARM64, .NET 8, Release. Investigation branch:
`investigate/script-edit-pipeline`. Application code is unchanged. No supplied ROM was opened.

## What happens on an edit

1. TextEditorViewModel.Content rebuilds six full-document syntax layers synchronously.
2. CodeBody forwards the change to CodeTool.ScriptChanged and checks embedded text lengths.
3. ScriptChanged performs immediate completion/closer work and schedules compilation after
   a 500 ms pause. It also finds the existing script length before scheduling.
4. The deferred action compiles, updates bytes/formats/pointers as needed, and refreshes
   associated script bodies. Its full duration is distinct from the half-second idle wait.
5. The Mac TextEditor measures document width with FormattedText on TextChanged and maintains
   six display layers plus the transparent editable TextBox. Diagnostic decorations are
   already coalesced by our earlier optimization.

Source locations: TextEditorViewModel.cs:49/98; CodeBody.cs:398/418/442;
CodeTool.cs:295/366/439; Avalonia Controls/TextEditor.axaml.cs:91/209.

## Confirmed compiler bottleneck

Two warmups and seven measured calls per case; medians below are isolated component probes.
The pipeline's timer-owner records identify the deferred action as CodeTool, rather than an
unrelated table-refresh timer. These component timings are not meant to be summed.

| Operation | 500 repeated commands, ms | 2,000 repeated commands, ms |
| --- | ---: | ---: |
| Complete deferred update, no UI attached | 703.70 | 2,811.31 |
| Compile only | 695.94 | 2,789.86 |
| Find script length | 0.22 | 0.82 |
| Collect associated scripts | 0.24 | 0.79 |
| Format script | 0.34 | 1.24 |
| Embedded text-length check | 2.98 | 11.78 |

The complete deferred update allocates 1,083,112,328 bytes per 500-command edit and
4,325,330,944 bytes per 2,000-command edit on the measured thread. These are temporary
managed allocations over an operation, not retained RAM or whole-process/native memory.

## Attached text-control measurements

All figures are median milliseconds from the 18-case baseline; UI columns use the standalone
headless production TextEditor. Deferred compilation excludes the 500 ms idle wait. Presentation
combines queued layout/render work before and after compilation; columns are separate medians.

| Workload | Repeated statements | Immediate, no UI | Immediate, UI | Deferred, UI | Presentation, UI |
| --- | ---: | ---: | ---: | ---: | ---: |
| commands | 100 | 1.17 | 0.99 | 148.99 | 8.84 |
| commands | 500 | 3.81 | 5.72 | 709.42 | 29.86 |
| commands | 2,000 | 13.81 | 47.63 | 2795.41 | 148.98 |
| comments | 100 | 1.19 | 1.24 | 143.19 | 9.09 |
| comments | 500 | 10.41 | 10.52 | 707.13 | 50.97 |
| comments | 2,000 | 93.25 | 113.03 | 2802.28 | 233.23 |
| dialogue | 100 | 1.19 | 0.95 | 144.61 | 6.53 |
| dialogue | 500 | 4.91 | 6.24 | 665.98 | 40.69 |
| dialogue | 2,000 | 19.43 | 50.71 | 2653.95 | 166.57 |

At 2,000 comment-rich statements, immediate work also grows noticeably. Source inspection
identifies repeated comment searches starting at the beginning of the document in the
highlighter, but that specific loop has not yet been independently timed or optimized.
The UI cost combines width measurement, bindings, layout, text shaping, and headless rendering;
this benchmark does not attribute all of it to the width-measurement handler alone.

## Why the variable spelling matters

The shipped default `scriptvariablealiases` list has **32,784 slots, 32,736 empty** in this
fixture: only 48 names are populated, at high variable IDs. ScriptArg.Convert (line 140)
tries enum/alias matching before hexadecimal parsing. ArrayRunEnumSegment.TryMatch (line 287)
walks the list, converts empty slots into numeric strings, normalizes options, and evaluates
partial matches. A hexadecimal operand can therefore pay for a full unsuccessful scan before
the numeric fallback; an alias near 0x4000 still scans thousands of preceding empty slots.
These matching methods are unchanged by our recent improvements.

An additional isolated compile comparison changes only the 500 repeated variable operands;
the fixed initial command is unchanged. Every sample checks identical output bytes and no
mutation of the synthetic model.

| Operand spelling | Median compile ms | Managed bytes allocated per compile |
| --- | ---: | ---: |
| `0x4001` | 702.73 | 1,068,447,352 |
| `16385` | 7.68 | 24,153,352 |
| `temp1` | 346.22 | 543,733,352 |

The byte hashes match across all three spellings. The decimal case avoids the repeated
alias scan through the matcher's existing integer fast path. This is diagnostic evidence,
not an implemented optimization or a recommendation that users rewrite scripts in decimal.

## Recommended next step

Add focused argument-matching correctness and allocation coverage before implementing a
fast path or lookup preparation. Preserve existing exact/partial/quoted name priority,
duplicate `~n` selection, explicit numeric forms, offsets, sparse numeric entries, and
lookup invalidation when lists change. Differential checks against the existing matcher
should include aliases that resemble numeric literals and deliberate ambiguous names.

Start narrowly at argument resolution; do not rewrite compilation, debounce edits, move ROM
mutation to background threads, or cache results globally without invalidation. The current
compiler's relocation, pointer updates, orphan handling, and history remain necessary.
Before accepting any optimization, verify expanding/shrinking scripts, caller pointers,
embedded dialogue, neighboring bytes, and undo/redo in addition to same-size edit benchmarks.

Secondary candidates are full-document highlighting (especially comment-rich documents),
repeated stream/text-length checks, and full-document text measurement/layout in the Mac
control. They should be investigated after the dominant argument-resolution cost.

## Reproduction and limits

See tools/ScriptEditingBenchmark/README.md for commands and fixture details. Raw reports
are retained under ignored artifacts/script-performance/: edit-pipeline-baseline.json,
edit-pipeline-components.json, and edit-pipeline-lookups.json. Reports include all timing
samples, allocations, runtime identity, correctness checks, and hashes; require complete=true.
The 18-case baseline passes 1,368 checks, the component probe passes 170 checks, and the
spelling comparison passes 58 checks: 1,596 checks total. Paired UI/Core syntax-layer hashes
match for every workload and size; equivalent-spelling compiled-byte hashes also match.

These are synthetic same-size argument edits using production code. They do not reproduce
the user's earlier Windows session or prove its specific stutter cause. The attached-control
path assigns TextBox.Text, not native key events, and excludes the complete TabView help/
autocomplete/event bindings, native compositor, relocation, and real ROM models. Scripts
containing dialogue are included, but dialogue text typing and multi-body script graphs
remain future workloads. Measurements are descriptive, not timing-based correctness gates.
