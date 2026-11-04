# NPC hover investigation — 2026-11-04

The main measured cost is constructing every event editor when entering a different tile,
including empty tiles. Script summarization for the hovered NPC is much smaller in these
fixtures. A coordinate-first lookup is the recommended next implementation; this investigation
does not change production behavior.

## Reproduce

From the repository root:

```sh
dotnet build tools/MapHoverBenchmark/MapHoverBenchmark.csproj -c Release -p:NuGetAudit=false
mkdir -p artifacts/map-hover
DOTNET_TieredCompilation=0 dotnet tools/MapHoverBenchmark/bin/Release/net8.0/MapHoverBenchmark.dll > artifacts/map-hover/release.json
```

The tool uses the real Core event construction, event lookup, script summarizer and map Hover
method under an offscreen Avalonia application. It creates twelve independent fabricated 32×32
maps: 1/8/32/64 NPCs, each with null scripts, shared simple dialogue, or unique branching scripts
with five text resources. It validates the actual summary count and matching NPC before timing.
The branching scripts contain a conditional jump and a call/return; no game ROM is opened.

Each phase has five samples of forty operations, after warm rendering/discovery and explicit
warmup. Reported times are medians of sample means, not latency percentiles. Allocations use
GC.GetAllocatedBytesForCurrentThread. Tiered compilation is disabled for stable comparisons;
there are no timing-based pass/fail assertions. JSON preserves every sample and its operation
count. A separate initial Debug run was exploratory; the results below are Release, arm64,
.NET 8.0.5. Raw local runs are in ignored artifacts/map-hover/release-1.json and release-2.json.

## Measurements

First Release run, branching-dialogue workload (milliseconds per operation):

| Phase | 8 NPCs | 32 NPCs | 64 NPCs |
| --- | ---: | ---: | ---: |
| Construct event group | 1.141 | 4.478 | 9.367 |
| Existing empty-tile lookup | 1.144 | 4.510 | 9.354 |
| Existing NPC-tile lookup | 1.135 | 4.529 | 9.350 |
| Summarize one script | 0.055 | 0.057 | 0.055 |
| Full Hover entering an empty tile | 1.192 | 4.553 | 9.411 |
| Full Hover, empty→NPC pair | 2.431 | 9.155 | 18.755 |
| Full Hover staying in same cell | 0.034 | 0.035 | 0.034 |
| Coordinate-first candidate, empty | 0.020 | 0.044 | 0.072 |
| Coordinate-first candidate, NPC | 0.160 | 0.181 | 0.212 |

The empty→NPC pair includes TWO Hover calls; it must not be compared as a single lookup.
The candidate phases measure lookup/construction only, without full Hover or visual layout.

A second independent Release process reproduced the 32-NPC branching result: empty lookup
4.532 ms, NPC lookup 4.531 ms, summary 0.056 ms, candidate empty lookup 0.044 ms and candidate
NPC lookup 0.186 ms, with identical allocation counts. Complexity matters even when every NPC
shares a script: the 32-NPC simple-dialogue case took 1.776 ms/2,107.47 KiB for empty lookup,
versus 0.215 ms/421.97 KiB with null scripts in the first Release run.

On the 32-NPC branching map, existing lookup allocated 5,325.55 KiB for an empty tile and
5,326.79 KiB for an NPC tile. Candidate lookup allocated 54.42 and 221.57 KiB respectively;
summarizing one script allocated 68.39 KiB. This supports avoiding unrelated editor creation
before considering summary caching. High allocation suggests potential GC pressure; no GC
pause trace or native frame-rate claim is made here.

## Why it happens

- MapEditorViewModel.Hover already reuses the current summary for unchanged cells. It performs
  a new lookup when the tile changes or hover content is invalidated.
- BlockMapViewModel.EventsUnderCursor calls GetEvents before checking coordinates.
- GetEvents obtains EventGroup, whose getter constructs a new EventGroupModel each time.
- EventGroupModel constructs full view models for all objects, warps, scripts and signposts.
- ObjectEventViewModel builds selector options and performs script classification/basic-content
  analysis in its constructor. FillBasicContent traverses scripts and creates text editors even
  for NPCs unrelated to the hovered tile. Real sprite/option tables can add costs absent here.

The tool's candidate reacquires the current object table, reads row coordinates, and constructs
only matching NPC view models. It uses no persistent cache. It verifies coordinates, script
address and summary parity for the fabricated NPC cases. It deliberately omits production
selection and DataMoved wiring and other event kinds, so it is evidence for a design, not a
drop-in implementation.

## Recommended implementation and preservation checks

1. Add a coordinate-filtered event creation path for hover. Preserve existing event order and
   construct only matching current rows. Keep ordinary selected editors and their callbacks
   intact. Reuse the existing constructor/wiring helpers where possible rather than duplicating
   allocation, script or editing logic.
2. Cover objects, warps, script events, signposts and fly events, including stacked events,
   negative/edge coordinates, borders, scale, connected maps and hidden events. Compare the
   resulting types/order/addresses and hover summaries against the existing lookup.
3. Retain HoverContentVersion and same-cell invalidation on edits, script/text relocation,
   pointer changes, Undo/Redo, reload, map navigation and visibility changes. Reacquire tables
   on each lookup; do not retain mutable row/editor instances across history operations.
4. Verify cold metadata discovery and rendered map content still match. Lazy model reads can
   discover text metadata through history: keep this work on the UI thread. Background tasks
   require a separate immutable-snapshot design and are unnecessary for the first optimization.
5. Add failing-before regressions that count unrelated editor creation or script-analysis work
   on empty/unrelated tiles without wall-clock assertions. Protect bytes, metadata and pending
   Redo across warm hover; test editing selected objects, dialogue, flags and pointers after
   filtering, including relocation and correction/retry.
6. Re-run this benchmark, actual-view hover/history/thread-safety tests and the complete Mac
   suite after implementing. Then measure representative real maps read-only and native input
   responsiveness before claiming a general user-visible speedup.

Defer summary caching until measuring the filtered implementation. A correct cache would need
dependency/version handling for dialogue resources, branch targets, sprite/item/trainer tables,
relocation and history; the measured single-script cost does not justify that complexity yet.

## Validation and limits

Each complete benchmark run passes 120 checks, including expected NPC/branch counts, prototype
parity, same-cell summary reuse and unchanged warm-hover bytes, metadata and clean history.
Focused existing regressions freshly pass: MapHoverPreview 134, MapHistoryRefresh 23 and
MapThreadSafety 22 (179 assertions, zero failures). The complete 46-group suite was not rerun
for this tooling-only investigation; its preceding audit result remains 7,883 assertions.

These synthetic maps contain no real Pokémon/item/trainer graphics tables and do not measure
native Cocoa pointer delivery, tooltip layout, rendering, cold-start latency or frame rate.
No user ROM, production source, settings or game-engine behavior is changed.
