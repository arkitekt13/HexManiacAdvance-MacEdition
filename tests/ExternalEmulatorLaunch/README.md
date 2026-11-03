# External emulator launch

The green triangle, F5 and Run menu use the macOS application chooser on first use and
remember its path in Application Support/HexManiacAdvance/external-emulator.txt. Missing
applications trigger selection again. Launch uses `/usr/bin/open -a` with separate arguments,
without changing Finder associations. F6 remains the built-in emulator.

Run `scripts/verify-mac.sh --no-restore` to rebuild and run this group with the other Mac checks.
The group uses fabricated ROMs, temporary application/preferences and an injected process starter;
it never launches an actual app or touches user preferences. It covers first-use cancellation,
persistence across instances, missing-app cancellation, paths with spaces, pending-script saves,
rejected drafts, save cancellation/failure, nonzero launch results and repeated clicks.
Picker checks cover explicit `choose application ... as alias`, cancellation/error handling,
spaces and the trailing slash in an application alias. The previous Avalonia file-filter picker
displayed installed applications disabled on the user's Mac; this path has no file-type filter.
Apple documents the application list and Browse button in its
[command reference](https://developer.apple.com/library/archive/documentation/AppleScript/Conceptual/AppleScriptLangGuide/reference/ASLR_cmds.html).

Native check: launch the editor, select a saved ROM, click the triangle, select mGBA.app and
confirm mGBA receives the ROM (use Browse if it is absent from the application list). Restart HMA
and check no picker appears. Test Cancel and Save and
Launch with unsaved edits on a disposable ROM. Native picker application-bundle selection and
mGBA's actual response require this manual check; automated tests do not claim native launch.
