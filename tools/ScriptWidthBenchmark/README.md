# Script document-width benchmark

From the repository root:

```sh
dotnet build tools/ScriptWidthBenchmark/ScriptWidthBenchmark.csproj -c Release -p:NuGetAudit=false
DOTNET_TieredCompilation=0 dotnet tools/ScriptWidthBenchmark/bin/Release/net8.0/ScriptWidthBenchmark.dll artifacts/script-performance/width-current.json
```

The benchmark uses the actual TextEditor in a headless Skia window. A derived control surrounds
UpdateDocumentWidth, so counts/time/allocations describe real calculations, including deferred
ones, rather than TextChanged notifications or queue requests. It uses the unchanged production
measurement routine; no measurements are omitted and no constructor-handler reflection is used.

Five workloads cover 100/500/2,000 short lines, 500 Unicode/emoji dialogue lines and a long line
made from 500 repeated units. JSON includes actual source line/character counts (plus three
wrapper lines), seven settled-edit samples after setup/warmup and seven ten-update bursts.
It records complete source and rendered-layer checks, actual measurement sources/calls, timing,
allocations, native layer widths and corner dimensions, and diagnostics-only controls. Queue and
post-measurement scroll-extent restoration are included in total drain time, outside the nested
UpdateDocumentWidth timer. Independent phase medians must not be added as typing latency.

The paired coalescing baseline was captured at 14f688ce after mechanically extracting the exact
existing calculation into protected methods, before queueing/lifetime changes. Before and after
use the same observer, fixtures, warmups, samples, runtime and tiered-compilation setting. Every
report passes 190 invariants. See [SCRIPT_WIDTH_COALESCING.md](../../tests/SCRIPT_WIDTH_COALESCING.md).

The earlier reflection-wrapper/same-size omission experiment remains documented in
[SCRIPT_WIDTH_LAYOUT_INVESTIGATION.md](../../tests/SCRIPT_WIDTH_LAYOUT_INVESTIGATION.md); its
315-check reports are historical attribution data, not the current benchmark procedure.
No compiler, full TabView host, native keyboard/compositor, background work, game ROM or settings
is used. Edits are controlled model updates and allocations are temporary measured-thread bytes.
