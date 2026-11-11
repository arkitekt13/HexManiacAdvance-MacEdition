# External emulator launch

The green triangle, F5 and Run menu use a direct native file picker on first use and
remember its path in Application Support/HexManiacAdvance/external-emulator.txt. Missing
applications trigger selection again. Launch uses `/usr/bin/open -a` with separate arguments,
without changing Finder associations. The built-in emulator UI/F6 shortcut is currently disabled.

Run `scripts/verify-mac.sh --no-restore` to rebuild and run this group with the other Mac checks.
The group uses fabricated ROMs, temporary application/preferences and an injected process starter;
it never launches an actual app or touches user preferences. It covers first-use cancellation,
persistence across instances, missing-app cancellation, paths with spaces, pending-script saves,
rejected drafts, save cancellation/failure, nonzero launch results and repeated clicks.
Picker checks cover direct selection starting in Applications, opaque application bundles,
no file-type filter, cancellation/error handling, spaces and trailing-slash normalization.
Ordinary files, bundle contents and folders are rejected before preference writes or launch.
Both the original filtered picker and the subsequent registered-application chooser left mGBA
disabled on the user's Mac. The direct picker bypasses both application-list and type filtering.
Apple documents `choose file` and package handling in its
[command reference](https://developer.apple.com/library/archive/documentation/AppleScript/Conceptual/AppleScriptLangGuide/reference/ASLR_cmds.html).

Native check: launch the editor, select a saved ROM, click the triangle, select mGBA.app and
confirm mGBA receives the ROM. Also try Cancel and selecting an ordinary file: neither should
launch or overwrite the remembered emulator. Restart HMA
and check no picker appears. Test Cancel and Save and
Launch with unsaved edits on a disposable ROM. Native picker application-bundle selection and
mGBA's actual response require this manual check; automated tests do not claim native launch.

The helper-readiness extension brings this group to 31 assertions on macOS: preference write
failure, a removed app while the picker is open, recovery after a launch exception, unreadable
preferences and closing the selected ROM during selection all remain safe without a launcher
implementation change. Tests still inject every launch and never start a native emulator.

The [November 10 native repair verification](NATIVE_PICKER_VERIFICATION.json) confirms the user
could select mGBA in the direct picker. Desktop observation verified the exact disposable ROM
opened in mGBA and a restarted isolated host reused the saved choice without another picker.
The current focused group passes 39 assertions; native observations are separate.
