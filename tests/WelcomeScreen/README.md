# Welcome screen regression

Checks the real compiled welcome control at 1200×800, 800×600, and 500×400; verifies the existing
Open command binding, zero-tab visibility and return after closing the last tab, visible open
errors, shell mounting, and the actual assembly version in the footer. No ROM or native window
is required; the file picker itself still needs a live macOS check.
The button check activates its actual hover, pressed, and keyboard-focus pseudo-classes and
verifies that the rounded full-size surface changes without coloring only the content presenter.

```sh
dotnet restore tests/WelcomeScreen/WelcomeScreen.csproj
dotnet build tests/WelcomeScreen/WelcomeScreen.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1
dotnet tests/WelcomeScreen/bin/Debug/net8.0/WelcomeScreen.dll
```

Optionally pass a PNG output path to render a preview of the actual control without a window.

Manual verification: relaunch with no ROM, inspect the lettering and version footer, click Open
ROM and select a ROM, then close the final tab to return to welcome. Also check picker Cancel,
Cmd+O, drag-and-drop, and launching with a ROM directly through Finder.

The layout checks also cover the credit block and footer links at all three sizes. The welcome
screen credits Haven, xavierkeys and arkitekt13 with clickable GitHub attribution links; Mac Help opens
the fork README macOS section, while issue reporting and GitHub open this fork. The porter credit links the verified original
Mac repository (Xkeys-bot/HexManiacAdvance-mac), retaining the original author name xavierkeys. Browser launching
and native keyboard/mouse appearance still need a live macOS check.
