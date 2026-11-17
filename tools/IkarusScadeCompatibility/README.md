# Experimental SCADE / Ikarus compatibility builder

Standalone development tool. It does not change or enable any HMA utility.
Supports only the bundled SCADE 2026-11-15 and reviewed Ikarus FR V3.2 hashes.

```sh
dotnet run --project tools/IkarusScadeCompatibility -- /absolute/repository /absolute/unused-output.noindex /absolute/clean-FireRed-1.0.gba
```

The tool creates fresh SCADE and a vanilla/Ikarus donor in memory, independently
checks IPS decoding, rejects every three-way conflict outside the relocated region,
repoints typed blockset fields and preserves every byte where SCADE differs from vanilla.
It writes disposable ROMs only to the new output folder. Do not distribute those ROMs.
The `files` subfolder contains an exact-source BPS and matching TOML for evaluation.
All original metadata definitions/lists remain; relocation anchors and reservations
are appended. Raw block anchors follow the current HMA convention so full new-map
palettes remain accessible. Allocator probes verify the relocated arena is not reused.

The BPS requires fresh SCADE output, not vanilla or an already Ikarus-patched project.
Runtime compatibility remains experimental until the manual gameplay checklist passes.
See the investigation plan and test report. Originals and installed apps are never edited.
