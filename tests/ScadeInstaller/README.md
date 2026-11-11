# SCADE installer regression

Run `scripts/verify-mac.sh`, or restore/build this project then execute its DLL.
All permanent test ROMs are fabricated; no Tutorial Village or commercial ROM is
required. The default suite includes bundled patch/TOML integrity and parser
checks; applying the original patch to a clean FireRed 1.0 ROM is an opt-in mode.

Coverage includes all four BPS operations, signed forward/backward relative copies,
overlapping target copies, multi-byte integers, empty output, truncation/overflow,
source/target/patch CRCs, metadata/footer boundaries and bounded output allocation.
Twelve independent vectors were generated and round-tripped with Marc Robledo's
[Rom Patcher JS](https://github.com/marcrobledo/RomPatcher.js) at commit
`4f17297cd1dc0835a0b0747497fa15f5f63f1623` (all four action types). The JSON stores
fabricated source, expected target and encoded patch, not third-party implementation
code. The decoder follows [byuu's BPS specification](https://github.com/Alcaro/Flips/blob/master/bps_spec.md).

The normal HMA EditorViewModel opening path verifies that a relocated vanilla-name
anchor loads from supplied metadata, initial open leaves its TOML byte-identical,
and save/reopen retain its addresses and formats. Publication tests verify matched
filenames, sidecar-first ordering, failure cleanup, retry and races without replacing
another writer's ROM; existing file/directory collisions are protected. An internal
fault-injection callback is used only for publication tests. WorkflowFixture covers
pending/rejected source gates and unchanged original bytes/metadata/history. The
headless native menu exposes a separate SCADE action and retains Tutorial Village.

Independent inspection of the supplied BPS verifies its patch CRC and all 185,880
command bounds; the later real-base run is recorded separately below. A real native save dialog and helper gameplay are separate manual
checks; a headless menu test is not native picker confirmation.

An optional `--command-stream-comparison DIRECTORY` mode reads fabricated
`source.bin`, independent `target.bin` and a `test-only.bps` with adjusted synthetic
checksums. This executes the actual SCADE command stream on fabricated data without
claiming to apply the original patch to a real game; its separate checks are not
added to the default suite count. Comparison artifacts remain outside Git.

## Opt-in real-base verification

The supplied clean FireRed 1.0 has now passed **37 real-base assertions**; see
[real-base-verification.json](real-base-verification.json). Output agrees byte for
byte with Rom Patcher JS using the original patch/checksums. Normal opening keeps
the creator TOML untouched; all 541 anchor addresses and formats survive an actual
Save, and all addresses survive reopening. The test deliberately changes a stat
in a disposable copy so Save cannot silently return without serializing metadata.
Three map references render, and Undo/Redo work after saving. The uploaded source
and loaded clean-base copy remain unchanged. No ROM data is committed.

To repeat with independently generated expected output:

```sh
dotnet tests/ScadeInstaller/bin/Debug/net8.0/ScadeInstaller.dll --real-rom "/path/to/clean-base.gba" "/path/to/independent-output.bin"
```

Both input paths must be absolute. The test creates and deletes its own temporary
ROM/TOML pairs; it never opens the input ROM directly in HMA. Native picker behavior
and in-game feature testing remain manual. The default synthetic group now passes
137 assertions after strengthening real-save coverage; this does not replace or
increase the historical complete-suite result in verification.json.

## Opt-in DS graphics IPS review

```sh
dotnet tests/ScadeInstaller/bin/Debug/net8.0/ScadeInstaller.dll --ds-graphics-review "/path/to/clean-base.gba" "/path/to/supplied.ips.txt" "/absolute/path/to/ignored-report-folder"
```

Requires the exact supplied patch hash and a repository checkout containing the
bundled HUBOL patch. It creates all three bases in memory, renders all maps before/
after, inspects collateral changes and deletes its temporary ROM outputs. An optional
`--skip-maps` final argument repeats raw-pointer/graphics/event-link diagnostics
without rendering. The report folder contains JSON and representative PNGs, never
ROM files. This is an investigation, not a shipped graphics utility or an automatic
part of the synthetic suite. See [confirmed findings](../DS_GRAPHICS_COMPATIBILITY_REVIEW.md).

The same probe now accepts the exact supplied Ikarus FR v3.2 hash through the
`--graphics-review` alias. See [Ikarus findings](../IKARUS_GRAPHICS_COMPATIBILITY_REVIEW.md).
Its event check follows recognized child script bodies, including map headers and
conditional header rows, so an unchanged entry opcode cannot hide later edits.

## November 11 creator update and count correction

The replacement pair and manifest use integration date `2026-11-11`; no new
upstream release number was supplied. The current synthetic group passes 138
assertions. Independent patching agrees byte for byte, and the Ikarus utility's
127 combined default/real-base checks pass across vanilla, HUBOL and updated SCADE.
The self-contained publish copies the exact replacement assets.

**The uncorrected creator pair failed strict metadata preservation.** The received
`data.abilities.descriptions` format requests 390 four-byte pointers beginning at
`0x100B050`, ending at `0x100B668`. The supplied `graphics.moves.backgrounds.all`
anchor begins at `0x100B544`, overlapping it by 292 bytes. Initial opening retains
the supplied sidecar unchanged and all 556 addresses load. A real Save retains all
addresses but emits an empty ability-description format, the only format mismatch.
The old creator pair already had an empty format at that anchor. The failure is preserved in the
[candidate evidence](update-2026-11-11-verification.json). The BPS remains exact; the
original creator TOML remains in Git history and Downloads. The shipped TOML now
explicitly limits descriptions to the independently verified 317 entries. The
unchanged strict preservation assertions now pass, with 39 real-base assertions
including both adjacent table lengths. No engine/compiler change is made.
See [correction evidence](metadata-count-correction-verification.json) and the
[shipped correction note](../../OptionalAssets/SCADE/Metadata-Correction.txt).
Earlier real-base results above apply to the original pair, not this replacement.
