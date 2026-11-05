# HexManiacAdvance Mac Edition

A macOS edition of **HexManiacAdvance**, the all-purpose editor for Pokémon GBA ROMs,
with continued work on Mac input, map editing, scripting, reliability and responsiveness.
This fork is being prepared for helper testing; feedback from real projects is welcome.

## Built on remarkable work

**Haven** created [HexManiacAdvance](https://github.com/haven1433/HexManiacAdvance), its editing
engine and extensive documentation. Thank you for building the tool that makes this possible.

**xavierkeys** established the [original working Mac port](https://github.com/Xkeys-bot/HexManiacAdvance-mac)
using Avalonia. Thank you for the incredible work bringing HMA to macOS and laying the
foundation for this edition's continued development.

This fork is maintained by [arkitekt13](https://github.com/arkitekt13).
Mac Edition builds on those projects; it does not claim their original features as new work.

**Learning HMA? Start with [Haven's wiki and tutorials](https://github.com/haven1433/HexManiacAdvance/wiki).**
Use this README for Mac setup and differences, and the upstream wiki for general editing,
script commands, data formats and ROM-hacking guidance.

## What you can edit

HMA provides editors for maps, events, dialogue, scripts, Pokémon, trainers, moves, items,
sprites, palettes and other ROM data, plus hex editing, navigation, patches and backups.
Its primary targets are the English GBA releases:

| Game | Game code |
| --- | --- |
| Ruby | AXVE |
| Sapphire | AXPE |
| FireRed | BPRE |
| LeafGreen | BPGE |
| Emerald | BPEE |

Other files and heavily modified ROMs may have reduced or incompatible functionality.
No game ROMs are included. The tutorial village and playable tutorial patch are still in development.

## On macOS

### Use a helper build

1. Look for a Mac application asset on **[this fork's Releases page](https://github.com/arkitekt13/HexManiacAdvance-MacEdition/releases)**,
   or use a helper build supplied by the maintainer; upstream Windows downloads are different.
   If no Mac asset is available, follow [Build from source](#build-from-source).
2. Choose `osx-arm64` for Apple Silicon or `osx-x64` for an Intel Mac, then extract the download
   and move `HexManiacAdvance.app` into Applications.
3. Launch the app and use **Open ROM**, **Command-O**, or drag a `.gba` file into the window.

Packaged apps include the .NET runtime; testers do not need to install .NET or Visual Studio.
The current bundle declares macOS 12.0 as its runtime floor, which is not a certification of
all macOS versions or Intel hardware; see [helper testing and remaining release checks](docs/MAC_HELPER_TESTING.md).
Microsoft's [current .NET/macOS support information](https://learn.microsoft.com/en-us/dotnet/core/install/macos)
is separate from that floor.

Current helper bundles are **ad-hoc signed, not notarized**. If macOS blocks a trusted build,
the targeted quarantine-removal command for the app in Applications is:

```sh
xattr -dr com.apple.quarantine "/Applications/HexManiacAdvance.app"
```

### Build from source

Install the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) for your Mac's
architecture, then:

```sh
git clone https://github.com/arkitekt13/HexManiacAdvance-MacEdition.git
cd HexManiacAdvance-MacEdition
dotnet run --project src/HexManiac.Avalonia/HexManiac.Avalonia.csproj
```

The first run restores dependencies. For later launches after restoration, add `--no-restore`.
Build the Avalonia project directly; the Windows WPF solution is not the Mac launch target.
A supported-runtime upgrade is a separate pre-public-release task; see the checklist below.

To build a self-contained app:

```sh
./publish-mac.sh osx-arm64    # Apple Silicon
./publish-mac.sh osx-x64      # Intel
```

Output is `artifacts/mac/<architecture>/HexManiacAdvance.app`. The script can be invoked by
absolute path from another directory; it stages and verifies a new bundle before replacing
the previous build for that architecture. For example, launch an arm64 build with:

```sh
open "artifacts/mac/osx-arm64/HexManiacAdvance.app"
```

### Connect an emulator

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

## Mac controls at a glance

| Action | Mac control |
| --- | --- |
| Open / Save / Save As | Command-O / Command-S / Command-Shift-S |
| Undo / Redo | Command-Z / Command-Shift-Z |
| Copy / Cut / Paste in a text editor | Command-C / Command-X / Command-V |
| Tab, warp, arrow, hex or palette context menu | Control-click or secondary click |
| Pan a map with a trackpad | Option + primary-button drag |
| Select placed map blocks | Control + primary-button drag |
| Zoom a map | Status-bar minus/plus buttons or the existing wheel gesture |
| Replace a short field | Tab or Shift-Tab into it, then type over the selected value |

Mixing-bag membership uses **Command-click in the block selector**, which is distinct from
Control-click context menus. In dialogue, Enter inserts a line break; a blank line introduces
a paragraph/page break. Overflow warnings are visual guidance, not automatic text wrapping.

## What this edition adds and restores

### Maps and everyday editing

- **Trackpad-friendly controls** - Option-drag panning, accessible context menus and zoom buttons.
- **Useful map status** - Tile dimensions, native pixel dimensions on hover and labeled cursor coordinates.
- **Map PNG export** - Export into `MapImages` beside the ROM, with overwrite, numbered-copy and cancel choices.
- **NPC creation choices** - Choose a sprite before placement and retain that choice within the open ROM session.
- **Stable previews and selectors** - Bounded large sprites and blocksets, steadier NPC hover previews and repaired tileset selection.
- **Continuous field editing** - Encounter tables retain focus/caret, mouse suggestions work, and field Delete stays in the field.
- **Predictable map history** - Fixes for interrupted gestures, same-cell edits, copied blocks and movement permissions.

### Scripting and data protection

- **Dialogue and movement scaffolds** - Eligible commands insert `<auto>` and a complete editable block, including after command completion.
- **Visible assistance** - Dialogue overflow/error underlines, restored unused flag/variable helpers and cleaner suggestion dismissal.
- **Focused clipboard actions** - Mac copy, cut and paste stay on the active script/text field.
- **Protected pending source** - Navigation, Save, Save As, Close, reload and Undo/Redo account for pending or rejected drafts.
- **Compilation rollback** - Rejected script/resource compilation restores bytes, pointers, relocation, metadata and history while retaining source and diagnostics.
- **Safer persistence** - Targeted save, patch/project import, external reload and first-launch/settings fixes.

These include focused changes in the shared Core as well as the Mac UI; Core is **not**
unmodified. Existing game-engine and script numeric-parsing rules are retained.
HMA scripts (`.hma`) should be dropped onto an open ROM or its map tab; they can be specific
to a game/revision and may need accompanying assets, unlike ordinary binary documents.

### Measured reductions in repeated work

Recorded workloads remove unnecessary zoom bitmap publications, reduce unrelated event-editor
construction during hover, coalesce repeated script-width calculations and improve highlighting,
argument lookup and table/portrait preparation. These are workload-specific improvements,
not a universal app-speed multiplier.

See the [accomplishments PDF](output/pdf/HexManiacAdvance_MacEdition_Accomplishments.pdf),
[detailed changelog](CodexChanges.md) and [performance evidence](tests/README.md).

## Help, feedback and verification

- **General HMA tutorials:** [Haven's wiki](https://github.com/haven1433/HexManiacAdvance/wiki).
- **Editor architecture:** [Haven's Developer Guide](https://github.com/haven1433/HexManiacAdvance/wiki/Developer-Guide).
- **Mac Edition bugs:** [this fork's issue tracker](https://github.com/arkitekt13/HexManiacAdvance-MacEdition/issues).
- **Helping test:** [first-run checklist and bug-report details](docs/MAC_HELPER_TESTING.md).
- **Current work and limits:** [FutureDevelopment.md](FutureDevelopment.md) and the [readiness investigation](tests/FIRST_LAUNCH_READINESS_REVIEW.md).

Run the complete synthetic/headless Mac regressions from the repository root:

```sh
scripts/verify-mac.sh
```

Use `--no-restore` when dependencies are already restored. The runner reports groups and
assertions; it is separate from the original unit/integration suites described in
[tests/README.md](tests/README.md). Native input, Finder, signing and gameplay still need
real-machine testing. [PORT_STATUS.md](PORT_STATUS.md) and [RELEASE_NOTES.md](RELEASE_NOTES.md)
preserve historical port/release notes rather than current setup instructions.

## License

[MIT](LICENSE), retaining the original project's copyright and attribution.
