# Map encounter help link

Build this project and execute its DLL, or use `scripts/verify-mac.sh`.

The reported More Info crash comes from MapTab.Navigate launching a relative
`open` executable after the process's current directory has disappeared. HMA sets
its current directory inside the app bundle; replacing that bundle during a build
can remove the old directory while a running process still refers to it. The
stack trace establishes the missing-directory failure; bundle replacement is the
likely trigger, not proof of the user's exact session sequence.

The regression removes only its own temporary current directory and reproduces
the old relative-launch GetCwd/FileNotFoundException. It then exercises the repaired
process launch with an absolute executable and `/` working directory. A harmless
`true` process replaces `open`, so the test never opens a browser. Other checks
cover exact URL argument preservation, invalid schemes, missing processes,
launch exceptions, nonzero exits and stderr diagnostics: 19 assertions.

MapTab now awaits that launcher and reports failures through the originating
viewport's normal error notification. It retains the original upstream legendary
encounter documentation URL. This is a narrow help-button repair; script
recognition, encounter guidance, compilation, ROM data and history are unchanged.
No personal ROM, settings or fixtures are opened or changed. Native browser
opening from the More Info button remains a manual confirmation.

The [follow-up launch audit](LAUNCH_PATH_AUDIT.md) extends this to other browser,
Finder, emulator and chooser paths. The expanded group passes 31 assertions,
including real harmless process launches from a removed parent cwd for those
configurations. No external application or browser opens during the checks.
