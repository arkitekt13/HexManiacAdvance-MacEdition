# Script workflow safety baseline

**598 checks** pass; the complete Mac suite passes **27 groups / 6,112 assertions**, with zero
failed or skipped groups. The group includes the LF/CRLF baseline, pending-context/persistence
regressions and HMA-008 failure-atomicity checks. Successful editor compilations
now produce one history entry, so existing history traversal loops execute fewer assertions.

```sh
dotnet build tests/ScriptWorkflowSafety/ScriptWorkflowSafety.csproj --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 180 dotnet tests/ScriptWorkflowSafety/bin/Debug/net8.0/ScriptWorkflowSafety.dll
```

Restore dependencies first; scripts/verify-mac.sh automatically discovers the project.
The group exercises production CodeBody.Editor.Content, manual debounce, compiler, relocation,
history, save commands and atomic file writing on fabricated data only. No supplied ROM or
gameplay is involved.

Supported contracts protected here:

- Ordinary unknown commands, missing operands, incomplete hexadecimal numbers and bad aliases
  report errors without changing model bytes/serialized metadata; correcting source recovers.
- Eight pending valid edits coalesce, leave bytes unchanged before the callback, and apply
  the final value after settling.
- Blocked growth relocates the script and dialogue, retains both callers, preserves exact
  variable/opcode/value/dialogue bytes and leaves adjacent sentinels untouched.
- All undo records restore the original bytes and serialized metadata, and every redo record
  restores its captured state. The existing FreeSpaceSearch preference is excluded because
  it is maintained outside undo.
- Settled save/reopen retains all bytes, named anchors and caller pointer-source metadata;
  undo/redo across Save restores dirty/saved state, and a settled saved document closes cleanly.

The group explicitly navigates to the root anchor after complete undo and redo to inspect
the restored model. It does **not** claim automatic selection follows relocation history:
the initial stricter candidate assertion failed because the body stayed at its previous
address even though all bytes and metadata restored. That observation is retained in the
diagnostic review, rather than silently counted as a passing presentation test.

The original pending-navigation/undo/save/close and late-resource-error failures are characterized
by the separate [review tool](../../tools/ScriptWorkflowReview/README.md). The tool reports current
observations; it does not require defects to persist. No normal-suite ROM fixture is needed.

HMA-006 follow-up: pending edits settle at their original address before navigation/mode reuse;
stale posted callbacks are invalidated, rejected drafts survive navigation, and Undo/Redo settle
work before changing history. Public Undo is intentionally available for a pending first edit
while ROM history is still unchanged. The original navigation regression fails against the
investigation baseline. Save/Close and failed-resource compilation were handled in the follow-ups below.

HMA-007 follow-up: pending source enables Save and an unsaved marker; Save/Save As include the
settled edit, Close honors the normal prompt, rejected drafts retain their tab/source and block
persistence, and external reload does not replace pending source. The first new assertion
fails before the persistence fix. Resource mutation on compiler failure is covered below.

HMA-008 follow-up: late resource errors restore every ROM byte and serialized metadata entry,
including relocation pointers and ROM expansion. Rejected source/diagnostics survive navigation;
Save/Save As remain blocked. Correction/retry is one undoable operation, and failures preserve
existing Redo. Raw parser checks cover existing, placeholder and auto resources, exception
rollback and repeated successful edits merged into an existing caller token. A separate editor
case rejects placement after resources compiled successfully. The first new atomicity assertion
fails against untouched commit 3aa58ab7 in a temporary snapshot.
