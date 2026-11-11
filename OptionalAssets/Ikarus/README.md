# Ikarus Visual Upgrade

The Utilities action applies the exact reviewed Ikarus Tileset Patch FR V3.2.
Original patch notes and contributor credits are preserved in Creator-README.txt
and shown in the confirmation dialog. Ikarus created the base tileset; see those
notes for the full contributor list and LibertyTwins bug-fix credit.

Recommended for fresh or minimally edited English FireRed projects, not enforced.
Other games/languages are blocked. Vanilla FireRed, HUBOL, CFRU and SCADE appear
compatible; automated checks cover FireRed 1.0, bundled HUBOL and supplied SCADE.
Gameplay, FireRed 1.1 and other CFRU builds require further testing.

This is more than graphics: map cells, movement/elevation permissions, block
behaviors/layers and a Five Isle Meadow tile-change routine also change.

A new named ROM/TOML backup, including unsaved edits, must verify before patching.
Existing files are never overwritten. Cancellation before choosing a path does
not compile source or write files. Rejected script drafts block the upgrade.
The patch is one undoable operation and remains unsaved until Save. Preserve
backups until gameplay testing is complete. Normal filesystem failures are
cleaned up; power loss during the two-file publication may leave an orphan TOML.
