# Map mixing-bag review — 2026-11-04

The feature is ported. Windows Control-click maps to Command-click on macOS through
HexContent.CommandModifier. The bag's tooltip incorrectly retained literal Ctrl-click text;
it now shows the platform's actual modifier and explains selecting a bag member as the brush.
No input or map-writing algorithm is changed by that correction.

## How to use it on Mac

1. In the map's **Blocks** picker, Command-click (⌘-click) each block to add/remove membership.
2. Select a bag member as the active brush, then paint; each placement chooses a bag member.
3. Command-click the bag icon to empty it. Selecting a brush outside the bag paints normally.

This uses the 16×16 map-block picker, not the raw 8×8 graphics-tile selector inside Edit Block.
Bag membership is editor brush state, not stored ROM data. Ordinary Mac Control-click is used
for secondary actions elsewhere and does not toggle this bag.

## Verified controls and single-stamp behavior

The standalone review creates a fabricated 24×20 map and uses actual MainWindow/MapTab pointer
events on the scaled block image and bag icon. It verifies Command add/remove/clear, negative
Mac Control-click controls, selected brush identity, no byte/metadata/history changes from bag
membership, and deterministic painting with a one-member bag. The latter must change only the
intended cell from 4000 to 4001, preserve movement permissions, and round-trip bytes/metadata
through Undo/Redo. Sixteen positive assertions include the fill characterization's valid first
stamp; they do not count the known failed fill-preservation result as passing behavior.

Reproduce from the repository root:

```sh
dotnet build tools/BlockBagReview/BlockBagReview.csproj -p:NuGetAudit=false
dotnet tools/BlockBagReview/bin/Debug/net8.0/BlockBagReview.dll
```

The normal MapSelection regression freshly builds and passes 394 assertions after the tooltip
change, retaining scaled pointer selection, permissions, stamping, previews, save/reload and
history coverage. No user ROM or settings is modified, and no emulator is launched. The full
Mac suite was not rerun for tooltip/investigation-only work. Native OS pointer delivery remains
normal desktop verification.

## Separate confirmed Core fill defect — HMA-010, not implemented

With AutoUpdateCollision=false, CollisionIndex=-1 means preserving existing movement settings.
A one-block bag containing block 1 stamps correctly: 4000→4001. The public Core double-click
fill operation then changes adjacent matching cells to FC01 instead of 4001. The probe prints:

```text
FLOOD_FILL_PRESERVES_MOVEMENT:False
FLOOD_FILL_VALUES:4001,FC01,FC01
CHECKS_PASSED:16
```

The fill probe invokes explicit Core Click/DoubleClick kinds after the positive pointer tests,
so it does not rely on the headless clock's double-click counting. It resets the prior stroke's
duplicate-cell cache through GetBlock before the independent fill check. That stroke reset is
not a production change or a claim about native repaint-after-Undo behavior.

BlockMapViewModel.PaintBlockBag directly packs collisionIndex<<10 into replacement values;
-1 therefore becomes FC00 in the stored upper bits. Its neighboring-cell matching uses the
previous full block value but has no preserve-mode permission fallback. DrawBlock and ordinary
PaintBlock already have preservation handling. This is shared Core behavior, independent of
Mac shortcut mapping, and has not been fixed by this investigation.

Recommended next fix: add a failing regression for preserve-mode bag fill, then resolve
permission bits using the existing fill/paint semantics. Cover explicit permissions, elevation/
collision bits, one/multiple members, connected versus disconnected matching regions, all map
edges, neighboring data and exact Undo/Redo/save/reload. Random tests should assert valid bag
membership and preserved bits rather than require a particular random sequence.

## Useful follow-up areas

- Audit Mac shortcut/help text, especially remaining literal Ctrl labels, against actual input
  routing before declaring functions missing.
- Exercise bag/flood/rectangle/9-grid/border tools together with modifier release, capture loss,
  map switching and new painting after Undo; treat unmeasured cases as coverage gaps.
- For further speed work, profile complete script-tab rendering and completion/scaffolding
  bursts; the width optimization leaves other shaping/layout costs requiring attribution.
