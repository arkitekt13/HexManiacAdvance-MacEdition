# Experimental SCADE / Ikarus compatibility candidate

November 16, 2026. **A local evaluation build, not a release or utility update.**

Follow-up, November 17: the reviewed pair is now bundled in a separate experimental
**development** utility. The published 1.0.1 app is unchanged. See
[installer scope and instructions](IKARUS_UTILITIES.md). The statements below describe
the original experiment and its testing limits.

The candidate resolves every direct three-way byte conflict between the exact
bundled SCADE 2026-11-15 output and Ikarus FR V3.2. Broader gameplay compatibility
remains experimental. No installed app, original project, bundled asset or public
release was replaced.

## Construction and scope

The builder regenerates SCADE from exact clean English FireRed 1.0 and creates an
independent vanilla/Ikarus donor. It compares **every IPS write** against both
vanilla and SCADE, rather than relying only on HMA's recognized tables. All
184,935 conflicting bytes lie in Ikarus's added `0x900000–0x954370` region.
Unexpected conflicts elsewhere cause an immediate failure.

That region is copied from the donor into verified all-FF space at
`0x1F80000–0x1FD4370`. Only 61 typed blockset-header references into the old region
are repointed. Raw pointer-looking values inside compressed art are not rewritten.
Every byte where SCADE differs from vanilla remains unchanged: **16,852,636 bytes**.
The original SCADE graphics at the conflicting addresses are preserved.

The candidate's terrain and intended nonvisual map changes match Ikarus. The
remaining six changed animation references keep their donor targets; no native
callback is silently relocated or replaced. The song-table edit is now identified:
row 18 (`se_sliding_door`) is redirected to the existing row-9 `se_exit` data.
Its header is valid and the bytes match the original Ikarus edit. This is an
inherited door-sound change, not evidence of an overwritten SCADE music asset.

Seven legacy sprite-frame ranges change exactly as in the donor; each contained
vanilla bytes before patching. They are recorded separately from SCADE's expanded
sprite data. Their broader gameplay use still belongs in manual review.

## Matching TOML and a caught regression

The original **556 named SCADE definitions and 79 lists** remain. The matching TOML
appends **83 named relocation/reservation entries**. It does not remove original
sprite definitions to conceal collisions.

An early metadata revision formatted block arrays as ordinary tables. Full testing
caught palette truncation on existing/new maps in the current HMA reader. That
revision was discarded. The final TOML uses HMA's existing raw-block convention,
keeps explicit addresses, and reserves gaps between the known assets. Allocator
probes at sizes 1, 4, 28, 512 and 4,096 bytes all return beyond the relocated arena.
No HMA/Core fix is required for this pair; the prepared blank-selector UI repair
remains independent deferred work.

Creating a test map intentionally increases `data.maps.layouts`; that is an
expected edit. All original definitions are verified before that edit, and the
saved definitions are verified after reopening.

## Completed checks

- **327 editor assertions, zero failures.**
- All **425 terrain maps** match the Ikarus donor, excluding event sprites from
  terrain comparison because SCADE intentionally expands/changes them.
- All **60 blockset pairings and 61 standalone previews** match.
- Every blockset's raw blocks, behavior/layer attributes, decompressed tiles,
  palettes and animation reference matches the donor.
- All **7,834 recognized overworld frame ranges** are examined separately.
- A new Pallet map exposes the complete **900-block palette**.
- Normal Save, exact Undo/Redo and reopen pass using disposable outputs.
- The Mac controls pass **32 additional real-project selection checks**; existing
  and new maps both expose 900 blocks in the corrected pair.
- Both HMA and an independent Python decoder reproduce the identical candidate;
  the independent decoder verifies the original SCADE BPS stage too.
- Wrong vanilla input, already-patched input, a modified source and a corrupted
  patch are rejected. A second fresh build reproduces identical BPS/TOML bytes.
- mGBA 0.10.5 boots the candidate, reaches the fresh-game introduction, continues
  a copied save, traverses house stairs and the outdoor warp, and displays Pallet
  art, scrolling, walking, NPC dialogue/emote, night colors and water animation.
  The user's original save hash remains unchanged.

## Files and safe use

The local package is
`artifacts/compatibility/Ikarus-SCADE-Compatibility-Experimental-2026-11-16.zip`.
It contains the BPS, matching TOML, credits, checksums and verification reports;
**no ROM or save is included**. It is intentionally excluded from distributed
`OptionalAssets` and has not been wired into Utilities.

The BPS source is **fresh SCADE output**, not vanilla FireRed and not an already
Ikarus-patched/edited game. Source SHA-256:
`67c78da450f8631b7c57d92fb9345acd822ac2b3a37efa585656a6f55b15f99c`.

| Artifact | SHA-256 |
| --- | --- |
| Experimental BPS | `32831d272be7f87f28d4b39db90c1cb220387883821b485d73ee7afffaa730ff` |
| Matching TOML | `9ae24bede428941f7fe77ed7a252ebc0159795475639c6ed62d88f95aac89e50` |
| Candidate ROM (local test only) | `6e7841faf2a9dff16385f4160ff02c582405098e531b763238de5e06499cc728` |

Keep the output ROM and TOML under the same basename before opening in HMA. The
current Ikarus utility still applies the original IPS; this candidate is separate.
No existing edited project is migrated or repaired by this experiment.

## Remaining manual review before distribution

The complete direct-conflict inventory and editor checks pass. They do not prove
all runtime interactions. Pending checks are representative wild/trainer battles,
expanded overworld species, other towns' door/water/tile animations, collision and
elevation paths, battery-save persistence, Five Isle Meadow progression and other
script-driven map changes. Other SCADE/CFRU builds are unsupported.

## Reproduction and evidence

- [Standalone builder](../tools/IkarusScadeCompatibility/README.md)
- [Full editor review driver](../tools/IkarusScadeCandidateReview/README.md)
- [Aggregate candidate evidence](../tests/ikarus-scade-compatibility-review/candidate-verification.json)
- [Full editor results](../tests/ikarus-scade-compatibility-review/candidate-editor-verification.json)
- [Independent two-stage BPS verification](../tests/ikarus-scade-compatibility-review/candidate-bps-verification.json)
- [Initial investigation and remaining gameplay checklist](IKARUS_SCADE_COMPATIBILITY_PLAN.md)
