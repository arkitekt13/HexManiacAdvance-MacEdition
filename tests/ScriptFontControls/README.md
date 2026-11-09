# Explicit code text-size controls

Build this project and run its DLL. All ROM/source data comes from the synthetic
WorkflowFixture; no personal files or preferences are opened.

Actual MainWindow/TabView pointer input checks the Code Type header's minus/plus
controls, one-step size changes, the existing 8–20 bounds, retained source focus,
caret/selection, and normal wheel scrolling. Command-modified wheel/trackpad
input, including fractional and horizontal deltas, must never resize code text.
The 12 assertions also require unchanged source, ROM bytes, structural metadata,
saved/history state and pending compilation work.

An optional absolute PNG argument renders the real header for visual inspection.
Keyboard navigation shortcuts are unchanged: Command-minus remains Go Back.
The controls use the existing CodeTool font property across its code modes.
