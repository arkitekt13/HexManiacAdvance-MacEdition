# HexManiacAdvance Mac Edition 1.0.0

The initial Mac Edition release combines a native macOS port with improvements to map
editing, scripting, navigation and project reliability. Built on Haven's HexManiacAdvance
and the original Mac port by xavierkeys; maintained by arkitekt13.

## Download

Apple Silicon: HexManiacAdvance-MacEdition-1.0.0-osx-arm64.zip

The prepared app is Developer ID signed, notarized by Apple and includes its ticket.
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

This file is prepared release text. Creating/notarizing the local archive does not publish
a GitHub release; replace the download filename with the public asset link when publishing.
