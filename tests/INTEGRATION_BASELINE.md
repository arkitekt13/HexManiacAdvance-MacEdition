# Second upstream project baseline — 2026-11-02

Project: `src/HexManiac.Integration`; branch: `improve/upstream-test-harness`.
Host: macOS ARM64, .NET SDK 8.0.300 / runtime 8.0.5; Debug; xUnit 2.4.1.
No commercial ROMs were supplied, searched for, or copied.

Current status: **67 cases: 26 passed, 0 failed, 41 skipped, 0 engine errors**, verified in
Debug and Release. The 26 passes are new host/synthetic checks; all 41 original ROM cases
remain skipped, so this is not evidence that the game workflows have passed.

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

## Current type-swap fixture reference

The stale `ability_type_swaps.hma` reference now names the current checked-in
`Scripts/Add Mechanics From Later Generations/AnyGame_PixilateStyleAbilities.hma` script.
That script explicitly creates the same `data.abilities.typeswaps` table asserted by the
original test. A shared path constant and a ROM-free check verify the required table declaration;
the script itself, import behavior, and final table assertion are unchanged. It is read before
ROM loading as before, so a missing script still fails rather than being hidden by a ROM skip.

Debug run `artifacts/upstream-tests/run.EHaCxt`: **46 cases: 5 passed, 0 failed, 41 skipped,
0 engine errors**. The former missing-script failure now reaches the absent-ROM guard.
This does not prove that importing the script or migrating its table works on a real ROM.

## Dialogue-pointer test correction

Both FireRed/Emerald overflow scans now share a reader using the offsets their original
switch already calculated: loadpointer (0x0F) stores a bank byte before its pointer (+2),
whereas preparemsg (0x67) stores its pointer immediately after the opcode (+1).
These layouts match existing `scriptReference.txt` lines 127 and 292; definitions are unchanged.
Previously both scans ignored the calculated offset and always read +2, so preparemsg
could produce an invalid address and escape the existing range guard without checking text.

Seven ROM-free cases cover both opcodes at two locations, null pointers, unsupported commands,
and unchanged source bytes. Before correction both non-null preparemsg cases failed while
five other cases passed; afterward all seven passed. Debug run `artifacts/upstream-tests/run.LDHMpz`
records **53 cases: 12 passed, 0 failed, 41 skipped, 0 engine errors**. The original range
and buffer exclusions and overflow assertions remain intact. Actual ROM dialogue coverage
still requires fixtures; no game definitions or application parsing code changed.

## Build-independent model consistency assertions

`IntegrationTests.AssertNoConflicts` now invokes a test-only counterpart of
`PokemonModel.ResolveConflicts` using public inspection APIs under the model's run lock.
The original diagnostic's pointer backlinks, table/inner-element sources, overlapping runs,
and matched/parent-dependent lengths become xUnit assertions in Debug and Release.
Its allowances for out-of-range and certain interior ROM pointers are retained. The
production diagnostic and all application/model/reference code are unchanged. Keep the
test-only rules aligned with that diagnostic if upstream changes them in future.

Before the fix, a Release run with the first seven regression cases returned no assertion
for deliberately stale pointer metadata or overlapping runs: **60 cases: 17 passed, 2 failed,
41 skipped, 0 engine errors** (`artifacts/upstream-tests/run.eg2SR2`). This demonstrates
that the old helper did not validate these conditions in Release.

Fourteen final ROM-free cases cover empty and consistent models, unchanged bytes/runs,
corrupted pointer/table/inner-element backlinks, overlap detection, parent-dependent lengths,
matched-word consistency without repair, and three original out-of-range-pointer exemptions.
All pass in both configurations:

| Configuration | Cases | Passed | Failed | Skipped | Engine errors | Evidence |
| --- | --- | --- | --- | --- | --- | --- |
| Debug | 67 | 26 | 0 | 41 | 0 | `artifacts/upstream-tests/run.9rmBl7` |
| Release | 67 | 26 | 0 | 41 | 0 | `artifacts/upstream-tests/run.HQ2wDZ` |

A cross-check of the default first-project selector initially exposed an empty-array/nounset
error in macOS Bash. The invocation now always includes its result-directory argument;
afterward the first project retained **2,632 cases: 1,584 passed, 0 failed, 1,048 skipped,
0 engine errors** (`artifacts/upstream-tests/run.xBVE8W`). Integration selection was rerun
successfully after the shell fix. Both selectors retain strict result validation.

Remaining limits: VSTest/native UI and real ROM workflows remain unverified here; the Mac
regression suite was not rerun because this work changes only upstream tests and their host.
This work does not address application performance or the first project's disabled features.
