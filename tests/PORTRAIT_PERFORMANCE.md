# Trainer portrait preparation and freshness review — 2026-11-02

Branch: `investigate/trainer-portrait-preparation`, based on `b0008438`.
Host: Apple M3 Pro, macOS ARM64, .NET 8.0.5, Release, Avalonia 11.2.3.
The initial checkpoint below is tools/documentation only. Separately committed correctness
and model-scoped image-reuse follow-ups are recorded at the end.

## Scope and method

The previous trainer benchmark identified expensive preparation of portrait dropdown
choices. This harness isolates the actual portrait enum field in a fabricated 128-row
table, with 16/64/128 uncompressed 64×64 4bpp images and a 64-image compressed case.
It uses real PokemonModel, ComboBoxArrayElementViewModel, TableTool navigation, and
production TabView/AngleComboBox bindings in an in-memory headless/Skia window. It is
not the complete 40-byte trainer/team fixture, and counts are experimental inputs,
not a claim about a stock game. No ROM or researched address is used.

Phases have two warmups and seven measured samples. Times and managed-thread allocations
are per 100 operations, except ten popup open/close state-and-layout cycles. Popup
state is checked in both the actual control and its bound selector. These timings do
not measure native popup painting, compositor work, frame rates, or user input latency.
Allocation is cumulative temporary managed allocation, not retained RAM. Initial-case
JIT/tiering effects are visible; the 16-image candidate timing must not be interpreted
as a scaling result. Allocation growth and later-case costs are the stronger evidence.

## Recorded results

Final run: `artifacts/trainer-portrait-performance/repeat.json`.

| Images | Encoding | Fresh field candidates, ms / 100 | Candidate allocations, MB / 100 | Control-free navigation, ms / 100 | Navigation allocations, MB / 100 |
| --- | --- | --- | --- | --- | --- |
| 16 | Uncompressed | 82.526* | 43.405 | 26.094 | 50.049 |
| 64 | Uncompressed | 88.263 | 171.856 | 91.585 | 179.799 |
| 128 | Uncompressed | 176.679 | 343.125 | 159.080 | 352.809 |
| 64 | LZ compressed | 166.344 | 185.117 | 169.586 | 193.175 |

*First-case runtime warmup/tiering affects this timing. Do not compare it directly with
the following cases to estimate per-image cost. MB is decimal.

The 64-image uncompressed candidate costs about 0.883 ms and 1.719 MB each. Doubling
the image count roughly doubles its allocation. The controlled LZ input nearly doubles
CPU cost, but its regular fabricated pixel pattern is not representative of all real
graphics compression ratios or decompression work.

| Images/encoding | Closed-selector navigation + binding/layout, ms / 100 | Bound-phase MB / 100 | Popup state/layout cycles, ms / 10 |
| --- | --- | --- | --- |
| 16 uncompressed | 419.361 | 247.741 | 2.584 |
| 64 uncompressed | 563.426 | 385.501 | 4.274 |
| 128 uncompressed | 708.569 | 570.277 | 2.375 |
| 64 compressed | 646.085 | 400.656 | 2.055 |

The bound phase includes navigation and the rest of the tab; component columns are
not additive. The small popup-state numbers are not evidence that rendering every
native popup image is cheap. Fresh candidate rendering already occurs while switching
rows with the dropdown closed, before unchanged-choice reuse is checked.

The existing model image-cache experiment returned matching pixels for every fixture.
For 64 uncompressed images, 100 batches of cache reads took 0.271 ms / 0.154 MB after
warming, versus 88.263 ms / 171.856 MB for full fresh field construction. Those workloads
are different: cache reads omit enum/view-model setup, and no cache has been wired into
the application. This is evidence of reusable work, not a shipped or predicted speedup.
A cache also retains image data: 64 arrays of 4,096 shorts contain about 0.5 MiB before
object/native overhead. Process working-set or long-session retained memory was not measured.

## Confirmed correctness finding: live image choices can stay stale

