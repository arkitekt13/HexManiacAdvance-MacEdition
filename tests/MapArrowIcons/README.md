# Map arrow icon regression

Uses the real icon resources, converter, Path control, and Skia renderer without a window or ROM.
It reproduces cloning failures for the three rotated line-arrow resources, then checks visible
rendered pixels for all six map arrow types across repeated instances and verifies the original
resource bounds and transforms remain unchanged.
It also checks the map template uses the main theme foreground, renders all six arrows in that
color, and compares the rotated arrows against the right arrow with a small edge antialiasing tolerance.
Complete Border-and-Path rendering also verifies that every direction uses the same opaque
background color from the theme.

```sh
dotnet restore tests/MapArrowIcons/MapArrowIcons.csproj
dotnet build tests/MapArrowIcons/MapArrowIcons.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1
dotnet tests/MapArrowIcons/bin/Debug/net8.0/MapArrowIcons.dll
```

Manual verification: reopen a map and check the left, right, top, and bottom resize arrows.
