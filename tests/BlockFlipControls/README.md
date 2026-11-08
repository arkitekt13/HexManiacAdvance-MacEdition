# Tile block flip-control approach paths

Build this project and run its DLL. The complete Mac runner also discovers it.
All palettes, tiles, block bytes, metadata and ROM bytes are synthetic.

The real SelectedBlockEditor and BlockEditor are hosted with the four-field
FireRed-style layout. Tests move a pointer in one-pixel steps from each tile to
each flip control before clicking. Jumping directly to a button center misses
the original defect: the tiny empty gap outside TileCanvas triggers PointerExited,
hiding the arrow before a normal pointer path reaches it.

The baseline fails at the upper-left foreground vertical arrow's approach path.
The repair replaces the canvas's external margin with an equally sized padded,
transparent interaction border and hides controls only after exiting that whole
area. Tile positions, dropdown spacing and model flip commands stay unchanged.

1,825 assertions cover all eight pieces on both layers, horizontal/vertical flip,
280/300/350px editor widths and 14/20pt fonts. Every approach step keeps the arrow
visible, its positional hit target remains the button, and each click changes
only the intended axis bit in the intended piece exactly once. Tile, palette and
other block bits stay intact; leaving the region hides controls. The isolated
control test also leaves its synthetic ROM bytes untouched.

Native macOS confirmation used a disposable app window containing the same real
control and synthetic block data. Coordinate clicks passed through the former
upper-/lower-left gaps, then reached their vertical arrows; both commands flipped
the expected block bits while the controls remained visible. The user's Tutorial
Village ROM and personal settings were never opened. MacSecondaryActions (43) and
BlockPanelLayout (162) were freshly rebuilt and rerun too: 2,030 assertions across
three groups, zero build warnings/errors. The full suite was not rerun for this
localized hover-region change.