The fresh candidate correctly renders updated pixels, and the existing model cache
returns the updated pixels after its scope is replaced. However, the live dropdown
retains its old VisualComboOption objects because FilteringComboOptions.Update compares
only option text. New pixels with the same labels are discarded during TryCopy.
All four mutation probes use the 64-image uncompressed fixture; the compressed case
establishes read-only rendering/cache equivalence only.

| Probe | Fresh candidate pixels changed | Existing model cache refreshed | Live choices retained | Live choices stale after refresh | Stale after redo |
| --- | --- | --- | --- | --- | --- |
| Sprite edit through ISpriteRun.SetPixels | Yes | Yes | Yes | Yes | Yes |
| Palette edit through IPaletteRun.SetPalette | Yes | Yes | Yes | Yes | Yes |
| Shared-pointer repoint, then edit the new sprite | Yes | Yes | Yes | Yes | Yes |
| Model.Load with changed sprite bytes, then refresh | Yes | Yes | Yes | Yes | Not a history operation |

The repoint probe registers a second source before calling RepointToNewCopy, respecting
its existing shared-pointer requirement. Undo/redo restores exact bytes and pointer
state, and undo restores named anchor definitions. The bug is in presentation freshness,
not evidence of ROM corruption. The model-reload probe bypasses file I/O/watcher logic.
Invalid stored portrait 255 is preserved, with 64 image choices plus a numeric fallback.

This behavior is in shared Core view-model code; it is not established as Mac-specific.
The probes compare actual live choice pixel data used by the bound dropdown, not native
screenshots. They do not certify all import, palette, compressed-edit, or format-change paths.

## Invalidation/source review

| Area | Existing behavior | Implication |
| --- | --- | --- |
| ArrayRunEnumSegment.GetComboOptions, line 240 | Renders every portrait for each candidate. | Real repeated work exists before UI reuse. |
| ComboBoxArrayElementViewModel.TryCopy, line 165 | Passes candidate choices into the retained FilteringComboOptions. | Unchanged labels can keep obsolete image objects. |
| FilteringComboOptions.Update, line 82 | Compares Text sequences, then can return early. | Cache invalidation alone cannot repair this existing stale presentation. |
| BaseModel indexer, IDataModel.cs line 185 | Writes clear CurrentCacheScope. | Ordinary token-based pixel/palette writes invalidate model-local images. |
| ViewPort.Refresh, line 1443 | Clears the model cache and rebuilds table preparation. | The probe still reproduces stale live choices after this refresh. |
| SpriteTool.ImportSpriteAndPalette, line 784 | Uses sprite/palette import paths and calls Refresh afterward. | Import orchestration reaches relevant refresh behavior, but file dialogs/remapping were not executed. |
| ViewPort.ReloadMetadata, line 2491 | Loads replacement bytes/metadata. | The underlying model-load path was probed; external-file orchestration remains separate. |
| ModelCacheScope.GetImage, line 104 | Caches by sprite start using SpriteDecorator.BuildSprite. | Existing infrastructure may help, but key/palette/index/page equivalence must be established. |

The existing image cache uses a different rendering entry point from GetComboOptions:
the latter passes an enum index to GetPixels and resolves a palette with FindMatchingPalette;
the former uses SpriteDecorator.BuildSprite and a sprite-start key. Equal pixels in these
single-palette/page fixtures do not prove equivalence for indexed palettes, aliases, custom
formats, shared sources, palette hints, format edits, or every compressed representation.

## Recommendation

The measured cost warrants further work, but **do not introduce a new persistent portrait
cache now**. First make a small, separately tested correctness change so new visual option
content is reflected even when labels stay the same. Keep the existing freshly rendered
candidate path for that checkpoint. Cover selection/filter preservation, popup close/reopen,
sprite/palette edits, undo/redo, repointing, and reload before optimizing image reuse.

