# Future development

## HMA-001 — Resolve native `.hma` drag-and-drop import failure

**Status:** Deferred; still unresolved after the user's retry of the routing fix.

**Reported behavior:** A dropped script once opened as another tab; later attempts appeared to do
nothing, and the user reports it still does not work after commit `93eb5e13`.

**Completed partial work:** Map-tab drops now route to their owning ROM, local-file copy effects
are explicit, handled child events reach the window, invalid/busy targets have messages, and prior
edits/imports retain undo boundaries. The headless regression passes 24 checks. These checks do
not reproduce the user's remaining native Finder/script-specific failure.

**Next investigation:**

1. Obtain the actual script, expected patch result, and exact ROM revision; inspect its game/version
   guards and relative asset requirements without applying it to the user's working ROM.
2. Capture the native Finder payload and target tab, then confirm whether drag-over, drop, file
   loading, Editor.Open, and ViewPort.TryImport are each reached.
3. Trace script completion, update-in-progress state, parser errors, and intentional no-op paths
   on a disposable ROM copy; distinguish native delivery failure from script incompatibility.
4. Add a regression for the confirmed failure before changing the implementation.

**Completion criteria:** The user's reproduction reliably applies the intended edits or displays
an actionable error, never silently opens the script as a binary tab, and preserves unrelated
ROMs, source files, metadata, and existing undo history.

## HMA-002 — Show the copied map brush beneath the cursor

**Status:** Implemented and confirmed by the user in the native Mac editor.

**Requested behavior:** After selecting placed blocks, show a translucent image of that exact
pattern at the stamp destination, so copying trees does not leave only an empty outline.

**Existing support:** Shared Core already builds `MultiTileDrawRender` for the side-panel brush
preview. Windows and the Mac map view previously showed only a rectangle at the cursor.

**Implemented scope:** The Mac view reuses that image in a clipped, non-interactive overlay at
55% opacity, aligned with the stamp footprint and map zoom, behind arrows/tutorials and the
selection outline. Pointer movement and zoom reuse its bitmap. Active selection/painting,
cancellation, reveal-beneath mode, and leaving the map hide it; auto-tiling brushes retain their
outline. The expanded 340-check map selection group preserves stamping, collision bits,
elevations, metadata, saving/reloading, and undo/redo across saves. Shared Core editing
algorithms are unchanged.

## HMA-003 — Review sparse-alias partial-match exceptions

**Status:** Fixed separately from lookup reuse; 1,596 script argument/mutation safety checks pass.

**Original reproduction:** A sparse option list with high-ID aliases such as `temp1` at 0x4001
and `result` at 0x8000, plus ordinary named options, could throw for `$4001`. The original
ArrayRunEnumSegment.TryMatch included unnamed slots as numeric partial matches, then sorted
using the original null option strings; SystemExtensions.SkipCount dereferenced null.
ScriptArgumentSafety retains the reproduction and now requires no exceptions while cached
and uncached results agree.

**Implemented:** Unnamed decimal slot labels no longer intercept unquoted valid explicit-hex
input; actual named aliases retain their existing precedence before numeric fallback. Null
partial-match candidates sort by the same synthesized decimal names used during matching.
The fix covers both the default-list `$4001` crash and a silent `$123` -> decimal 123 error
in a minimal unnamed list, while preserving quoted numeric-name queries and real aliases.

**Scope boundary:** This is a separate sparse-name correctness repair, not part of caching;
decimal/hex fallback, pointer parsing, offsets, reservations, and flag/variable allocation
algorithms are unchanged. Native screen behavior is not the subject of this shared-Core fix.

## HMA-004 — Correct Mac script autocomplete caret mapping after the first line

**Status:** Fixed in the Avalonia overlay; 282 focused headless checks pass, with native
screen appearance still to be checked during normal use.

