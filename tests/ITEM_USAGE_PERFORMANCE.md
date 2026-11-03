# Delayed item map-usage search review — 2026-11-02

Branch: `investigate/item-usage-search`, based on `3c065e0c`.
Host: Apple M3 Pro, macOS ARM64, .NET 8.0.5, Release, Avalonia 11.2.3.
Scope: investigation and benchmark-only experiment; application code is unchanged.

## Normal scheduling and why this matters

TableTool.AddChildrenFromStreams creates MapOptionsArrayElementViewModel while refreshing
an item. Its constructor schedules Load after 0.5 seconds. The actual Mac DelayWorkTimer
is used by the investigation, with the same dispatcher supplied through ViewPort and
Singletons; earlier immediate-timer item-table runs are not used as a foreground baseline.

The delayed callback runs even while UsageOptionsOpen is false and ShowPreviews has never
been opened. Load scans events and immediately constructs GotoMapButton for each match;
the button constructor calls MapEditorViewModel.GetMapPreview, constructing a BlockMapViewModel
and cropping a map image before the user requests the previews. When scheduled from the
UI context, the timer resumes on that context: async delay does not make the subsequent
scan run in the background. All measured callback samples confirm the UI-thread execution.

Ten rapid item selections correctly coalesce to one final scan, so this evidence does not
identify a broken debounce mechanism. It identifies expensive work performed after the
normal delay. A long callback on the UI thread can cause a noticeable pause; native frame
or input latency was not measured, so that is an implication of the code/timing evidence,
not a measured native animation/frame-rate result.

## Method and integrity

The standalone ItemUsageBenchmark uses the real TableTool, map-usage owner, native timer
implementation, and an offscreen Avalonia synchronization context. Initialization and
already-UI dispatcher operations are controlled inline; it is not a full native window
or complete MacFileSystem-dispatcher benchmark. Callback execution time and managed-thread
allocation exclude the half-second waiting period, recorded separately as DelayMs.

The explicitly supplied FireRed 1.1 file is copied into an independent in-memory model.
Source/model SHA-256 remain
`729041b940afe031302d630fdbe57c0c145f3f7b6d9b8eca5e98678d0ca4d059`.
Metadata remains identical, no undo history or editor error is created, and no ROM is
committed. Tests cover item indices 13, 1, 50, and 0, plus final index 22 after rapid browsing.

Each item has its first observed delayed callback and three repeated callbacks. Component
phases have two warmups and five measured samples; times/allocations are per one complete
query, not per 100. They run in a warmed model scope and are not interchangeable with cold
or end-to-end callback measurements. Their costs cannot be added/subtracted as exact shares
of a normal callback. Allocation is cumulative temporary managed allocation, not retained
RAM, peak process memory, native allocation, or an assertion that the app holds 623 MB.

Ordered actual button destinations (bank/map/event address) are compared with an independent
uncached matcher, then repeated actual searches must retain exact order and thumbnail pixel
hashes. The tool uses reflection to inspect existing private destination fields without
changing app APIs. These checks preserve the existing search behavior; they do not establish
that every possible item use is discovered. This map-preview path searches object/coordinate
scripts and hidden items; other search categories/actions and other games need separate coverage.

## Actual delayed callback results

Final-code runs: `artifacts/item-usage-performance/repeat.json` and `final-repeat.json`.
Both pass **71 checks**, with identical ROM identity, ordered results, thumbnail hashes,
event/root counts, and hidden-item counts. The first investigation passed 50 checks and an
intermediate prototype run passed 54; neither is substituted for the final workload.
An intermediate reference enumerator repeatedly fetched its bank wrapper; the final helper
holds one bank wrapper like production. Comparisons below use only the final code.

The callback below is the median of three repeated actual delayed callbacks in final-repeat;
the prior final-code run is shown for context. MB is decimal cumulative managed allocation.

| Item index | Result events | Hidden-item events | Earlier callback ms | Final callback ms | Final callback MB |
| --- | --- | --- | --- | --- | --- |
| 13 | 8 | 3 | 200.873 | 224.856 | 302.558 |
| 1 | 1 | 0 | 183.916 | 189.770 | 248.059 |
| 50 | 0 | 0 | 185.570 | 185.753 | 240.332 |
| 0 | 33 | 12 | 472.309 | 448.800 | 623.229 |

The first observed item-13 callback takes about 396 ms in the earlier final-code run, versus
about 201 ms on repeats; initialization/JIT/warmth matter. The final-repeat first item-0
callback is about 506 ms versus about 449 ms on repeats. The requested delay itself remains
about half a second. Even the no-result item pays roughly 186 ms / 240 MB for the scan.
These are actual unchanged application callbacks in this controlled host, not extrapolated
foreground browsing numbers or results from an implemented optimization.

