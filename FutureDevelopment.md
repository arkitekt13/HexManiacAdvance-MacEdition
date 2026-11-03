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

**Status:** Implemented; automated preview checks pass, with native visual confirmation pending.

**Requested behavior:** After selecting placed blocks, show a translucent image of that exact
pattern at the stamp destination, so copying trees does not leave only an empty outline.

**Existing support:** Shared Core already builds `MultiTileDrawRender` for the side-panel brush
preview. Windows and the Mac map view previously showed only a rectangle at the cursor.

**Implemented scope:** The Mac view reuses that image in a clipped, non-interactive overlay at
55% opacity, aligned with the stamp footprint and map zoom, behind arrows/tutorials and the
selection outline. Pointer movement and zoom reuse its bitmap. Active selection/painting,
cancellation, reveal-beneath mode, and leaving the map hide it; auto-tiling brushes retain their
outline. The 211-check map selection group preserves stamping, collision bits, metadata, and
undo/redo. Shared Core editing algorithms are unchanged.
