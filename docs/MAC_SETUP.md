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
