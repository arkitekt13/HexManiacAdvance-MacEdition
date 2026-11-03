# Flag/variable helper characterization

Read-only application review using fabricated map models and the production Flags scanners
and EventTemplate allocator. The tool writes only a JSON report; it never opens a supplied
ROM in its original mode or changes application code. Setup writes and manual edits affect synthetic memory only.

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

## Usage-detection investigation

The optional `--usage` mode adds twenty handcrafted command cases across five game identities,
two detached conditional header types, and a read-only supplied BPRE1 ROM review:

```sh
python3 scripts/run-upstream-command.py 240 dotnet tools/FlagVariableReview/bin/Debug/net8.0/FlagVariableReview.dll --usage artifacts/script-safety/usage-detection-review.json 'sampleFiles/Pokemon FireRed.gba'
```

Omit the ROM argument for synthetic cases only. The real-ROM mode reads the file into memory,
uses existing model initialization, performs scans without saving, and verifies both model-byte
and disk-file immutability. It writes a separate `.json` report and never loads/saves sidecar metadata.
The initial loader can build metadata; model-byte immutability is checked after initialization.

The bounded reference collector checks twelve engine-reviewed opcode roles and both variable-or-value
fields in conditional map headers. It shares HMA's decoder and root discovery, records traversal
limits/unknown commands, and does not establish complete usage or native-code safety. References
missed at one site can still be detected elsewhere, so missing global IDs and individual operand
coverage must be distinguished. Source snapshot URLs/hashes are in `primary-source-manifest.json`.

See [the investigation](../../tests/FLAG_USAGE_INVESTIGATION.md) for results and approval boundaries,
and [the safety group](../../tests/FlagUsageSafety/README.md) for supported behavior tests.
