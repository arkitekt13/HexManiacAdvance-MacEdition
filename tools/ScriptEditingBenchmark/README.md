# Script editing pipeline investigation

This diagnostic executable changes no application code and never opens a supplied ROM.
It builds a fabricated BPRE-tagged model using the shipped script definitions, creates the
production CodeTool/CodeBody, and optionally attaches the production Avalonia TextEditor
to a headless Skia window. Restore cached dependencies before building.

```sh
dotnet build tools/ScriptEditingBenchmark/ScriptEditingBenchmark.csproj -c Release --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 600 dotnet tools/ScriptEditingBenchmark/bin/Release/net8.0/ScriptEditingBenchmark.dll artifacts/script-performance/edit-pipeline-baseline.json
python3 scripts/run-upstream-command.py 300 dotnet tools/ScriptEditingBenchmark/bin/Release/net8.0/ScriptEditingBenchmark.dll artifacts/script-performance/edit-pipeline-components.json --probe
python3 scripts/run-upstream-command.py 90 dotnet tools/ScriptEditingBenchmark/bin/Release/net8.0/ScriptEditingBenchmark.dll artifacts/script-performance/edit-pipeline-lookups.json --lookup
```

The baseline has 18 cases: 100/500/2,000 repeated statements, commands/comments/embedded
dialogue, with and without the text control. Each case has two warmups and seven measured
one-character immediate-argument changes. Dialogue uses fixed, separate synthetic text
addresses so repeated edits do not request fresh `<auto>` allocations. Source comments are
retained rather than silently lost through decompilation.

The dispatcher retains the production debounce API but queues its action for explicit
execution. This separates immediate content work, deferred compilation, and queued layout/
headless rendering; the half-second idle wait is excluded. Timer owners and execution times
are recorded to distinguish the CodeTool timer from other deferred editor work.

Every edit must reach CodeBody, defer ROM writes, compile without errors, retain its address,
and change exactly one expected argument byte. Source/layer hashes, all timing samples,
UI-thread allocated bytes, and runtime/platform identity are exported to JSON. The file is
checkpointed after each case; only `complete: true` establishes a finished report.

The component probe measures isolated compiler, script-length, script-collection, formatting,
and text-overflow operations at 500/2,000 statements. These are diagnostic calls, not additive
attribution of a complete update. The lookup probe compiles equivalent hexadecimal, decimal,
and named-alias operands at 500 statements; all three must produce identical bytes and leave
the model unchanged. Times are descriptive, with no hardware-dependent pass/fail threshold.

Limits: the UI path assigns TextBox.Text with a one-character-different string, rather than
injecting native keyboard events. It excludes TabView help/autocomplete/event bindings, the
native window/compositor, multiple connected script bodies, growing/shrinking scripts,
relocation/repointing, and real ROM models. The dialogue workload edits an argument in a
script containing dialogue, not the dialogue text itself. Allocation figures are temporary
managed allocations on the current thread, not retained RAM or native/whole-process memory.

Results and proposed follow-up are documented in tests/SCRIPT_EDIT_PIPELINE.md.
