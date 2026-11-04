# NPC hover investigation — 2026-11-04

The main measured cost is constructing every event editor when entering a different tile,
including empty tiles. Script summarization for the hovered NPC is much smaller in these
fixtures. The implementation now filters coordinates before constructing hover event editors;
the baseline investigation and benchmark-only prototype below motivated that change.

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

First Release run BEFORE implementation, branching-dialogue workload (milliseconds per operation):

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

### Implemented production path

First Release run after coordinate filtering, using the same tool, runtime, workload, warmup
and sample procedure (milliseconds per operation):

| Phase | 8 NPCs | 32 NPCs | 64 NPCs |
| --- | ---: | ---: | ---: |
| Existing full event group (render/selection path) | 1.149 | 4.539 | 9.255 |
| Filtered empty-tile lookup | 0.043 | 0.065 | 0.099 |
| Filtered NPC-tile lookup | 0.184 | 0.205 | 0.239 |
| Summarize one script | 0.055 | 0.056 | 0.054 |
| Full Hover entering an empty tile | 0.096 | 0.119 | 0.151 |
| Full Hover, empty→NPC pair (two calls) | 0.361 | 0.399 | 0.463 |

For 32 branching NPCs, empty lookup allocation falls from 5,325.55 to 84.21 KiB and NPC lookup
from 5,326.79 to 249.93 KiB. Lookup time falls approximately 98.6% and 95.5% respectively.
Full Hover for the empty→NPC pair falls from 9.155 to 0.399 ms and 10,823.05 to 504.85 KiB.
Full event-group creation and standalone summary costs remain comparable to the baseline;
the change removes unrelated construction from hover instead of changing script analysis.
Production filtering includes all event kinds, validation and existing callbacks, so it has
some overhead versus the earlier NPC-only benchmark prototype.

Raw after-change samples are in ignored artifacts/map-hover/implemented-release-1.json and
implemented-release-2.json. The second independent process reproduced the 32-NPC results:
empty lookup 0.065 ms, NPC lookup 0.203 ms and empty→NPC Hover pair 0.400 ms, with identical
allocation counts. These remain synthetic CPU/allocation results, not native FPS.

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

## Implementation and preservation checks

1. EventsUnderCursor with autoSelect=false now passes tile coordinates into the existing event
   group construction helper. Each current object/warp/script/signpost row is checked before
   its editor is created; fly points are filtered before construction with original flight indices.
   Existing group order and DataMoved/goto callbacks are retained. Rendering and ordinary
   selection continue to construct complete event groups, including cold metadata discovery.
2. MapHoverLookup compares types/order/row addresses with full enumeration across stacked
   events, all five event kinds, map-specific fly points, edge/empty/negative coordinates,
   borders, zoom and hidden events. Existing map selection/menu regressions cover interactive
   paths. Hover never replaces the selected editor. Connected-map routing is unchanged;
   a dedicated native connected-map hover check remains manual coverage.
3. HoverContentVersion and same-cell invalidation on edits, relocation, pointer changes,
   Undo/Redo, reload, navigation and visibility are unchanged. Tables are reacquired for each
   lookup; there is no persistent row/editor cache. MapHistoryRefresh protects actual dialogue,
   flag and script-pointer fields and current same-cell preview contents across Undo/Redo.
4. Lazy model reads can discover text metadata through history. Preparation remains on the UI
   thread, protected by MapThreadSafety. No background work or model/history algorithm changes
   are introduced. Compilation, numeric parsing and allocation/relocation behavior stay intact.
5. A tracked sprite list verifies real constructor work without timing assertions: empty hover
   constructs no NPC selectors and a two-NPC stack does twice the work of one matching NPC.
   The empty-tile assertion fails on the previous implementation. Warm hover preserves bytes,
   metadata, clean history and pending Redo; position edits are immediately visible through
   Undo/Redo. These tests use only fabricated data.

Defer summary caching until measuring the filtered implementation. A correct cache would need
dependency/version handling for dialogue resources, branch targets, sprite/item/trainer tables,
relocation and history; the measured single-script cost does not justify that complexity yet.

## Validation and limits

Each complete benchmark run passes 120 checks, including expected NPC/branch counts, prototype
parity, same-cell summary reuse and unchanged warm-hover bytes, metadata and clean history.
After implementation, scripts/verify-mac.sh --no-restore freshly builds and passes all 47
regression groups and 7,933 assertions, with zero failures and zero skipped groups. This includes
the new 50-assertion MapHoverLookup group and existing MapHoverPreview 134, MapHistoryRefresh 23
and MapThreadSafety 22, plus compilation/resource failure safety, retained script drafts,
Save/reload, pointer and Undo/Redo protections. The separate upstream xUnit suite was not rerun.

These synthetic maps contain no real Pokémon/item/trainer graphics tables and do not measure
native Cocoa pointer delivery, tooltip layout, rendering, cold-start latency or frame rate.
No user ROM, settings or game-engine behavior is changed. Production changes are confined to
coordinate filtering and shared event-group construction; no persistent caching is added.
