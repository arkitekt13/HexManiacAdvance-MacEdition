# Script editor layout regression

This check uses the actual Avalonia/Skia text renderer without opening a window or loading a ROM.
It reproduces the zero-width NUL placeholders, then checks the real editor's six display layers,
matching fonts, character positions, comments, line endings, and unchanged editable content.

Run from the repository root after restoring dependencies:

```sh
dotnet build tests/ScriptEditorLayout/ScriptEditorLayout.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1
dotnet tests/ScriptEditorLayout/bin/Debug/net8.0/ScriptEditorLayout.dll
```

Visual checks after restarting the editor: open an NPC script, inspect the arguments and comments,
scroll in both directions, and verify that the caret and selection line up with the colored text.
