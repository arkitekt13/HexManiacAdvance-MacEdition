# Ikarus utility paths

Development update, November 17, 2026. These changes are not yet in the published
1.0.1 app. No general SCADE/Ikarus compatibility is claimed for the original IPS.

## Vanilla FireRed

**Utilities → Ikarus Visual Upgrade — Vanilla FireRed…** accepts 16 MiB English
FireRed 1.0/1.1 vanilla-based projects. Expanded ROMs and other games, languages
or revisions are blocked. Edited vanilla projects remain allowed; an unedited or
minimally edited project is recommended. A 16 MiB size/header check does not prove
that another custom ROM base is compatible.

The confirmation explains changes to graphics, maps, movement/elevation, block
behavior/layers and the Five Isle Meadow routine. Choose a new backup name; the
ROM and TOML, including unsaved edits, must be backed up and verified before the
patch touches the live project. Existing files are protected. The change remains
unsaved and supports Undo/Redo. FireRed 1.1 and gameplay need further testing.

## SCADE experimental

**Utilities → Ikarus Visual Upgrade — SCADE (Experimental)…** accepts only the
exact fresh SCADE 2026-11-15 output produced by the bundled SCADE utility. It
rejects altered data, older/different builds, vanilla and already-patched output.
It is not a repair tool for an existing project patched with the original IPS.

1. Create a fresh SCADE ROM from exact clean English FireRed 1.0.
2. With that ROM selected, choose the experimental SCADE/Ikarus utility.
3. Read the testing limits, then choose a folder and a **new** ROM name.
4. HMA verifies the patched output and installs `YourName.gba` alongside
   `YourName.toml` before opening the new project. The source pair is unchanged.

The new TOML retains all 556 original SCADE named definitions and 79 lists and
adds 83 definitions to identify/reserve relocated graphics. Existing ROMs or TOML
files are never overwritten. Cancellation before choosing a path writes nothing.
Normal publication failures remove this operation's staged files and matching
TOML; power loss between publishing the two files may leave an orphan TOML.

The exact source, BPS, TOML and output identities are pinned in the installer and
manifest. This is a relocated compatibility patch, not the original IPS applied
blindly. See the [experiment report](IKARUS_SCADE_COMPATIBILITY_EXPERIMENT.md) for
all-byte conflict analysis, asset/map comparisons and native smoke coverage.

**Experimental limits:** broader battles, expanded species in-game, regional
animations, battery-save persistence and later progression remain unverified.
The utility displays this qualification before creating the new project.

## Credits

- **SCADE: ShinyMiner & Team**, [CFRU Expansion](https://github.com/Shiny-Miner/CFRU-expansion).
- **Ikarus: Ikarus & Many Contributors**, including LibertyTwins for the fixed
  release, [Ikarus Tileset Patch V3.2](https://www.pokecommunity.com/threads/ikarus-tileset-patch-v3-2-fixed-version-gen-4-tiles-for-fire-red.532686/).

The bundled confirmation preserves the creator credits and notes. ROMs and saves
are not distributed with either utility.
