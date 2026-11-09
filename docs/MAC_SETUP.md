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

1. Open your **English FireRed** project (vanilla, HUBOL, CFRU or SCADE).
2. Choose **Utilities > Ikarus Visual Upgrade…** and read the confirmation.
3. Choose **Back Up and Continue…**, then choose a folder and a **new backup name**.
4. HMA verifies both the backup `.gba` and matching `.toml` before applying the upgrade.
5. Test the result. **Save** keeps it; **Undo** restores the pre-upgrade state.

An unedited or minimally edited project is recommended, but ordinary edits are allowed.
The backup includes unsaved ROM changes and current metadata; existing backup files are
never replaced. Correct rejected script source before proceeding. Canceling the confirmation
or backup picker leaves your project alone. Your original files are not automatically saved.

This patch changes graphics **and** some map layouts, movement permissions, block behaviors
and a Five Isle Meadow tile-change routine. It appears compatible with vanilla FireRed,
HUBOL, CFRU and SCADE. Automated checks cover English FireRed 1.0, the bundled HUBOL build
and the supplied SCADE build; gameplay, FireRed 1.1 and other CFRU builds need further testing.
Other games and non-English ROMs are blocked. Keep your paired backup until testing is complete.
Creator notes and full contributor credits are available in the confirmation dialog.

## Create a SCADE ROM base

SCADE is Shiny Miner's CFRU Expansion / DPE-gen9 build, separate from the tutorial.

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

## If macOS blocks a downloaded app

Current development bundles are ad-hoc signed and are not notarized.
After moving a trusted downloaded app into Applications, open Terminal and run:

```sh
xattr -dr com.apple.quarantine "/Applications/HexManiacAdvance.app"
```

Then open HexManiacAdvance from Applications again.

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
