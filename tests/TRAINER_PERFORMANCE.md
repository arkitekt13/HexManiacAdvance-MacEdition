# Trainer table investigation — 2026-11-02

Branch: `investigate/trainer-table-performance`, based on `dcbf99f4`.
Host: Apple M3 Pro, macOS ARM64, .NET 8.0.5, Release, Avalonia 11.2.3.
The shared refresh-scoped table discovery improvement is already included in this baseline.
The initial checkpoint added tools and documentation only. The implementation follow-ups
below retain that historical baseline and distinguish UI restoration from optimization.

## Scope and method

The harness uses actual PokemonModel/ViewPort/TableTool instances, trainer text-stream
view models, and production TabView templates/bindings/layout in an in-memory headless
window. Skia rendering is exercised outside timing. Six fabricated binary fixtures use
the unchanged 40-byte trainer schema and all four trainer team layouts, with controlled
1–6 member counts and optional uncompressed trainer portraits/Pokémon preview sprites.
Each fixture has 768 trainers, 412 species names, 512 move/item names, and 64 portrait
options. Name-only portrait sources isolate the no-artwork cases. The fixture imports
only symbolic lists and table groups from default resources, never real game addresses.
Navigation cycles through trainers 0–99; the full 768-row table supplies selector choices
and all other trainer pointers are checked for preservation during the edit probes.

Each timing is a median of seven samples, after two warmups, per 100 operations. UI
timings include navigation plus explicit dispatcher/layout work. Managed-thread allocation
is cumulative allocation, not retained RAM or a process working-set measurement. Single
component times cannot be added to form a formal sampling-profiler breakdown. First-case
JIT/runtime costs and subsequent GC state can affect timings; allocation/reuse counts
are more reproducible. No native input, compositor, GPU frame rate, or real-ROM latency
is measured. Graphics decompression is deliberately excluded.

## Recorded baseline

Final detailed run (`artifacts/trainer-table-performance/repeat.json`):

| Scenario | Navigation preparation, ms | Preparation allocations, MB | Navigation + production headless bindings/layout, ms | Bound-phase allocations, MB |
| --- | --- | --- | --- | --- |
| One member, default moves/items, no artwork | 58.923 | 55.518 | 844.947 | 561.067 |
| One member, explicit moves/items, no artwork | 64.155 | 108.244 | 1,051.312 | 623.896 |
| Six members, explicit moves/items, no artwork | 282.541 | 379.810 | 1,350.897 | 923.835 |
| Six explicit members + Pokémon preview data | 281.317 | 380.141 | 1,473.144 | 925.764 |
| Six explicit members + trainer portrait choices | 369.695 | 551.761 | 1,522.417 | 1,106.141 |
| Mixed 1–6 members/all four formats + both artwork sources | 197.118 | 336.025 | 1,234.953 | 878.964 |

All numbers are per 100 switches, not single-switch latency. MB is decimal. Team preview
data is present in the model for the indicated cases, but the current production template
does not display it; the functional finding below explains why. The bound phase includes
navigation, so columns are not additive. Layout costs include the entire tab, not just
the team field. This is a baseline, not before/after evidence of an implemented fix.

| Isolated component | Median ms / 100 operations | Allocated MB / 100 operations |
| --- | --- | --- |
| Serialize six-member explicit teams | 0.142 | 0.450 |
| Validate pre-serialized six-member explicit teams | 84.054 | 106.562 |
| Construct generic stream candidates for those teams | 85.472 | 109.148 |
| Construct specialized trainer stream candidates | 85.091 | 109.153 |
| Construct four 512-option held-item dropdowns per trainer | 4.146 | 16.358 |
| Construct one 64-image portrait dropdown per trainer | 87.171 | 171.853 |
| Update portrait selector with prebuilt choices (experiment) | 0.100 | 0.019 |
| Generate six-member Pokémon preview collections | 28.636 | 16.338 |

The prebuilt-choice component excludes rendering/construction and does not implement
invalidation or a new editor cache; its ratio is not a promised whole-app speedup.
Preview timing comes from the first preview-artwork scenario and is especially sensitive
to sprite-path warmup; do not use it to extrapolate native rendering cost.

For 100 known move IDs 1–100 out of 512 options, matching `- MOVE0001`-style lines
took 3.282 ms / 3.722 MB; bare names took 0.342 ms / 0.394 MB in the six-member case.
Both forms returned ID 1 for the recorded `MOVE0001` comparison. This confirms extra
matching work on these inputs, not equivalence for every ambiguous or partially typed
name. Early return depends on the requested ID, so real move-name distributions matter.

