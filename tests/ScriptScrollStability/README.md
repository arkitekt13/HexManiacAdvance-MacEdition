# Long script click/scroll stability

The default plain-text fixture preserves the command structure, blank lines and
dialogue line lengths of the reported two-section tutorial script. Dialogue is
replaced with neutral characters; the user's story text and ROM are not included.
An optional absolute plain-text path exercises the same checks with another script.
No ROM is loaded from that path and no script is compiled into a personal game.

Build this project and run its DLL, or use scripts/verify-mac.sh.

The regression uses the actual CodeToolMultiTextBox ScrollViewer and TextEditor
controls, plus a complete MainWindow/script tab backed by a synthetic ROM. Checks
cover unfocused/already-focused visible clicks, exact caret hit positions, two text
sections, LF/CRLF, two font/window sizes, manual scroll, drag selection, arrow-key
caret reveal, typing, unchanged pending source/debounce work and unchanged synthetic
ROM bytes/metadata/history. Future tests must retain keyboard caret reveal, rather
than suppressing every BringIntoView request.

On checkpoint 554b16db, the baseline fails immediately: a click on visible source
moves the outer scroll offset from 55 to 5. A separate exploratory test reproduces
long-box focus jumps, including 900/1500-position cases. Disabling only the outer
viewer's automatic focus reveal stabilizes initial clicks. The repair disables only that focus behavior; native caret requests
remain enabled. The final regression passes 195 assertions, including keyboard and
model-driven moves to both ends of the long source. The exact pasted script also
passes those 195 assertions through the optional local-file route; its story text
is not committed. [verification.json](verification.json) records the fresh 13-group /
3,266-assertion focused run and its validation boundaries.

Native trackpad interaction and real-ROM UI confirmation remain user checks.

## Delayed compilation after editing an earlier section

The follow-up camera-event fixture preserves the new report's command structure,
line counts and text widths with neutral dialogue and synthetic addresses. Its
entry section is later in ROM than the long destination section. The isolated
function-table name is compiled as a numeric placeholder; no gameplay is claimed.

On 0a8da532, first using the earlier editor's caret, scrolling to the later source,
and pressing Enter near its end reproduces the delayed jump. Refreshing the other
body assigns it back into Contents even when it is the same object, rebuilding
its TextEditor and restoring an offscreen caret. The source also passes through
an unnecessary intermediate empty string. The new regression fails with outer
offset 1198 → 100.8125.

The repair avoids the intermediate clear and replaces a collection item only
when its identity actually changes. Unchanged editors retain their controls and
caret; changed source and genuinely different bodies still update normally.
No BringIntoView request is suppressed, and the compiler transaction is unchanged.

The group now passes 245 assertions. Added coverage uses actual script font sizes
14/20, a remembered earlier caret, three repeated bottom-of-source Enter edits,
settled delayed compilation, preserved controls/source/focus/caret, unchanged ROM
bytes/structural metadata for blank lines, real dialogue changes and exact
Undo/Redo. The optional local-file route also passes 245 assertions with the
user's exact pasted source; its story is not committed. The shorter first section
is kept visible by clamping exploratory click offsets to its actual height.

Native confirmation used a disposable MainWindow and synthetic ROM with delayed
compilation: the old code jumped 1209 → 114.78125; the repaired code stayed at 1195
through compilation and retained focus in the later field. No personal ROM or
preferences were opened. The broader native trackpad/gameplay checks remain user
checks; see compile-refresh-verification.json for the focused regression result.
