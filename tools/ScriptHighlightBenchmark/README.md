# Shared-Core highlighting benchmark

Run from the repository root:

```sh
dotnet run --project tools/ScriptHighlightBenchmark/ScriptHighlightBenchmark.csproj --configuration Release -- artifacts/script-performance/highlight-current.json
```

This exercises TextEditorViewModel with 256 keywords and 4,096 constants at three document
sizes. Each case uses two warmups and seven measured content changes, reporting median
elapsed time and thread-local allocated bytes. Six exact output-layer hashes allow output
comparison between revisions. Source text and layer lengths are checked on every size.
Use Release on the same runtime/machine and compare complete reports; timings are descriptive,
not pass/fail thresholds. No ROM, compiler, UI, model service, or network connection is used
by the executable. Normal project dependency restore may require NuGet access.

See tests/SCRIPT_PERFORMANCE.md for baseline/checkpoint evidence and coverage limits.
