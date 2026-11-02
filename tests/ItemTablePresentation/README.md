# Item table presentation regression

Run through `scripts/verify-mac.sh --no-restore` or build this project and execute its DLL.
This group shares the fabricated ItemFixture with the benchmark, uses no ROM, and mounts
the real TabView, AngleTextBox/description TextBox, palette, and PixelImage controls.

The initial regression fails before the change because ten fixed-size icon refreshes
announce width and height changes. After the change, each still publishes PixelData,
unchanged dimensions/scale stay quiet, and the actual bitmap is identical. Metadata-only
reshaping must resize the bound bitmap; a wide-sprite presentation copied through the
normal paged-view-model API verifies that a genuine scale transition still notifies.

Actual bound field editing checks price, the quoted selector label for a name containing
spaces, and a longer description. An occupied formatted guard forces description growth
to use the existing relocation path, preserving unrelated pointers. Sprite/palette writes
must change the actual bound bitmap. Each byte-edit scenario checks exact source/edited
snapshots through undo/redo and restores its starting bytes afterward. No editing or
history algorithm is replaced. The synthetic wide-sprite test uses direct presentation
copying rather than claiming every format-import workflow was exercised.

Deferred timers are intentionally controlled through both dispatchers. Native appearance,
file-dialog imports/exports, delayed map-usage scans, and real-ROM editing remain separate
checks. The optional ROM benchmark is read-only and recorded in tests/ITEM_PERFORMANCE.md.
