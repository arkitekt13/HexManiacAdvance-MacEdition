# Script document-width attribution

From the repository root:

```sh
dotnet build tools/ScriptWidthBenchmark/ScriptWidthBenchmark.csproj -c Release -p:NuGetAudit=false
DOTNET_TieredCompilation=0 dotnet tools/ScriptWidthBenchmark/bin/Release/net8.0/ScriptWidthBenchmark.dll artifacts/script-performance/width-layout-current.json
```

The investigation uses the actual TextEditor in a headless Skia window after the comment-search
optimization. It wraps the existing constructor TextChanged delegate to time the complete width
handler without application instrumentation. Reflection requires exactly one matching instance
delegate and the probe checks that it actually runs. A second arm omits that delegate only from
disposable controls after warming the unmodified control; same-size edits keep geometry fixed.
This is a diagnostic experiment, not a safe general replacement for width measurement.
The attribution targets the synchronous handler at 26ec1e5c; update the probe if a future handler
only queues work, so it counts/times actual measurements rather than queue requests.

Five workloads run in both arms: 100/500/2,000 short lines, 500 Unicode/emoji dialogue lines,
and a single long line made from 500 repeated units. Reports include actual line/character
counts, seven measured edits after setup/warmup, complete samples, source/layer invariants,
handler calls/time/allocations, endpoint geometry, ten-edit queued bursts and diagnostics-only
controls. Timing the handler does not attribute all queued layout/render work to it.

No compiler, CodeBody/TabView host, game ROM, native keyboard/compositor or background worker
is used. Source edits are controlled model updates; native typing latency is not measured.
The tool changes neither production source nor controls outside its disposable windows.
See [SCRIPT_WIDTH_LAYOUT_INVESTIGATION.md](../../tests/SCRIPT_WIDTH_LAYOUT_INVESTIGATION.md).
