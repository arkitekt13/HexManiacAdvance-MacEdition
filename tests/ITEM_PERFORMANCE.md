# Item table preparation and UI review — 2026-11-02

Initial branch: `investigate/item-table-preparation`, based on `04e158f1`.
Host: Apple M3 Pro, macOS ARM64, .NET 8.0.5, Release, Avalonia 11.2.3.
The first checkpoint adds tools/evidence, with no application change.

## Workload and measurement boundaries

The standalone ItemTableBenchmark mounts the actual grouped item editor, description
TextBox, conditional fields, compressed icon/palette controls, and surrounding TabView.
Fabricated 44-byte item tables cover 375/1024 rows, icons on/off, and normal/long descriptions.
A separate optional scenario uses the supplied BPRE1 16 MiB ROM's discovered tables. Synthetic
addresses are confined to fabricated data; the ROM is read from its supplied path and not
modified. Timed loops use two warmups and seven samples; synthetic phases have 100 operations,
while the ROM scenario has ten (items 0–9). Cumulative calling-thread managed allocations
are not retained RAM or peak process memory. Native input/compositor latency is not measured.
The initial synthetic case is affected by JIT/tiering and cannot support CPU scaling claims.

Deferred timers are deliberately excluded from foreground navigation. The first exploratory
passes used an immediate dispatcher, forcing the map-usage search on every item. Replacing
only the ViewPort dispatcher was insufficient because ToolTray obtains its dispatcher from
Singletons. Both are controlled in the final harness. The preliminary multi-GB/second-scale
numbers in `rom-baseline.json` and `rom-components-before.json` are **not** the foreground
baseline and must not be presented as an editor regression or optimization speedup. Their
main lesson is that deferred work needs separate measurement; native delayed-scan behavior
remains unmeasured here. Earlier interrupted exploratory runs are not completed results.

## Completed baseline

Valid baseline: `artifacts/item-table-performance/before.json`, passing 762 checks.

| Fixture | Operations | Navigation without controls, ms / MB | Bound navigation/layout, ms / MB | Explicit selected-icon refresh, ms / MB |
| --- | --- | --- | --- | --- |
| 375 synthetic, no icons | 100 | 30.021 / 14.943 | 936.687 / 492.531 | — |
| 375 synthetic, icons | 100 | 9.707 / 22.551 | 861.206 / 462.234 | 1.257 / 0.849 |
| 1024 synthetic, icons | 100 | 10.580 / 31.312 | 863.365 / 473.244 | 1.264 / 0.849 |
| 375 synthetic, long descriptions | 100 | 16.575 / 28.299 | 960.725 / 484.421 | 1.193 / 0.849 |
| Supplied FireRed 1.1 | 10 | 22.955 / 25.883 | 213.020 / 107.215 | 0.813 / 0.622 |

The corrected real-ROM foreground-only exploratory run additionally recorded 56.272 ms /
25.884 MB per ten navigations before the larger baseline's accumulated warmup. These are
component measurements with visible runtime sensitivity, not fixed native latency estimates.

Synthetic short-description candidate construction is about 0.24 ms / 0.994 MB per 100 in
warmed scenarios; long descriptions increase it to about 2.1 ms / 4.258 MB. Icons/palettes
are small preparation components. The real-ROM run has actual dependent tables and trainer
streams that the isolated fixtures omit; usage lookup and navigation discovery deserve
separate follow-up. Larger UI totals include the surrounding hex view, bindings, controls,
and layout, so they cannot all be attributed to the item icon or description field.

## Conservative improvement candidate

SpriteElementViewModel currently announces PixelWidth, PixelHeight, PixelData, and SpriteScale
on every tile refresh, including fixed 24x24 icons. PixelImage reacts to dimension notifications
by updating its bitmap, then repeats that work for PixelData; scale notifications also cause
layout. Diagnostic navigation recorded 103 of each notification for synthetic icon cases and
13 of each for the ROM case, even though dimensions and scale stayed constant.

A narrow follow-up can notify width/height/scale only when their published values change,
while continuing to render and publish PixelData on every explicit refresh. This avoids
persistent image caching or changes to decoding, palette resolution, import, data writing,
pointer relocation, or history. It should be treated as a modest presentation cleanup;
the selected-icon phase is already small, and a large overall browsing gain is not implied.
Coverage must include actual bitmap freshness after edits/palette changes, dimension and
scale changes, bound price/name/description edits, forced description relocation, and undo/redo.

## Coverage limits

Read-only baseline checks preserve exact bytes/metadata and empty history, verify selected
indices, actual bound descriptions/icons, and capture comparable fixture/text/pixel hashes.
The ROM scenario covers items 0–9, not every item/effect or every ROM. No commercial ROM is
committed. Both tools and regression tests leave delayed map usage outside foreground work.
File-dialog import/export, native scrolling/input, retained RAM, and other games remain separate.

