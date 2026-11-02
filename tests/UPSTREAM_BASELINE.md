# Upstream engine baseline — 2026-11-02

Infrastructure branch: `improve/upstream-test-harness`, based on `7faa0a43`.
Host: macOS ARM64, .NET SDK 8.0.300 / runtime 8.0.5, Debug configuration.
Engine: xUnit 2.4.1; existing assertion bodies and dependency versions retained.
Scope: `src/HexManiac.Tests` only; `src/HexManiac.Integration` remains unported.

Current status: all 32 originally failing behavior cases pass, and the absent-ROM no-data case
is an explicit skip; see [Metadata and optional-fixture follow-up](#metadata-and-optional-fixture-follow-up)
for the current zero-failure baseline and its remaining coverage limits.

## Observed results

Two complete in-process runs produced identical failure sets and totals:

| Discovery entries | Executed cases | Passed | Failed | Skipped | Engine errors |
| --- | --- | --- | --- | --- | --- |
| 2,570 | 1,523 | 1,490 | 33 | 1,047 | 0 |

The runner correctly exits nonzero. This is an established failing baseline, not a passing suite.
The 1,047 skips comprise 1,036 AutoSearch cases and 11 Upgrade cases without optional ROMs.
BasicLoad instead reports one failed no-data theory when its sample folder is absent; its original
behavior is deliberately retained for this checkpoint. No personal ROM was copied or loaded.

VSTest builds but cannot connect its test host: this session denies its localhost socket with
`SocketException (13): Permission denied`. The zero-discovery guard rejects the resulting zero
exit code. The explicit `--in-process` option uses xUnit's original discovery/execution engine,
retains standard xUnit XML and validated JSON, and does not require that socket. Normal VSTest
execution remains unverified in this environment. Neither package upgrades nor assertion changes
were used to obtain these results.

## Initial triage (source inspection, not completed fixes)

| Cases | Category | Evidence and next step |
| --- | --- | --- |
| 15 | Trainer/table dependency assumption | Null reference in `Flags.GetMinimumLevelForPokemon` while iterating an absent evolution table, reached through trainer text validation; determine the correct behavior for missing optional structures rather than hiding the failure with fabricated fixtures. |
| 6 | Save/close test-double contract | Save tests, two Diff cases, and one ChangeHistory case do not consistently configure successful metadata saves, while our save-safety fix now honors failure; inspect fixture setup and retain separate failure-path coverage. |
| 3 | Metadata/version behavior | Two migration expectations and one matched-word warning fail; investigate Core's platform-sensitive metadata version and controlled-dispatch behavior without changing migration semantics to match display versions. |
| 8 | Script text, length and caret behavior | Several failures show extra dialogue line breaks, incorrect compiled lengths, or brace/caret placement; investigate LF/CRLF assumptions and retain exact byte/pointer assertions. |
| 1 | Missing fixture policy | `BasicLoadTests.CanLoad` receives no data without sample ROMs; make the optional fixture policy explicit in a later test-quality checkpoint. |

These classifications are investigative leads. No application fixes or weaker assertions are
included. A build warning also identifies `NamedByteTests.Goto_ByteNamePartialText_ReturnsByteRunName`
as a public method lacking `[Fact]`; it is not part of the discovered baseline.

## Failed cases

- `BasicLoadTests.CanLoad`
- `ChangeHistoryTests.Edit_Close_Saved`
- `CodeToolTests.CommandWithPointerToValidData_ChangePointerToUnknown_CreateNewCopyOfExistingData`
- `CodeToolTests.PokeMart_FreespaceAddress_InsertReasonableScript`
- `CodeToolTests.ScriptWithTextAfterBranch_Decompile_IncludedInLength`
- `CodeToolTests.Script_AddOpenBrace_AutoAddCloseBrace`
- `CodeToolTests.TrainerScript_ManyAutos_AllTextPointersAreCorrect`
- `CodeToolTests.TrainerScript_MultipleFormatSections_Compiles`
- `DiffTests.TwoTabsSameModel_SaveAsTab_OtherTabClosed(tabToEdit: 0)`
- `DiffTests.TwoTabsSameModel_SaveAsTab_OtherTabClosed(tabToEdit: 1)`
- `ListTests.HashMatches_UpdateVersion_ListUpdates`
- `ScriptTests.EventScriptWithText_ExpandTextSoItMoves_ScriptPointerUpdates`
- `ScriptTests.ScriptWithPointerSlightlyAfterScript_NoopEdit_NoDataChange`
- `TableGroupTests.NoGroups_UpdateVersion_Groups`
- `TableTests.AppendViaTableToolUpdatesParent`
- `TableTests.CanContractTrainerTeamViaStream`
- `TableTests.CanExtendTrainerTeamViaStream`
- `TableTests.ChangePokemonCountChangesOtherTeamsWithSamePointer`
- `TableTests.ChangePokemonCountUpdatesTrainerTeam`
- `TableTests.ChangeStructTypeChangesOtherTeamsWithSamePointer`
- `TableTests.ChangeStructTypeChangesTrainerTeam`
- `TableTests.ChangingTypeFromMovesToMovesAndItemsKeepsSameMoves`
- `TableTests.HexEditingWorksForTrainerPokemon`
- `TableTests.SimplifyStructTypeClearsExtraUnusedData`
- `TableTests.TrainerPokemonTeamEnumSelectionSelectsEntireEnum`
- `TableTests.TrainerTeam_ExpandPokemon_NewPokemonHasValidValues`
- `TrainerTeamTests.TrainerWithNullPokemon_ChangeCountToOne_OnePokemon`
- `TrainerTeamTests.TrainerWithOnePokemon_ChangeCountToZero_NullPointer`
- `TrainerTeamTests.TrainerWithTwoPokemon_ChangePokemonCountToThree_PokemonTeamRepoints`
- `ViewPortSaveTests.CallingSaveMultipleTimesOnlySavesOnce`
- `ViewPortSaveTests.FileRaisesCloseEventIfSavedAfterEdit`
- `ViewPortSaveTests.ViewPortTakesNewNameOnSave`
- `ViewPortSaveTests.ViewPortWarnsIfLoadedMatchedWordValueDoesNotMatch`

## Evidence and next checkpoint

Each run retains `discovery.log`, `execution.log`, `results.xunit.xml`, and `summary.json` under
an ignored `artifacts/upstream-tests/run.*` directory. Run `scripts/verify-upstream.sh --in-process`
to restore/build/run again; use `--no-restore` once the chosen runner/configuration is restored.

First runs used `run.FKkzkw` and `run.z4Oy45`. Their case ordering differed, but the failed-case sets
were identical. Existing Mac console regressions are reported separately, never added to these
xUnit case totals. An existing FocusAnimation check failed once during broad verification and
passed its immediate isolated rerun; its clock-sensitive offscreen behavior warrants separate
investigation and was not changed to manufacture a passing result.

Next: review test doubles and missing-structure behavior, fix each confirmed issue in a separate
checkpoint, then port the second integration project. Script performance work is outside this
infrastructure checkpoint.

## Save fixture follow-up

The five affected test methods (six cases because the Diff test has two inputs) now explicitly
configure successful metadata writes for their successful-save scenarios. No shared stub defaults,
production code, existing assertions, case counts, or skips changed.

| Discovery entries | Executed cases | Passed | Failed | Skipped | Engine errors |
| --- | --- | --- | --- | --- | --- |
| 2,570 | 1,523 | 1,496 | 27 | 1,047 | 0 |

Resolved original cases:

- `ViewPortSaveTests.CallingSaveMultipleTimesOnlySavesOnce`
- `ViewPortSaveTests.FileRaisesCloseEventIfSavedAfterEdit`
- `ViewPortSaveTests.ViewPortTakesNewNameOnSave`
- `DiffTests.TwoTabsSameModel_SaveAsTab_OtherTabClosed(tabToEdit: 0)`
- `DiffTests.TwoTabsSameModel_SaveAsTab_OtherTabClosed(tabToEdit: 1)`
- `ChangeHistoryTests.Edit_Close_Saved`

The full Debug in-process run is retained in `artifacts/upstream-tests/run.hrvB0n`; comparison
against the original baseline confirms exactly these six failures disappeared and no new failures
appeared. The runner still correctly returns failure for the remaining 27 cases. The existing
SaveSafety regression group also passed all 17 assertions, including ROM/metadata failures,
successful retries, and retention of an unsaved document after close-save failure.

At this checkpoint the largest unresolved cluster was the 15 trainer/table null-reference cases;
the subsequent fix is recorded below.

## Trainer validation follow-up

The root cause was in production code: `Flags.GetMinimumLevelForPokemon` and
`Flags.GetPokemonDevolutions` enumerated a null evolution table. Trainer validation can run on
a partial model that has trainer/species data but no recognized evolution table. Both helpers
now return an empty hint dictionary when that table is absent, which also supports their cache
and map-template callers without inventing evolution restrictions.

The original trainer/table tests and their fixtures were not changed. Nine new cases exercise:

- Missing evolution data, with unchanged ROM bytes and metadata.
- Trainer validation with known species but no evolution table.
- Below/at/above minimum evolution levels with real synthetic evolution data.
- Inherited minimum-level warnings after a stone evolution.
- Non-level evolution arguments and multiple evolution slots.

Before the fix, the two new missing-table cases reproduced the exception and the seven positive
cases already passed (`artifacts/upstream-tests/run.dOyZsU`). After the fix, all nine pass and the
15 original trainer/table failures disappear (`artifacts/upstream-tests/run.xLzIyD`):

| Discovery entries | Executed cases | Passed | Failed | Skipped | Engine errors |
| --- | --- | --- | --- | --- | --- |
| 2,579 | 1,532 | 1,520 | 12 | 1,047 | 0 |

Failure-set comparison confirms no new failures. The remaining failures comprise eight script
cases, three metadata/version cases, and the missing-ROM-data theory. The runner still correctly
returns failure for those cases; no optional ROMs were supplied or loaded. Normal evolution
warnings remain enabled, and the existing valid-table derivation algorithms are unchanged.

## Script newline follow-up

The eight original script failures shared a production-code cause in `InsertMissingClosers`:
it inserted hard-coded CRLF separators and used two-character newline offsets even when the
input contained LF. This introduced empty dialogue lines, changed compiled text/stream lengths,
and could insert a closing brace inside the following `end` command.

Brace completion now recognizes CRLF, LF, and CR separators independently, leaves existing
separators intact, uses the first existing separator for new lines (or the host default when
none exists), and computes caret displacement from actual insertions/removals. Existing script
test assertions and fixtures are unchanged.

Nineteen new regression cases cover complete-block idempotence, mixed separators, missing
closers, opening braces at EOF, a lone brace, inline braces, caret placement, intentional blank
dialogue lines, exact encoded text lengths, compiled bytes and pointers, and undo/redo round trips.
The undo checks retain the current separate stream/format/code history entries rather than
claiming script compilation is one atomic undo operation.

All eight original failures and all nineteen new cases pass:

| Discovery entries | Executed cases | Passed | Failed | Skipped | Engine errors |
| --- | --- | --- | --- | --- | --- |
| 2,598 | 1,551 | 1,547 | 4 | 1,047 | 0 |

The full Debug in-process result is retained in `artifacts/upstream-tests/run.OaIigU`. Comparison
with the preceding trainer checkpoint confirms exactly the eight original script failures
disappeared and no new failures appeared. All fourteen Mac regression groups also passed with
1,102 assertions. Native typing/caret appearance has not been visually checked, and large-script
performance remains a separate investigation.

The four remaining failures are:

- `ViewPortSaveTests.ViewPortWarnsIfLoadedMatchedWordValueDoesNotMatch`
- `ListTests.HashMatches_UpdateVersion_ListUpdates`
- `TableGroupTests.NoGroups_UpdateVersion_Groups`
- `BasicLoadTests.CanLoad` (no optional ROM fixture data)

The upstream runner still correctly exits nonzero for these unresolved cases.

## Metadata and optional-fixture follow-up

Two migration failures were caused by the standalone Core build silently omitting
`SharedAssemblyInfo.cs`: its solution-relative include matched no source file in that build.
Consequently Core lacked its declared version attributes and reported 0.0.0. A project-relative
include restores the intended 0.6.1 identity; the version reader and migration rules are unchanged.
Three new tests verify the declaration, export/reload round trip, and older-version comparison.

The matched-word warning failure had a different cause in the ControlledDispatch test helper.
It used cancellation of an infinite Task.Delay to represent completed work, which allowed
initialization continuations to race the unchanged warning assertion. TaskCompletionSource now
represents successful completion, permits synchronous followups to drain, and faults/propagates
workload exceptions. Three new tests reproduced the old behavior and pass with the fix; the
production Mac dispatcher and warning-validation code are unchanged.

BasicLoad's no-data theory is now an explicit optional-fixture skip. Missing or empty folders
produce one sentinel case with a reason; supplied `.gba` paths are sorted and extension matching
is case-insensitive. Only that absent-fixture sentinel skips: missing specified files still fail.
Five new tests exercise discovery and this distinction using disposable paths, with no real ROMs.

Two complete Debug in-process runs produced the same results:

| Discovery entries | Executed cases | Passed | Failed | Skipped | Engine errors |
| --- | --- | --- | --- | --- | --- |
| 2,609 | 1,561 | 1,561 | 0 | 1,048 | 0 |

Evidence is retained under `artifacts/upstream-tests/run.YJ8JLd` and `run.xFz02J`. All eleven new
regression cases pass. The three original assertions pass unchanged; the fourth original failure
is reclassified as one explicit absence skip, rather than being claimed as executed successfully.
The other 1,047 skips still represent unavailable optional-ROM cases.

This green result applies only to `HexManiac.Tests` through the in-process runner. The separate
`HexManiac.Integration` project remains unported, VSTest's socket remains unavailable in this
session, and skipped cases do not establish real-ROM compatibility. The broad Mac regression
check again exposed the already recorded intermittent FocusAnimation clock assertion; its
isolated rerun and the full repeat passed without changing that test, with fourteen groups and
1,102 assertions successful on the full repeat.

## First-project reopening assertion follow-up

The ROM reopening test now compares sorted identity multisets for lists, matched words,
named anchors, and offset pointers before its original value comparisons. Previously,
lost entries or duplicated survivors could pass because only surviving entries were inspected.
The historical-version upgrade test retains its deliberate migration exemptions.

Seventeen synthetic cases exercise the same comparison without commercial ROMs. Before
strengthening it, eight missing/duplicate scenarios were falsely accepted; four extra-entry
scenarios failed through lookup exceptions. All seventeen now pass, including matching and
reordered collections. The full Debug in-process run (`artifacts/upstream-tests/run.Co2QcJ`)
recorded **2,626 cases: 1,578 passed, 0 failed, 1,048 skipped, 0 engine errors**.
Only test code changed; actual ROM reopening coverage still requires the optional fixtures.
The separate integration project remains deferred until this first-project review is complete.
