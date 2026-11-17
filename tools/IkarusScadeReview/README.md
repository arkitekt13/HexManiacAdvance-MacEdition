# SCADE / Ikarus collision review

Read-only investigation of the exact bundled pair; this is not a compatibility patch builder.
Requires an exact clean English FireRed 1.0 ROM accepted by the SCADE manifest.

```sh
dotnet run --project tools/IkarusScadeReview -p:NuGetAudit=false -- /absolute/repository /absolute/clean-base.gba /absolute/results.json
```

The driver creates SCADE in memory, independently applies all IPS literal/RLE records,
compares that output with HMA's IPS decoder, and inventories writes against the fresh
SCADE model's live overworld frame ranges, named tables and blockset headers.
It writes only the requested JSON report, verifies the base file remains unchanged,
and does not open or save a user project, change settings, or produce a combined ROM.

Frame ranges are deduplicated by `(address, length)` and can share owners. Counts
are ranges, not Pokémon species. The metadata-driven inventory is a lower bound
on meaningful conflicts; unformatted native routines/data need separate investigation.
Raw pointer-looking sequences are candidates, not an exhaustive relocation list.
All-FF spans are observations, not reservations or proof of safe allocation.

See [the investigation and plan](../../docs/IKARUS_SCADE_COMPATIBILITY_PLAN.md).
