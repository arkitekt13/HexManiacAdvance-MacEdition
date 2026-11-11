# Native packaged review host (investigation prototype)

This opt-in investigation host opens the packaged MainWindow, native MacFileSystem
and real UI dispatcher with a separate profile. It is not a production entry point
or a default regression group. Never use a personal project with this host.

The November 10 run used a copy of the unpacked `5179545e` ARM64 app under an
ignored artifacts directory. Build this project with `BundleBinaries` pointing to
that copy's `Contents/MacOS`. The executable, managed host files and runtime config
must live alongside the packaged assemblies; its dependency manifest must include
the packaged app's dependencies. The copied bundle's executable/name/identifier
were changed to `NativePackagedReview` / `HMA Native Review` /
`io.github.hexmaniacadvance.native-review`, then signed ad hoc. This preparation
was manual; there is no supported automatic launcher yet. Do not replace a bundle
while it is running.

The host stores settings and emulator selection in `isolated profile` beside the
copied `.app`. Production assemblies remain unchanged. The test explicitly bypasses
ordinary App desktop startup, IPC and update hooks, so it cannot certify those.
It also lacks a desktop application lifetime: the standard Quit action did not
exit the review host. The saved disposable session was ended by terminating only
its verified process. Consequently normal application Quit/profile persistence
and full restart were not verified by this run. Do not interpret this host
limitation as a production Quit defect.

See `tests/NATIVE_RELEASE_REVIEW.json` for manual observations, disk verification,
the confirmed chooser failure, and remaining release checks. Manual observations
are not automated assertions and must not be added to regression totals.
