# Second upstream project baseline — 2026-11-02

Project: `src/HexManiac.Integration`; branch: `improve/upstream-test-harness`.
Host: macOS ARM64, .NET SDK 8.0.300 / runtime 8.0.5; Debug; xUnit 2.4.1.
No commercial ROMs were supplied, searched for, or copied.

## Portable host checkpoint

The project now targets .NET 8, references Core and the first test project through relative
paths, links the shared version and test-environment sources, and receives resource data
through portable content items from the first project. Windows copy/del/xcopy commands
were removed. The runner selects this assembly with `--integration`; the default still
runs only the first project's assembly. Package versions and original test bodies at this
checkpoint are unchanged. Restore is bounded and uses a single MSBuild node after the
parallel restore host stalled; local cached feeds completed successfully.

Before adding host resource checks, the original suite discovered **41 cases: 0 passed,
1 failed, 40 skipped, 0 engine errors** (`artifacts/upstream-tests/run.ZNHK5O`).
The failure is `DataTests.UseTypeSwapScript_UpgradeVersion_TableStillIncluded`, which reads
nonexistent `resources/scripts/ability_type_swaps.hma` before checking its ROM fixture.
The repository's current Pixilate script declares the expected `data.abilities.typeswaps` table.

Four new ROM-free checks verify nonempty script reference data, default metadata, and the
Fairy/Pixilate scripts in host output. With these checks the baseline is **45 cases:
4 passed, 1 failed, 40 skipped, 0 engine errors** (`artifacts/upstream-tests/run.qmzYAL`).
The harness correctly reports failure; this is not a green game-workflow baseline.

The two ignored dialogue-pointer offsets and Debug-only conflict assertions remain separate
follow-ups at this checkpoint. Actual map/script/image workflows require the optional ROMs.
