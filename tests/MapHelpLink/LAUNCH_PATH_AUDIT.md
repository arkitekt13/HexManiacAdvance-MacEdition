# Mac launch-path audit — 2026-11-10

Scope: local call sites that open a browser, Finder, a file, or the external
emulator. No project ROM or preferences were opened or changed. The known
intermittent metadata-save denial remains unresolved; this audit does not change
AtomicFileWriter or treat the missing cwd as its proven cause.

| Path | Finding and action |
| --- | --- |
| Map encounter More Info | Already used the guarded web helper; retained its upstream URL and error reporting. |
| Welcome-screen credits, About license, quick-edit wiki, built-in core help | Direct/legacy launches depended on the parent's cwd or lacked failure containment. Route through the existing web helper, with an inline/status error on failure. |
| Main Help menu, update notice, assertion report | Route through the Mac filesystem launcher, which now resolves the system open executable absolutely and catches expected launch errors. Stub-backed release-link tests retain the exact fork URL. |
| Go To and hex documentation links in Core | Their shared NativeProcess single-argument builder now uses /usr/bin/open and / on macOS. Windows/Linux shell-launch behavior and the executable-with-arguments overload remain unchanged. |
| Default file opens and scripts folder | Native open receives a separate target argument; application-relative resources resolve against AppContext.BaseDirectory. |
| Finder reveal, including dialog actions | Reuse the absolute open builder, pass -R and the path separately, set / as the child cwd, and handle dialog-action failures. |
| External emulator and application chooser | Executables were already absolute; explicitly set / rather than inheriting the old app directory. Selection, arguments, preference and ROM workflows are unchanged. |
| Hidden Claude CLI backend | Already has an explicit Application Support working directory; left unchanged. This does not re-enable the integration. |

The shared default-open builder preserves filenames containing spaces and quotes
as single arguments. Web-help validation still limits its helper to http/https.
Known UI links do not pass shell-parsed URL strings. Generic executable launches
retain their arguments and use the application base as their working directory;
this audit is not a generic command-execution redesign.

Focused verification passes MapHelpLink 31, ExternalEmulatorLaunch 35, TabMenus
34, UpdateNotice 14 and CrashReporting 31: five groups / 145 assertions, zero
failures or skips and zero build warnings/errors. MapHelpLink exercises default
file opens, Finder, emulator, chooser and Core documentation configurations from
a genuinely removed parent cwd, substituting harmless true processes so no browser
or app is launched. It also retains error/exit-code and URI-argument checks.
Native browser, Finder and chooser UI confirmation remains manual; individual
external handlers are not claimed to have been clicked in a real Cocoa session.
