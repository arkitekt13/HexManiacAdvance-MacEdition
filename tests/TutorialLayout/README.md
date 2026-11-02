# Tutorial panel layout regression

Checks the real tutorial control at two host widths without opening a window or loading a ROM.
It repeatedly calls the pointer entry/exit handlers and verifies right-edge anchoring, the collapsed
72-pixel icon strip, the 320-pixel expanded panel, containment of the five visible descriptions,
and room for the full text of all 36 descriptions at the compact size.

It also checks the map markup's tutorial/arrow drawing order and the production preview controller's
map-only ownership, source filtering, removal on exit, same-NPC re-entry, and empty-cell clearing.
These checks do not open a native popup window or exercise a physical mouse.

Run from the repository root:

```sh
dotnet restore tests/TutorialLayout/TutorialLayout.csproj
dotnet build tests/TutorialLayout/TutorialLayout.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1
dotnet tests/TutorialLayout/bin/Debug/net8.0/TutorialLayout.dll
```

Manual verification still required: hover into the strip, move onto the expanded descriptions,
move between cards, dismiss a card, and move back onto the map to check collapse with a real mouse.
Check the short expansion animation, confirm arrows stay behind the cards, and move from an NPC
onto the tutorials and back to verify that dialogue previews close and reopen in the proper place.
