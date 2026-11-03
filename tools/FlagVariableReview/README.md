# Flag/variable helper characterization

Read-only application review using fabricated map models and the production Flags scanners
and EventTemplate allocator. The tool writes only a JSON report; it never opens a supplied
ROM or changes application code. Setup writes and manual edits affect synthetic memory only.

```sh
dotnet build tools/FlagVariableReview/FlagVariableReview.csproj --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 120 dotnet tools/FlagVariableReview/bin/Debug/net8.0/FlagVariableReview.dll artifacts/script-safety/flag-variable-review.json
```

Restore dependencies first. This characterization is deliberately outside the regression
runner: shortcomings are reported as current observations, not assertions requiring future
versions to keep returning unsafe results. Parse/game/connected-script/header-root checks
ensure the fabricated setup actually reaches the production scan paths.

Cases cover a source-only copyvar operand, a manual flag edit after allocator initialization,
a header-condition variable with its script detached from ordinary events, exhausted normal
variable/flag ranges, and two session reservations without ROM writes. The report uses decimal
JSON numbers; the accompanying [review](../../tests/FLAG_VARIABLE_REVIEW.md) renders IDs in hex.

Source review additionally covers the reserved native-variable sets, traversal limits, map
roots, insertion controls/shortcut and allocator lifetime. No allocator repair, native UI
verification, arbitrary assembly disassembly or audit of an actual modified game is claimed.
