# Ikarus FR v3.2 compatibility review - November 9, 2026

**Cleaner than the earlier DS patch in the tested structures, but not a purely
cosmetic update for an existing project.** No utility or runtime change was made.

## Inputs, credits and scope

The supplied `Ikarus Tileset Patch FR (V3.2).ips` is 459,140 bytes, SHA-256
`cbc79d5613c24d682119c6a43ba191baae39a0c7fab8a0cbf53311891a3c8b7c`.
Its 5,838 records request 597,600 bytes of writes, including RLE. Actual writes
include pointer/code-area references outside the README's three occupied graphics
ranges; the largest write ends at 0xEA463C. There is no IPS truncation footer and
none of the tested ROM sizes changes.

The README (SHA-256 `dd3a799ff2b84888ebcbc87a64b1ccf19b6433ea8cb469b678a8586cc00c2513`)
credits Ikarus for Ikarus Lost Property v2.4, LibertyTwins for bug fixes and a list
of graphics contributors. Preserve its full credits if this asset is distributed
in a future feature. No supplied patch or ROM data was added to the repository.

Tested the same exact clean FireRed 1.0 base (CRC32 DD88761C), the repository's
bundled HUBOL generated with UPS checksums enforced, and the supplied SCADE
October 21, 2026 build with its matching TOML. All work used detached byte arrays
and disposable temporary outputs, never personal game files/preferences.
[Detailed results](ikarus-graphics-review/results.json) retain input/output hashes,
all map comparisons, table checks and parsed script-body changes.

## Results

| Check | FireRed 1.0 | Bundled HUBOL | Bundled SCADE |
| --- | --- | --- | --- |
| Maps rendered before / after | 425 / 425 | 425 / 425 | 425 / 425 |
| New map-render failures | 0 | 0 | 0 |
| Changed map images | 120 | 120 | 120 |
| Maps with changed block cells | 99 | 99 | 99 |
| Maps with changed collision/elevation bits | 43 | 43 | 43 |
| Maps using changed behavior/layer attributes | 212 | 212 | 212 |
| Changed map dimensions | 0 | 0 | 0 |
| Newly invalid checked ability-description pointers | 0 | 0 | Baseline limitation below |
| Still-referenced compressed graphics failing decode | 0 | 0 | 0 |
| Lost definitions through actual save/reopen | 0 of 549 | 0 of 549 | 0 of 541 |
| Distinct recognized live script bodies changed | 1 | 1 | 1 |

The 212 figure counts affected maps, not unique blocksets; maps share blocksets.
Pokémon names, ability names, base stats, move battle stats, item records and
trainer records in the checked active tables remain unchanged. No changed
formatted Pokémon battle sprite was identified. Affected formatted sprites were
decoded separately from the tile-only map comparison.

This avoids the earlier patch's four corrupted vanilla ability-description
pointers, five broken HUBOL credits graphics and two overwritten HUBOL script
entrypoints referenced by 15 objects. Existing named definitions and formats all
survive actual metadata serialization and reopening.

## Non-cosmetic changes

The patch changes substantially more map data than the earlier DS patch (which
changed cells on 20 maps and collision/elevation bits on 17). These may be authored
adjustments for the new art, but applying the file to an edited game can overwrite
layout, movement and tile-behavior work.

At **0x168983**, a routine used by **FIVE ISLE MEADOW.3-56** changes from:

```text
setmaptile 12 21 687 1
return
```

to an immediate `return`, with the unused bytes filled with FF. Both signpost row
0 and map-header row 0 reach it. HMA decodes the replacement as valid bytecode;
this is an explicit change in behavior, not a malformed script. Its purpose and
progression effect have not been established by in-game testing. Do not treat it
as an unchanged gameplay routine simply because script entrypoints remain valid.

## Method and limits

The probe independently applies literal/RLE records and compares with HMA's IPS
output, verifies exact patch-delta Undo/Redo, loads the existing metadata, renders
all maps, forces real output saving and reopens the result. Cell/per-cell movement
bits and used block attributes are hashed independently of rendered pixels.
Objects, script events, script signposts, map headers and conditional header rows
are examined, following child bodies through HMA's script parser. Parsed-script
root counts are 1,959 / 1,945 / 1,958, with no thrown parse errors; unsupported
custom opcodes/native code are not thereby certified.

Refined the investigation tool to count only script runs actually covering changed
bytes: GetNextRun can otherwise return the next unrelated script. The extra body
walk avoids missing changes after an unchanged entry opcode. Corrected the PNG
exporter's BGR555 channel order; raw image-comparison hashes were unaffected. This
was review-tool work, not an application display bug. Representative outdoor and
interior renders were visually inspected; game PNGs remain ignored artifacts.

The creator's SCADE TOML deliberately leaves data.abilities.descriptions without a
table format; that baseline is unchanged, and its address 0x100B050 is beyond all
IPS writes. No claim of ability-description UI coverage is made for that build.

The review completes 35 investigation invariants with zero build warnings/errors;
these are not a gameplay compatibility certificate. The default synthetic group
still passes 137 assertions. No production code changed, so no full Mac-suite
rerun was necessary. Emulator boot, actual movement/warps/progression, native UI,
FireRed 1.1, other CFRU builds and existing edited projects remain unverified.

## Recommendation

Use this as a candidate for a **fresh, disposable ROM base** first. Validate Five
Isle Meadow progression, doors/warps and movement on changed maps before offering
it in Utilities. Any future installer should preserve a verified ROM/TOML backup
and detect conflicts with existing project changes. It should explicitly disclose
map/behavior/script changes rather than promise artwork-only replacement.
