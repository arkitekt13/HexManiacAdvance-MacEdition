# Tutorial Village distribution assets

This feature is intentionally disabled until the finished tutorial is included.
Add **tutorial.ups** and **tutorial.json** here, then build/publish normally.
The files are copied to `resources/Optional/Tutorial/` in the application.
Do not include a base or patched ROM in this repository or release.

Build the UPS patch from the exact unmodified **English FireRed 1.1** ROM to the
finished tutorial ROM. Record SHA-256 values with `shasum -a 256 FILE`.
Create `tutorial.json` with this structure, replacing all hash placeholders:

```json
{
  "title": "Tutorial Village",
  "baseRom": "English FireRed 1.1",
  "baseSha256": "SHA256_OF_UNMODIFIED_BASE_ROM",
  "patchSha256": "SHA256_OF_TUTORIAL_UPS",
  "outputSha256": "SHA256_OF_FINISHED_TUTORIAL_ROM"
}
```

Utilities → Create Tutorial ROM… requires the matching base ROM to be open,
with no pending or rejected script source. It verifies the manifest, patch,
base ROM and output, asks for a new `.gba` filename, then opens the new ROM.
It never overwrites a ROM or existing `.toml` sidecar, never applies a patch
backwards, and never changes the original tab or its history. Cancellation
does not write a file. Choose another name if an output already exists.

The action stays disabled when assets are missing/invalid, no ROM is selected,
the base differs, or script edits are pending. Its tooltip explains why.
Assets are loaded when the app starts; rebuild and restart after adding them.
