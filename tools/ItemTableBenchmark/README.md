# Item table benchmark

Build in Release, then run from the repository root:

```sh
dotnet run --project tools/ItemTableBenchmark/ItemTableBenchmark.csproj -c Release -- artifacts/item-table-performance/current.json
```

An optional second argument is the path to a local ROM; it is read into memory, never
modified or copied into the repository. A third argument `--rom-only` limits scenarios
to that explicitly supplied ROM. ROMs are not bundled and remain ignored by Git.

Four fabricated fixtures cover 375 items without icons, 375/1024 with compressed 24x24
icons and palettes, and 375 with 1,000-character descriptions. The optional real-ROM case
uses the model's own discovered formats and tables, not fabricated ROM addresses. The
synthetic fixture derives its 44-byte item schema from the existing reference file and
includes pocket-dependent type fields in full navigation; direct dropdown timings cover
only the unconditional hold-effect and pocket fields.

Each phase has two warmups and seven measured samples, retaining all samples and medians.
Synthetic phases use 100 operations; the ROM case uses ten (items 0–9). Real-ROM phases
also isolate table refresh, dependent array/stream usage searches, and navigation-list
reads outside the refresh scope. Construction phases include per-row address lookups.
The first scenario is sensitive to JIT/tiering and must not be treated as a scaling result.

The real TabView, templates, bindings, layout, Skia drawing, text boxes, and PixelImage
buffers are mounted offscreen. Navigation excludes native input/compositor latency. The
selected-icon refresh phase retains the same item and explicitly calls UpdateTiles, then
pumps layout. Untimed notification counts include the diagnostic navigation plus three
verification rows (103 synthetic / 13 ROM); they do not contaminate timed phases.

Both ViewPort and its Singletons use the controlled dispatcher. ManualWorkTimer coalesces
scheduled work without executing it, so deferred map-usage scans are excluded. Immediate
map searches are not a valid foreground-browsing baseline. This harness does not establish
the latency or allocation of the native delayed scan after the user pauses.

Assertions preserve source bytes, metadata, history, and error state; check current item,
description binding and icon dimensions; and record source/description/pixel hashes plus
actual bound bitmap hashes for comparing revisions. It prints BENCHMARK_CHECKS_PASSED only
after the complete JSON has been saved. It is a standalone measurement tool, not a timing
threshold in the Mac suite. See tests/ITEM_PERFORMANCE.md for findings and limits.
