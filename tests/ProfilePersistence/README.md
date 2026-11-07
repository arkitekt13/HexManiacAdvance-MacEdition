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

The repair passes 28 assertions. New profiles mark escaped arrays with
RecentFilesFormat = Json; unmarked profiles retain literal backslashes and quoted
commas/brackets. Saving an older profile migrates its exact paths. Malformed/null/
missing recent entries remain safe and leave unrelated preferences intact.

The final group passes 32 assertions after adding independent diff/search-limit
round trips. The existing search-limit reader used the diff key; the repair reads
its own key and retains the original parsing, bounds and missing/invalid defaults.
