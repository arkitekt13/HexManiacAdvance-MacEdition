# Script editing pipeline investigation

This diagnostic executable changes no application code and never opens a supplied ROM.
It builds a fabricated BPRE-tagged model using the shipped script definitions, creates the
production CodeTool/CodeBody, and optionally attaches the production Avalonia TextEditor
to a headless Skia window. Restore cached dependencies before building.

```sh
dotnet build tools/ScriptEditingBenchmark/ScriptEditingBenchmark.csproj -c Release --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 600 dotnet tools/ScriptEditingBenchmark/bin/Release/net8.0/ScriptEditingBenchmark.dll artifacts/script-performance/edit-pipeline-current.json
python3 scripts/run-upstream-command.py 300 dotnet tools/ScriptEditingBenchmark/bin/Release/net8.0/ScriptEditingBenchmark.dll artifacts/script-performance/edit-pipeline-components-current.json --probe
python3 scripts/run-upstream-command.py 90 dotnet tools/ScriptEditingBenchmark/bin/Release/net8.0/ScriptEditingBenchmark.dll artifacts/script-performance/edit-pipeline-lookups-current.json --lookup
```

The baseline has 18 cases: 100/500/2,000 repeated statements, commands/comments/embedded
dialogue, with and without the text control. Each case has two warmups and seven measured
one-character immediate-argument changes. Dialogue uses fixed, separate synthetic text
addresses so repeated edits do not request fresh `<auto>` allocations. Source comments are
retained rather than silently lost through decompilation.

Set HMA_BENCHMARK_REVISION to the tested commit/checkpoint when running; the report otherwise
explicitly labels itself as a working-tree run. Keep the original baseline reports rather
than overwriting them when measuring a new revision.

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

## NPC-style source documents (100–500 actual lines)

```sh
python3 scripts/run-upstream-command.py 180 dotnet tools/ScriptEditingBenchmark/bin/Release/net8.0/ScriptEditingBenchmark.dll artifacts/script-performance/npc-realistic-current.json --npc
```

This mode uses 100, 250, and 500 actual source lines including comments, braces and dialogue,
not 100–500 repeated commands with extra source lines. Its compound NPC conversation contains
conditional calls to a shared reaction routine, varied variables/flags, and two-line dialogue
streams at separate fabricated addresses. It is a representative synthetic workload rather
than an actual user's script. Each size is measured with and without the production TextEditor,
editing a numeric argument, one PCS dialogue character, or a comment digit: 18 cases total.
Ten warmups precede seven measured edits in this mode; the older modes retain two warmups.

Any initial consolidation/relocation of the multi-section source occurs during setup. The
measured edits must retain the settled script address and change exactly the expected ROM
byte; comment edits must change none. Source line counts and paired UI/Core layer/source
hashes make fixture-size and content differences visible. The existing compilation cache is
used unchanged; no editor behavior is modified by adding these workloads.

Autocomplete is measured separately in an independent CodeBody without a CodeTool compile
subscription, using `msgb` and `setvar tem` at the first line, second line, middle and end.
The core query must offer suggestions without ROM writes. For attached cases the real
AutocompleteOverlay receives a routed TextChanged notification at the prepared caret; its
immediate/post-layout options are recorded against core results. First-line positive controls
must agree. Later-line mismatches are reported as characterization findings, not counted as
successful autocomplete behavior or silently filtered out. This is not a native keyboard or
full TabView test, and applying a suggestion is outside the measured workload.

See tests/NPC_SCRIPT_BENCHMARK.md for repeated measurements and the separately tracked Mac
autocomplete mapping finding.
