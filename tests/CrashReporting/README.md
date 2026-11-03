# Crash reporting regression

Run `scripts/verify-mac.sh`. These checks use synthetic exceptions, a stub filesystem dialog,
and disposable folders; they do not crash the running editor, open Finder or a browser, send
an issue, or require a ROM.

Checks cover the actual app assembly version and shared welcome identity, platform diagnostics,
inner exceptions, reporting before an editor exists, the Mac fork issue URL, bounded browser
URLs, complete clipboard reports, append-only logging, Finder's saved-file target, and truthful
dialog actions after blocked or invalid log paths. A synthetic assertion also checks its version
label without opening the native dialog.

Mac logs are stored at `~/Library/Logs/HexManiacAdvance/crash.log`; other platforms use the
application's local-data Logs folder. Core's metadata version and migration rules are unchanged.
The GitHub action opens a draft; users paste the copied full report after reviewing it.
Native dialog appearance and actual Finder/browser/clipboard interaction remain manual checks.
