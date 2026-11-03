# Specialized stream templates

This headless/Skia regression uses the production TabView with real model-backed trainer,
sprite, palette, and ordinary text-stream view models. The trainer fixture is linked from
the standalone trainer benchmark; it imports existing symbolic lists/schema/partitions
but fabricates every address and byte. No ROM or native window is used.

The original template order fails on the missing trainer TextEditor. The regression
requires the production template order, without an in-memory reordering experiment, to:

- Show the trainer TextEditor, exact team text, enabled default buttons, and published
  64×64 preview bitmaps for all four team layouts during row navigation.
- Execute both visible default commands in fixtures without learnsets/base stats and
  verify the existing zero-data fallback and exact undo; this is not real move legality
  or game-specific default-item coverage.
- Grow a team through the actual bound TextBox, force pointer relocation, update previews,
  preserve unrelated trainer pointers, and restore exact bytes/pointers with undo/redo.
- Show actual sprite bitmaps and the image-editor command, palette controls/swatches,
  and ordinary text fallback, while preserving bytes/metadata/history during navigation.

Run `scripts/verify-mac.sh --no-restore` after restoring dependencies, or build/run this
project directly. The runner automatically includes it in the Mac regression groups.
The test-only Avalonia.Headless dependency matches the application's Avalonia 11.2.3.
Native appearance, external image import/export dialogs, game execution, and real-ROM
learnsets/default behavior remain manual or fixture-dependent work.
