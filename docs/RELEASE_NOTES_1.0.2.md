# Mac Edition 1.0.2

Three improvements for Apple Silicon Macs; all previous editor features remain included.

- **Help → Check for Updates…** checks GitHub immediately, bypassing the daily startup cache. It reports whether an update is available, the app is up to date, or the check could not complete. View Available Update opens the release page; HMA does not install updates automatically.
- **Blockset dropdowns show the current selection.** Navigating between maps refreshes the visible primary/secondary selection even when the selected list index stays the same. Existing maps and newly created maps retain selectable blocksets and correct pointer writes, with Undo/Redo.
- **Separate Ikarus utility paths.** Vanilla FireRed keeps its verified named backup and Undo/Redo workflow, restricted to 16 MiB English FireRed 1.0/1.1 vanilla-based projects. The new **SCADE (Experimental)** option accepts only exact fresh bundled SCADE 2026-11-15 output and creates a separate named ROM/TOML pair, with relocated graphics and all original SCADE definitions retained. Edited/different/already-patched SCADE bases are rejected.

The original Ikarus IPS is not SCADE-compatible: its added graphics overlap expanded sprite data. The experimental compatibility package relocates those assets and supplies matching definitions. It is not a migration or repair tool for existing projects. Automated map/asset/editor checks and basic emulator coverage passed; broader battles, regional animations, battery persistence and progression remain unverified. See [utility instructions and limits](IKARUS_UTILITIES.md).

## Updating

Save your work and quit HMA before replacing the app in Applications. Keep each project’s `.gba` and matching `.toml` together. The independent Mac app version becomes 1.0.2; Core/TOML stays 0.6.1, avoiding metadata migration solely because of the app update.

The published 1.0.0/1.0.1 apps do not have the new manual Help button. Download 1.0.2 from the release page to obtain it; their automatic startup checks may cache results for up to a day.

## Credits

- **SCADE:** [ShinyMiner & Team — CFRU Expansion](https://github.com/Shiny-Miner/CFRU-expansion).
- **Ikarus:** [Ikarus & Many Contributors — Ikarus Tileset Patch V3.2](https://www.pokecommunity.com/threads/ikarus-tileset-patch-v3-2-fixed-version-gen-4-tiles-for-fire-red.532686/), including LibertyTwins for the fixed release.

No ROMs or emulators are included. See [1.0.0 notes](RELEASE_NOTES_1.0.0.md) for the original feature list and [1.0.1 notes](RELEASE_NOTES_1.0.1.md) for the previous crash repair.
