# Full disposable candidate review

Run only against an output folder created by the experimental compatibility builder:

```sh
dotnet run --project tools/IkarusScadeCandidateReview -- /absolute/repository /absolute/build.noindex /absolute/clean-FireRed-1.0.gba
```

It compares all 425 terrain maps, 60 blockset pairings and 61 standalone previews
against the vanilla/Ikarus donor; checks every SCADE-specific byte and all 7,834
recognized frame ranges; verifies the allocator arena, original metadata, a new
Pallet map's complete palette, normal Save, Undo/Redo and reopening. Event sprites
are excluded from the terrain comparison and checked separately, because SCADE
intentionally expands/changes them. Seven Ikarus legacy frame edits must match the
donor and must have been vanilla bytes before patching.

The tool writes only `roundtrip.gba`, `roundtrip.toml` and `editor-verification.json`
in the supplied disposable build folder. No user preferences or project files are
opened. It retains all failed assertions in the JSON, then exits unsuccessfully if
any fail. New-map creation intentionally increases the map-layout table length;
original metadata is checked before that edit, and saved metadata is checked again
after reopening. Native emulator coverage is recorded separately.
