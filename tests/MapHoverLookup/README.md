# Coordinate-first event hover

Run `dotnet run --project tests/MapHoverLookup` or the complete Mac suite.

The fabricated map contains stacked NPCs, a warp, a script event, a hidden-item signpost and
fly spawns, plus unrelated events elsewhere and a fly point on another map bank. The test
compares filtered results with full event enumeration across border/zoom combinations and
edge/empty coordinates. Selection retains its ordinary editor and hover cannot replace it.

A tracked sprite list counts the real NPC constructor's selector accesses. Before the fix,
empty hover fails because it constructs all NPC selectors. Afterward, empty hover constructs
none, and a stack containing two NPCs constructs exactly twice the selector work of a single
NPC. This is deterministic work verification, not a timing threshold or a production counter.

The 50 assertions also protect immediate coordinate edits, Undo/Redo, pending Redo while
hovering, visibility and unchanged warm-hover bytes/metadata/history. Existing MapHistoryRefresh
tests protect actual dialogue/flag/script-pointer editors and same-cell preview freshness;
MapThreadSafety protects cold metadata discovery and UI-thread ownership. Rendering and
selection continue using complete event groups; only hover filters coordinates before creating
event editors, with current tables reacquired on each lookup and existing callbacks retained.

No game ROM is opened. See [the performance report](../MAP_HOVER_PERFORMANCE.md) for measured
Release improvements and the limits of synthetic/offscreen measurements.
