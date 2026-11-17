# Ikarus / SCADE compatibility investigation and plan

November 16, 2026. Investigation only: no compatibility patch, public release,
installed-app replacement or project migration was performed.

## Conclusion

A version-specific compatible ROM base appears feasible. The preferred approach is
to import/repack Ikarus's intended map assets into a fresh SCADE base, reserving
nonconflicting space and updating typed references. Applying the existing IPS as-is,
changing patch order, or removing conflicting TOML definitions does not preserve
both sets of data.

This work is separate from HMA's blank blockset-selector bug and from the proposed
manual update command. Fixing selection display does not resolve patch collisions.

## Exact inputs and reproducible findings

- SCADE bundled manifest: **2026-11-15**, patch SHA-256
  `8f5deb58cded1c4389e7ef1aa70bceed3b878aa1c539762c7406b9f032f53019`.
- Supplied SCADE TOML SHA-256:
  `7d415b24934b740b2a3b98c31c2a79a7fa9aaf282e7e5fe7a7a34d0af0a6e6c2`.
- Fresh SCADE output: 32 MiB, SHA-256
  `67c78da450f8631b7c57d92fb9345acd822ac2b3a37efa585656a6f55b15f99c`.
- Ikarus FR V3.2 IPS SHA-256:
  `cbc79d5613c24d682119c6a43ba191baae39a0c7fab8a0cbf53311891a3c8b7c`.

A clean English FireRed 1.0 input was patched to SCADE entirely in memory. An
independent literal/RLE IPS decoder agrees byte-for-byte with HMA's IPS decoder.
The clean input file remains unchanged. The report contains hashes, addresses
and counts, not ROM contents.

| Observation | Result |
| --- | --- |
| IPS records / requested bytes | 5,838 / 597,600 |
| Bytes actually changed on current fresh SCADE | 472,701 |
| Distinct live overworld frame ranges with changed bytes | 476 of 7,834 |
| Changed frame ranges in added graphics region `0x900000–0x954370` | **469**, with **185,022 changed bytes** |
| Remaining seven affected frame ranges | Legacy locations below `0x400000`; review their intended semantics separately |
| Blockset headers changed | 25 of 61 observed: one primary, 24 secondary |
| Changed header pointer fields | 68: 22 tileset, 20 block-array, 20 attribute-array, six animation references |
| Additional named table touched | `sound.tracks`: two bytes in the row-18 pointer at `0x4A335C` |

Frame ranges are deduplicated by `(address, length)`, not Pokémon species or NPC
IDs. The sound pointer change also has the same original bytes in clean FireRed
1.0; it is an edit to review, not an established SCADE-specific corruption.
Metadata-based coverage is a lower bound: it cannot certify unknown native code
or unformatted data.

A concrete overlap is Pallet's Ikarus block array at `0x9190C4`. Fresh SCADE still
references an overworld sprite starting at `0x919000`, extending across that
address. Its frame-list references include `0x1254848` and `0x12548C0`. HMA's block
size estimator encounters those sprite boundaries, truncating the palette;
existing map-cell usage can force a larger read, explaining the difference
between existing and newly created maps.

In the reported-project copy review, Pallet exposed 865 blocks while a new map
with the same pair exposed 641. In the pre-Ikarus SCADE backup both exposed 729.
Pallet's standalone secondary preview went from a 128×192 image with 36 colors
to a 128×16 image with one color. These comparisons were read-only/in-memory;
no original project was repaired or rewritten.

The supplied creator notes list three graphics regions. Actual writes in the
`0x900000` region extend through **`0x954370`**, beyond the listed `0x953F82`.
The IPS also edits lower-address map data, pointers and routines. Therefore the
full record inventory, rather than the README ranges alone, must drive the port.

## Recommended implementation

### 1. Pin a reproducible baseline and protect SCADE

Support the exact supplied SCADE build first. Generate it from the verified clean
FireRed 1.0 base and supplied TOML. Record all hashes, named definitions, tables,
overworld lists/frames and other protected assets before importing anything.

Use a separate clean FireRed 1.0 + Ikarus donor to recover the upgrade's intended
assets. Confirm the donor's revision-specific hooks individually; the creator's
release page identifies FireRed but does not establish revision interchangeability.
The creator also provides tileset source files, useful for validating or rebuilding
art. Neither the donor ROM nor any full game ROM should be distributed.

### 2. Build a typed asset and reference inventory

Inventory changed tilesets, palettes, block arrays, attributes, animation tables
and native animation routines. Preserve shared assets, alignment, compression,
primary/secondary tile numbering and palette usage. Read asset lengths from valid
donor structures, not from SCADE's conflicting metadata.

Trace references from every affected map layout and blockset header, including
literal pools, animation callbacks and any map-change code. The audit found 82 raw
pointer-looking sequences in the IPS; these are candidates, not a safe replacement
list. Do not globally replace every matching four-byte value in graphics/code.

