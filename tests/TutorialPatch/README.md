# Tutorial patch framework

Run through `scripts/verify-mac.sh`, or build the project and execute its DLL.
The default group uses fabricated ROMs/patches; an internal test loader supplies the
fabricated base identity while exercising the same validation and publication code.
The public shipped loader always pins the known clean 16 MiB English FireRed 1.1 SHA-256.

Checks cover all three assets, production base pinning, wrong game/revision/edited base,
UPS forward direction/checksums/expansion, output hash and original source/history.
Matching UTF-8 TOML (including BOM) is preserved byte for byte, validated through HMA's
parser and checked against candidate named definitions. Missing/tampered/empty/old or
out-of-ROM definitions are rejected. Publication covers file/directory/sidecar collisions,
TOML-first order, raced ROM publication, cleanup and refusal to save arbitrary bytes.
Normal opening must not regenerate the supplied TOML; actual Save/reopen retains every
named definition. Pending/rejected source, Undo back to the exact base, diagnostics and
Redo protection remain covered. TabMenus covers the disabled native action without assets.

Optional exact-real-base packaging/lifecycle check:

```sh
dotnet tests/TutorialPatch/bin/Debug/net8.0/TutorialPatch.dll /absolute/path/to/clean-firered-1.1.gba
```

This uses a temporary fabricated one-byte lesson patch, not the unfinished village project.
It exercises the public loader, canonical base hash, paired output, all real definitions,
Pallet Town rendering, actual Save/reopen, Undo/Redo and unchanged source bytes. Temporary
pairs are removed. The finished Tutorial Village patch/gameplay and native picker behavior
remain manual checks after final assets arrive. See [packaging instructions](../../OptionalAssets/Tutorial/README.md).
