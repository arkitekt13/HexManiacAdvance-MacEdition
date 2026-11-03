# Local file-drop and HMA import regression

The production MainWindow receives routed file drag/drop events in a headless Avalonia window.
Fabricated binary data and small HMA scripts exercise the actual Editor.Open/ViewPort.TryImport
parser; no commercial ROM, native Finder operation, user settings, or real file writes are needed.

```sh
dotnet build tests/FileDrop/FileDrop.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 60 dotnet tests/FileDrop/bin/Debug/net8.0/FileDrop.dll
```

Restore dependencies first. `scripts/verify-mac.sh` also discovers this group automatically.
The test's file-system stub reads copies from a dictionary. Avalonia prevents applications from
implementing IStorageFile, so the test constructs the framework's internal BclStorageFile wrapper
through reflection against the pinned 11.2.3 dependency to supply real local-file URI drag data.

The 24 checks cover:

- Copy feedback and copy drop results, including a child rejecting drag-over or handling drop.
- Case-insensitive `.HMA` handling and paths containing spaces.
- A map drop selecting its owning ROM, with another open ROM left unchanged.
- Real HMA byte edits at an explicit fabricated address, without creating a binary script tab.
- Repeated imports, separate undo/redo boundaries, preserved earlier unsaved edits, and metadata.
- Visible rejection without a ROM, from an accidentally opened HMA tab, or during a busy update.
- An unreadable script preserving the selected map and data.
- Unchanged ordinary binary-file opening, text-drag handling, and source-script contents.

The original handlers fail the same test by opening a dropped HMA as binary data when no ROM is
selected. Source review also shows the map-specific failure: MapEditorViewModel is not an editable
viewport and its TryImport returns false, so the old generic Open route could create another tab.
The corrected UI route selects that map's underlying ROM before using the established importer.

Import boundaries call the existing ChangeHistory.ChangeCompleted method; history is never cleared.
A busy update is rejected with a message rather than queued. The Core script parser, game/version
guards, relative-asset path context, repointing rules, and script error behavior remain unchanged.

These tests establish input routing and simple-script imports; they do not establish compatibility
of an unprovided patch with a particular game revision or exercise native Finder drag negotiation.
After relaunch, drop an appropriate HMA onto the ROM or its map tab; the ROM tab should become
selected and display edits/errors. Saving remains the user's action.
