# Blockset selection stability

Uses a real MainWindow, MapTab, header bindings and routed dropdown input with three synthetic
maps. A second header refreshes the shared blockset cache, reproducing different option instances
with the same addresses. Before the fix, selecting a row raises the same Avalonia
`ArgumentOutOfRangeException` in SelectedItems enumeration as the user's two crash reports.

Both primary and secondary selectors cover ordinary mouse input, the stale shared-cache case,
reordered and shrinking option caches, a selected choice missing from the shared cache, and
keyboard selection. Checks verify stable collections
during selection, immediate four-byte pointer writes, Save before queued UI work, independent
reload, correct selection by address after UI synchronization, retained previews, exact Undo/Redo
and saved-state behavior. Queued refreshes after Undo/navigation must not reapply edits or update
another map; invalid indices write nothing. Stable option objects must still refresh image content, and an explicit header refresh must
redraw changed graphics at unchanged addresses, including Undo restoration.

```sh
dotnet restore tests/BlocksetSelection/BlocksetSelection.csproj
dotnet build tests/BlocksetSelection/BlocksetSelection.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1
python3 scripts/run-upstream-command.py 90 dotnet tests/BlocksetSelection/bin/Debug/net8.0/BlocksetSelection.dll
```

The default synthetic run reads no user ROM, settings or metadata sidecar and writes no project files. Saves remain in memory.
Native macOS pointer/trackpad and compositor behavior still need a live check on the user's map.

## Optional real-project review

Pass `--project` followed by an absolute ROM path with a matching TOML. This opt-in mode
reads the specified pair, loads the actual Mac controls, navigates between existing maps,
creates a new map in memory and switches its secondary blockset away and back. It checks
visible address text, inherited pointers and exact four-byte writes, and verifies the original
files remain unchanged. All storage writes are stubbed. The block-palette counts are diagnostic:
selection success does not establish compatibility between patches that overlap in ROM space.
