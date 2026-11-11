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

This builds bundled HUBOL and SCADE with their patch checksums enforced, opens each normally,
adds a disposable unsaved edit, backs up, applies and checks exact expected bytes. It checks
fresh map snapshots and an already-initialized Map Editor, data-tab Undo/Redo and earlier
history, then performs actual Save/reopen with every named definition retained. The supplied
original is hashed again afterward. Supplemental assertions are separate from default totals.
Set `HMA_IKARUS_DIALOG_PREVIEW=/absolute/path.png` to render the offscreen confirmation.

Native Mac save-picker behavior, gameplay and other CFRU/custom projects remain manual checks.
All-map structural comparisons are recorded separately in
[the compatibility review](../IKARUS_GRAPHICS_COMPATIBILITY_REVIEW.md).
