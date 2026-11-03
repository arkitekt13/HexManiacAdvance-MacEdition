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