## Where the work goes

The fixture enumerates 2,059 relevant events: 1,876 object/coordinate script roots and
183 hidden items. Script roots contain only 1,379 distinct addresses, leaving 497 repeated
roots. Identical scripts are therefore traversed repeatedly within the same item query.
The original Flags.GetAllScriptSpots still bounds traversal and follows script-pointer
branches; this investigation does not replace it, change its limits, or alter script parsing.

Final-repeat component medians:

| Item | Enumeration ms / MB | Matching without previews ms / MB | Matched preview creation ms / MB |
| --- | --- | --- | --- |
| 13 | 31.728 / 24.403 | 144.107 / 240.293 | 41.092 / 62.329 |
| 1 | 31.356 / 24.358 | 143.139 / 240.334 | 5.550 / 7.790 |
| 50 | 31.587 / 24.358 | 141.941 / 240.398 | 0 / 0 |
| 0 | 32.605 / 24.358 | 145.722 / 235.540 | 294.474 / 387.743 |

Matching includes enumeration, so these columns are not independent additive totals.
Enumeration/model wrappers are material, script traversal adds substantial work even without
matches, and preview construction is particularly expensive when many events match.
The code constructs a complete map view model before AutoCrop extracts the small preview.

## Benchmark-only repeated-root reuse experiment

For each query, the experiment builds a fresh Dictionary<int,bool> keyed by root script
address, holding whether that root references the selected item. Every event retains its
own bank/map/location and eventual preview; only repeated script analysis is shared.
The opcode filter is also constructed once per query rather than per event. Hidden items
are still checked separately. The experiment calls the original Flags.GetAllScriptSpots
and argument-reading methods; no compiler, ROM definitions, researched offsets, pointer
updates, flags/variables, parser, or application cache is changed.

The dictionary is discarded at the end of that one matching enumeration. It is not shared
between queries, items, models, edits, reloads, or undo/redo. This avoids the persistent-cache
invalidation architecture that would add risk to a binary ROM editor. It remains a tool-only
prototype, not a shipped application improvement or proof of all fringe cases.

| Item | Original matching ms | Experiment ms | Original MB | Experiment MB |
| --- | --- | --- | --- | --- |
| 13 | 144.107 | 84.749 | 240.293 | 125.637 |
| 1 | 143.139 | 82.968 | 240.334 | 125.679 |
| 50 | 141.941 | 82.335 | 240.398 | 125.742 |
| 0 | 145.722 | 83.986 | 235.540 | 120.982 |

The earlier final-code run repeats the direction: 143.284→87.612, 150.242→86.481,
141.939→83.991, and 146.680→84.477 ms, with identical allocation medians. The experiment
reduces matching time by about 39–42% in the final repeat and temporary allocations by
about 48%, or approximately 115 MB per query. All ordered raw event matches equal the
uncached reference; actual repeated callback results and thumbnail hashes also remain stable.
This does not demonstrate a 40% improvement to the whole callback, table browsing, or app:
previews, scheduling, publication, and other work remain unchanged, and the experiment is
not installed in the actual callback being measured.

## Recommended implementation order

1. Start with item-query-local reuse of the boolean script-match result and one filter buffer;
   keep the current event order, per-event preview validity checks, and original parser/traversal.
2. Add focused synthetic regression cases before changing the app: shared roots across maps,
   different event positions, positive/negative/hidden matches, branch/cycle/malformed inputs,
   cancellation before/during publication, and changed script operands/pointers between queries.
3. Include edits, relocation, reload, and undo/redo tests proving every new query sees current
   bytes; require exact event ordering and preview pixel equivalence in before/after ROM runs.
4. Then remeasure the real delayed callback. Consider demand-driven search/thumbnail generation
   only as a separate UX/lifecycle change, with loading/selection behavior tested explicitly.

Do not blindly move the whole scan to Task.Run: model access, UI publication, cancellation,
file reloads, and retained event/map objects need a deliberate concurrency design. Do not
change the script compiler, discovered addresses, or traversal limits to improve this metric.
No application implementation is included in this review. Build passes with zero warnings/errors
on the final incremental build (the initial Core rebuild retained eight existing warnings).
The broader suites were not rerun for this tools/docs-only change; their last recorded baseline
remains 1,780/26 upstream passes with 935/41 optional-ROM skips and 18 Mac groups/1,444 assertions.
