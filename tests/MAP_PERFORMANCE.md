# Map presentation performance — 2026-11-02

Branch: `improve/map-rendering-performance`, based on `9febf8f7`.
Host: macOS ARM64, .NET 8.0.5, Release benchmark; actual Avalonia/Skia offscreen controls.

## Confirmed opportunity and scope

Single-cell painting already updates the affected Core map pixels directly in DrawBlock;
this review did not replace that behavior. A confirmed avoidable cost exists downstream:
PixelImage treated SpriteScale notifications as pixel changes and scheduled a full bitmap
conversion/publication for every completed zoom step, even when map pixels were unchanged.

Zoom and ScaleAffectsLayout now update Width/Height through the same layout helper used
after pixel publication. They preserve the existing bitmap and pending background pixel
work. PixelData/dimension changes, debug-grid changes, and transparency changes retain their
pixel-update path. Buffer swapping and generation guards are unchanged. No Core code changed.

## Reproducible benchmark

The fixture constructs binary tables, two-color uncompressed RSE-style blocksets, palettes,
and checkerboard cells at fabricated addresses, then uses real PokemonModel, ViewPort,
BlockMapViewModel, and PixelImage instances. It is not a commercial ROM, screenshot, mocked
renderer, or generated stand-in image. Three map sizes fit the existing map-size constraint.
Events are absent; NPC rendering, compressed graphics, and game-specific workflows are outside
this fixture's coverage.

After a warmup map, each size records one cold pixel build, 100 cached pixel reads, and seven
samples of ten alternating 1×/2× zoom steps. Each step pumps the dispatcher until the expected
image dimensions have been applied. A Source-property notification counts a bitmap publication;
this is not a GPU-driver upload counter. Frame conversion happens before that publication in
the existing update path. Output JSON retains raw timings, runtime identity, allocations, and
rendered-pixel SHA-256 hashes.

| Map cells | Bitmap pixels | Before: 10 zoom steps, median ms | After: median ms | Bitmap publications / 70 zooms | UI-thread allocated bytes / 10 zooms |
| --- | --- | --- | --- | --- | --- |
| 16 × 16 | 256 × 256 | 1.737 | 0.019 | 70 → 0 | 22,080 → 5,520 |
| 48 × 48 | 768 × 768 | 12.963 | 0.019 | 70 → 0 | 42,560 → 5,520 |
| 96 × 64 | 1536 × 1024 | 33.917 | 0.019 | 70 → 0 | 73,280 → 5,520 |

All three rendered map pixel hashes match exactly before and after. The fixtures' source
bytes are checked unchanged by the benchmark. Cached reads reuse the existing pixel array.
The single cold-render samples vary between runs; no cold-render speed improvement is claimed,
and the Core renderer was not modified.

These timings measure offscreen bitmap-presentation processing until explicit dimensions
are updated. They exclude native window layout, input handling, compositor/GPU work, and
real-ROM event complexity. Do not describe the ratio as a measured improvement in overall
zoom speed or frame rate. The robust result is **70 unnecessary bitmap publications removed
per 70 zoom-only steps** at each size. Background-thread allocations are excluded from the
UI allocation counts. Actual pixel edits continue to publish replacement frames.

Raw reports: ignored `artifacts/map-performance/before.json` and `after.json`.
Run the benchmark with the command in tools/MapRenderingBenchmark/README.md.

## Regression evidence

Before the change, the real synthetic-map regression failed because zoom replaced the bitmap
(`/tmp/hma-map-before.log`). Afterward MapRendering passes **93 assertions**, covering all
three sizes, exact displayed BGRA bytes across zoom, rendered pixels, source bytes and exported
metadata, pointer/header preservation, reveal masks, layout opt-out/re-enable, actual painting,
undo/redo, selection overlays, ordinary sprite images, debug/transparency updates, pending
frames during zoom, and map-context replacement.

The synthetic map sits outside a native map tab, so its caches are explicitly cleared after
undo/redo before comparing rendered pixels. The tests verify the real history/data and renderer
paths; they do not claim native-tab invalidation or mouse-coordinate correctness. Header/event
pointer slots are checked unchanged, but there is no NPC event payload in these fixtures.

Validation:

- Full Mac runner: **15 groups, 1,205 assertions**, no groups skipped
  (`/tmp/hma-map-performance-final-mac.log`).
- First upstream project: **2,657 cases: 1,609 passed, 0 failed, 1,048 optional-ROM skips**,
  zero engine errors (`artifacts/upstream-tests/run.SRrv9T`).
- Second upstream project: **67 cases: 26 passed, 0 failed, 41 optional-ROM skips**,
  zero engine errors (`artifacts/upstream-tests/run.c6xIGm`).
- Diff against `9febf8f7` confirms **no changes anywhere in src/HexManiac.Core**: map bytes,
  compiler/repointing, pointer definitions, flags/variables, map rendering/editing algorithms,
  cache invalidation, and undo logic retain their existing implementations.

Real-ROM workflows and native zoom/scroll/hover feel remain unverified. This is one isolated
presentation optimization, not a claim that every map-editor bottleneck has been addressed.
