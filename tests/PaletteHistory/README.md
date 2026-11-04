# Palette history and editor freshness

Run `dotnet run --project tests/PaletteHistory`. The pre-fix real-popup regression restored
ROM bytes after Undo while leaving the editable color field at the undone value. Its local
multi-color delta baseline was also retained. The fix closes stale popups when external
refresh replaces palette contents, with an explicit guard for the popup's own synchronous edits.

The 36 assertions run compressed/uncompressed synthetic sprites through real popup entry,
Cmd+Z, Undo/Redo, exact byte/metadata and bound-bitmap snapshots, popup reopen, preserved redo
while inspecting, correction/retry, model replacement, context switching, detached palette
notifications and control detach/reattach. ColorEditorInput separately verifies that continuous
own-field changes do not close the popup or lose focus. No user ROM or clipboard is used.
