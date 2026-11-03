# Trainer portrait cost and freshness review

Run from the repository root with .NET 8:

```sh
dotnet run --project tools/TrainerPortraitBenchmark/TrainerPortraitBenchmark.csproj --configuration Release -- artifacts/trainer-portrait-performance/current.json
```

This standalone headless/Skia executable uses actual PokemonModel, enum candidate,
TableTool, TabView, and portrait selector code. The fixture isolates one byte-sized
trainer portrait enum in a 128-row table; it is not a full trainer/team benchmark.
Every address, palette, and 64×64 4bpp sprite is fabricated. Cases contain 16, 64, or
128 uncompressed portraits, plus 64 compressed portraits using HMA's LZ codec.
No application code or cache behavior is changed by this tool.

Each phase has two warmups and seven measured samples. Candidate construction, table
navigation, and headless closed-selector binding/layout use 100 operations per sample;
popup state/layout cycles use ten. JSON retains medians, raw times/allocation samples,
source/pixel hashes, runtime identity, and freshness observations. Allocation is cumulative
managed calling-thread bytes, not retained RAM or total-process/native allocation. The
first case is sensitive to tiered JIT warmup; compare allocations and later-case timing
alongside the full report. No elapsed-time threshold determines correctness.

An explicit experiment reads already prepared images from the existing ModelCacheScope.
It checks equality against freshly rendered portrait pixels for these fixtures. The
experiment excludes enum/view-model setup and is not an implemented editor optimization,
cache invalidation guarantee, or whole-app speedup. The real app's palette/index/page
semantics differ from that address-only cache. The implemented follow-up instead reuses
the original enum renderer with sprite/palette/index context inside the model scope;
see the report and PortraitPreparationTests for its separate coverage.

Mutation probes use the real sprite/palette write APIs, a real shared-pointer repoint
command followed by editing, and model reload followed by refresh. They compare new
candidates, live retained choices, and the existing model image cache, and verify exact
undo/redo bytes and anchor restoration where history applies. A numeric portrait value
of 255 must remain unchanged with its fallback choice. The tool records stale choices
as a finding rather than asserting that existing application behavior is correct.

The final marker is `BENCHMARK_CHECKS_PASSED:N`; failure exits nonzero. This tool is not
a new verify-mac group. Native popup painting, image file dialogs/index remapping, file
watchers, multi-palette/index-specific formats, format/count changes, and real-ROM
behavior are outside this fixture. Import/reload orchestration is reviewed in the source.

See [PORTRAIT_PERFORMANCE.md](../../tests/PORTRAIT_PERFORMANCE.md) for measured evidence,
the initial freshness finding and separately verified correctness/performance follow-ups.
