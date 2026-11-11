# DS graphics IPS compatibility investigation - November 9, 2026

**Do not advertise this exact file as a graphics-only patch for all three bases.**
No utility was implemented and no production code changed. All changes were made
to detached byte arrays or disposable temporary outputs; the uploaded base and
personal games/preferences were preserved. Temporary ROMs were deleted.

## Exact inputs and procedure

The user-supplied `link491s DS ROM Base v1.6.3 ... by ShinyMiner.ips.txt` is binary
IPS, not text. Its SHA-256 is
`73addd57354acebd58ec55b7b7e3fbea14ee389a5ca90b762261260edfd8033d`.
It has 511 records requesting 72,846 bytes of writes (including RLE), with its
largest write ending at 0xEAE050. Tested inputs:

- Clean English FireRed 1.0, CRC32 DD88761C, SHA-256
  `3d0c79f1627022e18765766f6cb5ea067f6b5bf7dca115552189ad65a5c3a8ac`.
- HUBOL produced from that base using the repository's bundled UPS with source,
  patch and output checksums enforced.
- SCADE October 21, 2026 produced using its original bundled BPS and supplied TOML;
  input SHA-256 `15e44f658a27070352e731b167715f21bebf6a67e6e55e51b81420765d4dcc6f`.

The opt-in probe independently applies the literal/RLE records, compares that
output with HMA's IPS patcher, verifies exact patch-delta Undo/Redo, opens using
existing metadata, renders every listed map, forces actual output serialization
and reopens. It compares geometry, block cells, collision/elevation bits, used
block attributes, named anchors, selected gameplay tables and affected sprites.
Rendered before/after samples were visually inspected; PNGs remain ignored artifacts.
[Machine-readable results](ds-graphics-review/results.json) retain every map result,
input/output hashes, invalid pointers and affected live script references.

## Results

| Check | FireRed 1.0 | Bundled HUBOL | Bundled SCADE |
| --- | --- | --- | --- |
| Maps rendered before and after | 425 / 425 | 425 / 425 | 425 / 425 |
| Newly failing map renders | 0 | 0 | 0 |
| Changed map images | 223 | 223 | 223 |
| Maps with changed cells | 20 | 20 | 20 |
| Maps with changed collision/elevation bits | 17 | 17 | 17 |
| Changed map dimensions / used block attributes | 0 / 0 | 0 / 0 | 0 / 0 |
| Confirmed invalid ability-description pointers | 4 | 0 | See baseline limitation below |
| Referenced credits graphics no longer decompressing | 0 | 5 | 0 |
| Map objects pointing into overwritten script entrypoints | 0 found | 15 across two entrypoints | 0 found |
| Existing named definitions after save/reopen | Ability descriptions lost | Five credits definitions lost | All 541 retained |

### Vanilla: confirmed pointer corruption

The 13-byte record at **0x24FC33** writes `Vital Spirit` text across the final
four entries of the ability-description pointer table at 0x24FB08. Those entries
previously addressed valid descriptions and now contain out-of-range values:

| Ability ID | Ability | Pointer location |
| --- | --- | --- |
| 74 | Pure Power | 0x24FC30 |
| 75 | Shell Armor | 0x24FC34 |
| 76 | Cacophony | 0x24FC38 |
| 77 | Air Lock | 0x24FC3C |

HMA subsequently cannot retain `data.abilities.descriptions`. This is a byte-level
collision, not just missing UI formatting. The patch also title-cases Bulbasaur and
several ability names. Pokémon base stats, move battle stats, item stats and trainer
records in the checked tables remain unchanged.

### HUBOL: confirmed credits and script collisions

Five compressed credits assets at **0x71EA40, 0x71F1E0, 0x71F7B0, 0x71FDD0 and
0x7202F0** stop decoding as valid GBA LZ data. Each still has an incoming pointer
that addresses it after patching. HMA loses the Starter1 animate, Starter3 idle/
animate and Mascot idle/animate definitions during serialization.

The IPS also changes script entry bytes at **0x71A250** and **0x71C4A0**, which
remain referenced by **15 object events**. The first opcode changes from 0x0F to
0x55 at the former and from 0x6A to 0x10 at the latter. These are live event links,
not just HMA interpreting unreferenced graphic bytes as code. The affected map/
object indices are recorded in the JSON. No in-game crash is claimed; the observed
script-byte overwrite itself is sufficient to reject universal compatibility.

Expanded active species, ability, move, item and trainer tables in this HUBOL
build remain unchanged in the checked ranges; that does not offset the collisions.

### SCADE: passes the checked structure/map scope, with important limits

All **541** custom anchor addresses/formats survive saving and reopening, no checked
NPC entrypoints are overwritten, and checked active species/ability/move/item/
trainer tables are unchanged. No new map-render failures were found.

The supplied creator TOML intentionally has an empty format for
`data.abilities.descriptions` at 0x100B050, so it is not available as an HMA table
before or after the graphics patch. Its address lies above every IPS write; the
probe does not claim to have validated its UI semantics.

On all three bases, title-screen graphics also change. Some still-valid compressed
assets become smaller; size reduction alone is not corruption. The overwritten
old tileset at 0xEA1D68 has no remaining tracked incoming pointer and is excluded
from the live-asset failure count.

## Recommendation and validation limits

Do not add this exact file as a generally compatible graphics utility. Seek a
creator-approved corrected variant or a separately vetted SCADE-specific option.
It changes existing map cells and collision/elevation bits, so it can overwrite
project edits even on a base without the identified structural collisions. A
mandatory paired ROM/TOML backup remains appropriate for any later implementation.

The final map pass completed 35 investigation invariants; the supplemental pointer/
raw-compression/event-link pass completed 32. These counts describe the review
harness, **not** a compatibility pass. The default ScadeInstaller regression still
passes 137 assertions with no build warnings/errors. No production source changed,
so the complete Mac suite was not rerun. Native UI, in-game gameplay, custom existing
projects, FireRed 1.1 and other CFRU builds remain outside this verification.