After that, evaluate a narrow experiment using the existing model-local image cache,
with exact pixel/format/palette comparisons and explicit invalidation tests. If that
requires a large dependency tracker or general cache architecture, defer it. The current
evidence does not justify that complexity. No lazy/global cache is implemented here.
Items remain the next broader table-profiling target; this review does not implement
item changes or start an unrelated optimization.

## Verification and limits

The first complete run passed 55 checks. The final repeat adds popup-state binding checks
and numeric fallback preservation, passing 66 checks. Source/pixel hashes and all four
freshness observations repeat exactly across both runs. Staleness is recorded as a finding;
the success marker does not claim the existing bug was repaired. Read-only phases preserve
exact model bytes/metadata and create no undo history. Mutation probes verify undo/redo
bytes and anchors where applicable. The allocator search cursor is not required to rewind.

The final Release build passed with zero warnings/errors; the initial dependency rebuild
retained eight existing Core warnings. No application or existing test files changed, so
the broader suites were not rerun for this tools/docs checkpoint. Their last baseline is
the preceding move-validation checkpoint: 1,641/26 upstream passes with 1,048/41 optional-ROM
skips and 16 Mac groups/1,328 assertions. Native UI behavior, import dialogs/remapping,
real ROMs, long-session memory, and broader palette/format cases remain unverified.

## Follow-up 1: live choice correctness

`fix/portrait-choice-freshness` compares visual dimensions, pixels, scale, display-index
behavior, type, text, and enum indices when retaining choices. Equal images still retain
the existing collections. No rendering or cache change is included in this checkpoint.

The new offscreen control regression first reproduced stale live pixels while freshly
constructed candidates were correct. Fixing that revealed another Mac issue: replacing
ItemsSource coerced the visible selection to -1 without changing the model value. The
AngleComboBox now resynchronizes on the VM's selection notification using SetCurrentValue,
preserving its binding. The test requires the actual selected WriteableBitmap to change.

The eight compressed/uncompressed edit, palette, shared-pointer repoint, and reload
scenarios pass 68 assertions, including exact-byte undo/redo where applicable. Ten new
upstream cases guard visual comparison and text filtering. Native appearance, real ROMs,
image-import orchestration, and long-session memory remain outside these checks.

## Follow-up 2: reuse enum-rendered images within the model scope

Branch `improve/portrait-image-preparation`, based on correctness commit `fe5fe625`.
The existing address-only `GetImage` cache remains untouched: its rendering policy is
not interchangeable with the enum path. Instead, a small dictionary inside the existing
ModelCacheScope retains the original enum-rendered pixels and width, keyed by the concrete
sprite-run reference, palette-run reference, and enum index. Palette lookup still happens
on each candidate. Rendering still uses page zero, the same initial-blank-page policy,
the same default palette, and the same SpriteTool.Render call.

Only exact SpriteRun/LzSpriteRun and PaletteRun/LzPaletteRun types participate; other
implementations render fresh. These concrete run objects have immutable format properties
and reference identity. Metadata-only replacement therefore changes the key even without
a scope reset; byte writes, refresh, undo/redo, and reload retain their existing model-cache
invalidation. No persistent/global cache, dependency tracker, renderer replacement, or
ROM-editing algorithm was introduced. Consumers treat retained pixel buffers as read-only.

Sixteen new upstream cases compare all fixture choices against the original uncached
renderer, cover warm pixel-buffer reuse and allocation, writes without explicit Refresh,
metadata-only sprite dimensions/palette pages/hints, default palettes, compressed palettes,
aliased sprite pointers with separate enum indices, separate models, and an unknown sprite
subclass with additional mutable context that must bypass caching. The 64-portrait warmed
allocation tests failed before the change at 1,726,968/1,859,576 bytes (uncompressed/compressed)
and pass the same 400,000-byte budget afterward. Read-only preparation preserves exact bytes,
metadata, and empty undo history. The earlier 68 live-bitmap/control assertions also pass.

### Controlled before/after measurement

