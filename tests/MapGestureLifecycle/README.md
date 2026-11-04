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

The original ghost-paint regression failed before implementation. These are enforced tests,
separate from the historical observations in tools/MapTransitionReview. Native OS activation,
trackpad hardware, border/connection resizing and fully combined 9-grid/wave edits remain
outside this group's direct scenario coverage. Same-cell-after-history and Space reveal are
separate outstanding findings, not declared fixed by this group.
