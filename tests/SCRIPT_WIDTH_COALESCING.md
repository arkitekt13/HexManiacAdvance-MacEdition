# Coalesced script width updates — 2026-11-04

The editor now queues one width calculation at render priority for a burst of TextChanged
notifications. Source updates, syntax highlighting, caret commands and compilation timing
remain immediate/unchanged. The calculation reads the current text, font/culture and viewport
when it runs; it retains the existing FormattedText calculation and scrollbar-corner rule.

Context/template changes and detachment invalidate old queued work. Reattachment schedules
current content, and template replacement unsubscribes the previous scroll viewer. Scroll
changes that happened after the latest request retain their existing precedence over measured
layer widths; simply applying the deferred width last would otherwise overwrite native extent
widths. The differential regression reproduced that ordering difference before its correction.

## Paired benchmark

The benchmark now observes the actual UpdateDocumentWidth method, not constructor TextChanged
handlers or queue requests. A protected rendering-method extraction permits an observing
subclass without application counters or reflection. The before run uses the exact old
calculation, mechanically extracted at application checkpoint 14f688ce before introducing
queue/lifetime changes. The same observer and fixtures are used after coalescing.

Release/.NET 8.0.5/arm64, tiered compilation disabled, six setup warmups, three additional
settled-edit warmups, seven measured settled edits and seven ten-update bursts per workload.
Both sides use standalone production TextEditor controls and synthetic source. No game ROM,
compiler, full TabView host, native keyboard/compositor or worker thread is used.

Median actual width-calculation time per TEN-update burst, milliseconds:

| Workload | Before | After run 1 | After run 2 |
| --- | ---: | ---: | ---: |
| 100 short lines | 6.685 | 0.665 | 0.669 |
| 500 short lines | 39.598 | 3.340 | 3.296 |
| 2,000 short lines | 192.364 | 25.401 | 28.525 |
| 500 Unicode/emoji dialogue lines | 154.286 | 15.949 | 12.440 |
| One long line, 500 repeated units | 17.912 | 1.732 | 1.728 |

Every baseline burst performs ten actual calculations of one identical final source value.
Every after burst performs one. Width allocation falls from 17.01 to 1.74 MB for 500 short
lines, 68.97 to 6.94 MB for 2,000 and 54.90 to 4.95 MB for Unicode dialogue. These are temporary
managed measured-thread bytes, not retained RAM. Timing varies with GC/cache/host activity;
the deterministic result is ten calculations becoming one, with matching output.

Total queued UI drain also falls: in the second after process, 500-short-line bursts decrease
from 69.853 to 28.559 ms, 2,000 from 329.930 to 146.493 ms and Unicode dialogue from 209.278
to 73.393 ms. These drain times INCLUDE the nested width calculation, queue work, post-measurement
extent restoration, layout and rendering. They must not be added to width time. Separate phase
medians are not additive native typing latency.

Settled isolated edits still perform one calculation. At 500 short lines, width time is
3.452 ms before and 3.346 ms in the second after run; no general per-character speedup is
claimed. Diagnostics-only changes still measure zero times.

## Preservation evidence

All three reports pass 190 source/rendered-layer/actual-measurement/burst/diagnostic checks.
Before/after comparisons match all six layer hashes, final layer widths and scrollbar-corner
dimensions in all five workloads. Source labels count repeated content lines; JSON records
actual line/character counts including three wrapper lines. Raw samples remain under ignored
artifacts/script-performance: width-coalesce-before.json, width-coalesce-after-1.json and
width-coalesce-after-2.json. Reproduction: [ScriptWidthBenchmark](../tools/ScriptWidthBenchmark/README.md).

ScriptWidthUpdates adds 448 deterministic assertions. The first burst assertion fails before
coalescing with ten actual calls. The current control is compared with an immediate-scheduling
reference using the same unchanged measurement routine, covering exact displayed/source text,
all layer widths/transforms, native extents/offsets, caret/selection and corner dimensions.
Cases include growing/shrinking/empty documents, Unicode/CJK/emoji, LF/CRLF/tabs/trailing spaces,
long lines, both scrollbars, resize, font sizes, proportional/fallback fonts, culture, interleaved
source/scroll requests, queued context/template replacement and detach/reattach. Diagnostics-only
changes request no measurement; old work cannot paint abandoned context/template geometry.

Full verification: scripts/verify-mac.sh --no-restore freshly builds and passes 50 groups and
13,118 assertions, with zero failures and zero skipped groups. Existing script/resource failure
atomicity, rejected-source/diagnostic retention, correction/retry, HMA-006/007, Save/reload,
autocomplete/scaffolding, clipboard and Undo/Redo protections continue to pass. The separate
upstream xUnit suite was not rerun; fresh dependency builds retain eight existing Core warnings.

No Core/compiler, numeric/name matching, ROM/resource allocation/relocation, metadata, history,
game-engine behavior, settings or user ROM changes. Native platform event delivery and actual
typing latency remain manual verification; this is a focused presentation-work optimization.
