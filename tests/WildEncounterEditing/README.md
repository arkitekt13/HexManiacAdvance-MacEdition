# Wild encounter editing

Run `scripts/verify-mac.sh --no-restore` to rebuild and execute this group with the Mac suite.
The fixture uses the real TabView, generic stream TextBox and autocomplete overlay in a headless
Skia window. All ROM bytes, species names, pointers and encounter slots are fabricated.

The matrix uses the production grass (12 slots with percentage comments), surf (5), tree/rock-smash
(5) and fishing (10 with rod comments) list formats from tableReference.txt, substituting a fabricated
species-name table. Each runs with LF and CRLF input and retains every unedited slot.

The first level-edit assertion fails before the fix: refreshing the retained stream group emits
CollectionChanged.Replace with identical old/new group references. Avalonia destroys the focused
TextBox, detaches it and hides autocomplete even though the stream view model was retained.

Checks cover live control identity, keyboard focus, caret progression for each typed character,
species-prefix suggestions, mouse and Enter acceptance, LF/CRLF input, exact slot bytes, other
slots/rate/pointers/metadata, Undo/Redo and navigation refresh. InstantDispatch makes model refresh
deterministic; native keystroke timing and a real-game fixture are not claimed.
