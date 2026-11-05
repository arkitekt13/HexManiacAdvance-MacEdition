# Helper distribution readiness investigation

Baseline: `8dbe5f2b`, branch `codex/helper-release-readiness`, November 4, 2026 (local).
This investigation precedes application changes. No personal preferences, user ROMs or
sidecars were changed. All probes use temporary synthetic data and stub settings; the
original Mac port notes remain historical evidence rather than current instructions.

## Scope and evidence

- Inspected App startup, argv/single-instance/Finder document routing, MacFileSystem,
  EditorViewModel opening/settings/history, the welcome UI, external emulator selection,
  update checks, publish targets/script and current GitHub information.
- Built and ran FirstLaunchReview against unchanged application code; raw observations are
  [startup-before.json](release-readiness/startup-before.json).
- Six existing groups freshly pass 752 assertions: WelcomeScreen 33, StartupUpdates 42,
  ExternalEmulatorLaunch 24, SaveSafety 17, ScriptWorkflowSafety 598 and ExternalFileChanges 38.
- Existing default settings yield zero tabs/recent files; missing stored recent paths are
  filtered. Existing emulator checks cover selection cancellation, persisted choice, moved
  applications, paths with spaces, Save and Launch, invalid source, failed saves and duplicate clicks.
- A real no-restore arm64 publish fails NETSDK1047 because this configuration has not restored
  runtime-specific assets; this is a prerequisite, not evidence that a normal restored publish fails.

## Confirmed findings and implementation boundaries

| Finding | Evidence | Proposed change |
| --- | --- | --- |
| Startup filenames are truncated | `/tmp/Tour.gba backup.gba` becomes `/tmp/Tour.gba`; `/tmp/Tour: Village.gba` becomes `/tmp/Tour`. | Preserve literal existing paths and only consume successfully parsed address suffixes; retain both historical address forms and options. |
| Rejected ROM metadata can produce a startup crash | Missing anchor Name sets Editor.ErrorMessage with no tabs, then App.TryOpenFile indexes -1 and throws ArgumentOutOfRangeException. | Await an explicit open result, never index a missing tab, retain errors, and bind optional address navigation to the actual newly opened live tab. |
| Settings access can abort startup | Actual ReadAppMetadata on a temporary chmod-000 file throws UnauthorizedAccessException; startup creates the editor before installing crash hooks. | Use defaults on app-settings I/O failure, expose a nonmodal warning and prohibit overwriting unread settings; make settings-write failure nonfatal. |
| ROM metadata permission/parse failures need a safe boundary | MetadataFor reads without catching UnauthorizedAccessException; StoredMetadata uses int.Parse while ExecuteOpen handles only null/range validation exceptions. | Surface metadata failure without treating unread metadata as absent, never overwrite it, and permit retry after correction. |
| Packaging depends on caller directory and deletes old output first | Calling the real publish script from a temporary directory with `invalid-rid` exits MSB1009 and deletes that directory's `artifacts/mac/keep.txt`. | Resolve repository-relative paths, validate architecture before mutation, stage/sign before publication and preserve prior output on failure. |
| Bundle metadata comes from a Git tag, not application assembly version | Script uses git describe while the app uses SharedAssemblyInfo; minimum OS is advertised as 11 although .NET 8 documentation established a later floor. | Match application version, document the runtime floor separately from tested/supported OS coverage, and verify self-contained resources/architecture/signature. |
| GitHub guidance is stale | README directs users to upstream Windows releases, claims Core is unmodified and promotes disabled controls; repository About repeats these claims. | Rewrite the Mac-facing README and About, keep Haven's wiki/general references prominent, explain source builds and actual shipped controls. |
| Downloads/reporting are not ready | GitHub REST reports zero releases, has_issues=false and has_discussions=false. | Do not promise downloadable assets; document helper reporting and record issue-enablement as a distribution prerequisite. |
| Original porter attribution has a broken target | Commit author is xavierkeys, but profile/repository lookups for xavierkeys fail; fork parent is verified `Xkeys-bot/HexManiacAdvance-mac`. | Thank xavierkeys and link the verified original Mac port; retain Haven credit/tutorial links and identify arkitekt13 only as maintainer. |

## GitHub and platform references checked

- [This repository metadata](https://api.github.com/repos/arkitekt13/HexManiacAdvance-MacEdition)
  identifies the parent as [the original Mac port](https://github.com/Xkeys-bot/HexManiacAdvance-mac).
- [Mac Edition releases](https://api.github.com/repos/arkitekt13/HexManiacAdvance-MacEdition/releases)
  returned an empty array; description still advertises the built-in emulator/MCP and unchanged Core.
- [Haven's original HMA](https://github.com/haven1433/HexManiacAdvance) and
  [wiki](https://github.com/haven1433/HexManiacAdvance/wiki) remain the general reference.
- [.NET macOS installation](https://learn.microsoft.com/en-us/dotnet/core/install/macos) and
  [OS policy](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md)
  distinguish minimum runtime prerequisites from currently supported Apple versions.
- [.NET 8/9 end of support](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/)
  is November 10, 2026; a supported runtime upgrade needs a separate compatibility effort before
  public release, rather than an untested framework/package migration in this audit.

## Implementation and verification sequence

1. Commit this evidence and reproducible probe separately from production changes.
2. Add regressions, then fix explicit startup paths/open-result handling and metadata failures;
   preserve successful opening, cancellation, source protection, metadata and history behavior.
3. Isolate app settings for tests; fix read/write failures without touching personal preferences.
4. Harden packaging and test failure paths with disposable fake toolchains, then attempt a real
   self-contained arm64 build and inspect its resources, version, architecture and signature.
5. Rewrite Mac-facing setup/features/credits/help and helper checklist; update repository About.
6. Run the complete Mac suite serially once final application/test changes are stable; commit
   each independent implementation with its own changelog entry and validation.

## Explicit limits / deferred work

Headless results do not certify native Finder cold-start event timing, Gatekeeper/notarization,
an Intel Mac, older macOS releases, a second user's machine or gameplay. Do not change a live
ROM to prove these paths. Test native pickers/mGBA only when a tester can safely do so; retain
existing launcher behavior unless a failure is reproduced. Do not enable optional assistant or
embedded-emulator UI, change numeric/compiler/game-engine rules or undertake runtime migration
as a side effect. Signing/notarization credentials and release assets are not fabricated.

## Packaging implementation verification

The staged packager builds successfully with a normal restored `bash publish-mac.sh osx-arm64`.
The real bundle executable is Mach-O arm64, `codesign --verify --deep --strict` succeeds,
CFBundleShortVersionString is 0.6.1 (matching the assembly's 0.6.1.0), minimum runtime OS is
12.0 and the bundle occupies about 145 MiB on this host. Architecture-specific outputs now
live under `artifacts/mac/osx-arm64` and `artifacts/mac/osx-x64`.

PublishedBundleReview resolves assemblies from that real bundle, not a project reference,
and passes seven checks from an unrelated directory: empty startup, no errors, resource-directory
resolution, populated script references, isolated preference save/reload and bundled runtime
libraries. It finds 61 resource files. This is a managed-loader probe, not a native UI launch
or a claim of testing without .NET installed. MacPackaging separately passes 30 assertions
with a fake toolchain for failure preservation, complete-resource/runtime gates, signature
verification, architecture coexistence, output location and application-derived versioning.
