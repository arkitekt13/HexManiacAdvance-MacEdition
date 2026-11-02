# Pokémon table presentation investigation — 2026-11-02

Initial investigation: `investigate/pokemon-table-performance`, based on `82245513`.
Implementation follow-up: `improve/table-navigation-preparation`, based on `756de902`.
Host: Apple M3 Pro, macOS ARM64, .NET 8.0.5, Release, Avalonia 11.2.3.
The initial checkpoint added the benchmark without application changes; the follow-up
below reduces duplicate navigation discovery in TableTool.

## Method and limits

The benchmark constructs fabricated binary data using the existing 28-byte Pokémon
base-stats schema and a names/stats table group. It navigates through the parent names
table using real ViewPort/TableTool logic. The production Avalonia TabView, field
templates, bindings, lightweight text presenters, and Skia renderer are exercised in
an in-memory window using Avalonia.Headless. No screenshot approximation or simplified
replacement table control is used.

Each phase records seven 100-operation samples after two warmups. Allocations are
cumulative managed UI-thread bytes, not retained memory, process memory, or native
graphics memory. Separate diagnostic models collect access/notification counters so
the counters do not bias timings. Single cold-open/mount samples are retained in JSON
but are not comparable size benchmarks: the first case also pays process/JIT/theme costs.

The fixture excludes portraits, evolutions, Pokédex data, scripts, and other Pokémon
group tables. The extra 250 unrelated tables are a sensitivity experiment, not a measured
stock-ROM table count; 1,024/2,048 species are stress cases. No real ROM, native input,
Mac compositor, frame rate, or end-to-end interaction latency is measured.

## Baseline results

The recorded first run used the following medians per 100 operations:

| Species | Extra tables | Navigation without controls, ms | Navigation + headless binding/layout, ms | Navigation allocations, MB | Binding/layout phase allocations, MB |
| --- | --- | --- | --- | --- | --- |
| 412 | 0 | 23.807 | 556.967 | 28.49 | 274.60 |
| 412 | 250 | 60.465 | 922.531 | 208.57 | 635.55 |
| 1,024 | 250 | 59.898 | 948.879 | 211.84 | 638.10 |
| 2,048 | 250 | 62.204 | 942.440 | 217.58 | 643.81 |

MB is decimal. The second timing column includes navigation and explicit dispatcher/layout
work; it is not an additive phase. The results suggest substantial presentation overhead
in this fixture but do not attribute all of that overhead to a specific control or binding.

For 412 species, isolated 100 paired reads of TableSections and TableList changed from
1.613 ms / 3.54 MB with no extra tables to 35.591 ms / 174.81 MB with 250 extra tables.
In contrast, constructing the main selector's candidate choices cost 1.539 ms / 2.54 MB
with 412 species and 250 extra tables. These independent component timings cannot be
summed or treated as a formal sampling-profiler breakdown.

The selector-only experiment with prebuilt choices allocated 19,200 bytes per 100 updates,
versus 2,536,000 bytes with fresh candidates at 412 species, or 11,542,400 bytes at 2,048
species. That is a useful allocation opportunity, but not the largest initial CPU target.
No such cache is installed in the editor by this checkpoint.

Forced full-tab Skia drawing alone cost about 109–110 ms / 100 draws and 70.15 MB in the
first run. Navigation plus a forced full draw cost 779–1,180 ms / 100 switches. Those
draws repaint the whole tab into a bitmap and do not model normal retained native
drawing; the binding/layout phase is reported separately to avoid that overstatement.

Raw local reports: `artifacts/pokemon-table-performance/baseline.json`, `repeat.json`,
and `confirmation.json` (the final harness with explicit visible-control assertions).
They are ignored generated output; this report and the reproducing harness are tracked.
The repeat reproduced the navigation allocation totals exactly. Control-free navigation
medians were 23.707, 60.630, 59.646, and 60.409 ms in the same fixture order; headless
binding/layout medians were 552.169, 811.148, 809.608, and 810.651 ms. The latter varied
by up to about 15% between runs, reinforcing why these are descriptive measurements
rather than timing gates or a claimed native-interaction improvement.

## Source-confirmed repeated work in the baseline

1. `TableTool.TableSections`, `TableList`, and `UnmatchedArrays` enumerate, resolve,
   group, and sort tables repeatedly. TableList reads TableSections twice and then
   performs another scan itself. DataForCurrentRunChangedCore requests those properties
   and notifies both lists on each row switch; the actual TabView binds to both.
   An untimed 100-switch pass counted 400 model anchor-list reads, 100 section-list
   notifications, and 100 table-list notifications even though table definitions did not
   change. Adding 250 tables raised run lookups from 6,211 to 106,211 and anchor-name
   lookups from 5,299 to 130,299. These counters describe the control-free path only.
2. `ITableRunExtensions.GetTable` creates a new NoDataChangeDeltaModel for each lookup.
   The class inherits ModelDelta's fifteen empty dictionary allocations. Repeated
   discovery therefore creates change-tracking objects during read-only UI preparation.
   An isolated allocation probe measured 1,360 managed bytes per fresh token on this
   runtime. Four scans of the 250 added tables across 100 paired list reads imply
   100,000 extra tokens, or 136 MB of the measured 171.27 MB allocation increase.
   This arithmetic describes the current scan pattern, not a measured saving from a fix.
   This is a source explanation for substantial allocation pressure, not a reason to
   weaken change tracking or replace the model's transaction semantics.
