# Map selection and stamping input regression

Uses the real MapTab/MainWindow input handlers and shared Core selection, drawing, and history
with fabricated map data. No ROM or native desktop session is required.

The object panel also checks the restored Create Script (+) action for a blank object: visibility,
command availability, exact starter bytes, attached pointer/source metadata and Undo/Redo.
Its standalone fabricated object table does not depend on a real game's event tables or sprites.

```sh
dotnet build tests/MapSelection/MapSelection.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 60 dotnet tests/MapSelection/bin/Debug/net8.0/MapSelection.dll
```

Restore dependencies first. The normal Mac regression runner discovers this group automatically.

On macOS, Control-left-drag now uses the existing secondary-button selection path. Release
captures a rectangle of placed blocks, and ordinary left-click stamps it elsewhere. A physical
secondary-button drag remains supported. Command-left-drag remains rectangle painting.

Option-left-drag pans the map on macOS without discarding the active brush. Hold Option before
pressing, then drag over the map; releasing Option early continues panning until mouse-up.
Middle-button drag remains supported. Checks cover both release orders and 1x/2x zoom, exact
pan distance, copied/single-tile brush retention, unchanged ROM bytes/metadata/history and normal
painting immediately afterward. Native trackpad interaction still needs a manual confirmation.

The selection checks on macOS exercise Control held through release, Control released before mouse-up,
and physical secondary-button dragging at 1x and 2x. Each case captures a 2x3 pattern, verifies
selection makes no ROM edits or undo entries, stamps it twice, checks every block and collision
pair plus all unrelated bytes and metadata, and verifies exact undo/redo. A separate check selects
a single block through the palette view-model press/release methods and verifies Command-drag
still paints a rectangle. On other platforms, the native secondary path and Control rectangle-fill
path are checked without assigning the Mac gesture.

The headless render timer is advanced after layout so post-zoom input hits the current image
geometry rather than an older scene. The fixture supplies a minimal fabricated behavior list for
the block editor; no researched game addresses or application editing algorithms are replaced.

NPCs/events are not copied by this selection; starting a secondary gesture on an event follows
the existing event context-menu behavior. Native macOS device interaction still needs a live check.

The unchanged selection assertion fails against the original MapTab handler on the first Mac
Control-drag, which leaves no captured brush. The original gesture group passed 117 checks.

## Translucent stamp preview

The preview expansion originally passed 211 checks on macOS. The overlay uses the existing copied brush at
55% opacity, with a Viewbox mapping source pixels to the stamp footprint rather than applying
the sidebar's intrinsic scale again. Its inner surface shares the map's fractional alignment
transform, while a fixed outer viewport clips the preview behind arrows, tutorials, and outlines.

Tests compare actual preview bitmap pixels with the sidebar image, check source dimensions,
exact destination alignment and size at fractional/whole zoom and odd/even window widths,
and require zero bitmap uploads during pointer-only movement, zoom, and resizing. The same
tests continue stamping through the non-interactive overlay and verify exact map/collision bytes,
metadata, and undo/redo. Selection/painting gestures, leaving the map, cancellation, and
reveal-beneath mode hide the preview; a ready single-block brush also receives a preview.
Auto-tiling 9-grid brushes retain their existing outline instead of showing a misleading image.

## Mixed-elevation persistence

The group now passes 340 checks on macOS. Its copied rectangle contains elevation-4 ground,
elevation-0 Cross Layers transitions, elevation-3 ground, and an elevation-3 wall. The same
block graphics appear with different permissions, so correct preview pixels alone cannot pass
the data checks. Every existing gesture/zoom/repeated-stamp scenario uses this mixed pattern.

An additional routed copy/stamp calls the production ViewPort Save command, writes fabricated
bytes and serialized metadata to unique temporary files through AtomicFileWriter, and loads
those files into a fresh PokemonModel. Assertions verify block IDs, collision bits, elevations,
all unrelated bytes, and anchor/pointer metadata. Undo after saving, saving the undone state,
redo after that save, and saving the redone state each preserve the exact expected data.
Temporary files are removed in finally; no supplied ROM is opened or modified.

This verifies editing and persistence, not player movement in an emulator or an automatic
diagnosis of stair behavior. Cross Layers still requires appropriate block behavior in-game.
