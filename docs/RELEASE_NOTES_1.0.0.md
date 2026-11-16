# HexManiacAdvance Mac Edition 1.0.0

The initial Mac Edition release combines a native macOS port with improvements to map
editing, scripting, navigation and project reliability. Built on Haven's HexManiacAdvance
and the original Mac port by xavierkeys; maintained by arkitekt13.

## Download

Apple Silicon: [Download the Apple Silicon ZIP](https://github.com/arkitekt13/HexManiacAdvance-MacEdition/releases/download/v1.0.0/HexManiacAdvance-MacEdition-1.0.0-osx-arm64-icon.zip)

The app is Developer ID signed, notarized by Apple and includes its ticket.
Unzip, move HexManiacAdvance.app to Applications, then open it and choose a ROM.
The .NET runtime is included. No game ROM or emulator is distributed.
Intel hardware is not verified for this release.

## Highlights

- A map reference beside script source, with pan/zoom, optional grid and click-to-open
  object, script-event and signpost code while preserving incomplete drafts.
- Improved dialogue, movement and trainer-battle text assistance; readable selections
  and stable long-script editing.
- Finished Tutorial Village creation from your own exact clean English FireRed 1.1 base,
  with the original matching TOML installed before opening the chosen project name.
- SCADE creation from exact clean English FireRed 1.0, with verified matching metadata.
- Ikarus Visual Upgrade with a verified named backup before patching and Undo/Redo.
- Working Find result previews, Go To focus, Mac shortcuts and external emulator selection.
- Nonintrusive update notices for compatible future GitHub releases.

Keep each project's .gba and matching .toml together. Mac Edition's 1.0.0 release number
is independent of the Core/TOML metadata version; existing projects are not migrated
merely because the application version changed.

## Verification and limits

The application baseline passes 89 Mac regression groups / 25,320 assertions, plus
separate real-base tutorial checks. The signed packaged runtime passes 871 project
lifecycle assertions, including 60 close/reopen cycles and 40 continuous saves.
The extracted download passes signature, stapled-ticket and Gatekeeper checks.
These checks do not certify every tutorial gameplay path, every expanded ROM combination
or Intel hardware. Detailed records are linked from tests/README.md.

SHA-256: `a381bf93be8626c5645c99e54a5f57f416f3ce2da7e9cab5cb61c080ab7ddde6`.

Built on [Haven's HexManiacAdvance](https://github.com/haven1433/HexManiacAdvance)
and [xavierkeys' original Mac port](https://github.com/Xkeys-bot/HexManiacAdvance-mac).
See [the user guide](https://github.com/arkitekt13/HexManiacAdvance-MacEdition/blob/avalonia-port/docs/MAC_SETUP.md)
and [report Mac Edition issues](https://github.com/arkitekt13/HexManiacAdvance-MacEdition/issues).

## November 16 icon refresh

The current download adds the approved map-based HMA app icon and was signed and notarized again. Both bundle versions remain 1.0.0. Application code and project compatibility are unchanged. Earlier 1.0.0 users need to download it again; the updater does not announce same-version packaging changes. The original archive remains available as a fallback. [Verification](../tests/MacPackaging/icon-refresh-1.0.0-verification.json). Packaging source: [6aa063f9](https://github.com/arkitekt13/HexManiacAdvance-MacEdition/commit/6aa063f9995b90f34633184c218dcb36becf68b0); the original release tag is preserved.

## Utility credits

- **SCADE:** [ShinyMiner & Team — CFRU Expansion](https://github.com/Shiny-Miner/CFRU-expansion).
- **Ikarus Visual Upgrade:** [Ikarus & Many Contributors — Ikarus Tileset Patch V3.2](https://www.pokecommunity.com/threads/ikarus-tileset-patch-v3-2-fixed-version-gen-4-tiles-for-fire-red.532686/), including LibertyTwins for the fixed release.

Full creator notes and contributor lists are preserved with the utilities.
