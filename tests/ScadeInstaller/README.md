# SCADE installer regression

Run `scripts/verify-mac.sh`, or restore/build this project then execute its DLL.
All permanent test ROMs are fabricated; no Tutorial Village or commercial ROM is
required. The default suite includes the actual bundled patch/TOML integrity and
parser checks but does not apply them to an unprovided clean FireRed 1.0 ROM.

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
command bounds; actual application and in-game compatibility still require the exact
clean 1.0 base. A real native save dialog and helper gameplay are separate manual
checks; a headless menu test is not native picker confirmation.

An optional `--command-stream-comparison DIRECTORY` mode reads fabricated
`source.bin`, independent `target.bin` and a `test-only.bps` with adjusted synthetic
checksums. This executes the actual SCADE command stream on fabricated data without
claiming to apply the original patch to a real game; its separate checks are not
added to the default suite count. Comparison artifacts remain outside Git.
