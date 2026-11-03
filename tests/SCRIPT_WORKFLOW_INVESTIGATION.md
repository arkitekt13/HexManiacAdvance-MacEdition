# Script editing, recovery and persistence investigation

Date: 2026-11-03. Baseline: `8147279b`; branch: `investigate/script-workflow-recovery`.
Scope: investigation, tests and tooling only. No production source, engine behavior,
researched addresses, flag/variable helpers or supplied ROM is changed.

Follow-up: HMA-006 and HMA-007 are fixed on the implementation branch; the observations below remain the
original investigation evidence. See ScriptWorkflowSafety/README.md and changelog entries 066–067.

## Outcome

The settled successful workflow is protected by 548 checks across LF/CRLF. All 27 Mac test
groups pass 6,098 assertions with no skipped groups. Separate diagnostic probes reproduce
three correctness priorities; they are not performance measurements or fixes.

| Priority | Reproduction | Observation |
| --- | --- | --- |
| 1: pending context | Edit script A, navigate to script B before debounce, then execute the pending callback. | A remains at value 1; B changes from 7 to A's pending value 2. |
| 1: pending history | Compile value 2, queue value 3, Undo before debounce, then execute the callback. | Undo restores 1 and makes Redo available; the pending callback writes 3 and clears Redo. |
| 2: persistence boundary | Save/Save As or close before debounce finishes. | Save can write the older binary snapshot; a saved document can close without prompting while work remains queued. |
| 3: late compile failure | Change embedded dialogue, then introduce a later invalid command. | The parser returns failure after resource writes; relocation can also clear visible diagnostics during source replacement. |

Two complete runs of all 23 diagnostic probes produce identical JSON observations, with
no unexpected exceptions. Two probes corroborate findings through the real Avalonia TextEditor
in a headless Skia window. This is not a native/full-window reproduction or proof of every
map/NPC navigation path; it demonstrates the shared script editor paths and affected data.

## 1. Deferred compilation can outlive its script context

CodeTool.ScriptChanged captures source text and an existing run, but its delayed callback
later passes the mutable CodeBody into CompileScriptChanges. UpdateContents reuses that body
for another address. CompileScriptChanges then reads body.Address/body.CompiledLength at
execution time. There is no captured-context validity check at that boundary.

The fixture has two independent scripts: root 0x1000 with value 1 and other 0x1800 with value 7.
Queue root source with value 2, navigate to 0x1800, then flush. The same body is reused, and
the other script's bytes change to 2 while root remains 1. The attached TextEditor probe
confirms the edit reaches the view model, navigation focus succeeds, the text box loses focus,
one callback remains queued, and the same wrong-address write occurs.

The related Undo probe confirms that restoring model history does not invalidate the queued
compile: its later write creates new history and removes the prior Redo branch.

Source: CodeTool.cs lines 263–275 (body reuse), 328–366 (callback/scheduling), and
439–454 (mutable body address and immediate compilation). Git blame traces the callback/debounce
code to upstream-history commit `e4473d1a4` (2025-12-01), before our recent optimizations.
That provenance is not an execution comparison against the latest Windows release.

**Proposed boundary, not implemented:** Associate pending work with stable script/model/parser
identity and an edit generation; define whether navigation settles work or preserves a draft.
Undo/redo and mode/context changes must not let old work apply later. Simply canceling a timer
without retaining the user's edit would trade one data-loss problem for another.

## 2. Save and Close see compiled history rather than pending source

On a saved model with a queued source edit, Save.CanExecute is false. Save As is enabled and
writes the pre-edit binary/metadata; after callback execution the model changes and is dirty.
With an unrelated existing dirty byte, ordinary Save is enabled and likewise writes the old
script immediate (1), followed later by the callback changing memory to 2. The later dirty tag
does acknowledge the new compiled change; this is not a claim that ordinary settled Save loses data.

Closing the initially saved document during that interval raises Closed with no save prompt,
reference count 0 and a callback still queued. Flushing it afterwards changes the closed model;
the disk snapshot remains unchanged. No supplied game file is involved in these probes.

Source: ViewPort.cs lines 654–684 (Save/Save As), 707–724 (Close), and 1254–1264 (command
eligibility). The EditorViewModel command wrapper delegates to these commands and manages
listeners; it does not establish a pending-script barrier in the reviewed path.

