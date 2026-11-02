# Upstream engine baseline — 2026-11-02

Infrastructure branch: `improve/upstream-test-harness`, based on `7faa0a43`.
Host: macOS ARM64, .NET SDK 8.0.300 / runtime 8.0.5, Debug configuration.
Engine: xUnit 2.4.1; existing assertion bodies and dependency versions retained.
Scope: `src/HexManiac.Tests` only; `src/HexManiac.Integration` remains unported.

Current follow-up: the save fixture correction resolves six original failures; see
[Save fixture follow-up](#save-fixture-follow-up) for the updated counts and evidence.

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

The next largest unresolved cluster is the 15 trainer/table null-reference cases; the three
metadata/version cases, eight script cases, and one missing-ROM-data case also remain pending.
