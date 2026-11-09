# SCADE installer assets

Bundled from the creator-provided October 21, 2026 test build of CFRU Expansion
and DPE-gen9, supplied for integration into Mac Edition. See the unchanged
[creator README](Creator-README.txt) for its description, support Discord and
upstream link: <https://github.com/Shiny-Miner/CFRU-expansion>.
Credit Shiny Miner and the CFRU Expansion/DPE-gen9 contributors, building on
Skeli's CFRU and Ghoulslash's DPE; this installer does not claim their engine work.

The BPS and TOML are preserved byte for byte. `scade.json` pins their SHA-256 hashes,
source size and the patch's required source CRC32. Copy all assets to
`resources/Optional/SCADE` during build/publish; no base or patched ROM is bundled.
The .txt creator README is included in the application as well.

## Using the installer

1. Open an **unmodified English FireRed 1.0** ROM (the Squirrels base).
2. Choose **Utilities > Create SCADE ROM…**.
3. In the standard Mac save dialog, choose a folder and enter a new ROM name.
4. HMA verifies the patch/base/output and creates `Your Name.gba` and
   `Your Name.toml` together, then opens the ROM with the supplied definitions.

FireRed 1.1, an edited game, or an already-patched ROM will not work. The action
stays disabled when the base/assets do not match or scripts are pending/rejected.
It never overwrites an existing ROM or TOML, or saves/changes the original game.
Do not apply SCADE to an existing Tutorial Village project.

The creator's TOML is published **before** the new ROM, and HMA does not open the
ROM until both files exist. Version 99.0.0 in that metadata prevents initial
vanilla-definition updates. Subsequent saves legitimately serialize the project's
current definitions, including moved data; they need not remain byte-identical.

On ordinary write failures, staged files and the newly owned sidecar are removed.
A power interruption between the two final renames can leave an orphan TOML;
choose another name or remove that unused sidecar after checking it. The ROM is
published last, so this ordering never intentionally publishes a ROM before its
metadata. Existing collisions remain untouched.

Updating SCADE requires a matching patch/TOML pair, updated manifest hashes/base
identity, and renewed verification; replacing only one file disables the action.
This feature adds no general BPS file-import or drag-and-drop behavior.
