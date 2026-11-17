# Ikarus Visual Upgrade

The Utilities action applies the exact reviewed Ikarus Tileset Patch FR V3.2.
Credit **[Ikarus & Many Contributors](https://www.pokecommunity.com/threads/ikarus-tileset-patch-v3-2-fixed-version-gen-4-tiles-for-fire-red.532686/)**, including LibertyTwins for the fixed release.
Original patch notes and contributor credits are preserved in Creator-README.txt
and shown in the confirmation dialog. Ikarus created the base tileset; see those
notes for the full contributor list and LibertyTwins bug-fix credit.

Recommended for fresh or minimally edited English FireRed projects, not enforced.
The vanilla utility accepts only 16 MiB English FireRed 1.0/1.1 projects; other
games/languages/revisions and expanded bases are blocked. Edited vanilla projects
remain allowed, with the warning and mandatory verified backup. FireRed 1.1 and
gameplay require further testing. This original IPS is not SCADE-compatible; use
the separate exact-base experimental SCADE utility, with its relocated assets and
matching TOML. No general HUBOL/CFRU compatibility is claimed.

This is more than graphics: map cells, movement/elevation permissions, block
behaviors/layers and a Five Isle Meadow tile-change routine also change.

A new named ROM/TOML backup, including unsaved edits, must verify before patching.
Existing files are never overwritten. Cancellation before choosing a path does
not compile source or write files. Rejected script drafts block the upgrade.
The patch is one undoable operation and remains unsaved until Save. Preserve
backups until gameplay testing is complete. Normal filesystem failures are
cleaned up; power loss during the two-file publication may leave an orphan TOML.