**Original reproduction:** With LF content `lock\nmsgb`, place the caret at the end (index 9). Core
GetTokenComplete at line 1, column 4 returns suggestions, but the overlay subtracts the previous
line's length plus 2, obtains column 3, and requests the wrong character position. Its own
first-line `msgb` and `setvar tem` controls show the expected eight/ten suggestions. The NPC
benchmark records all later-line mismatches separately rather than treating them as passes.

**Original source evidence:** Both TargetTextChanged and AutocompleteOptionChosen in Avalonia
Controls/AutocompleteOverlay.axaml.cs retained the Windows `length + 2` line traversal while
splitting on Environment.NewLine, which is LF on Mac. The regression now exercises both
querying and option application, as well as quote/space acceptance.

**Implemented:** A shared bounded line/span locator now drives querying, Enter/mouse acceptance
and quote/space handling. Acceptance preserves existing source separators and other lines,
and absent suggestions clear stale options. The new AutocompleteMapping group reproduces
the old failure and verifies LF/CRLF/mixed documents, selection/caret boundaries, actual
TextInput, keyboard/mouse acceptance, caller pointers and live CodeTool undo/redo. Matching,
numeric interpretation and shared-Core compilation are unchanged.

## HMA-005 — Harden unused-ID usage detection after evidence review

**Status:** Investigation and safety baseline completed; production changes await approval.

**Evidence:** [FLAG_USAGE_INVESTIGATION.md](tests/FLAG_USAGE_INVESTIGATION.md) cross-checks twelve
command handlers and byte mappings against FireRed/Emerald/Ruby engine references. Synthetic
cases reproduce missed additional variable operands, selected commands outside the scanner's
opcode list, and both conditional-header comparison fields across five game identities.
The supplied BPRE1 review finds one globally missing special-variable reference, with no
additional ordinary persistent ID missing within its bounded coverage; this is not a safety
guarantee for modified games or native-code usage.

**Preservation requirements:** Retain existing native reserves, command widths, literal versus
variable distinctions, flag/event/root coverage and read-only analysis. Do not treat every high
number as a variable or change researched addresses. FlagUsageSafety protects supported behavior;
the review tool reports omissions without requiring them to persist.

**Possible next scope, requiring approval:** Add only engine-confirmed missing operands/header
fields with failing-before/passing-after tests. Handle freshness, reservations, native-reserve
audits, legal ranges, custom engines and incomplete-scan reporting as separately reviewed work.

## HMA-006 — Prevent deferred script work from applying to a changed context

**Status:** Fixed with pending-work flushing, callback generations, retained rejected drafts
and explicit history boundaries; 563 workflow checks and all 27 Mac groups pass.

**Reproduction:** Queue an edit to script A, navigate to script B before the half-second compile
callback, and run the callback; A's pending source writes B's bytes. CodeBody reuse changes
Address while the closure retains A's source/run. Focus loss does not flush it in the attached
control probe. A pending compile can also reapply a new value after Undo and clear Redo.

**Implemented:** Finish pending work before body reuse or mode changes, guard posted callbacks
with generations, retain rejected drafts by their script/parser context, and settle work before
Undo/Redo. Undo can discard an active rejected draft without undoing unrelated compiled data.
New regressions cover original/destination bytes, pending-first-edit Undo, Redo, draft retention,
stale callbacks and mode switches; compiler/repointing algorithms are unchanged.

Evidence and limits: [SCRIPT_WORKFLOW_INVESTIGATION.md](tests/SCRIPT_WORKFLOW_INVESTIGATION.md).

## HMA-007 — Account for pending script edits during Save and Close

**Status:** Fixed with source-aware Save/Save As/Close barriers, unsaved indicators and
external-reload guards; 579 workflow checks and all 27 Mac groups pass.

**Reproduction:** Save As before debounce writes the older binary; ordinary Save does likewise
when an unrelated compiled change makes Save enabled. The later callback makes memory dirty.
A saved tab can close with no prompt and a compile callback still pending, losing the unsaved
draft from the file. Settled Save/reopen and settled Close pass the new safety baseline.

