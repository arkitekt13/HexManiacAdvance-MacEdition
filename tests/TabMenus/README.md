# Mac tab menus and Finder reveal

Run `scripts/verify-mac.sh --no-restore` to rebuild and execute this group with the Mac suite.
These checks use real MainWindow tab headers/context menus and headless pointer events with
fabricated ROMs/maps. Finder requests are recorded by an injected file-system capability; no
actual Finder window, user file, patch output or emulator is opened.

The group now passes 33 assertions, including the normal UI's hidden/disabled built-in emulator
and Claude controls, absent native accelerators and F6 binding, a real F6 press that opens no
emulator, and retained external emulator toolbar/menu/F5 access. The underlying optional
implementations remain compiled and can be restored through OptionalFeatureVisibility.

Before the fix, the Control-click menu assertion fails although secondary/right-click works.
Coverage includes Control held through release/released early, moving without tab reordering,
the clicked tab's DataContext and Duplicate target, preserved menu entries, ordinary drag reorder,
middle-click close, correct file-reveal target for ROM and map tabs, spaces/quotes in launch
arguments, missing/untitled files and visible reveal failures. ROM bytes/history stay unchanged.

The Mac menu uses `/usr/bin/open -R` through `IFileRevealer` rather than invoking the shared
Windows `explorer.exe` method. The production Mac file system implements that capability;
tests substitute a recorder and separately verify argument construction.

Native check: after relaunch, Control-click and two-finger-click ROM/map tab titles. Confirm
Duplicate targets the clicked tab and Show in Finder selects its owning file. Native Finder
activation and real trackpad input still require this manual confirmation.
