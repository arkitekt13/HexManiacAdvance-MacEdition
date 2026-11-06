# Movement source validation

This group uses the real FireRed movement catalog with the existing fabricated-ROM workflow
fixture. It never reads a supplied ROM, app preferences or emulator configuration.

**1,981 assertions** pass. Coverage includes:

- `move.player`, `move.camera`, `move.npc 8` and `applymovement 8`, with new `<auto>` resources
  and existing externally allocated resources, across LF/CRLF/CR source.
- Realistic typos and the literal `...` marker rejected with an error span on the exact row.
- Earlier dialogue growth rolled back with all bytes, complete canonical metadata records,
  relocation pointers, allocation state, byte highlighting and model history unchanged.
- Rejected source/diagnostics retained across navigation; Save, Save As and Close protection;
  correction/retry, forced movement relocation and exact one-step Undo/Redo restoration.
- Existing Redo preserved after rejection and explicit draft discard, plus atomic raw-parser
  and placeholder-allocation failures without speculative addresses or caller-token changes.
- All 166 distinct FireRed movement names compiled to their established byte IDs, including
  duplicate-name priority. Supported partial/quoted/case variants, tabs, comments and numeric
  forms retain existing path-specific parsing, byte wrapping and terminator handling.
- Ordinary dialogue containing those words/punctuation remains valid.

```sh
dotnet restore tests/MovementValidation/MovementValidation.csproj --ignore-failed-sources -p:NuGetAudit=false
dotnet build tests/MovementValidation/MovementValidation.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
dotnet tests/MovementValidation/bin/Debug/net8.0/MovementValidation.dll
```

The first unknown-row rejection assertion fails on the pre-validation code. The separate
`--compatibility-only` mode was run before implementation to establish valid-input behavior;
it is not the full safety group. Raw tests for negative/out-of-range numeric values protect
existing parser behavior, not the validity of those values for an actual game engine.

Validation is confined to movement blocks handled by ScriptParser. The shared table writer,
argument matcher, numeric grammar, allocator and history algorithms are unchanged. Existing
streams keep their table-writer syntax; deferred streams keep their enum syntax. This does not
promise broader mart/decor validation or change how arbitrary standalone table edits work.

AutocompleteMapping separately verifies that the visible ellipsis indicator is disabled,
guarded against programmatic Click and skipped by keyboard wrapping. ScriptAssistance uses
the real catalog and checks actual movement bytes after typing/keyboard/mouse completion.
The complete Mac runner automatically discovers this project. Native input and in-game
movement/camera timing remain manual/emulator checks.

Final verification: [66 groups / 20,676 assertions](mac-verification.json), zero failures or
skipped groups. The isolated [upstream first-project run](upstream-verification.json) passes
1,667 cases with zero failures, 1,048 absent optional-ROM skips and zero engine errors. It uses
a copied working runner host with `sampleFiles`/ROM files excluded. Existing NU1900 feed and
xUnit1013 analyzer warnings remain separate; no package/runtime upgrade is included.
