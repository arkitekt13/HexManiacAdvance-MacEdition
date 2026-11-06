# Script-side map reference

All ROM/map inputs are fabricated. This group exercises detached NPC-map rendering
and the actual TabView/MainWindow controls through Avalonia.Headless.

Coverage includes:

- Native-sized map pixels with unchanged live bytes, serialized metadata, allocation
  state, byte highlighting, saved state and Undo availability.
- Event double-click, script-address link and map-header script routes; duplicate and
  shared-script tabs retain explicit map context independently from other ROMs.
- Actual Hex View/Map View button clicks, preserved selection/pan and app preference
  serialization. No-context and invalid/removed-map placeholders do not guess a map.
- Dragging, Escape, immutable image reuse during panning and no redraw while typing.
- Exact pending callback retention across real toggle clicks; rejected source and
  diagnostics survive rendering. Paste after non-text focus goes into script source,
  and Delete cannot write the hidden ROM selection. Correction/retry still works.
- Undo/Redo refresh through a read-only history notification, preserved Redo and stale
  render rejection when another ROM becomes active.

Build this project and run its DLL, or use `scripts/verify-mac.sh`.
Passing an absolute PNG path to the DLL saves an optional offscreen UI preview.
The red/green checkerboard is synthetic artwork, not a real game map.

The renderer captures bytes/metadata on the UI thread, then prepares only a detached
model in the background. The live compiler, matching, numeric and relocation rules
are unchanged. Preparation is lazy and does not run per keystroke or pan. Images are
released when inactive; only small per-tab/map pan offsets persist for the session.

Native trackpad feel, Retina/multiple-display appearance and real tutorial-village
gameplay remain user checks; these headless results do not certify those paths.
See [the investigation](../SCRIPT_MAP_REFERENCE_REVIEW.md) for the original side-effect
probe and design limits.