The earlier complete run (`baseline.json`) passed 7,825 checks; the final run added six
exact-token fixture assertions and passed 7,831. Fixture/text/preview hashes match across
both runs. Shared component allocation totals repeated exactly except an 88-byte
difference in one navigation median; bound-phase allocations vary by less than 1% and
elapsed medians vary by up to about 14%. These measurements are descriptive, with no
timing pass/fail threshold. Raw generated reports are ignored; this summary and the
reproducing harness are tracked.

## Confirmed functional finding: trainer template is shadowed

The production TabView places its generic StreamElementViewModel data template before
the specialized TrainerPokemonTeamElementViewModel template. In this Avalonia view the
generic template wins for the derived trainer view model. All six unmodified production
cases realized a generic `StreamTextBox`, no specialized trainer TextEditor, and zero
bound Pokémon preview images, including cases with valid preview graphics.

After all production timings in the mixed-artwork case, the harness moves only the
trainer template to the front of that one in-memory template collection and regenerates
the group's presenters. The specialized TextEditor, four expected Pokémon previews,
and both Default Moves/Default Items command buttons then appear, with matching bound
text/pixel hashes and unchanged model bytes. This reproduces the issue and demonstrates
the ordering cause without modifying application XAML or requiring a manual screenshot.
Neither default command is executed; their behavior requires separate coverage.

This is a port UI correctness issue, rather than an optimization result. Restoring the
specialized template will add the intended displayed work; its cost needs benchmarking
again after a real fix. Backend preview timings below do not describe images currently
displayed in the shadowed production template. Sprite and palette templates also follow
the same generic base template in the source, but those model types were not tested here.

## Confirmed repeated preparation

### Discarded generic stream construction

TableGroupViewModel.AddChildrenFromPointerSegment first constructs a generic
TextStreamElementViewModel for IStreamRun/ITableRun destinations, then replaces it with
a TrainerPokemonTeamElementViewModel for a trainer team. The specialized type's base
constructor already performs the same serialization and text-editor setup. The generic
candidate is unused, yet it has already built highlight layers and run TrainerTextFormatter.

The existing live team view model is reused successfully: untimed passes observed zero
member-collection changes and the same team editor object after 100 switches. Its TryCopy
then updates the existing TextEditorViewModel content, which can validate the new text
again. Avoiding the discarded generic candidate is a contained first performance fix;
it does not require a persistent model cache or altered team write/repointing behavior.

### Trainer move-name validation takes the fuzzy path

TrainerTextFormatter.CheckMoveError computes a stripped `moveName`, but calls TryMatch
with the original line, including the `- ` prefix. ArrayRunEnumSegment.TryMatch normally
returns early on an exact name; prefixed lines cannot take that exact branch and instead
scan/normalize all options and use partial matching. This creates allocation and CPU
cost for every custom move line, during candidate construction and existing-editor updates.

The harness measures both arguments independently and records their lookup results.
Passing the stripped token is a candidate change to the formatter only, not a proposal
to rewrite the shared enum matcher. Duplicate names, quotes, punctuation, case, partial
input, `~` disambiguation, warning positions, and real learnability checks need dedicated
tests before changing behavior. This fixture lacks learnsets/evolutions and does not
establish that the advisory move-legality warnings are correct.

### Trainer portrait options are rendered before reuse is checked

ArrayRunEnumSegment.GetComboOptions rebuilds all portrait pixels and VisualComboOption
objects whenever the sprite field's candidate ComboBoxArrayElementViewModel is constructed.
Only afterward does TableGroupViewModel.TryCopy/FilteringComboOptions.Update compare
labels and retain the existing dropdown collection. The retained portrait choices were
the same objects across the diagnostic navigation pass, so the newly rendered candidate
portraits are discarded on unchanged rows.

Prebuilt choices are measured only as a component experiment; no cache is installed.
Any future reuse needs correct invalidation for edited/imported sprites and palettes,
repointing, source-table changes, undo/redo, and external reload. Closed-popup behavior
and resource lifetime also need testing. Do not trade fresh images for stale visuals.

### Pokémon team previews are rebuilt per getter

TrainerPokemonTeamRun.Visualizations builds new pixel view models on every read. Repeated
requests returned equal pixel hashes but fresh image objects. TextStreamElementViewModel
publishes Visualizations notifications on each row switch. This is secondary to the
functional template fix and measured construction/validation costs, because the current
generic template does not bind those previews at all.

## Source locations at the parent revision

