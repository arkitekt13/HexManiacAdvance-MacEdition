# Combined tour-guide scripting

The group uses the production editor, controlled debounce, compiler, metadata, save/reopen
and Undo/Redo on fabricated bytes. It never opens a supplied ROM or app preferences.

**321 assertions** cover a script with two dialogue resources and four independent movement
resources: NPC 8, player 255, camera 127 and asynchronous NPC 9 followed by an explicit wait.
Across LF/CRLF/CR source, checks require exact opcode/actor/direction/terminator bytes,
independent pointer-source formats, six forced resource relocations with named anchors,
unchanged sentinels, late-error rollback, retained diagnostics/drafts, save protection,
correction/retry, navigation/stale callbacks and exact history/save/reopen restoration.
Additional checks protect reachable script sections following the main ending opcode and
idempotent formatting of a span containing inline resources.

Named anchors are compared as complete records in canonical name order because rollback can
change dictionary insertion order. All other metadata fields remain included; only the existing
out-of-history free-space search preference is excluded. This does not change production
serialization, history or matching behavior.

```sh
dotnet restore tests/TourGuideWorkflow/TourGuideWorkflow.csproj --ignore-failed-sources -p:NuGetAudit=false
dotnet build tests/TourGuideWorkflow/TourGuideWorkflow.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
dotnet tests/TourGuideWorkflow/bin/Debug/net8.0/TourGuideWorkflow.dll
```

The complete Mac runner discovers this group automatically. Optional `--probe-format` and
`--probe-invalid-movement` runs print observations, not regression assertion totals; see the
[investigation](../TOUR_GUIDE_SCRIPT_REVIEW.md) for the baseline failures and remaining input gap.

ScriptAssistance separately checks actual typing/keyboard/mouse command acceptance, including
the actor operand for move.npc/applymovement. ScriptMapReference exercises a movement-suggestion
click, pending-edit panning, unchanged pixels/model and Undo while the map pane is visible.
Native input, camera setup/teardown, collision, direction/timing and real tutorial gameplay
still require a native editor/emulator check. This is not an executable game demo.
