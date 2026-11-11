# Ikarus Visual Upgrade implementation checkpoint - November 9, 2026

Checkpoint: checkpoint/pre-ikarus-visual-upgrade-2026-11-09 at 27175c4f, pushed before edits.
The user authorized an edited-project recommendation, not a clean-ROM requirement.

Add a dedicated Utilities action using the exact reviewed patch and unchanged creator
credits. Block non-English-FireRed inputs; accept BPRE revisions including 1.1 with
an explicit untested-build warning. Do not require a clean CRC or reject ordinary edits.
Compatibility wording must distinguish observed FireRed 1.0 / bundled HUBOL / SCADE
checks from other CFRU builds and remaining native/gameplay verification.

Confirmation comes before a standard folder/name backup picker. Cancellation does not
flush source or write anything. After a chosen path, settle pending script source
across related views using existing compiler protections; rejected drafts stop work.
Capture current bytes and metadata, including unsaved work; prepare the patch on a
detached model and validate named metadata before mutating the live model.

A new ROM/TOML backup pair is published without replacing existing files and verified
on disk before applying. Recheck the live snapshot after asynchronous work and before
commit; never patch a changed, closed or different source tab. Keep backups after a
later failure. The original ROM/sidecar on disk are not implicitly saved.

Do not reuse ExportBackup as a success guarantee: its void command does not propagate
both write results, changes NextExportID and saves the original sidecar. Do not reuse
generic IPS import for undoable application: it clears history. Instead apply an
isolated byte delta, rebuild formats from current named metadata and insert one
custom history change only after success. A narrowly scoped reloadable ModelDelta
subclass restores metadata and prior change-tracking on rollback/Undo/Redo; existing
ordinary deltas retain their current implementation. Test prior and subsequent edits,
shared views, script source, exact bytes/definitions/history and real map pixels.

Implementation finding: refreshing an existing Map Editor discovers derived formatting
and ordinarily records it as another history token. A live-map regression reproduced a
first Undo that reverted only formatting and left the patch applied. The dedicated utility
therefore uses a synchronous transient NoDataChange token while rebuilding/rendering map
caches, preserving redo/saved state and prohibiting ROM writes. The document's existing map
caches refresh after history reversion as well. No global history behavior is switched on;
normal callers retain the existing path. Real vanilla/HUBOL/SCADE UI Undo/Redo and
save/reopen regressions now cover this difference from fresh detached map snapshots.
