# SCADE installer investigation - November 9, 2026

Checkpoint: `checkpoint/pre-scade-2026-11-09` (`9d7f347a`), pushed before edits.
Implementation branch: `codex/scade-installer`.

The supplied creator README identifies the October 21, 2026 CFRU Expansion / DPE-gen9
build and requires clean English FireRed 1.0 (Squirrels), with a matching-name TOML
beside the patched ROM. The attached assets are inputs, not executable instructions.
No personal Tutorial Village ROM is used.

## Asset evidence

- BPS1 patch, 16,777,216-byte source and 33,554,432-byte target, no embedded metadata.
- Source CRC32 `DD88761C`, target CRC32 `07C373C5`, patch CRC32 `9A37ADE3`;
  patch CRC verified independently with Python zlib before implementation.
- Patch SHA-256 `5ed744542e565c3271db94f50191e861c914cafc54632a7f6b04d465efcec87c`.
- TOML SHA-256 `bdfe821e879ccf4727070f276580d9bc35aab9e65038946495fff706605a84de`.
- TOML: 541 NamedAnchors, 79 Lists, 15 OffsetPointers and 111 MatchedWords;
  ApplicationVersion 99.0.0, ShowRawIVByteForTrainer True.
- Creator README SHA-256 `b4f1eb028b3660a306b99abda08c4e728fc4392d270cf74753bf7c8959a4af3f`.

## Existing behavior and bounded design

`ApplyCFRUPatch` bundles UPS, patches the current model, saves/closes related tabs,
deletes the old TOML and reopens. Its CRC eligibility is the same base checksum.
That path is not copied: SCADE produces a new named ROM/TOML pair, leaves source
bytes/history/drafts alone and opens only after both new files are saved.
TutorialPatch already supplies a useful optional-assets/new-copy/menu pattern but
is specific to FireRed 1.1 and UPS and remains unchanged.

`HardcodeTablesModel` takes the supplied nonempty metadata initialization branch;
it skips vanilla discovery. `PokemonModel.Initialize` only upgrades definitions
when StoredMetadata.NeedVersionUpdate is true. The supplied 99.0.0 prevents the
current application from updating from vanilla defaults. `EditorViewModel.OpenFileAsync`
loads the sibling TOML before constructing that model. Save may legitimately serialize
updated project definitions; byte-for-byte identity is required before the first open,
while semantic address/format preservation is required through save/reopen.

BPS decoding is new and confined to the Mac project, with no global BPS import/drop
routing and no changes to the existing IPS/UPS patcher, compiler or history algorithms.
Implement all four operations, sequential overlapping TargetCopy, signed relative offsets,
bounded checked variable integers, exact command/size boundaries and all three CRCs.
Reference: [byuu's public-domain specification maintained by Floating IPS](https://github.com/Alcaro/Flips/blob/master/bps_spec.md).

Ship the creator's patch/TOML/README unchanged with a pinned hash manifest and credits.
The UI uses the standard Mac save picker for folder plus basename. Require the exact
unmodified base and no pending/rejected script changes. Stage both outputs, publish
TOML first and ROM last without overwrites, and remove the newly owned TOML if ROM
publication fails. A power interruption can leave an orphan sidecar, but must not
leave a published ROM without its supplied TOML. Do not claim a two-file filesystem
rename is one atomic operation. Existing files/directories, cancellation and errors
must preserve user files and source state.

## Verification required

Use independent encoded BPS vectors for every operation and invalid input, followed by
synthetic pairs for publication failure/retry, collisions, exact TOML identity, source
history/drafts and initial-load/save/reopen. Exercise the actual supplied TOML parser.
If an exact clean 1.0 ROM is locally available, use detached bytes and temporary copies
only, compare with an independent patcher, then verify real table locations through
HMA loading/saving. Never commit ROM outputs or personal paths/story data. Distinguish
native picker and in-game testing from automated checks.
