# Script workflow investigation

Characterize production CodeTool/ScriptParser/ViewPort behavior using fabricated binary
models, manually controlled debounce callbacks and temporary ROM/metadata files. No supplied
ROM, native window, user settings, file watcher or game engine is loaded or changed.

```sh
dotnet build tools/ScriptWorkflowReview/ScriptWorkflowReview.csproj --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 180 dotnet tools/ScriptWorkflowReview/bin/Debug/net8.0/ScriptWorkflowReview.dll artifacts/script-safety/script-workflow-review.json
```

Restore dependencies first. The executable runs 23 probes and writes a separate JSON report;
it does not accept a ROM input. Temporary files are removed on fixture disposal. Save uses
production ViewPort commands and AtomicFileWriter through filesystem callbacks; native file
pickers, MacFileSystem retry policy and the full TabView shell are outside this tool.

Most probes change CodeBody.Editor.Content, the production editor's view model input. Two
additional probes attach the real Avalonia TextEditor in a headless Skia window, propagate
TextBox edits, pump layout/bindings and corroborate navigation after actual focus loss and
diagnostic loss after a late error. This is not native mouse/keyboard timing or gameplay testing.

Callback execution is deterministic and synchronous. It retains scheduling/coalescing but
does not wait 500 ms, model thread races or assert performance. Reports contain observations
and unexpected exceptions rather than a passing assertion count; existing failures are not
regression requirements. Compare reports and inspect exception fields before using evidence.

[WorkflowFixture.cs](WorkflowFixture.cs) is shared with the separate
[supported-workflow safety group](../../tests/ScriptWorkflowSafety/README.md).
See [SCRIPT_WORKFLOW_INVESTIGATION.md](../../tests/SCRIPT_WORKFLOW_INVESTIGATION.md) for findings,
source locations, reproduction boundaries and proposed work. Production code is unchanged.
