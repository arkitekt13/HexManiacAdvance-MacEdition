# Using Mac Edition

For installation, follow [Installation in the README](../README.md#installation).
Use a copy of your ROM and keep its matching `.toml` file beside it when moving your project.

## Connect an emulator

Open a ROM and click the **green Play button** (or **F5**) to launch an external emulator.
The first click asks you to select an installed application such as **mGBA.app**; use **Browse**
if it is missing from the initial application list. HMA remembers the selected path and asks
again if that app has moved or disappeared.

Unsaved ROM or script changes require **Save and Launch**; canceling, a failed save or rejected
script source prevents launch. The external emulator runs the saved ROM on disk.
To choose another emulator, quit HMA and remove only
`~/Library/Application Support/HexManiacAdvance/external-emulator.txt`, then click Play again.

The embedded emulator and Claude integration code remain in the repository, but their normal
buttons, menus and shortcuts are hidden/disabled. They are not required for editing or
external emulator use; RetroArch cores and assistant subscriptions are not setup requirements.

## Mac controls

### Keep a map beside script source

Open an NPC, script event or map script from the Map Editor, then click **Map View**
beside the **Code Type** dropdown. The right pane shows that source map and its name/ID;
drag or use two-finger scrolling to pan. **Hex View** returns to the ROM bytes.
The app remembers the view preference, and map pan positions last for the current session.
Check **Grid** beside the reference map's caption to show light 16 × 16 pixel tile
boundaries. It scrolls with the image and remembers its on/off setting between launches.

This is a static reference: NPCs appear at their stored map positions, and movement
commands are not simulated. It has no tile/event editing or map context menus.
Opening a script directly by address without a known map displays an explanation;
open it from a map to establish the reference. Other code types and tools retain Hex.

Preview preparation uses a detached copy, leaving live script drafts, diagnostics,
metadata and history alone. Map edits made in the editor appear when returning to
the script tab; Undo/Redo and external reload also refresh the reference.

### Keyboard and trackpad shortcuts

| Action | Mac control |
| --- | --- |
| Open / Save / Save As | Command-O / Command-S / Command-Shift-S |
| Undo / Redo | Command-Z / Command-Shift-Z |
| Copy / Cut / Paste in a text editor | Command-C / Command-X / Command-V |
| Tab, warp, arrow, hex or palette context menu | Control-click or secondary click |
| Pan a map with a trackpad | Option + primary-button drag |
| Select placed map blocks | Control + primary-button drag |
| Zoom a map | Status-bar minus/plus buttons or the existing wheel gesture |
| Resize code text | Minus/plus buttons beside Code Type; Command-scrolling does not resize text |
| Replace a short field | Tab or Shift-Tab into it, then type over the selected value |

Mixing-bag membership uses **Command-click in the block selector**, which is distinct from
Control-click context menus. In dialogue, Enter inserts a line break; a blank line introduces
a paragraph/page break. Overflow warnings are visual guidance, not automatic text wrapping.

## Apply the Ikarus Visual Upgrade

Mac Edition 1.0.2 provides separate paths. **Do not apply the original Ikarus IPS to SCADE**; its graphics region
overlaps expanded sprite data.

1. Open a **16 MiB English FireRed 1.0/1.1 vanilla-based** project.
2. Choose **Utilities > Ikarus Visual Upgrade — Vanilla FireRed…** and read the confirmation.
3. Choose **Back Up and Continue…**, then choose a folder and a **new backup name**.
4. HMA verifies both the backup `.gba` and matching `.toml` before applying the upgrade.
5. Test the result. **Save** keeps it; **Undo** restores the pre-upgrade state.

An unedited or minimally edited project is recommended, but ordinary edits are allowed.
The backup includes unsaved ROM changes and current metadata; existing backup files are
never replaced. Correct rejected script source before proceeding. Canceling the confirmation
or backup picker leaves your project alone. Your original files are not automatically saved.

This patch changes graphics **and** some map layouts, movement permissions, block behaviors
and a Five Isle Meadow tile-change routine. Gameplay and FireRed 1.1 need further
testing. Expanded bases, other games/languages/revisions are blocked by this path.
A matching size/header does not prove compatibility with another custom ROM base.
Keep your paired backup until testing is complete.
Credit **Ikarus & Many Contributors**, including LibertyTwins for the fixed release.
See the [original Ikarus release thread](https://www.pokecommunity.com/threads/ikarus-tileset-patch-v3-2-fixed-version-gen-4-tiles-for-fire-red.532686/); creator notes and full contributor
credits are also available in the confirmation dialog.

## Apply the experimental Ikarus upgrade to SCADE

1. Create a fresh bundled SCADE 2026-11-15 ROM from exact clean FireRed 1.0.
2. Choose **Utilities > Ikarus Visual Upgrade — SCADE (Experimental)…**.
3. Read the testing limits, then choose a folder and new ROM name.
4. HMA verifies the output and installs the matching TOML beside the new ROM before opening it.

Your source stays untouched. The exact source checksum is required; edited projects,
different SCADE versions and already-patched output are rejected. This does not repair
a project previously patched using the original IPS. All 556 SCADE anchors and 79 lists
remain, with 83 relocation/reservation definitions added. Broader gameplay remains
experimental; see [requirements and coverage](IKARUS_UTILITIES.md).

## Create a SCADE ROM base

SCADE is a feature-rich ROM base for building your own FireRed hack, created by **ShinyMiner & Team**. It builds on expanded CFRU (Complete FireRed Upgrade) and DPE (Dynamic Pokémon Expansion), adding features and fixes. HMA applies the supplied patch and installs matching editor definitions; SCADE is separate from the Tutorial Village project.
See the [original SCADE project](https://github.com/Shiny-Miner/CFRU-expansion).

1. Open a **clean English FireRed 1.0** ROM; FireRed 1.1 and edited ROMs are rejected.
2. Choose **Utilities > Create SCADE ROM…**.
3. Choose the folder and a new ROM name in the Mac save dialog.
4. HMA creates `Your Name.gba` and the creator's matching `Your Name.toml`
   beside it, then opens the new ROM with those definitions.

The original ROM and its metadata are preserved. Existing destination files are
never replaced; choose another name if either file already exists. Finish or
discard pending/rejected script edits before using the utility. Keep the TOML
beside the new ROM when moving or sharing your project. General BPS import is
not enabled by this feature. See [asset/version details](../OptionalAssets/SCADE/README.md).

## Create the Tutorial Village project

The **Utilities > Create Tutorial ROM…** action stays disabled until the finished
tutorial patch and matching definitions are included in a build.

1. Open an **unmodified English FireRed 1.1** ROM. The complete ROM hash must match;
   FireRed 1.0, edited ROMs and other games are rejected.
2. Choose **Utilities > Create Tutorial ROM…**.
3. Choose a folder and a new ROM name.
4. HMA creates `Your Name.gba` and the bundled `Your Name.toml`, then opens the pair.

The matching TOML is in place before opening, preserving the tutorial's custom definitions.
Your base ROM and its metadata are untouched. Existing files are never replaced, and
cancellation writes nothing. Finish or discard rejected/pending script edits first.
Keep the ROM and TOML together when moving the tutorial project.
See [release asset instructions](../OptionalAssets/Tutorial/README.md).

## Updates

On startup, Mac Edition checks this project's published GitHub releases, at most once per day.
A newer stable release with a compatible Mac download shows an **Update available** notice.
Choose **View Update** to open the releases page, or **Dismiss** to hide it for this session;
**Help > Update** remains available. Nothing downloads or installs automatically.
Offline checks fail quietly. Pushing changes to a branch does not create an update.

Mac Edition uses its own release numbering, starting at **1.0.0**; your project's existing
Core/TOML version remains independent. Keep your ROM and TOML together when moving projects.

## If macOS blocks a downloaded app

The released Apple Silicon Mac Edition 1.0.1 app is Developer ID signed and Apple-notarized. Extract its ZIP, move the app to Applications and open it normally. [Download it from GitHub Releases](https://github.com/arkitekt13/HexManiacAdvance-MacEdition/releases/tag/v1.0.1). Development bundles made with `package-mac.sh` remain separately ad-hoc signed.

If the notarized release is blocked, retain the exact warning and report it with the archive checksum; do not disable system-wide protections.

## Supported Macs

The current packaged app declares macOS 12.0 as its minimum runtime version. Older macOS
versions and Intel hardware still need independent release testing; see the
[tester checklist](MAC_HELPER_TESTING.md). Packaged apps include their .NET runtime.

## Source launch troubleshooting

- **`dotnet: command not found`:** Install the .NET 8 SDK, close Terminal, then open it again.
- **`cd: no such file or directory`:** Confirm the extracted source folder is in Downloads
  and is named `HexManiacAdvance-MacEdition-avalonia-port`.
- **App stops when Terminal closes:** Leave Terminal open while running the source version.

For general editing and scripting, use [Haven's tutorials](https://github.com/haven1433/HexManiacAdvance/wiki).

## Check for updates

Choose **Help > Check for Updates…** to check immediately, even if startup checked
earlier today. The banner reports whether a newer release is available, your app
is up to date, or the check failed and should be retried. **View Available Update**
opens this fork's release page; it does not install anything automatically.
Startup checks still use a daily cache. Earlier 1.0.0/1.0.1 apps lack the manual
button; download 1.0.2 to obtain it.
