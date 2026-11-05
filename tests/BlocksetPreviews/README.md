# Blockset previews

Exercises the real compiled primary/secondary selector templates and routed pointer entry,
with synthetic blocksets only. Verifies there is no native image tooltip, the map-owned preview
cannot intercept input, rows update promptly, and selection, dropdown/header closure, recycled
contexts and detachment dismiss old previews. A separate oversized image checks fitting and
window resizing even when a valid blockset's actual preview is small. Bytes, metadata and history
must remain unchanged. Native macOS flicker, popup overlap and visual usefulness still need a
live check; no full compositor behavior is claimed by the headless checks.

```sh
dotnet restore tests/BlocksetPreviews/BlocksetPreviews.csproj
dotnet build tests/BlocksetPreviews/BlocksetPreviews.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1
python3 scripts/run-upstream-command.py 60 dotnet tests/BlocksetPreviews/bin/Debug/net8.0/BlocksetPreviews.dll
```

Renderer regressions cover Ruby/Emerald and FireRed layouts, primary and secondary sets, two
blocks, partial rows and full capacity, and both inferred boundaries and formatted block/word
tables. Every output pixel, scale, blank row remainder, ROM byte and metadata record is checked.
The old renderer fails the two-block case with a 128x1040 image instead of 128x16. Normal map
rendering keeps its usage-based block reads; only standalone preview length changes. Secondary
previews still render a blockset in isolation, without importing a selected primary set's tiles.
