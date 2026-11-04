# Text-tool selection and rename crash regression

The reported crash contains `ArgumentOutOfRangeException` from a negative substring length in
Core PCSTool.UpdateSelectionFromTool, reached through TabView.StringToolContentSelectionChanged
when clicking or typing in the text tool. Avalonia's SelectionStart/SelectionEnd retain direction;
WPF's SelectionStart/SelectionLength provided a sorted start and nonnegative length.

This group uses production TabView and its bound TextBox with fabricated encoded PCS text and
a two-row fixed-width name table. The unmodified UI bridge reproduces the same exception with
a backward selection (`length=-7`); the expanded group passes 63 checks.

Additional cases type one character at a time in PCS/fixed names and the real `data.maps.names`
pointer-table format. Map-name stream edits retain the live TextBox/focus, encode the expected
text, preserve the other entry/pointers, restore complete bytes/metadata with Undo/Redo and refresh
correctly when navigating between entries. Map header dropdown selection is not covered here.

```sh
dotnet build tests/TextSelection/TextSelection.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 60 dotnet tests/TextSelection/bin/Debug/net8.0/TextSelection.dll
```

Restore dependencies first. The normal Mac runner automatically includes the group.

Checks repeated backward/forward selections and collapsed carets, unchanged bytes/history and
metadata during selection, keyboard replacement that shortens the name, bounded caret state,
preservation of the neighboring table name, exact byte/metadata undo, redo, and subsequent typing.
Fixture construction uses the existing formatter, then clears setup history to establish a clean
loaded-ROM baseline before user input; no assertions or application behavior are bypassed.

The fix normalizes and bounds selection ranges at the Avalonia UI boundary, guards feedback,
and restores subscriptions/guards in finally blocks. Core encoding, relocation, pointer updates,
and history algorithms are unchanged. This reproduces the logged text-tool failure without
requiring a commercial ROM or creating a new game map; native interaction on the user's map
still needs a live retry after relaunch.
