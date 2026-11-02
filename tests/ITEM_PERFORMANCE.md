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