**Proposed boundary, not implemented:** Define and test a pending-source state and a compile/
validation barrier for save/close. Decide explicitly how invalid drafts are retained or warned
about, how Cancel works, and how a failed compile affects closing. Do not merely force a file
write before compilation or suppress dirty state.

## 3. Resource writes can precede a later compile error

ScriptParser.Compile updates an existing stream through DeserializeRun while processing its
braced content. Only afterwards does it parse subsequent commands. A later error returns null
without undoing the earlier stream writes. A raw-parser probe confirms this independent of
the script editor's delayed callback.

- Same-size Hello! → Howdy! changes dialogue bytes despite a later rejected command; the
  error remains visible and those changed bytes can be saved.
- Longer dialogue crosses a blocked neighbor and relocates 0x3000 to 0x180 in the fixture.
  The root's text pointer and metadata change even though a later command fails.
- Both an error before the release/end terminator and one after it reproduce the issue.
- When the moved stream rewrites the source pointer, CodeTool assigns the changed source back
  to CodeBody.Content, whose setter clears errors. The real TextEditor probe records an emitted
  compile error, then HasError=false and zero error locations, while the invalid line stays visible.
- Legacy `<??????>` allocation also writes before a subsequent failure. In contrast, the
  tested deferred `<auto>` stream remains unchanged when its later command fails.
- Re-entering the original fixed-address source after a rejected relocated-dialogue edit can
  compile successfully without restoring the complete original bytes/metadata. Correction is
  not equivalent to rollback. Exact Undo must be considered separately.

Source: ScriptParser.cs lines 519–677, particularly stream DeserializeRun and eager placeholder
allocation before command errors; CodeTool.cs lines 452–459 and 343–347; CodeBody.cs lines 396–413.

**Proposed boundary, not implemented:** Make the failed-edit resource/metadata/history boundary
explicit. Preserve existing automatic repointing and successful stream edits, and retain
diagnostics for rejected source. A transactional/staged approach needs focused safety tests;
blocking Save alone would not undo the partial in-memory writes.

## Supported behavior and secondary observations

The [safety group](ScriptWorkflowSafety/README.md) verifies ordinary error rejection and
correction, rapid-edit coalescing, blocked script and dialogue relocation, two caller pointers,
neighboring data, exact complete history, temporary-file save/reopen and saved-tag behavior.

The initial proposed presentation assertion failed after Undo: binary data/serialized metadata
restore, but the body remains at 0x280 while the root anchor returns to 0x1000. Explicit navigation
refreshes the body; after Redo, selection can again stay at the prior address. The passing binary
safety checks explicitly reselect the root after full history traversal. Presentation behavior
is recorded separately, not hidden by weakening a claimed UI guarantee.

Unresolved local labels intentionally receive an ending stub through LabelLibrary.
ResolveUnresolvedLabels; missing label source is therefore not automatically a syntax error.
The unfinished-dialogue probes show InsertMissingClosers creating an empty block before the
following text, which can then be rejected as a command. This involves the helper's intended
"close before following command" rule; the probes do not establish how every native paste,
brace deletion or incremental typing sequence should behave. Neither behavior is changed.

## Reproduction and limits

Run [ScriptWorkflowReview](../tools/ScriptWorkflowReview/README.md) after building; JSON evidence
is at artifacts/script-safety/script-workflow-review.json and the repeated report beside it.
Diagnostic probes report observations; they do not assert that defects must remain.

Every binary, address, NPC-style conversation and metadata file is fabricated. Disk tests use
ViewPort commands with filesystem callbacks invoking production AtomicFileWriter on disposable
temporary files. Native picker/retry/watcher behavior, full MainWindow/TabView bindings, actual
ROM scripts, engine execution and arbitrary pointer graphs are outside this review.
Manual scheduling controls event order without a real 500 ms wait or thread-race simulation.
FreeSpaceSearch is the existing non-undoable preference excluded from serialized history
comparisons; caller pointers, anchors, resource formats and all binary bytes remain checked.

No speedup, retained-memory reduction, fixed defect or native visual improvement is claimed.
Future repairs should be separate, reversible changes with failing-before/passing-after tests.
