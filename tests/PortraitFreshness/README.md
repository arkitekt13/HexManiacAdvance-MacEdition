# Portrait freshness regression

Run through `scripts/verify-mac.sh --no-restore`, or build this project and execute its DLL.
The test mounts the real TabView and AngleComboBox using offscreen Skia rendering and the
synthetic portrait fixture shared with TrainerPortraitBenchmark; no ROM is required.

Eight scenarios cover compressed/uncompressed sprite edits, palette edits, shared-pointer
repointing followed by editing, and replacement-file model reload. Assertions check actual
bound bitmap changes, fresh choice pixels, retained unchanged collections, selection and ROM
value preservation, popup close/reopen, editor errors, and exact-byte undo/redo where applicable.
The reload scenario exercises Model.Load rather than the external-file watcher or file dialog.

Before the fix, the sprite scenario failed because the live portrait remained stale despite
correct newly rendered candidate pixels. After content comparison was corrected, it exposed
an Avalonia selection reset when ItemsSource was replaced; the bound bitmap assertion also
requires that selection synchronization fix. Native appearance and import orchestration
remain separate manual checks.
