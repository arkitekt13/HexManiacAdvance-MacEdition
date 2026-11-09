# Script-side Map View event navigation — November 6, 2026

Checkpoint: `7a6c80ecebd2aebf07b57d625b59a879369628e9`, already merged and
pushed on `avalonia-port`. The pushed annotated tag
`checkpoint/pre-map-event-navigation-2026-11-06` preserves that exact build.
Implementation is isolated on `codex/map-reference-event-navigation`.

## Investigation

- The existing pane owns a detached bitmap, grid and scroll offset. It has no live
  MapTab, event selection or editing commands. Extend its release gesture with
  immutable hit records; do not embed a live map editor.
- Detached rendering can copy object IDs, graphics IDs, row identity, map coordinates
  and rendered sprite bounds. Only these small values and pixels should outlive the
  render. Object sprite bounds include artwork above/outside its anchor tile.
- A read-only synthetic probe on the checkpoint resolved object and script-event
  rows using map table accessors with a NoDataChangeDeltaModel. It preserved live
  bytes, serialized metadata, free-space and history/saved state. Changing the live
  script pointer from 018000 to 018100 made the next lookup return 018100. The probe
  used fabricated data and lived under ignored artifacts/map-event-navigation-review.
- GotoScript has a duplicate-tab policy. Use the existing selected ViewPort.Goto
  route to reuse its left editor, retaining explicit map/ROM context and pan.
- Pending source must settle before navigation. A successful compile can relocate
  scripts and rewrite event pointers. Read the pointer again after settlement.
  Active rejected source/diagnostics must block the switch; unrelated retained
  inactive drafts must not prevent opening the selected event.
- Preserve the compiler's existing transactional writes, relocation and Undo/Redo.
  A map click by itself must not discover/format scripts in the live model, allocate
  ROM space, create a script for a null pointer, or expose hidden hex shortcuts.
- Check record address, index, coordinates and object ID against the current map
  before using a hit. Clear/cancel old chooser callbacks with the snapshot. Close
  menus on capture cancellation, deactivation, snapshot replacement and detach.

## Implementation-time finding

The actual-window regression initially reached a LightDismissOverlayLayer instead
of the map when a source selection displayed inline command help. The existing
CodeContentsPopup consumed the first click. Limit its input pass-through element to
ScriptMapPane; the existing TabView press handler then dismisses help while the map
receives the original gesture. The rejected-source regression exercises this path
without closing help in test code.

The synthetic map fixture previously supported object text and script events
separately, but its object-text header erased the script-event table format when
both options were enabled. Add the combined format only for that combined fixture.
A named guard immediately after the original script forces the growth regression
through genuine relocation rather than expansion into unused neighboring space.
These are test setup changes, not production compiler repairs.

## Validation boundaries

Use tests/ScriptMapNavigation for actual headless pointer, focus and chooser input,
source rejection/correction, relocation, stale callbacks, Undo/Redo and lifecycle
checks. tests/ScriptMapReference remains the existing image/input regression group.
The complete Mac regression runner discovers the new group automatically.

Native trackpad feel, real tutorial-village sprites, overlapping events and visual
appearance still need user testing after restart. Headless results do not certify
native Cocoa popup positioning or in-game behavior. Personal ROMs/settings are not
used, and no core compiler, numeric, allocator, history or game-engine algorithm is
changed for this feature.