3. `TableTool.SetupFromModel` constructs fresh names lists and ComboOption instances
   before FilteringComboOptions can compare and retain the existing choices.
4. `ComboBoxArrayElementViewModel` likewise prepares new field option objects/collections
   before TableGroupViewModel.TryCopy can reuse the old field. One synthetic 512-item
   dropdown constructor allocated 4.09 MB per 100 constructions. This is a component
   measurement, not all enum fields combined. Sprite-backed dropdowns are not covered.

Relevant source locations at the parent revision:

| File | Line | Responsibility |
| --- | --- | --- |
| `src/HexManiac.Core/ViewModels/Tools/TableTool.cs` | 25 | Repeated table-section discovery. |
| `src/HexManiac.Core/ViewModels/Tools/TableTool.cs` | 47 | Table-list discovery and repeated section reads. |
| `src/HexManiac.Core/ViewModels/Tools/TableTool.cs` | 351 | Per-row preparation and list notifications. |
| `src/HexManiac.Core/ViewModels/Tools/TableTool.cs` | 517 | Main-selector candidate construction. |
| `src/HexManiac.Core/Models/Runs/ArrayRun.cs` | 30 | Per-table read-only lookup token allocation. |
| `src/HexManiac.Core/Models/ModelDelta.cs` | 14 | Dictionary initialization inherited by lookup tokens. |
| `src/HexManiac.Core/ViewModels/Tools/ComboBoxArrayElementViewModel.cs` | 111 | Per-field dropdown candidate preparation. |
| `src/HexManiac.Core/ViewModels/Tools/FilteringComboOptions.cs` | 76 | Existing unchanged-choice comparison/reuse. |
| `src/HexManiac.Core/ViewModels/Tools/TableGroupViewModel.cs` | 83 | Existing field-member reuse. |
| `src/HexManiac.Avalonia/Controls/TabView.axaml` | 196 | Actual section/table selector bindings. |

## Existing efficiencies to preserve

HMA already retains compatible field members and unchanged main-selector collections:
the diagnostic passes observed zero member-collection changes and zero main-choice
collection replacements. The real tab also uses TextBoxLookAlike presenters until editing
is needed. The headless fixture realized 21 of these lightweight presenters; whole-tab
control counts include unrelated and hidden tools, so they are not Pokémon field counts.
This evidence does not support rebuilding the table UI or blindly adding virtualization.

## Initial implementation recommendation

First reduce duplicate table-navigation discovery within a refresh, then avoid publishing
unchanged navigation lists to bindings. Start with refresh-scoped reuse rather than an
unbounded persistent ROM cache. Both control-free and bound headless benchmarks should
be rerun to distinguish source-side allocation savings from actual presentation savings.

Before retaining snapshots across refreshes, tests must cover anchor creation/deletion/
rename, format/length changes, relocation, table switching, undo/redo, and external reload.
Selection/filtering must remain correct when the available tables or names change.
Any lookup-token reuse should be local and retain the no-data-change protection, rather
than changing global model or pointer behavior.

Next examine redundant per-field enum choices; main-selector candidate reuse follows.
Both require tests for renames, invalid stored enum values, option offsets, table growth,
undo/redo, and sprite-backed choices before broader caching is safe. Full-tab layout
still warrants profiling after source preparation is reduced; this benchmark does not
yet identify a single layout control as the dominant cost.

## Initial checkpoint correctness and scope

The benchmark checks selection/labels, actual bound HP values, immutable bytes/metadata
after read-only work, no navigation-created undo history, and real name editing followed
by undo/redo and exact byte restoration. The final repeat additionally checks metadata
restoration after that edit/undo cycle. The final harness also requires the selector
and HP presenter to be effectively visible during UI measurements. It validates its
synthetic scenarios only.

The final Release build completed with zero warnings/errors, and confirmation passed
1,304 checks across all four fixtures. The first and second complete measurements passed
1,292 and 1,296 checks respectively, before the additional restoration/visibility
assertions were added. Final confirmation reproduced the control-free allocation totals
exactly again. Existing Mac/upstream regression suites were not rerun for this tools/docs
checkpoint; no application or existing test code changed.

At the initial investigation checkpoint, no application/Core files, researched game
addresses, compiler, repointing, pointer definitions, free-space/flag/variable allocation,
history implementation, or existing tests were modified. The standalone benchmark is not a new group in verify-mac and does
not replace the upstream suites or testing a small ROM hack in the actual editor.

## Implemented follow-up: refresh-scoped navigation discovery

TableTool now discovers eligible table names and sorted section names once during a
synchronous DataForCurrentRunChangedCore call, reusing that snapshot for internal reads
and synchronous binding callbacks. A Lazy avoids discovery when nobody reads the lists;
a disposable scope restores the prior value when the call returns, throws, or nests.
Getters called outside that scope always rediscover from the current model. TableList
also uses one discovery result for its section validation and names, avoiding three
separate scans even outside a refresh.

