# NPC-style script benchmarks — 2026-11-03

Application checkpoint: `9cf708fc`; investigation branch: `investigate/realistic-npc-scripting`.
macOS ARM64 / .NET 8.0.5, Release, ten warmups and seven measured samples per case. Two
complete process runs are retained; application sources are unchanged. No supplied ROM was
opened. This is measurement and characterization, not an editor fix.

## Representative source documents

The workload sizes are **100, 250 and 500 actual source lines**, including comments, braces,
dialogue and labels. Each compound conversation locks/faces the NPC, checks variables/flags,
conditionally calls a shared reaction routine, shows two-line messages, updates state, and
releases/ends. There are 9/24/49 conditional calls and dialogue blocks respectively. These are
synthetic conversation patterns, not a claim that most user scripts contain that many messages.

For each size, the benchmark measures a scalar argument edit, one dialogue character, and a
comment digit, both with shared Core only and with the production TextEditor attached to a
headless Skia window: 18 cases. Any source consolidation/relocation during fixture setup is
excluded; subsequent same-size edits must retain their settled address. Exact comparisons
require only the intended argument/PCS byte to change, while comment edits change no ROM bytes.

## Attached-control timings

Each cell shows the range of the two runs' medians, in milliseconds. Phases are measured
separately; their median ranges should not be summed into a claimed native typing latency.
Deferred update excludes the 500 ms idle/debounce wait. Presentation includes queued layout/
headless rendering before and after compilation.

| Edit | Actual source lines | Immediate edit ms | Deferred update ms | Presentation ms |
| --- | ---: | ---: | ---: | ---: |
| argument | 100 | 2.36–2.77 | 14.48–14.48 | 35.55–35.88 |
| argument | 250 | 4.74–4.91 | 34.62–34.75 | 13.77–14.00 |
| argument | 500 | 10.09–13.36 | 34.38–35.02 | 29.50–29.70 |
| dialogue | 100 | 1.67–2.25 | 13.26–13.77 | 5.65–6.49 |
| dialogue | 250 | 4.50–4.63 | 34.72–35.35 | 12.60–12.93 |
| dialogue | 500 | 9.82–10.32 | 34.68–35.25 | 39.48–39.85 |
| comment | 100 | 1.54–1.90 | 11.93–12.40 | 4.74–4.79 |
| comment | 250 | 4.57–4.75 | 34.08–34.53 | 10.70–10.76 |
| comment | 500 | 9.87–10.17 | 35.67–38.48 | 25.00–25.78 |

At 500 actual source lines, immediate edits take roughly 10–13 ms in this attached-control
workload. This does not reproduce a large CPU-only typing stall, and is a much more relevant
scale than the earlier 2,000-statement stress test. Presentation still takes tens of milliseconds,
so native/full-editor responsiveness is not established. The smaller first argument case has
non-monotonic phase timings; JIT, GC and first-control effects remain possible, so this report
does not infer a scaling law or use timings as correctness thresholds.

## Autocomplete: fast core, incorrect Mac caret mapping

Separate query probes use an independent CodeBody without a CodeTool compile subscription:
`msgb` (command) and `setvar tem` (variable), at first/second/middle/final command positions.
Across both runs, core-query medians range from **0.0135 to 0.0784 ms**. Each query must return
suggestions without modifying ROM bytes. There is no evidence here of a core completion-speed
bottleneck worth optimizing. Popup layout/input/application cost is not timed by these queries.

The actual production AutocompleteOverlay is also characterized through a routed TextChanged
notification at a prepared caret after content/selection jobs settle. Every run gives:

- **6/6 first-line controls correct:** all eight command or ten variable suggestions agree
  with Core, proving that the actual routed handler and popup option collection are connected.
- **18/18 tested later-line probes disagree:** Core supplies those same suggestions but the
  overlay supplies none. Immediate and post-layout option collections agree on that failure.

Source inspection explains the mismatch: on Mac, Environment.NewLine is LF, but the overlay
subtracts each preceding line's length plus **2** when converting an absolute caret offset.
For `lock\nmsgb`, caret index 9 should mean line 1, column 4; the handler computes column 3,
and the core rejects completion inside that unfinished token. AutocompleteOptionChosen contains
the same traversal assumption, but applying a suggestion has not been tested in this benchmark.

This is a correctness finding, not an autocomplete performance regression or a native keyboard
reproduction. It is tracked as HMA-004 in FutureDevelopment.md. No production fix was made,
and the later-line mismatches are explicitly reported rather than counted as successful UI behavior.

## Recommendation

Prioritize a focused regression/fix for autocomplete caret mapping, including LF/CRLF, blank
lines, first/middle/last-line positions, offered/applied options and exact line/history changes.
Then let real scripting feedback guide additional work. A broad highlighting rewrite is not
justified by the immediate costs measured at these sizes. Width/layout work remains a possible
later target, requiring more attribution and full-editor/native evidence.

## Evidence and limits

Both complete NPC runs pass **2,892 measurement/invariant checks** each, while retaining the
explicit autocomplete correctness failures above. Paired attached/Core source and all six layer
hashes match, as do hashes across runs. Each report contains 18 edit cases and 24 completion
probes, full sample arrays, thread-local allocations, source/branch/dialogue counts, and the
actual core/overlay option arrays. The legacy isolated lookup mode still passes its 58 checks.

Raw reports: artifacts/script-performance/npc-realistic-warmed.json and
artifacts/script-performance/npc-realistic-repeat.json. The earlier two-warmup exploratory
npc-realistic-current.json is not used for this table. Reproduction is documented in
tools/ScriptEditingBenchmark/README.md. The tools are separate from the normal Mac regression
suite; that suite and upstream tests were not rerun for this tooling-only change.

Limits: synthetic data, fixed dialogue addresses, no real ROM/native keyboard input, standalone
TextEditor rather than complete TabView help/event bindings, manually flushed compiler debounce,
same-size edits rather than growth/new auto streams, no applied-completion timing, and managed
current-thread allocation rather than retained RAM. The shared reaction section and source-line
budget make this a more relevant workload, not an exhaustive reproduction of a user's ROM hack.
