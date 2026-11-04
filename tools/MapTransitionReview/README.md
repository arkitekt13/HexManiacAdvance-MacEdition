# Map transition audit

Mac-only synthetic real-control investigation, not a passing regression group. It changes no
production implementation and uses no user ROM. Build/run from the repository root:

```sh
dotnet build tools/MapTransitionReview/MapTransitionReview.csproj -p:NuGetAudit=false
for scenario in undo redo paint-capture pan-capture select-capture picker-capture background-capture space-focus space-tab tab-stroke map-stroke escape-stroke completed-pan completed-select; do
  dotnet tools/MapTransitionReview/bin/Debug/net8.0/MapTransitionReview.dll "$scenario" || break
done
```

Each scenario uses a separate process, fabricated map and real MainWindow/MapTab. Explicit
framework capture loss verifies the capture owner first; it does not emulate every native
reason for losing capture. Independent clicks are separated by 600 ms and asserted to be single
clicks. Focus probes assert focus actually moved. Setup/positive-control failures throw.

`OBSERVATION:...:FAIL` records a reproduced unmet invariant. Exit zero means the investigation
completed, **not** that these failures passed. `POSITIVE_CONTROLS` excludes all observations.
Do not add this executable to the automatic regression runner unchanged. Convert each confirmed
failure into a failing regression when implementing its fix.

Findings, counts, limits and implementation priorities: [MAP_TRANSITION_AUDIT.md](../../tests/MAP_TRANSITION_AUDIT.md).
