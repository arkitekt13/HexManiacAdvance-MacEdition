# Packaged project save/reopen review (Apple Silicon)

This opt-in release check uses the actual packaged Mac app/Core assemblies and
self-contained runtime, with the real Mac filesystem implementation. It does not
start the normal HMA session, native windows, its IPC server, or your profile.
Model scheduling is controlled with InstantDispatch; native UI timing is outside
this check. No production code changes are needed.

```sh
tools/PackagedProjectReview/run.sh "/path/to/unpacked/HexManiacAdvance.app" \
  "/path/to/clean English FireRed 1.1.gba" \
  "$PWD/artifacts/a-new-empty-review-directory"
```

The exact clean input SHA-256 is checked before any test project or profile is
written. A working hack or mismatched base is rejected; the input is only read.
The output directory must be empty. This tool creates disposable ROM/TOML copies
under that output and a private temporary directory; do not supply a working
project folder. Build requires the SDK, but the driver subsequently runs through
its apphost on the copied package runtime, not the installed shared runtime.

The script verifies the unpacked app signature before making a separate driver
copy. It keeps the original unpacked app unchanged, combines its dependency graph
with the small driver entry, and uses the original self-contained runtime config.
The app/Core assemblies and runtime remain byte-identical to the package. It
launches from an unrelated directory and checks the loaded assembly/runtime paths.

Coverage includes fresh defaults, packaged resource resolution, isolated settings
save/reload, invalid-sidecar refusal without overwriting files, correction/retry,
100 actual Save commands (60 edit/save/close/reopen cycles and 40 continuous-session
saves), growing/shrinking inline dialogue, short-field Undo/Redo, full game metadata,
exact ROM bytes on disk, retained definitions/lists and staging-file cleanup.
Locations include a private temporary directory and the output's Documents/workspace
path with spaces and Unicode. If /Applications/mGBA.app exists, remembered-emulator
preferences are exercised with simulated picker/launch callbacks; no emulator or
native picker is opened. Only owned test preferences are written. The normal HMA
profile is hashed before/after and must remain byte-identical.

The successful November 10 run passes 871 assertions using 16 MiB copies and
approximately 184 KB sidecars with 549 definitions. These are supplemental checks,
not added to the ordinary Mac runner's 89-group / 24,770-assertion total. The tool
lives under tools rather than tests because it requires a supplied commercial
base and is not a default regression group.

This does not certify a clean Mac without an SDK, Gatekeeper/download quarantine,
notarization, OS permissions outside the agent's permitted workspace, the original
native app entry point, Cocoa dialogs, or a native mGBA launch. The Mac was locked
during this review. No save failure was reproduced, which does not close the
intermittent metadata-save issue. See tests/PACKAGED_RELEASE_REVIEW.json for the
measured result, artifact integrity checks and pending native work.
