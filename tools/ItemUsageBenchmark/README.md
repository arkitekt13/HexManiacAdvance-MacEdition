# Delayed item map-usage search investigation

Run from the repository root with an explicitly supplied local ROM:

```sh
dotnet run --project tools/ItemUsageBenchmark/ItemUsageBenchmark.csproj -c Release -- artifacts/item-usage-performance/current.json 'sampleFiles/Pokemon FireRed.gba'
```

The ROM is read into an independent in-memory model. It is not modified or bundled, and
both file/model byte hashes, metadata, and empty history are checked after the investigation.
The completed fixture was the supplied FireRed 1.1 (BPRE1); this tool is not a claim of
cross-game or modified-ROM compatibility.

Both ViewPort and Singletons receive the same measured dispatcher. Its timers wrap the
actual Avalonia MacPlatform.DelayWorkTimer, retaining the half-second delay and cancellation.
The real TableTool schedules the real MapOptionsArrayElementViewModel.Load; an Avalonia UI
synchronization context is pumped until it completes. Callback samples separately record
scheduled-to-start delay, execution time, calling-thread allocation, and UI-thread identity.
Other dispatcher methods execute inline for controlled initialization/already-UI work; this
is not the complete native MacFileSystem dispatcher or a native window/input benchmark.

Items 13, 1, 50, and 0 exercise multiple, single, empty, and numerous match sets in this ROM.
Each has its first observed callback followed by three complete delayed repeats. Five-sample
component phases have two warmups and measure map/event enumeration, uncached script/hidden
item matching without previews, a query-local repeated-root reuse experiment, and preview
generation for the matched events. Times are per search, not per 100; allocation is cumulative
managed calling-thread bytes, not retained RAM/total native allocation. Component phases run
in a warmed model scope and cannot simply be added/subtracted to infer callback percentages.

The experiment is tool-only and shares only a boolean result for identical root script
addresses within one matching query, plus one opcode-filter buffer. It still calls the
original Flags.GetAllScriptSpots and never caches across queries or installs itself in the application. The later app
implementation and its separate actual-callback measurements are recorded in the report.
An independent reference matcher is compared with actual ordered map/event button results;
reflection reads the existing private button destination fields rather than adding app APIs.
Repeated actual callbacks must retain exact event order and thumbnail hashes. Raw experimental
matches must exactly match the reference, including hidden items and repeated event contexts.

Ten rapid selections must schedule only the final scan, with results for the final item.
The normal callback must start after the delay on the UI context, while the usage menu is
closed. This validates a controlled coalescing scenario, not cancellation of active native
editing or concurrent file reloads. Script mutation/repoint, malformed/cyclic fixtures,
other tables, and broader cancellation coverage are prerequisites for an application fix.

Success prints BENCHMARK_CHECKS_PASSED only after the JSON is saved and every assertion passes.
This standalone tool is not part of verify-mac. See tests/ITEM_USAGE_PERFORMANCE.md for the
results, source review, compatibility limits, and recommended implementation order.
