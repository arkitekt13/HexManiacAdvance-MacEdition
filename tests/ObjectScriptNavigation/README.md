# Object script navigation state

Run through `scripts/verify-mac.sh` or build and execute this group independently with the
same documented flags. Uses synthetic map/event/script data and a headless Avalonia MainWindow;
no personal ROM, settings or emulator is opened.

On production checkpoint 260e078d, the group fails when assigning a valid script to an object
whose arrow was created while its address was null. The command then reports CanExecute=true,
but the actual button remains disabled. Selecting away/back recreates the correct state,
which confirms the missing refresh rather than a hit-test or pointer-parsing failure.

The regression requires the same existing arrow to become enabled without reselecting the
NPC. Real pointer clicks must deliver the current script address to a captured navigation
callback and preserve ROM bytes. It covers programmatic assignment, text clearing/assignment,
valid-to-valid changes, out-of-ROM addresses and correction, Create Script and NPC templates.
Undo/Redo must restore exact bytes (and initial assignment metadata) and the correct enabled
state when the object panel is reopened. Map history refresh currently clears selection;
the test respects that behavior rather than requiring new selection persistence.

This is actual arrow/command/input coverage; its captured navigation callback does not certify
full tab creation or native focus. Existing map/script regression groups cover surrounding
workflows, with native real-ROM use remaining a manual check. Final group: 51 assertions.

[Focused verification](verification.json): 11 freshly built groups / 1,861 assertions,
zero failures or skips. The complete Mac and upstream suites were not rerun for this
two-notification repair.
