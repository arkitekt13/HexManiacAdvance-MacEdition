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

Mouse selection uses headless pointer movement, press and release in the popup's own TopLevel,
rather than directly invoking Button.Click. It checks that press preserves focus and keeps the
popup alive until release; the old focusable suggestion button fails this test before completion.

## Add/Show defaults to Table (November 16)

The group now also invokes the actual WildPokemonViewModel Add/Show commands for all
four encounter types, from Table, Text, Image, Raw Code, Logs and a closed tools panel.
The previous implementation fails the new Table-selection assertion. The repair keeps
navigation and allocation intact, explicitly supplies the Table tool's header address
(needed when Raw/closed tools skipped automatic updates), and selects Table afterward.

All 48 command scenarios render the real TabView and require a visible editable encounter
list without following its pointer. Show preserves every ROM byte, serialized metadata,
saved status and empty Undo history; Add retains its list pointer and default species slots.
Together with the original editing cases, the group passes 464 assertions. Every fixture
is fabricated; no personal project, application preferences or native window is used.

[Table-default verification](table-default-verification.json) records five focused groups /
1,276 assertions, zero failures/skips, and the guarded rebuilt 1.0.0 app with matching
Release assemblies and a verified ad-hoc signature. This is not a new full-suite total.
