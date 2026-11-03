# Flag/variable usage safety baseline

The focused group passes **535 checks**; the complete Mac runner passes **26 groups / 5,550 assertions**.

Production scanners are unchanged. This group protects supported behavior before a possible
usage-detection repair. It uses fabricated event layouts and byte vectors; it never opens a ROM.

```sh
dotnet build tests/FlagUsageSafety/FlagUsageSafety.csproj --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 120 dotnet tests/FlagUsageSafety/bin/Debug/net8.0/FlagUsageSafety.dll
```

Restore dependencies first. The normal `scripts/verify-mac.sh` runner discovers this group.

Checks cover five game identities (BPRE, BPGE, BPEE, AXVE, AXPE), existing native-variable
reserve controls, twenty handcrafted command widths and expected operand-role results,
variable-looking literals, VarGet's literal/temporary/special boundaries, existing direct-operand
detection, set/clear/checkflag usage and locations, header types 2/4 target roots, recursive calls
and a cycle, isolated coordinate/signpost roots, object flags, coordinate triggers, hidden-item
offsets, and byte immutability around command scans.

The role table is test/review infrastructure, manually checked against primary engine handlers
and command tables. Its twelve-command coverage is deliberately bounded. It does not execute
the game, change flag/variable allocation, validate every custom engine, or establish complete
native usage. The native reserve assertions are representative preservation controls, not
proof that the current lists are complete; full current inventories are in the investigation.

Known missing operands/header fields are reported by the separate review tool. They are not
passing tests that lock those omissions into expected behavior. A future authorized repair
must add initially failing detection tests and retain the supported behavior checks here.

See [FLAG_USAGE_INVESTIGATION.md](../FLAG_USAGE_INVESTIGATION.md) for the real-ROM results,
engine semantics, current reserve inventories and limits.
