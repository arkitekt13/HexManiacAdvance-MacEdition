# Map object sprite selection

Run `dotnet run --project tests/MapSpriteSelection` or the complete Mac suite. The 180 checks use a real map event panel with synthetic sprite options: ordinary images, a 64x64 sprite at ID 108, and an expanded 96x128 image. No game ROM is loaded by this regression group.

Before the fix, selecting 108 failed the stable-layout assertion. The history/reopen check also reproduced a blank selector: UI SelectedIndex was -1 while the object's graphics byte remained 108. Replacing the options after selection binding evaluation coerced the UI index, and the ignored negative write left the unchanged model index unpublished. Map arrow shortcuts also intercepted keys while the combo had focus.

The selector now fits full images inside 64x64 previews with fixed popup row sizes. An opt-in one-way bound index restores selection after option replacement; queued work reads the current model index and coalesces context changes. This is limited to ordinary/dynamic overworld graphics selectors. Native two-way selection still writes the actual graphics byte.

Coverage includes small/large/back transitions, retained full bitmap dimensions, fresh uploaded pixels, stable field positions, actual keyboard/wheel/pointer input, populated virtualized rows around 108, popup reopen, complete byte/metadata preservation, map Undo/Redo, and rapid switching between objects with different graphics IDs. The standalone native blank/trackpad sequence remains for user confirmation after relaunch.

Investigation also read the local FireRed sample fixture without saving it: IDs 106/107/109 are 32x32, and 108 is 64x64. At the existing 2x preview scale, 108 previously expanded to 128x128. Game sprite rendering, sprite data, numeric IDs, dynamic graphics rules, ROM encoding and pointers are unchanged.
