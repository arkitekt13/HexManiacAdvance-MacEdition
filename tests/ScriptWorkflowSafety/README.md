# Script workflow safety baseline

**548 checks** pass across LF and CRLF sessions. The full Mac runner passes **27 groups /
6,098 assertions**, with no groups skipped. Application code is unchanged.

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

Known pending-navigation/undo/save/close and late-resource-error failures are characterized
by the separate [review tool](../../tools/ScriptWorkflowReview/README.md). They are not passing
tests requiring defects to persist. Future approved repairs should add failing-before /
passing-after regressions while retaining this baseline. No normal-suite ROM fixture is needed.
