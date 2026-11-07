# Isolated startup and preference persistence

Uses temporary profile directories and disposable synthetic files only. No personal
Application Support directory, ROM, native picker, emulator process or network is
used. The real MacFileSystem receives an injected profile root.

The baseline saves five existing paths, then loses entries when the old RecentFiles
parser splits on brackets/commas inside names. The regression covers exact path/menu
reopening, quotes/apostrophes/Unicode, legacy Windows/literal paths, remembered
Map View/grid/assistance/zoom and emulator preferences across new instances.

Window dimensions currently use the safe 1200×800 defaults; explicit window-bound
persistence is not implemented and is not added by this audit. Native restored-window
behavior and actual emulator responses remain user/helper checks.
