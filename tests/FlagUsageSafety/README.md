# Flag/variable usage safety

The focused group passes **1,098 assertions** on synthetic models; no user ROM is opened.
The original 535-check baseline and failing-before evidence are preserved in the investigation.

```sh
dotnet build tests/FlagUsageSafety/FlagUsageSafety.csproj --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 120 dotnet tests/FlagUsageSafety/bin/Debug/net8.0/FlagUsageSafety.dll
```

The complete Mac runner discovers this group. Coverage spans BPRE, BPGE, BPEE, AXVE and AXPE:
twenty command vectors with engine-confirmed operand roles/widths, used-ID discovery and precise
Find Uses locations; literal and function-ID exclusions; VarGet boundary cases; conditional
header types 2/4 with either comparison operand; sentinel and incomplete-header handling;
recursive calls/cycles; raw operands hidden within a real find.item macro; coordinate/signpost
roots; object/hidden-item flags; native reserve controls; and unchanged bytes/metadata/history.
The original copyvar source-only case also verifies that new suggestions skip that used ID
while retaining distinct session reservations.

The implementation covers twelve reviewed command opcodes and conditional-header fields.
It does not change ID allocation ranges, native reserves, compiler/numeric rules or game-engine
behavior, and it does not establish complete native/custom-engine usage or fix snapshot freshness.
Do not equate absence from this bounded scan with a proof that an ID is globally safe.

See [FLAG_USAGE_INVESTIGATION.md](../FLAG_USAGE_INVESTIGATION.md) for the original engine review,
and [MAINTENANCE_REVIEW.md](../MAINTENANCE_REVIEW.md) for fresh before/after evidence.