| File | Line | Finding |
| --- | --- | --- |
| `src/HexManiac.Avalonia/Controls/TabView.axaml` | 454 | Generic stream template precedes derived templates. |
| `src/HexManiac.Avalonia/Controls/TabView.axaml` | 540 | Specialized trainer template with previews/default commands. |
| `src/HexManiac.Core/ViewModels/Tools/TableGroupViewModel.cs` | 232 | Generic stream candidate constructed before specialized replacement. |
| `src/HexManiac.Core/ViewModels/Tools/TableGroupViewModel.cs` | 236 | Specialized trainer stream candidate replaces generic one. |
| `src/HexManiac.Core/ViewModels/Tools/TextStreamElementViewModel.cs` | 79 | Candidate text triggers formatting/validation. |
| `src/HexManiac.Core/ViewModels/Tools/TextStreamElementViewModel.cs` | 88 | Reused editor receives new content and preview notification. |
| `src/HexManiac.Core/ViewModels/TextEditorViewModel.cs` | 123 | Text preparation invokes formatter error checking. |
| `src/HexManiac.Core/Models/Runs/TrainerPokemonTeamRun.cs` | 646 | Stripped move token is calculated but original line is matched. |
| `src/HexManiac.Core/Models/Runs/ArrayRunElementSegment.cs` | 289 | Exact/fuzzy enum-name matching implementation. |
| `src/HexManiac.Core/Models/Runs/ArrayRunElementSegment.cs` | 240 | Image choices render all sprite options. |
| `src/HexManiac.Core/Models/Runs/TrainerPokemonTeamRun.cs` | 363 | Team preview getter builds fresh images. |

## Recommended order

1. Correct specialized template selection, with headless checks for trainer controls and
   separate review of sprite/palette template selection; benchmark the restored feature set.
2. Avoid constructing the unused generic stream candidate for a known trainer destination;
   preserve the specialized constructor, pointer validation, editing, and history paths.
3. Test and correct the formatter's move-token argument, preserving matching and warning
   behavior for ambiguous/quoted/partial input before considering wider enum optimizations.
4. Investigate portrait-choice reuse or lazy image preparation with explicit source/image
   invalidation tests; revisit team preview reuse after the proper template is active.

## Correctness limits

Read-only phases compare exact full fixture bytes and serialized metadata and require no
undo history. Actual team edits grow 1-member teams and the selected 4-member mixed team,
forcing relocation past occupied guard bytes, or shrink a 6-member team to five. Checks
verify parent count/format, unchanged unrelated parent fields and all 767 other team
pointers, exact undo bytes and anchor definitions, and redo team/count/pointer restoration.
The allocator search cursor is not required to rewind after edits; full metadata equality
is required for read-only phases. Teams are independent; shared-team edits are not covered.

No compiler, binary write logic, game definition, pointer/repointing algorithm, undo
implementation, or existing test was changed. Current upstream/Mac regression results
remain those recorded for `dcbf99f4`; these suites are not rerun for tools/docs-only work.
The benchmark is a standalone investigation, not a new verify-mac group or a substitute
for a real-ROM mini-hack workflow.

The final Release build passed with zero warnings/errors; the first full dependency
compilation also reported the eight existing Core warnings. All six final fixtures and
the in-memory ordering probe passed all 7,831 benchmark checks. No default-move/item
commands, shared teams, or real-ROM legality checks are claimed as tested.

## Follow-up 1: restore specialized stream templates

Branch: `fix/specialized-stream-templates`, based on `ab860744`.
The generic StreamElementViewModel fallback now follows its trainer, sprite, and palette
specializations in production TabView. Their template bodies and command bindings are
unchanged. No Core/ROM behavior or preparation optimization is included in this checkpoint.

The new StreamTemplates headless regression failed on the missing trainer TextEditor
before the application change. After reordering, it passed 123 assertions covering all
four trainer formats, exact bound text, real preview bitmaps, visible enabled default
buttons, sprite bitmaps/Edit bindings, palette controls/swatches, and ordinary text fallback.
It invokes both trainer default buttons against the existing no-learnset/no-stats zero-data
fallback and undoes their changes; it does not establish real-ROM defaults or legality.
An edit through the production trainer TextBox grows a team, forces actual relocation,
updates previews, preserves unrelated pointers, and restores exact bytes/pointers on undo/redo.

The complete Mac verification passed 16 groups and 1,328 assertions, including the new
group. Existing upstream suites were not rerun for this XAML-only application change;
they will be rerun after the separate Core presentation optimization. The corrected UI
is benchmarked separately before that optimization, because restoring previews and the
specialized editor changes the feature set relative to the original generic-template baseline.

## Follow-up 2: construct the selected stream presentation only

Branch: `improve/trainer-stream-preparation`, based on the template fix `6c97667e`.
TableGroupViewModel now chooses trainer, palette, sprite, or generic text presentation
before constructing it, retaining the original replacement precedence in that order.
Pointer validation, stream parsing, specialized constructors, field reuse, change handlers,
write/repointing logic, enum matching, and image-choice preparation are unchanged. The
trainer no longer pays for a generic text candidate that would be immediately discarded.

