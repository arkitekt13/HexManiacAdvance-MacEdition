# Map gesture completion

Run through `scripts/verify-mac.sh --no-restore`, or build this project and run its DLL.
The parent runs isolated headless child scenarios and reports one combined CHECKS_PASSED count;
any failed child, setup assertion or behavioral invariant fails the group.

Fabricated ROMs drive actual MainWindow/MapTab pointer/focus/capture routing. Coverage includes
map/background pan, paint, Control selection and block-picker capture loss; interrupted tab,
map and independent-document context changes; exact final selection endpoints without a Move;
unrelated physical-button release; event dragging; pending object/wave templates; rectangles
with explicit permissions; and multi-member random bags. Normal completed pan/selection still
work after early modifier release. Capture owners and independent single-click counts are
asserted. All scenarios compare metadata, except alias navigation which checks its separate
post-navigation baseline rather than claiming initial metadata equality.

History checks preserve all unrelated bytes, exact random outcomes and separate Undo entries.
The paint case saves through StubFileSystem, independently reloads bytes/metadata, then checks
saved-state transitions across two Undo/Redo entries. Save is in-memory; no personal ROM or
settings are modified. Core completion is idempotent and preserves valid already-applied edits;
abandoning a pending template starts no creation or wave fill and opens no settings popup.

Escape and direct Cancel API checks cover painting, rectangles, event movement, picker, pan,
selection and pending object/wave templates. They assert capture is released, the brush clears,
held motion/release cannot paint or swap blocks, valid edits remain undoable/redone exactly,
empty operations create no history, and the next edit has a separate history boundary. A fresh
rectangle after cancellation must not restore a stale backup.

The original ghost-paint regression and Escape capture regression failed before their fixes.
These are enforced tests, separate from the historical observations in tools/MapTransitionReview. Native OS activation,
trackpad hardware, border/connection resizing and fully combined 9-grid/wave edits remain
outside this group's direct scenario coverage. Reveal ownership is covered below.

Rectangle-permission coverage runs a 12-case matrix of preserve/explicit 0/1/62 settings with
no bag, a one-member bag and a two-member bag over mixed original 0/1/16/63 permissions.
It checks rectangle growth/shrink restores outside cells, exact unrelated bytes/metadata,
Save/reload and exact Undo/Redo (including random results). Copied multi-cell patterns still
repeat their packed source data; permission-only rectangles retain block IDs.

Same-cell history coverage repaints after Undo, changes permissions after Redo, replaces art
without changing permissions, checks saved-state/history boundaries, preserves consecutive
same-position bag suppression and exact random Redo, and validates an initial direct top-left
draw. Only coordinate suppression resets at a fresh stroke; fill's original-region value remains.

Reveal ownership scenarios cover focus moving to a toolbar field, tab detachment, independent
context replacement, explicit navigation and normal release/reuse. They assert both VM/map
flags clear, native Space text input remains usable, Command event hiding/release remains
functional and ROM bytes/metadata are preserved. Alias navigation does not claim equality to pre-navigation
metadata; the stroke-navigation case additionally checks a post-navigation baseline. Native window deactivation delivery
and reveal-mask/compositor animation timing are not asserted by the headless scenarios.


Border single/copied-brush scenarios open the actual border image, disable only its fixture
slide transition and verify the pointer hit/capture owner. Held movement beyond all four edges
(including negative fractions), capture loss and release must preserve the exact valid stroke;
invalid sampling retains the brush, and one Undo/Redo restores complete byte snapshots/canaries.
Core/default/custom-dimension cases are covered by the separate BorderBounds group.


Nine-grid overrides are tested over all four inner-corner orientations with explicit permissions
00/01/3F selected before and after enabling the mode. Differential controls preserve original
copied/derived corner art and default packed permissions; fresh copying/Cancel clear prior
overrides, while another stroke with the same brush retains the explicit choice. Every unrelated
byte and metadata is checked, along with Save/reload and exact Undo/Redo boundaries. Separate
Core checks retain the original int-coordinate overload and validate fallback corners without
discovery. Negative copied-brush preservation (HMA-016) remains outside these fixes.
