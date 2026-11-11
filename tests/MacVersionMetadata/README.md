# Mac release identity and existing-project metadata

Synthetic existing-project checks assert that Mac Edition reports 1.0.0 while Core's
assembly and metadata format retain 0.6.1. Opening a current-format project must not
trigger the migration that would occur if the Mac release version were used by mistake.
The actual editor loader preserves byte-exact TOML on first open, all custom named
anchors/formats/lists, ROM bytes and saved/history state. A real edit and normal Save
retain the Core version; Undo/Redo and save/reopen preserve bytes and existing scripts.
Only disposable synthetic project files are used, never the user's tutorial.

Supplemental real-base SCADE and tutorial probes verify their original supplied definitions
on Mac 1.0 separately. MacPackaging checks that bundle/archive versions come from the Mac
app's version file even when SharedAssemblyInfo.cs contains a different Core version.

An optional absolute `.app` path checks the real published Mac assembly, bundled Core
assembly and both Info.plist version fields (three additional checks beyond the default 15).
It reads assembly metadata without launching the app or opening any personal ROM.