**Implemented:** Save and Save As settle source before writing; rejected drafts block persistence
with an explanatory error and remain available for correction or explicit Undo. Close settles
valid source before the existing save/discard/cancel prompt. Pending source enables Save, marks
the tab unsaved and prevents external reload, including after queued UI work. Existing ROM/
metadata failure protections remain passing; native full-window event ordering is not claimed.

Evidence and limits: [SCRIPT_WORKFLOW_INVESTIGATION.md](tests/SCRIPT_WORKFLOW_INVESTIGATION.md).

## HMA-008 — Review failed-compilation resource writes and preserve diagnostics

**Status:** Implemented with isolated parser/editor deltas, rollback on rejection and retained
source/diagnostics. Regression and headless TextEditor verification cover the original failures.

**Reproduction:** Editing an existing embedded dialogue stream writes/moves it before a later
invalid command returns a compile failure. Partial data can be saved. When relocation rewrites
the source pointer, CodeBody.Content clears the emitted error even while the invalid line
remains visible. Deferred auto streams and ordinary early errors are unchanged controls.

**Implemented:** Parser resource writes join the caller's token only on success. The editor
extends this boundary through placement, relocation/recompilation and formatting, and publishes
one history entry only after success. Failed attempts restore bytes, ROM length, metadata,
pointers and allocation state without clearing Redo. Rejected source keeps its original addresses
and diagnostics; correction/retry, successful relocation and HMA-006/007 remain protected.
Native full-window event ordering and game execution remain outside the synthetic checks.

Evidence and limits: [SCRIPT_WORKFLOW_INVESTIGATION.md](tests/SCRIPT_WORKFLOW_INVESTIGATION.md).

## HMA-009 — Dismiss stale autocomplete after source/context/visibility changes

**Status:** Fixed in the shared Mac overlay with focus/context/lifetime guards and 85 new
synthetic real-control regression checks; native OS event delivery remains manual coverage.

**Reproduction:** Open script suggestions, focus another control, then update the bound source.
The list closes on LostFocus but reopens while the editor is unfocused. Replacing the overlay
DataContext with an empty CodeBody retains old options. Hiding its parent leaves Popup.IsOpen
true and restoring it without typing retains the list; native Cocoa visibility is not proven.

**Implemented:** Suggestions require the current focused, enabled, effectively visible target
in the same attached, active window. Focus/context/target changes, hidden or disabled ancestors,
detachment and window deactivation discard options and close the popup. Target/ancestor/window
subscriptions are cleaned up and rebuilt for reuse; visibility/activation alone never restores
old options. Outside clicks dismiss without consuming the intended action, including controls
that keep the editor focused. Compiler/draft/history behavior is unchanged. Existing keyboard/
mouse acceptance is retained. The user's exact native trigger and non-overlay dropdowns remain unverified.

Evidence and scope: [SCRIPT_EDITOR_REVIEW.md](tests/SCRIPT_EDITOR_REVIEW.md).

## HMA-010 — Preserve movement permissions during mixing-bag flood fill

**Status:** Fixed in changelog entry 105, with automated synthetic regressions.

**Original reproduction:** Disable automatic movement selection, use CollisionIndex=-1 to preserve
permissions and a one-member block bag containing block 1. Start a stroke over a three-cell
connected region containing 4000. A normal first stamp correctly writes 4001; double-click fill
then produces 4001,FC01,FC01. The Mac Command-click bag controls work independently of this bug.

**Fix:** PaintBlockBag now resolves a negative preservation sentinel from the original region
value, matching ordinary PaintBlock. BlockBagFill covers preserve-mode, explicit-permission,
multi-member, region/boundary and neighboring-data behavior plus exact Undo/Redo/Save/reload.
The original shortcut/help investigation is retained as historical evidence.