## Follow-up: notify icon geometry only when it changes

Branch `improve/item-icon-notifications`, based on benchmark commit `1eb03f41`.
SpriteElementViewModel retains its last published width/height and compares the computed
scale before sending those notifications. The original pixel decoding, palette resolution,
rendering, and PixelData notification path are retained. A fixed-size visible icon therefore
requests one pixel upload through PixelData instead of three through width, height, and
pixels; unchanged scale also avoids a layout notification. Initial presentation and genuine
dimension/scale changes still notify. This is two small presentation-state integers per VM,
not an image cache, deferred rendering scheme, or editing/history change.

The new ItemTablePresentation group fails before the change on redundant dimension updates
and passes 48 assertions afterward. It verifies identical actual bitmaps on explicit refresh,
retained PixelData updates, metadata-only dimension changes, a real scale transition through
paged-view-model copying, actual bound price/name/description edits, forced description
relocation with unrelated pointers preserved, palette/sprite edits, and exact-byte undo/redo.
A spaced name retains its existing quoted selector label; test expectations follow that
existing behavior rather than changing application naming. Its README distinguishes the
wide-sprite presentation copy from complete format-import orchestration.

Both upstream suites pass: 1,780/26 cases, 935/41 missing-ROM skips, zero failures/engine errors.
All 18 Mac groups pass 1,444 assertions, including the new 48 and the existing trainer,
portrait, map, and script presentation checks. The Release rebuild succeeds with eight
existing Core warnings and zero errors. No test threshold was weakened or case suppressed.

### Before/after evidence

The same workload at `1eb03f41` (`before.json`) and with the notification change (`after.json`)
passes 762 checks with identical source, description, model-pixel, and actual bound-bitmap
hashes, formats, and field-type counts in all five scenarios. No builds/tests run concurrently
with timed phases. The untimed fixed-size icon diagnostics change from 103/13 width, height,
and scale notifications to zero, retaining all 103/13 PixelData notifications.

| Icon refresh case | Operations | Before ms | After ms | Before allocated MB | After allocated MB |
| --- | --- | --- | --- | --- | --- |
| 375 synthetic items | 100 | 1.257 | 0.833 | 0.848800 | 0.763200 |
| 1024 synthetic items | 100 | 1.264 | 0.775 | 0.848800 | 0.763200 |
| Long descriptions | 100 | 1.193 | 0.774 | 0.848800 | 0.763200 |
| FireRed 1.1, selected item | 10 | 0.813 | 0.796 | 0.621920 | 0.613360 |

The synthetic component is roughly one-third faster in this pair, saving about 0.4 ms
and 85.6 KB per 100 icon refreshes. That is approximately four microseconds per icon,
not a large user-visible browsing win. Real-ROM icon refresh time is essentially unchanged;
its preparation/lookup cost dominates this tiny presentation saving. The deterministic
reduction is two redundant bitmap-update requests and one scale notification per stable
refresh, with 856 fewer managed bytes allocated per measured icon refresh.

Whole-table timings are mixed: bound 375-item synthetic navigation changes from 861.206 to
895.192 ms per 100, 1024 items from 863.365 to 867.592, and long descriptions from 960.725 to
968.810; the supplied-ROM case changes from 213.020 to 188.207 ms per ten. The no-icon control
case changes from 936.687 to 941.696 ms. These variations do not establish a uniform browsing
speedup or regression, and the isolated tiny icon saving cannot explain the larger movement
in overall timings. Read-only preparation allocations barely change (64 bytes saved per row
in these scenarios); no retained-memory reduction or native latency claim is made.

The modest benefit is appropriate to the small change and added editing coverage. The review
does not justify a new item icon/description cache. More consequential work should profile
the normally delayed map-usage lookup separately, investigate dependent-usage preparation
with matching freshness tests, or follow concrete workflow feedback from native editing.


Repeat: `artifacts/item-table-performance/after-repeat.json` also passes 762 checks with
all integrity/presentation hashes, formats, and field counts matching both earlier runs.
Selected-icon refresh medians are 3.835 ms / 0.763288 MB for 375 synthetic items,
0.839 ms / 0.763200 MB for 1024, 3.822 ms / 0.763256 MB for long descriptions (all per 100),
and 0.773 ms / 0.613360 MB for the ROM (per ten). The synthetic timings vary substantially
between processes; even the isolated first-pair speedup is not consistently reproduced.
The repeat supports the allocation and notification reduction, not a stable elapsed-time
speedup. No further run was selected to replace the less favorable measurements. The
cleanup and added regression coverage are the outcome; larger optimization claims would
require more controlled profiling and a larger bottleneck.

The delayed search follow-up is now measured separately in
[ITEM_USAGE_PERFORMANCE.md](ITEM_USAGE_PERFORMANCE.md), including the actual Mac timer,
closed-menu behavior, ordered results, and a tool-only shared-script experiment.