Sorting, section grouping, StartsWith matching, matched-length table filtering, selected
indices, and notification order are retained. Every eligible lookup still uses the
existing GetTable/no-data-change token protection; no shared global token, persistent
cache, model-lookup rewrite, or new invalidation contract is introduced. Main/field
dropdown choice preparation, compiler, relocation, pointer updates, and undo remain
unchanged. Suppressing unchanged-list notifications is deferred to a separate change.

Seven new upstream regression cases exercise one discovery per refresh despite repeated
binding reads, fresh discovery outside the refresh, table creation/removal, table renames
with undo/redo, matched-length format changes with undo/redo, replacement metadata loading,
and forced table expansion/relocation with actual pointer updates and exact undo bytes.
Existing sorting tests and the broader upstream suite remain in place.

An exploratory optimized benchmark ran while a Debug test build was active. Its allocation
and correctness results were useful, but its timings are excluded from the final comparison.
The final measurements use the unchanged benchmark workloads, after all builds and test
runs finish; only the report's scope description is updated to apply to both revisions.

### Before/after measurements

Comparison uses the original `confirmation.json` (all 1,304 checks, before application
changes) and final `optimized.json` (the same 1,304 checks), with seven samples per phase.
Fixture SHA-256 hashes, whole-tab control counts, lightweight presenter counts, and
selector sizes match in all four fixtures. No build/test processes were started during
the final optimized measurement. The exploratory report is retained separately as
`optimized-exploratory.json` and is not used in this table.

| Species | Extra tables | Navigation preparation, ms before → after | Preparation allocations, MB before → after | Navigation + headless binding/layout, ms before → after | Binding/layout allocations, MB before → after |
| --- | --- | --- | --- | --- | --- |
| 412 | 0 | 23.930 → 22.359 | 28.487 → 25.944 | 558.272 → 540.957 | 274.601 → 268.685 |
| 412 | 250 | 57.336 → 29.889 | 208.574 → 80.245 | 923.608 → 674.841 | 636.998 → 327.007 |
| 1,024 | 250 | 61.636 → 31.303 | 211.844 → 83.515 | 809.728 → 708.299 | 637.292 → 330.279 |
| 2,048 | 250 | 60.448 → 33.513 | 217.580 → 89.252 | 814.717 → 634.544 | 642.329 → 334.550 |

All numbers are per 100 switches; MB is decimal cumulative managed-thread allocation,
not retained RAM. Binding/layout timings include navigation and explicit dispatcher/layout
work. The 412-species/250-extra-table fixture used 47.9% less preparation time, 61.5%
less preparation allocation, 26.9% less headless binding/layout time, and 48.7% less
allocation in that bound phase. The minimal fixture's 3.1% bound timing difference is
small compared with previously observed run-to-run variation; no noticeable native-UI
speed improvement is claimed for that case. Broader fixtures show the clearest benefit.

Untimed counts per 100 control-free switches fell from 400 to 100 anchor-list reads at
every size. With 250 extra tables, run lookups fell from 106,211 to 29,711 and anchor-name
lookups from 130,299 to 54,099. Member-collection changes and selector-choice replacements
remain zero. Section/table notification counts remain 100 each, as intended.

Outside a refresh, isolated paired getter reads now allocate 91.634 MB rather than
174.811 MB per 100 pairs with extra tables, because TableList discovers once rather
than three times. Main-selector and held-item candidate allocation measurements are
unchanged (2.536/11.542 MB for the 412/2,048-row main-selector cases, and 4.086 MB for
100 held-item constructors), confirming those later opportunities were not implemented.

Forced full-tab drawing remains a separate diagnostic: for 412 species with extra tables,
navigation plus forced draws changed from 1,149.602 to 921.670 ms / 100 switches, while
100 static draws remained approximately 108 ms. These are not native frame times.
Compiler, live script repointing, and native responsiveness are outside these measurements.

### Follow-up regression verification

- First upstream project: 2,664 cases, 1,616 passes, zero failures/engine errors, and
  the same 1,048 optional-ROM skips (`artifacts/upstream-tests/run.bMXgbP`).
- Second upstream project: 67 cases, 26 passes, zero failures/engine errors, and the
  same 41 optional-ROM skips (`artifacts/upstream-tests/run.f6G5X6`).
- Mac regression repeat: all 15 groups, 1,205 assertions, no skipped groups.
- Final Release benchmark: all four fixtures, 1,304 checks, including bound visible HP
  and selection, exact read-only bytes/metadata, and rename undo/redo restoration.
- The initial Mac run stopped on the previously observed intermittent FocusAnimation
  offscreen-clock movement assertion; the unchanged check then passed its 13 assertions
  in isolation and passed in the complete repeat. This checkpoint does not fix that
  test-clock instability or weaken the assertion.

The first full compilation reported existing Core warnings and the existing NamedByteTests
xUnit analyzer warning; the final incremental Release benchmark build had no warnings
or errors. No optional commercial ROM fixture was available for this follow-up.
