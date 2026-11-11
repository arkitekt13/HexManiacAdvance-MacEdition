# Tutorial Village distribution assets

The Utilities action is ready but stays disabled until the finished assets are included.
Add these three files here, then build/publish normally:

- **tutorial.ups** — the UPS patch from the exact clean English FireRed 1.1 base to the finished tutorial ROM.
- **tutorial.toml** — the finished tutorial project's matching TOML, saved with the current HMA version.
- **tutorial.json** — the manifest below, with verified hashes for the patch, output and TOML.

The files are copied to `resources/Optional/Tutorial/` in the app. Never include a base
or patched ROM in the repository or release. Preserve the finished TOML's bytes when
copying it into this folder; do not regenerate it from the unmodified base.

The shipped loader pins the base to a 16 MiB English FireRed 1.1 ROM (`BPRE1`),
CRC32 `84EE4776`, SHA-256
`729041b940afe031302d630fdbe57c0c145f3f7b6d9b8eca5e98678d0ca4d059`.
A changed byte, wrong revision/game, altered size or a manifest selecting another base
is rejected. A correct header alone is insufficient.

Record hashes with `shasum -a 256 FILE`, then create `tutorial.json`:

```json
{
  "title": "Tutorial Village",
  "baseRom": "English FireRed 1.1",
  "baseSha256": "729041b940afe031302d630fdbe57c0c145f3f7b6d9b8eca5e98678d0ca4d059",
  "patchSha256": "SHA256_OF_TUTORIAL_UPS",
  "outputSha256": "SHA256_OF_FINISHED_TUTORIAL_ROM",
  "tomlSha256": "SHA256_OF_MATCHING_TUTORIAL_TOML"
}
```

Utilities → Create Tutorial ROM… requires that exact unmodified base to be open,
with no pending/rejected script source. The user chooses a folder and new ROM name.
HMA verifies the assets, applies UPS forward on detached bytes, checks the output,
and confirms the supplied named definitions fit the patched ROM. It publishes
`Chosen Name.toml` byte for byte before `Chosen Name.gba`, then opens the new pair.
It does not regenerate the initial TOML, change the original project/history or save
anything on cancellation. Keep the two resulting files together when sharing a project.

Neither an existing ROM nor an existing TOML/file directory is overwritten. Ordinary
publication failures remove owned partial output and staging files; power loss between
the two renames can leave an orphan TOML. A later opening failure retains the verified
pair for retry. Normal subsequent Save may serialize metadata as usual.

Missing, mismatched, empty or outdated definitions keep the utility disabled. If HMA's
metadata version changes before packaging, save the finished project in that version
and rebuild the manifest; do not hand-edit the version to bypass validation.
Assets load at startup; rebuild and restart after adding or updating them.

Framework validation uses fabricated patches and an opt-in temporary one-byte lesson
on the real clean 1.1 base. Final Tutorial Village gameplay remains to be tested after
its actual patch and TOML are supplied.