Before: `artifacts/trainer-portrait-performance/freshness-fixed-before.json` at `fe5fe625`.
After: `artifacts/trainer-portrait-performance/cached-after.json`, with a second run at
`cached-repeat.json`. The same Release workload, two warmups, and seven samples were used;
no build or test process ran concurrently with the timed phases. Harness changes only
clarify the report description; workloads/assertions are unchanged. All three runs pass
66 checks with identical source/pixel hashes and complete freshness results, including
fresh live choices after sprite/palette edits, repointing, reload, and redo. Invalid value
255 remains a numeric fallback without ROM normalization.

Medians below are per 100 operations. MB means cumulative managed calling-thread allocation,
not retained memory. Before/after refer to the first after run for a single consistent pair.

| Images / encoding | Candidate ms, before → after | Candidate MB, before → after | Navigation without controls ms, before → after | Navigation MB, before → after |
| --- | --- | --- | --- | --- |
| 16 uncompressed | 68.490 → 2.298 | 43.405 → 3.725 | 23.296 → 6.698 | 50.013 → 10.326 |
| 64 uncompressed | 77.406 → 3.014 | 171.856 → 13.136 | 81.192 → 4.909 | 179.763 → 21.043 |
| 128 uncompressed | 154.873 → 6.172 | 343.125 → 25.685 | 160.786 → 8.468 | 352.773 → 35.333 |
| 64 compressed | 146.749 → 3.301 | 185.117 → 13.136 | 150.104 → 5.396 | 193.139 → 21.158 |

The initial 16-image case remains sensitive to tiering and should not be used for CPU
scaling claims. Later cases show approximately 92–93% less candidate allocation; the
64-uncompressed candidate component is about 26 times faster in this run. This is the
preparation component, not a whole-editor multiplier. Cold rendering still has to happen.

| Images / encoding | Bound closed-selector navigation ms, before → after | Bound allocation MB, before → after |
| --- | --- | --- |
| 16 uncompressed | 345.350 → 321.998 | 245.380 → 205.069 |
| 64 uncompressed | 435.055 → 324.568 | 383.108 → 220.846 |
| 128 uncompressed | 575.750 → 328.478 | 566.312 → 241.571 |
| 64 compressed | 516.320 → 334.729 | 398.855 → 220.792 |

Headless bound navigation improved in this pair, but this does not establish native
frame rate or input latency. Popup cycles and the separate address-only-cache experiment
are tiny/noisy and do not support a further improvement claim. The second after run is
recorded below as a repeat rather than selecting the fastest sample.

### Verification and practical limits

Both upstream projects pass: 1,667/26 cases, 1,048/41 optional-ROM skips, zero failures and
engine errors. The Mac suite passes 17 groups/1,396 assertions. An intermediate upstream
run hit the existing whitespace-allocation test at 8,152 bytes against its 4,096-byte
budget; the complete final run passes that unchanged test. No threshold was weakened or
case suppressed. Release build succeeds with eight existing Core warnings and no errors.

Retaining rendered buffers trades some lifetime memory for avoiding repeated decoding and
allocation. Sixty-four 64×64 short-pixel buffers contain about 0.5 MiB, or 1 MiB for 128,
plus dictionary overhead; choices may also retain equal buffers from an earlier scope.
This is not a claim of reduced retained RAM or a measured peak-memory bound. Full refresh
and writes rebuild the cache; the measured gain is repeated read-only preparation within
its lifetime. Long sessions, native UI, real ROMs, image-import dialogs/remapping, and every
custom/indexed-palette format remain unverified. Items remain the next profiling target.

Repeat after medians (per 100): 64 uncompressed candidates 2.959 ms / 13.136 MB,
128 uncompressed 5.979 ms / 25.685 MB, and 64 compressed 3.314 ms / 13.136 MB;
bound navigation respectively 327.469, 327.878, and 336.128 ms. This repeats the
preparation/allocation reduction without changing the correctness observations.
