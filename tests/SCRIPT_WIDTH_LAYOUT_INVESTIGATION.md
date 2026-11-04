# Script width/layout investigation — 2026-11-04

Application checkpoint: 26ec1e5c, after the comment-search optimization. This investigation
adds tooling and documentation only; no production width/layout behavior changes.

## What was measured

TextEditor's constructor registers a TextBox.TextChanged handler which constructs a Typeface
and FormattedText for the entire current document, reads Width, assigns all six colored layer
widths and updates CornerCover. TextScrollChanged separately assigns widths from the native
ScrollViewer extent and updates transforms. These responsibilities must be preserved together.

The probe wraps the exact existing instance delegate in disposable headless controls. It
fails unless exactly one expected handler is found and observed. Timing covers the whole handler,
including width calculation and assignments; it does not isolate FormattedText alone. Replacing
the subscription appends the wrapper in event order, so this is attribution tooling rather than
an untouched native-process trace.

Two arms warm the real unmodified control before measurement. The first retains the wrapped
handler; the second removes it for same-size, same-width character edits only. Seven measured
edits follow three additional warm edits. Five workloads are tested, with full source and six
display-layer checks. Both independent Release runs pass 315 invariants, and all five final
layer-width/corner-cover comparisons match. Removing the handler would be unsafe for general
growing/shrinking text; this is not a production implementation.

## Results

Second run, Release/.NET 8.0.5/arm64, tiered compilation disabled; medians in milliseconds:

| Workload | Source set | Queued layout/render drain | Width handler within drain | Handler allocation |
| --- | ---: | ---: | ---: | ---: |
| 100 short lines | 0.095 | 5.288 | 0.671 | 249,200 B |
| 500 short lines | 0.493 | 30.412 | 3.335 | 1,336,120 B |
| 2,000 short lines | 2.440 | 170.344 | 21.964 | 6,532,120 B |
| 500 Unicode dialogue lines | 0.708 | 74.599 | 15.644 | 4,941,728 B |
| One long line, 500 units | 0.152 | 11.958 | 1.734 | 109,768 B |

The workload names count repeated content lines; JSON records the additional three wrapper
lines and actual characters. These are standalone editor/model assignments, not keystrokes or
full CodeTool/TabView compilation. Phase medians are separate and not additive. Width timing is
nested inside the drain, not an extra cost to add to it. Allocations are temporary managed bytes
on the measured thread, not retained RAM.

The first independent run measured width at 3.40/21.23 ms for 500/2,000 short lines and 16.15 ms
for Unicode dialogue. Omitting the handler reduced first-run drain medians from 30.90 to 27.33,
162.04 to 144.93 and 74.87 to 59.20 ms respectively, with identical final source/layer hashes and
measured geometry. Most queued work remains even in the omission arm; width does not explain
all editor layout cost. Other text shaping, bindings, layout and rendering need separate tracing.

### Confirmed duplicate work

Every settled single edit invokes the width handler once, after the synchronous source-set
phase. A burst of ten model/source updates before draining the UI invokes it TEN times.
The second run records the actual text read by each callback: all ten read the same final
string (one distinct value) in every production arm. That is avoidable duplicate measurement,
not merely ten different documents needing ten widths.

| Ten-edit burst | Width calls | Distinct measured sources | Total width-handler time |
| --- | ---: | ---: | ---: |
| 500 short lines | 10 | 1 | 45.426 ms |
| 2,000 short lines | 10 | 1 | 178.066 ms |
| 500 Unicode dialogue lines | 10 | 1 | 153.212 ms |

Diagnostics-only bursts of 32 warning spans invoke width measurement zero times in all arms.
Thus diagnostic coalescing already remains separate; wiring width updates to every diagnostic
change would add unnecessary work.

## Recommended next implementation

1. Coalesce width updates into one UI/render-priority request reading the CURRENT text and
   font/culture when it executes. Keep one pending request per control. This directly targets
   measured duplicates; it will not remove the one measurement already needed for each isolated
   settled edit. No production optimization is implemented by this investigation.
2. Add a deterministic ten-source-update regression proving ten equivalent measurements become
   one while the latest source wins, counting actual measurements rather than queue requests.
   Update this attribution probe if the constructor handler starts queuing work. Protect
   context/template replacement, detach/reattach and
   queued old work, with no stale dimensions applied to a new editor context.
3. Compare exact layer text, widths/transforms, caret/selection, scroll offsets/extents,
   diagnostics, popup placement and CornerCover across growing/shrinking and empty documents,
   long lines, LF/CRLF/tabs, trailing spaces, proportional/fallback fonts, Unicode/emoji, font/
   culture changes, resize, horizontal/vertical scroll and both scrollbar combinations.
4. Preserve TextScrollChanged's extent/transform behavior and re-read the current viewport for
   corner sizing. Keep existing width semantics; character-count approximations would be unsafe
   for fallback fonts, tabs and Unicode. Evaluate reusing the TextBox's already measured extent
   only as a separate equivalence experiment, not an assumption that it equals FormattedText.Width.
5. Keep source/caret edits and compiler debounce immediate/unchanged. Do not move mutable model
   work to background threads or change parsing, compilation, allocation/relocation, diagnostics,
   draft retention, Save/reload or history. Re-run full Mac checks plus these measurements after
   an implementation, including the recent autocomplete lifecycle protections.

## Evidence and limits

Tool/reproduction: [ScriptWidthBenchmark](../tools/ScriptWidthBenchmark/README.md). Raw reports:
ignored artifacts/script-performance/width-layout-review-1.json and width-layout-review-2.json.
The second adds distinct-source tracking to the same measurement procedure; both retain raw
samples, call counts, allocations, hashes and geometry values.

No application code, settings, compiler, history or user ROM changes. The complete Mac suite
was not repeated for this tooling-only second step; the preceding optimization freshly passed
49 groups/12,670 assertions with zero failures/skips. Native OS input/rendering, full TabView
bindings, compiler timing and dynamic-size behavior are outside this probe; the same-size
omission arm does not establish that removing width measurement is safe.
