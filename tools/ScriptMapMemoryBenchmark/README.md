# Script map memory investigation

```sh
dotnet restore tools/ScriptMapMemoryBenchmark/ScriptMapMemoryBenchmark.csproj --ignore-failed-sources -p:NuGetAudit=false
dotnet build tools/ScriptMapMemoryBenchmark/ScriptMapMemoryBenchmark.csproj -c Release --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
dotnet tools/ScriptMapMemoryBenchmark/bin/Release/net8.0/ScriptMapMemoryBenchmark.dll /tmp/script-map-memory.json
```

Add `--closing-only` to run only the four-document close probe. All maps and 16/32-MiB ROM
buffers are fabricated. Windows render through Avalonia.Headless/Skia; no personal ROM,
preferences, emulator or native window is used. The tool is separate from the application.

The tool warms rendering, samples managed allocations/heap and process working set, then
uses forced collection and weak references after releasing frames marked NoInlining. It
covers display/clear cycles at 80×52, 128×128 and 256×256 tiles, four overlapping snapshot
requests, completed/rapid actual tab switches with Map View on/off, whole-window teardown,
and closing documents while another document remains open. Live source bytes are checked;
the detached-render phases also compare source metadata and saved/history state.

The diagnostic cleanup experiment deliberately clears *owned detached controls* only, using
reflection to inspect private bitmap ownership and document event targets. It removes the
identified subscriptions but does not release every retained control. It is **not a proposed
production cleanup implementation**. The full tab/control lifecycle needs further investigation.

Working set can retain allocator/renderer caches after release. A sampled heap peak is not
native RAM usage, and total allocated bytes are not retained bytes. Weak-reference results,
stable post-GC samples, and paired Map/Hex controls separate those observations. Forced GC
is instrumentation only; it must not be added to the app. Timing varies with host/background
activity and is not a real-ROM or native responsiveness claim.

See [the review](../../tests/SCRIPT_MAP_MEMORY_REVIEW.md) for preserved measurements, scope
and a bounded follow-up plan. The benchmark depends on private field names for diagnostics.

## Lifecycle-only comparison and ownership tracing

Append `--trace-only --no-root-trace` to run just actual Hex/Map tab-switch and window-close
phases, without the large snapshot/burst warmup. This is the protocol used by
`tests/tab-lifecycle-memory/before.json` and `after.json`. Before measurements precede any
production lifecycle repair; reflection tracing occurred after their settled heap samples.
Do not compare absolute heaps against the warmed full-corpus protocol as identical workloads.
Omit `--no-root-trace` to request a bounded reflection search from document event owners to a
watched detached visual/logical child. It ignores weak references/primitive ROM buffers and
can itself allocate heavily; use it only for diagnosis, never as production code or a timing
measurement. The baseline cleanup experiment remains in the protocol but has no controls to
clean once the repair releases them. Source bytes must match in every run.
