# Query-local map usage regression

Run with `scripts/verify-mac.sh --no-restore`. This group uses only fabricated map/event/script
addresses and the existing SyntheticMapFixture; no ROM is required. It runs the real public
MapOptionsArrayElementViewModel through ManualWorkTimer, parser, map preview creation, and history.

Four banks intentionally alias one map/header. Three NPC events and one coordinate event share
an additem script, and a separate hidden item provides a fifth match in each bank. Assertions
require exact ordered per-bank/event destinations and preserve thumbnail pixels; reflection only
reads existing private destination fields. A different enum query and unmatched item remain empty.

Each new query must see script operand edits, event pointer edits through AddressFieldStrategy,
hidden-item edits, branches, positive/negative cyclic traversal, malformed/out-of-range roots,
replacement model bytes, and restoration. Actual model history must restore exact bytes/pointers
through undo/redo. Invalid preview dimensions must suppress unusable buttons without contaminating
a later valid query. Cancellation before loading and during first-result publication must stop
results even when the next event would share a cached positive answer.

This group complements the explicitly supplied-ROM delayed-timer benchmark; it does not assert
native timing or every custom script format. The original traversal, parser, ROM definitions,
and editing algorithms remain in use. See tests/ITEM_USAGE_PERFORMANCE.md for before/after evidence.

A separate warmed negative-query allocation guard uses a 200-command script shared by all
sixteen scripted events across the four banks, excluding thumbnails. The unchanged
2,000,000-byte budget fails against the original implementation at 16,811,032 bytes and
passes with query-local reuse; it guards the optimization even when all functional
results would otherwise remain identical. This budget is not a native latency threshold.
