# Ikarus Visual Upgrade regressions

Run `dotnet run --project tests/IkarusUpgrade/IkarusUpgrade.csproj` or the complete
`scripts/verify-mac.sh` runner. Synthetic cases use fabricated bytes, never personal ROMs.

Coverage includes the exact bundled patch/credits, English FireRed-only and size gates,
revision-1 acceptance with explicit testing limits, ordinary edited-project acceptance,
paired unsaved ROM/metadata backup, path/file/sidecar collisions, publication failure cleanup,
tampered or unmatched backups, changed-source rejection, retained rejected source and sibling
script drafts, correction/retry, prior/subsequent edits and exact Undo/Redo. Injected format
reload failure checks rollback and edited-byte highlighting. Scoped derived formatting
preserves saved state and Redo while refusing byte writes. Actual offscreen native-menu and
confirmation controls cover menu presence, warning text, Cancel and Continue.

Optional exact-base verification (read-only uploaded source; temporary outputs deleted):

```sh
dotnet tests/IkarusUpgrade/bin/Debug/net8.0/IkarusUpgrade.dll /absolute/path/to/clean-firered-1.0.gba
```

This builds bundled HUBOL and SCADE with their patch checksums enforced and verifies
that the vanilla route rejects both expanded bases. On vanilla it adds a disposable
unsaved edit, backs up, applies and checks exact expected bytes. It checks
fresh map snapshots and an already-initialized Map Editor, data-tab Undo/Redo and earlier
history, then performs actual Save/reopen with every named definition retained. The supplied
original is hashed again afterward. Supplemental assertions are separate from default totals.
Set `HMA_IKARUS_DIALOG_PREVIEW=/absolute/path.png` to render the offscreen confirmation.

Native Mac save-picker behavior, gameplay and other CFRU/custom projects remain manual checks.
All-map structural comparisons are recorded separately in
[the compatibility review](../IKARUS_GRAPHICS_COMPATIBILITY_REVIEW.md).

## Cerulean trainer validation / sparse learnsets

The real-base matrix also follows Go To into Cerulean (3-3) before saving and
renders its event controls again after save/reopen. A separate opt-in project
review works only on disposable copies:

```sh
dotnet run --project tests/IkarusUpgrade -- --project "/absolute/path/project.gba"
```

The project review reads the adjacent TOML, scans learnset availability, loads
Cerulean and its trainer/rematch controls, selects a block from the palette and
a map tile, and verifies both original files and loaded ROM bytes remain unchanged.
It reports unreadable species rows without embedding a ROM in the repository.

The reported SCADE project contains five null learnset pointers at species indices
706, 1036, 1038, 1218 and 1228. Those pointers match the pre-Ikarus SCADE ROM;
Ikarus did not relocate or overwrite this table. The prior learnset scan threw a
NullReferenceException. The repair omits unavailable rows and retains readable
learnsets and existing trainer move warnings. Synthetic trainer tests cover null,
negative and out-of-range pointers with byte/metadata preservation.
