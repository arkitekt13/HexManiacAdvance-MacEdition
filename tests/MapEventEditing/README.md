# Map event keyboard editing

Run `dotnet run --project tests/MapEventEditing/MapEventEditing.csproj` or the complete
`scripts/verify-mac.sh --no-restore` suite from the repository root.

These checks host the real MainWindow, MapTab and Script Event AngleTextBox template with
a fabricated map/event table. Before the fix, both routed Backspace and forward Delete in
the focused Script Address textbox removed the event, reducing scriptCount from 1 to 0.

The checks cover character deletion, no-op deletion at field boundaries, caret arrows,
Escape, stable editor/focus, unchanged event count, pointer, bytes and metadata when deleting
redundant address zeroes, and no map movement during text editing. A second event field is
also exercised. Clearing the address preserves the existing null-pointer parsing behavior
and remains undoable. With map focus, both deletion keys still remove events with exact-byte
and metadata Undo/Redo; Escape cancels selection and arrow keys still pan.

This is headless routed-key coverage with synthetic data. After relaunch, manually confirm
the Mac keyboard's Delete key edits a Script Address character, Fn+Delete edits forward,
and clicking the map restores normal event shortcuts. No user ROM is used by the tests.
