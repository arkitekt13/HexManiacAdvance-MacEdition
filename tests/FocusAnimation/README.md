# Focus animation regression

Reproduces the reported TranslateTransform-to-Visual cast failure, then runs the same focus
animation used by MainWindow six times and checks movement, resizing, completion, and cleanup.
No ROM or native window is required. The check uses reflection only to pulse Avalonia 11.2.3's
internal clock types; update that small test adapter when upgrading Avalonia if its internals change.
Each outline has an independent clock, so draining the dispatcher cannot advance animation
time through a competing wall-clock pulse. Three cases run normally and three add a deliberate
35-ms delay between ticks and dispatcher drains. The movement assertion requires a change
between observed position/size values, rather than merely seeing a nonzero initial keyframe.
All original crash/completion/visibility/cleanup requirements remain; 25 assertions pass.
Application animation code is unchanged. See [the investigation](../MAINTENANCE_REVIEW.md).

```sh
dotnet restore tests/FocusAnimation/FocusAnimation.csproj
dotnet build tests/FocusAnimation/FocusAnimation.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1
dotnet tests/FocusAnimation/bin/Debug/net8.0/FocusAnimation.dll
```

Manual verification: relaunch the editor, open a ROM and its map editor, and hover the tutorials
to exercise the focus outline without a crash. Confirm the outline disappears when finished.