Evidence and limits: [BLOCK_BAG_REVIEW.md](tests/BLOCK_BAG_REVIEW.md).

## HMA-011 — Reset map stroke deduplication across independent edits

**Status:** Fixed in changelog entry 110. Original behavior: Painting the same cell after Undo
or changing its permissions after Redo could be skipped by persistent coordinate suppression.

Implemented: Reset coordinate suppression on visible maps at the beginning of a fresh non-double
stroke, preserving lastDrawVal for subsequent fill and suppressing redundant movement within
the stroke. Initial coordinates are invalid so the first top-left draw is never skipped.
Regression evidence: [MapGestureLifecycle](tests/MapGestureLifecycle/README.md).
Evidence and regression plan: [MAP_TRANSITION_AUDIT.md](tests/MAP_TRANSITION_AUDIT.md).

## HMA-012 — Finish interrupted map/picker gestures and history boundaries

**Status:** Fixed in changelog entry 107, with enforced synthetic lifecycle regressions.
Original behavior: Explicit pointer capture loss could leave paint/pan
active without a pressed button and leave selection/picker state stale. Switching tabs during
a stroke can merge it with the following stroke into one Undo step.

Implemented: Idempotent completion for capture loss, detach, old-context replacement and explicit
map navigation, including normal-release reentrancy, exact endpoints, per-tool state and one
history completion. See [MapGestureLifecycle](tests/MapGestureLifecycle/README.md).
Evidence and regression plan: [MAP_TRANSITION_AUDIT.md](tests/MAP_TRANSITION_AUDIT.md).

## HMA-013 — End live map interactions when Escape clears the brush

**Status:** Fixed in changelog entry 108. Original behavior: Escape cleared the active brush,
but continued held pointer motion still changed map bytes because Cancel left the drawing
interaction active.

Implemented: Route Cancel through HMA-012 interaction completion before clearing brush fields.
The UI relinquishes capture for the owning operation; valid edits remain a single Undo step.
Enforced key/API cancellation scenarios cover painting, rectangles, events, picker, pan, selection
and pending object/wave templates in [MapGestureLifecycle](tests/MapGestureLifecycle/README.md).
Evidence and regression plan: [MAP_TRANSITION_AUDIT.md](tests/MAP_TRANSITION_AUDIT.md).

## HMA-014 — Clear transient reveal mode when map focus/context changes

**Status:** Fixed in changelog entry 111. Original behavior: Space reveal could remain on when
focus moved to a toolbar field or another tab before key release.

Implemented: Clear transient reveal/Command state on focus loss, detach, context replacement,
explicit navigation/cancellation and window deactivation; remove the window subscription on
detach. Ignore old-context property notifications and align the reveal transition to the new
context. Enforced headless focus/tab/context/navigation/release controls preserve native Space
text input, Command event visibility and bytes/metadata. Native window activation delivery and
compositor animation timing remain manual verification limits.
Evidence and regression plan: [MAP_TRANSITION_AUDIT.md](tests/MAP_TRANSITION_AUDIT.md).


## HMA-015 — Preserve movement permissions for a non-bag rectangle brush

**Status:** Fixed in changelog entry 109. Original behavior: Selecting single block 1, setting
CollisionIndex=-1, then Command-dragging a non-bag rectangle wrote FC01. The setter packed the
negative sentinel into tilesToDraw, which FillRect passed directly to RepeatBlocks.

Implemented: Single-cell rectangle brushes use RepeatBlock with the chosen art and separate
permission setting, preserving each destination's movement bits when negative. Bags use the
same existing path; copied multi-cell patterns keep their packed-source repetition semantics.
Regressions cover mixed 0/1/16/63 permissions, preserve/explicit settings, one/two-member bags,
rectangle growth/shrink, copied patterns, permission-only edits, exact bytes/metadata and
Save/reload/Undo/Redo. Evidence: [MapGestureLifecycle](tests/MapGestureLifecycle/README.md).
