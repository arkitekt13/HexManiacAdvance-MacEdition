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
