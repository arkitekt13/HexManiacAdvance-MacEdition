# Mixing-bag flood fill

Run through `scripts/verify-mac.sh --no-restore` or build this project and execute its DLL.

Uses fabricated map data with the real BlockMapViewModel, ModelDelta, Save, Undo and Redo paths.
Preserve-mode cases fail before the fix with FC01 on neighboring cells (for example, the
original movement value 16 requires 4001).

The 80-case matrix covers original movement values 0, 1, 16 and 63; preserve mode and explicit
values 0, 1, 16 and 62; one/two-member bags; connected L regions at all map edges; matching
isolated cells; same block with different permissions; scaled/translated coordinates; exact
unrelated bytes and metadata; and Save/reload before and after Undo/Redo. Save captures bytes
and metadata in memory through StubFileSystem and reloads an independent PokemonModel. Redo
must restore the exact random outcome. An empty bag must leave bytes and history untouched.

These are Core assertions; actual Mac bag membership/clear pointer routing is reviewed by
`tools/BlockBagReview`. No user ROM, personal settings or emulator is used by this test.
