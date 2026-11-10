# Script selection deletion

Build this project and run its DLL, or use `scripts/verify-mac.sh`.
All ROM data and project files are temporary, fabricated WorkflowFixture inputs;
no personal ROM or preferences are used.

The original editor reproduces the reported immediate Backspace failure when a
multiline dialogue selection runs backward (selection anchor after its active
end). The same sample selected forward deletes successfully, and a bare Avalonia
TextBox also deletes it correctly. The reproduction uses msgbox.npc and the
reported dialogue shape, with actual routed Backspace input in MainWindow.

TextEditor forwards selection notifications to script context observers, which
publish the selection anchor as model caret context. The synchronous caret-move
request fed that anchor back into the native TextBox while it was updating its
selection for deletion. The view now ignores that reentrant caret echo only
while reporting its own selection update. Explicit model-driven caret requests
outside the notification remain active. Compiler and Core behavior are unchanged.

132 assertions cover both selection directions, content-only and newline-inclusive
selections, Backspace, Delete and replacement typing; immediate source changes,
deferred compilation, exact bytes/metadata through one Undo and Redo, focus,
no implicit saves, explicit caret requests and read-only source protection.
This is headless input testing rather than a native Cocoa session recording.
