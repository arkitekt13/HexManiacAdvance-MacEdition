# Map preparation thread ownership

Run `dotnet run --project tests/MapThreadSafety/MapThreadSafety.csproj` or the complete Mac suite.

The fixture has a real NPC object whose generic script references unformatted PCS text. A cold
PixelImage render discovers that text and records pointer/string metadata through normal history.
A real Save button is bound to EditorViewModel.Save, reproducing the user's exact invalid-thread
stack before the fix: Task.Run -> map pixels -> NPC discovery -> history -> Save.CanExecuteChanged
-> Avalonia Button access on a worker thread. The regression collects dispatcher exceptions and
requires both a completed bitmap and no exception; merely suppressing the report cannot pass.

Checks retain metadata-only discovery semantics and unchanged ROM bytes, require all Save/UI
notifications on the UI thread, coalesce update bursts, reject queued work for abandoned/null
contexts and detached images, resume safely on reattachment, and preserve painting Undo/Redo.
Worker refresh requests are marshalled to the UI thread. A cold 96x64 NPC map checks that the
context setter defers preparation and higher-priority UI work runs before rendering.

The renderer now queues/coalesces live map preparation on the UI thread. It does not treat lazy
model access as pure background pixel work. This avoids metadata/history/model races, rather
than just moving the Save notification or ignoring the exception. Any future parallelization
must use an immutable prepared snapshot.

Reported timings are synthetic measurements, not latency promises for every real map. After
relaunch, manually reopen a ROM and enter the map editor, then check NPCs, zoom and painting.
The automated check does not open or modify user ROMs.