### 3. Reserve space and relocate conflicting assets

Keep SCADE's original sprites and their references intact. Allocate the Ikarus
assets in a dedicated, verified nonconflicting arena, then update their typed
references. Unchanged, compatible existing map assets can remain in place.

Fresh SCADE has several large all-FF spans, including approximately 2.84 MiB
starting at `0x1328F38`, which makes storage capacity promising. These are only
candidates: verify no table, source-build reservation, code reference or other
asset owns the chosen space, and reserve it explicitly. Creator build maps or an
exact source/configuration would strengthen this check. Avoid assuming that
unformatted or all-FF bytes are automatically free.

Rebuild animation references and Thumb callback/literal addresses as needed.
Moving graphics alone while leaving old callbacks pointing at their original
locations is insufficient.

### 4. Port intended map and behavior edits deliberately

Preserve Ikarus's matching map cells, collision/elevation data, block attributes
and layering. Review door/water animations and the Five Isle Meadow routine edit.
The earlier review recorded 99 changed map-cell layouts and 43 maps with changed
movement bits on its tested baseline; recompute those expectations for this build.

Classify every remaining IPS write as an intended map/visual/behavior edit, a
reference update, or an unrelated/conflicting edit. Explicitly review the song
pointer and other low-address writes; do not copy them merely because they belong
to the original IPS. Keep a source-address-to-destination mapping and an allowlist
of intended SCADE changes.

### 5. Generate the exact compatible base and metadata

Create a new combined output and matching TOML. Retain SCADE's custom definitions
and add/update metadata for relocated map assets. Validate all blockset lengths and
references in HMA before Save and after reopen. Removing sprite definitions would
hide an overlap rather than restore overwritten sprite bytes.

A checksum-verified **BPS for the exact fresh SCADE output** is a suitable initial
artifact. It can address relocated assets above the IPS 24-bit address range and
reject the wrong base. A later one-step clean-FireRed-to-combined-base utility is
possible. HMA already applies BPS, but creation of this new compatible patch needs
a build/generation step; it is not produced by the existing IPS utility.

Preserve original creator assets and credits. Initial installation should create
a new ROM/TOML pair from the exact supported base. General support for edited
projects, or recovery of an already-overlaid project, is separate work.

### 6. Require structural, editor and gameplay acceptance

Automated gates:

- Every protected SCADE sprite frame and non-map asset retains its original bytes.
- Every changed byte belongs to the approved map assets, references or intended hooks.
- Map dimensions/events/scripts and intended visual/behavior changes match the donor.
- All map/tileset/palette data decode, and every pointer/animation target is valid.
- Existing maps and new maps expose the complete intended primary/secondary palettes.
- New-map creation, building selection, previews, block editing, Undo/Redo, Save and
  reopen work without changing unrelated SCADE definitions.
- Wrong SCADE versions and already-edited bases are rejected before output creation;
  cancellation/failure leave originals untouched.

Manual emulator gates:

- Boot, save/load, Pallet/Pewter/Cerulean and other affected outdoor maps.
- Town transitions, warps, entrances/exits, doors, water and tile animations.
- Collision/elevation, trees, ledges and representative script-driven tile changes.
- Expanded SCADE overworld Pokémon/NPCs, including frames from the overwritten region.
- Representative wild/trainer battles and relevant SCADE features.
- Five Isle Meadow progression and any altered sound behavior.

Map rendering and metadata retention alone are insufficient acceptance criteria.

## Milestones and remaining uncertainty

1. **Feasibility gate:** relocate Pallet's assets in a disposable fresh SCADE base;
   require a complete new-map palette, correct rendering and byte-identical protected
   sprites, then verify it in the emulator.
2. **Full port:** import the remaining affected blocksets and reviewed hooks using the
   same reproducible mapping process.
3. **Distribution gate:** full automated checks, manual gameplay checklist, exact-base
   BPS/TOML generation, then optional HMA utility integration.

The audit supports feasibility, not a completed port or an effort estimate. Native
animation references and the complete low-address write review are the main unknowns.
Do not repair an existing combined project by applying a future clean-base patch to it.
Start from the pre-Ikarus SCADE backup and handle later user edits separately.

## Evidence and primary sources

- [Detailed fresh-build collision report](../tests/ikarus-scade-compatibility-review/results.json)
- [Read-only audit driver](../tools/IkarusScadeReview/README.md)
- [Earlier review and its historical baseline/limits](../tests/IKARUS_GRAPHICS_COMPATIBILITY_REVIEW.md)
- [Ikarus fixed-release description, source assets, animation notes and credits](https://www.pokecommunity.com/threads/ikarus-tileset-patch-v3-2-fixed-version-gen-4-tiles-for-fire-red.532686/)
- [ShinyMiner & Team's CFRU Expansion project](https://github.com/Shiny-Miner/CFRU-expansion)
