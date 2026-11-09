# Testing Mac Edition

Use a disposable copy of a ROM you can work with and keep its matching `.toml` sidecar with it.
The sidecar stores HMA's names/formats and other metadata; retain both files when moving a
project. Do not upload game ROMs to GitHub. A minimal script, screenshots, reproduction steps
and the game's code/revision are usually more useful for a report.

## First-run checks for helpers

- [ ] Record your macOS version, Apple Silicon/Intel architecture, app version/build supplied
  and whether you launched a downloaded app or built from source.
- [ ] Launch with no ROM: welcome screen, credits, Open ROM and help links are readable.
- [ ] Cancel Open, open your copied ROM, close its last tab and reopen it.
- [ ] Try a ROM path with spaces; test Finder Open With and dropping onto the window/app icon,
  both when the app is closed and when it is already running.
- [ ] Select mGBA or another installed emulator through Play, using Browse if necessary;
  cancel once, then select it and confirm a later app launch remembers it.
- [ ] Check Save and Launch with an edit on your disposable ROM, and Cancel with pending text.
- [ ] Edit a short field and script dialogue with Command-C/V, keyboard and mouse suggestions,
  and Tab navigation; confirm Delete edits a field rather than deleting its map event.
- [ ] Pan/zoom a map, use a warp context menu, choose NPC sprites and change both blocksets;
  try Save, reopen, Undo and Redo around an edit.
- [ ] Export a map PNG twice: check Overwrite, Save Updated Copy and Cancel.

There is no need to reset your own preferences to test another feature. Report the setup you
actually used; a fresh profile, older OS, Intel Mac and clean machine without .NET are separate
coverage points rather than assumptions that every helper must reproduce.

## Reporting a bug

Open an issue in [Mac Edition](https://github.com/arkitekt13/HexManiacAdvance-MacEdition/issues),
including:

1. What you expected and what happened, with the smallest repeatable steps.
2. macOS, architecture, build/app version and emulator version if relevant.
3. Base game/revision and relevant patches or prior modifications.
4. Whether it also happens after reopening, whether Undo helps, and any visible error.
5. A screenshot, minimal script or crash report when relevant; review logs for personal paths
   or other information you do not want public before attaching them.

Crash reports are stored at `~/Library/Logs/HexManiacAdvance/crash.log`; the crash dialog
can copy the full report or reveal it in Finder. Settings and emulator choice live under
`~/Library/Application Support/HexManiacAdvance/`; ROM metadata lives beside the ROM.
Please report port bugs here rather than automatically sending them to Haven or the original
porter. Their projects and tutorials remain credited and linked in the README.

## Preparing a local helper archive

From a clean source checkout, run `./package-mac.sh osx-arm64` (or `osx-x64`).
The result under `artifacts/releases` contains a self-contained app, START-HERE.txt,
BUILD-INFO.txt, the original LICENSE and this helper checklist. A matching `.zip.sha256`
file can be checked beside the ZIP with `shasum -a 256 -c <filename>.zip.sha256`.
Keep the build identification with any bug report. Dirty tracked source is explicitly labeled;
no user ROM, settings or emulator is packaged, and no release is uploaded by this command.

## Maintainer distribution checklist

- [x] Separate investigation evidence from fixes; keep feature/fix commits reviewable.
- [x] Build a real self-contained arm64 bundle and verify architecture, resources, version and signature.
- [x] Verify the published managed startup loader with isolated settings from another working directory.
- [x] Provide current Mac setup, controls, external emulator instructions, feature scope and attribution.
- [x] Enable the fork's issue tracker and provide a report template.
- [ ] Verify an actual downloaded bundle on a clean helper Mac without .NET installed.
- [ ] Verify cold/warm Finder document activation, native pickers and external emulator launch on that build.
- [ ] Build and test Intel on Intel hardware; a fake-toolchain x64 packaging test is not Intel certification.
- [ ] Test and record the OS versions actually supported; the declared 12.0 runtime floor is not that matrix.
- [ ] Upgrade to a supported .NET runtime before public release: [.NET 8 support ends November 10, 2026](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/).
- [ ] Decide Developer ID signing/notarization and publish architecture-labeled assets/checksums with a distinct Mac release identity.
- [ ] Include the original LICENSE and applicable third-party notices with the distributed archive.

Use the [release checklist](MAC_RELEASE_CHECKLIST.md) for candidate gates and platform results.
The optional welcome/tutorial frameworks are implemented, and an initial Tutorial Village
welcome image is included. Final image approval, tutorial assets/gameplay, the broad
tooltip audit and in-app guides remain future work.
Automated regression counts do not substitute for native Mac or gameplay observations.
