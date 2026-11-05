# Fitted hex and Go To image hints

Build this project and run its DLL, or use `scripts/verify-mac.sh --no-restore`.
The 56 assertions use the real MainWindow/TabView/Go To token templates and fabricated
pointer/sprite data; no user ROM, settings, emulator or native macOS window is used.

Before the repair, the oversized hex image immediately closed its native tooltip and cached
hover state prevented same-source re-entry. The separate `goto` invocation failed against the
old compiled token template, which opened a native image tooltip. Earlier audit evidence also
showed a 2,048-pixel Go To image clipping to a 400-pixel window and intercepting its source click.

Checks cover complete small/tall/wide image fitting (including transformed source-image edges),
input transparency, stable hex hover, same-cell and same-pointer re-entry, scrolling, selection,
compiled token mouse and keyboard selection, text-only tips, empty/replaced/mutated collections,
recycled contexts, small-window resizing, Go To closure/reopening, content detach/reattach,
document-tab changes, window closure and exact ROM/metadata/history immutability.

ImagePreviewLayer is a window-owned Canvas with a DownOnly Viewbox; it creates no image popup.
It releases source subscriptions and content on dismissal. Text-only hints retain their native
service, and intentional interactive palette/map-list popups are outside this change.
Native trackpad, multiple display and compositor behavior still benefit from normal user testing.
