# Map history and hover freshness

Run `dotnet run --project tests/MapHistoryRefresh`. A synthetic NPC is selected through the real
map, its actual dialogue editor changes its text, and the preview is reopened before Undo.
Before the fix, Undo restored all model bytes but left the edited dialogue visible in the hover.

The 23 checks cover exact dialogue byte/metadata Undo, Redo preservation after rebuilding a
same-cell hover, current child-editor contents, hidden/revealed events, and actual object flag
and script-address fields across history. The alternate end script is fabricated fixture data.
The map now explicitly invalidates summaries on refresh, event visibility, primary-map changes
and map data/display invalidation; unchanged pointer coordinates no longer suppress rebuilding.
The Mac overlay drops stale content immediately and retains normal same-cell cache behavior
between model refreshes. MapHoverPreview separately protects zero flicker and input transparency.

No user ROM is loaded. Compilation, encoding, pointer writes/relocation and shared history
algorithms are unchanged; the new shared state controls presentation cache validity only.
