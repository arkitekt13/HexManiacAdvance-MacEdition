# Synthetic map presentation regression

Uses actual Core map rendering, edit history, and Avalonia/Skia headless-window PixelImage controls.
No ROM, native window, mouse input, or network service is needed. Fabricated binary tables,
uncompressed tiles, palettes, and two checkerboard blocks are shared with the benchmark.

```sh
dotnet build tests/MapRendering/MapRendering.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
dotnet tests/MapRendering/bin/Debug/net8.0/MapRendering.dll
```

Restore normally before using --no-restore. The full scripts/verify-mac.sh runner automatically
discovers this group and requires a positive assertion count.

Checks three map sizes, zoom bitmap reuse, exact map and displayed pixels, immutable source
bytes/metadata, header pointers, painting, undo/redo, selection overlays, masks, pending frames,
context switching, ordinary sprites, layout scaling opt-out, and debug/transparency refreshes.
The standalone fixture explicitly clears map caches after undo/redo; native map-tab invalidation
is not exercised. Event pointer slots are present but no NPC payload is supplied.

See tests/MAP_PERFORMANCE.md for benchmark evidence and remaining native/ROM coverage limits.

Controls attach to a headless window before live zoom/pixel notifications are tested.
Never-attached controls intentionally do not subscribe to model changes;
TabViewLifetime protects that contract and restoration on reattachment.
