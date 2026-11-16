# Tutorial patch framework

Run through `scripts/verify-mac.sh`, or build the project and execute its DLL.
The default group uses fabricated ROMs/patches; an internal test loader supplies the
fabricated base identity while exercising the same validation and publication code.
The public shipped loader always pins the known clean 16 MiB English FireRed 1.1 SHA-256.

Checks cover all three assets, production base pinning, wrong game/revision/edited base,
UPS forward direction/checksums/expansion, IPS literal/RLE records and EOF, output hash and original source/history.
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

This uses a temporary fabricated one-byte lesson patch, separate from the shipped village.
It exercises the public loader, canonical base hash, paired output, all real definitions,
Pallet Town rendering, actual Save/reopen, Undo/Redo and unchanged source bytes. Temporary
pairs are removed. See [packaging instructions](../../OptionalAssets/Tutorial/README.md).

## Finished Tutorial Village package (November 16)

The shipped manifest now explicitly selects the user-supplied tutorial.ips. The original
matching tutorial.toml is bundled unchanged. Legacy manifests without patchFile continue
to select tutorial.ups; only those two fixed filenames are allowed. Missing/invalid assets,
wrong magic, output mismatches, malformed/truncated IPS and edited bases are rejected.

The default group passes 77 assertions, including IPS vectors and production asset loading.
To test the actual supplied package and compare it with independently decoded output:

```sh
dotnet tests/TutorialPatch/bin/Debug/net8.0/TutorialPatch.dll /absolute/path/to/clean-firered-1.1.gba /absolute/path/to/independent-output.bin
```

This adds 20 checks: exact output comparison, chosen-name ROM/TOML publication, initial
TOML byte preservation without regeneration, all 547 named addresses/formats and 73 supplied
named lists, Tutorial Village rendering at 80x56 tiles, real edit/Save/reopen and Undo/Redo.
All writes use a disposable folder. The original base and tutorial project remain unchanged.
TabMenus accepts the clean-base path as one optional argument to check actual utility
availability in a headless MainWindow (three supplemental checks).

These checks do not certify every tutorial event/gameplay path or native picker behavior.

[Supplied-package verification](supplied-village-verification.json) records the fresh
89-group / 25,320-assertion pass, 23 separate real-base/menu assertions without double
counting, exact supplied/output/bundled hashes and guarded 1.0.0 app publication.