The new upstream allocation regression failed before the change at 2,603,048 managed bytes
per warmed refresh of the six-member explicit-team fixture. It passes the unchanged
1,800,000-byte budget afterward, retaining the same live editor, exact team text, all six
highlight layers, diagnostics, ROM bytes, serialized metadata, and empty undo history.
The test links the existing synthetic fixture; its explicit imports support projects
with implicit usings disabled and do not change fixture data or workloads.

The first upstream suite passed 2,665 cases with 1,617 passes, zero failures/engine errors,
and the existing 1,048 optional-ROM skips (`artifacts/upstream-tests/run.r1Xdo0`). The
expected pre-fix run had only the new allocation failure (`run.YAeu3l`); no test budget
was relaxed to obtain the passing result.

After the Core change, the complete Mac run again passed all 16 groups and 1,328
assertions, including restored templates and the real UI edit/repointing checks. The
second upstream suite passed 26 cases with the existing 41 optional-ROM skips, zero
failures, and zero engine errors (`artifacts/upstream-tests/run.etba6Z`). The new allocation
test is Core-only and is part of the first upstream project's case count, not an extra
Mac assertion group. No new dependency is added to the application or either upstream suite.

### Corrected-UI before/after benchmark

Reports: `artifacts/trainer-table-performance/corrected-ui-before.json` at the template
fix, and `corrected-ui-after.json` after direct presentation selection. Both use the same
six workloads, two warmups, seven samples, and 100 operations per sample, with no builds
or tests running concurrently. Only the report scope wording and explicit fixture imports
changed in the harness; workload code and fabricated binary inputs are unchanged.

Both complete runs passed 7,831 checks. All fixture, team-text, and preview-pixel hashes,
UI summaries/control counts, and reuse/notification diagnostics match exactly. Both
versions use the specialized trainer TextEditor and bind the same preview counts. This
comparison therefore isolates the construction change from restoring previously hidden UI.

| Scenario | Preparation ms before → after | Preparation MB allocated before → after | Headless navigation/binding/layout ms before → after | Bound-phase MB allocated before → after |
| --- | --- | --- | --- | --- |
| Basic one member | 61.347 → 57.547 | 55.518 → 54.622 | 938.053 → 851.429 | 571.202 → 567.386 |
| Custom one member | 63.002 → 49.830 | 108.244 → 89.749 | 1,048.932 → 1,090.499 | 636.518 → 622.579 |
| Custom six members | 282.514 → 196.459 | 379.810 → 270.662 | 1,879.663 → 1,552.707 | 1,037.445 → 923.071 |
| Custom six + Pokémon preview data | 280.196 → 193.142 | 380.141 → 270.993 | 1,921.620 → 1,984.147 | 1,074.151 → 960.124 |
| Custom six + portrait options | 365.054 → 280.059 | 551.761 → 442.613 | 1,956.812 → 2,026.019 | 1,222.764 → 1,111.107 |
| Mixed formats/counts + all artwork | 196.405 → 166.936 | 336.025 → 298.948 | 1,848.558 → 1,844.463 | 1,047.368 → 1,007.079 |

Values are per 100 switches; MB is decimal cumulative managed-thread allocation. The
bound phase includes preparation and cannot be added to its columns. In the custom-six
case, preparation time decreased 30.5%, preparation allocation decreased 28.7%, and bound
allocation decreased 11.0%. With portrait options, preparation time decreased 23.3% and
allocation decreased 19.8%. Every six-member case eliminated 109,148,000 preparation
bytes per 100 switches, exactly the isolated discarded generic-candidate allocation.
The basic/one-member cases save less, as expected. The mixed-case allocation delta differs
from its component total by only 88 bytes, consistent with earlier measurement variation.

Headless bound elapsed results are mixed: the custom-six case improved 17.4%, while
custom-one, preview, and portrait cases measured approximately 3–4% slower, and the
mixed case was essentially unchanged. Earlier identical-revision UI runs varied by up
to about 14%, and different GC/layout work can fall within the sampled intervals. These
measurements do not establish a uniform bound-UI or native responsiveness improvement.
The reproducible saving is redundant construction/validation removed and lower managed
allocation, with unchanged visible features/output. Further UI speed claims require
layout/render profiling and real-ROM interaction testing.

The explicit generic/specialized constructor, validation, lookup, portrait construction,
and preview-generation component phases still exercise the same implementations; they
are not individually optimized here. No move-token matching or portrait caching change
is bundled with this result. The final benchmark build had zero warnings/errors; complete
Core rebuilds retained the existing warnings.
