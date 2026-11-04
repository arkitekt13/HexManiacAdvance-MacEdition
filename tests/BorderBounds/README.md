# Border bounds

Run with `scripts/verify-mac.sh --no-restore` or build this project and execute its DLL.

Fabricated maps exercise actual BorderEditor reads and both drawing overloads with default 2x2
and explicit 3x4 borders. Invalid negative fractions, negative whole cells, exact/beyond maximum
edges, NaN and infinities must preserve every byte, metadata and a pre-existing Redo entry.
Outside reads return -1. Valid edge origins retain existing right/bottom brush clipping and
packed-word behavior. In-memory Save, independent reload and exact one-step Undo/Redo verify
valid edits, custom layout fields and neighboring canaries.

The first negative-fraction write assertion fails before the fix. Rejected origins create no
history token; valid pattern origins clip to the border's intersection without shifting tiles.
Real pointer/capture routes are covered separately by MapGestureLifecycle's border scenarios.
No personal ROM, settings or emulator is used.
