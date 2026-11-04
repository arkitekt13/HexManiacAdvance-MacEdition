# Stable NPC hover previews

Run `dotnet run --project tests/MapHoverPreview` or `scripts/verify-mac.sh --no-restore`.
The group uses synthetic content and a fabricated NPC map; no user ROM is loaded.

The old pointer-positioned, hit-testable tooltip could be repositioned over the pointer when a
complex preview filled the viewport. Map pointer exit hid/removed the tooltip; re-entry or a
same-cell move reopened cached content in a new tooltip. The regression reproduced 38 open/close
transitions for 20 pointer moves, while a simple summary stayed stable.

The map now owns one input-transparent overlay anchored beside the hovered tile. It retains the
existing summary/control within a cell, fits complete text/image content to the viewport without
cropping, and stays below tutorial/editor panels. It is not an automatic or native popup. The
existing HexContentToolTip content templates are reused, without changing other tooltip hosts.

The 134 assertions cover both simple and five-text/five-image previews, actual pointer moves and
clicks through previews, zero visibility oscillation or spurious map exits, source ownership,
all images/content, four viewport corners, small-window resizing, detach/reattach, context changes,
empty-cell clearing, same-cell re-entry, the real map's NPC summary at 1x/2x, tutorial dismissal,
wheel zoom dismissal, stable controls, unchanged bytes/metadata, and no ROM undo history.

Very tall summaries scale down to fit the available map area, including their text; they remain
noninteractive so map input passes through. Native trackpad hover remains for user confirmation
after relaunch. Script scanning/summary limits, sprite decoding, ROM edits, compilation, pointer
relocation, and shared history algorithms are unchanged.
