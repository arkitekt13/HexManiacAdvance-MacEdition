# Mac Edition release checklist

Use this to prepare a private helper build first, then a public release. An unchecked
item is work or missing evidence—not proof of a bug. Record the build commit, date,
Mac architecture, macOS version and result beside each completed native check.
Use disposable ROM copies with their matching `.toml` files; keep personal settings intact.

## What is already verified

These results apply to the recorded checkpoints, not automatically to a later release candidate.

- [x] Complete Mac regression run: **62 groups / 18,164 assertions / zero failures or skipped groups**.
  [Exact results](../tests/optional-release-assets/mac-verification.json), November 5, 2026.
- [x] Downloaded source builds on the existing Apple Silicon development Mac; ARM64 helper
  packaging, ZIP integrity and extracted ad-hoc signature checks pass.
  [Installation rehearsal](../tests/INSTALLATION_REHEARSAL.md), baseline `6c64f826`.
- [x] Published managed startup passes seven isolated-profile/resource checks; this did **not**
  launch a native window or test a machine without .NET.
- [x] Optional welcome-image and tutorial-patch frameworks are implemented; PNG/JSON/UPS publish
  copying and synthetic tutorial protections pass. Final assets are not included yet.
- [x] Mac setup, helper instructions, original-project credits and Haven's tutorial links exist.

## 1. Before sending a private helper build

- [ ] Choose a candidate commit and build from a clean checkout; keep its build identification
  with the tester ZIP. Do not label a dirty or older package as that candidate.
- [ ] Run the [Mac regressions](DEVELOPMENT.md#run-regression-checks) against the candidate and
  retain the exact totals. Rerun after application/test changes, rather than reusing old results.
- [ ] Build with `./package-mac.sh osx-arm64`; verify its `.zip.sha256`, extracted signature,
  resources and runtime. Inspect the actual archive for accidental ROMs or personal files.
- [ ] Launch that packaged candidate locally from Applications and do a short native smoke test:
  Open ROM, edit, Undo/Redo, Save, close/reopen, map navigation and external emulator launch.
- [ ] Include [helper instructions](MAC_HELPER_TESTING.md), build/architecture identification,
  LICENSE and applicable third-party notices. Explain the actual signing status and installation steps.
- [ ] Identify an Apple Silicon helper and provide a small, specific test assignment. Intel and
  older-macOS coverage may remain pending in an explicitly limited private test round.

## 2. Native checks for helpers

Use the **downloaded candidate ZIP**, not only a developer's source build. Record failures with
reproduction steps; do not mark an untested path as passing because a headless test covers it.

- [ ] On another Mac without an installed .NET SDK/runtime, extract the archive, move the app
  to Applications and cold-launch it. Confirm the supplied runtime is sufficient.
- [ ] Record macOS's actual download/quarantine behavior and verify the documented installation
  instructions work for that signing state. Do not disable system-wide protections.
- [ ] With no ROM open, check welcome layout, Open/cancel, credits, help and Mac Edition issue links.
- [ ] Test Finder Open With and file drops with the app closed and already running, including a
  ROM path containing spaces. Confirm the intended document opens without an unwanted second instance.
- [ ] Check native Open/Save As cancellation and destinations; save/reopen the `.gba` and its
  matching metadata. Confirm a rejected script draft remains visible and cannot silently be saved.
- [ ] Select mGBA through Play/Browse, cancel once, relaunch and confirm the selection is remembered;
  test Save and Launch on a disposable edit.
- [ ] Exercise script Command-C/V, completion by typing and selecting suggestions, `<auto>` blocks,
  dialogue overflow indicators, correction/retry and Undo/Redo through a successful script edit.
- [ ] Edit wild encounters and short fields with keyboard and mouse suggestions; check focus,
  Tab selection and Delete inside a script-address field.
- [ ] Use trackpad Option-drag panning and zoom controls; check warp context menus, NPC sprite
  selection, both blocksets and border editing. Watch previews while moving the pointer and switching windows.
- [ ] Export the same map PNG twice and verify Overwrite, Save Updated Copy and Cancel.
- [ ] Verify Help/issue destinations and crash-report copy/reveal behavior using a controlled
  test or naturally occurring report; do not deliberately crash a session with unsaved work.
- [ ] Retry the reported `.hma` patch on a disposable **matching-revision** ROM and record whether
  the intended bytes change. This remains an unverified report, not a confirmed port defect.

For each supported platform claim, keep a row here:

| Candidate commit | Architecture | macOS version | Clean/no-.NET launch | Native smoke result | Tester/date |
| --- | --- | --- | --- | --- | --- |
| Pending | Apple Silicon | Pending | Pending | Pending | Pending |
| Pending, only if shipping Intel | Intel | Pending | Pending | Pending | Pending |

## 3. Before a public release

These are release decisions or verification gates. They do not mean the current app is known to fail.

- [ ] Resolve the separately tracked supported-runtime upgrade and validate compatibility before
  packaging the public candidate. The rehearsal bundled **8.0.5**; do not treat that historical
  runtime as approved for release. Update source-install instructions to match the chosen runtime.
- [ ] Complete the clean-machine and native smoke checks above on the actual candidate; fix
  reproduced data-loss, crash or installation failures before calling that platform supported.
- [ ] State the architectures and macOS versions actually verified. Ship ARM64 alone if Intel
  remains untested; build/test Intel on Intel hardware before advertising an Intel release.
  A declared minimum OS is not evidence of a tested support range.
- [ ] Decide Developer ID signing/notarization versus explicitly documented unsigned/ad-hoc
  distribution. Test the chosen download/install experience; do not describe an ad-hoc build as notarized.
- [ ] Run the separate [upstream unit](../tests/UPSTREAM_BASELINE.md) and
  [integration](../tests/INTEGRATION_BASELINE.md) checks for the candidate, recording passes,
  failures and optional-ROM skips separately from Mac assertion groups. Do not call skipped
  ROM/gameplay cases verified.
- [ ] Review open helper findings and triage them: release blocker, documented limitation or later
  improvement. Track actual reproduced failures rather than expanding scope speculatively.
- [ ] Set a distinct Mac Edition release identifier and verify the app, archive, release notes and
  build information agree. Keep source commit and architecture visible to bug reporters.
- [ ] Prepare architecture-labeled ZIPs/checksums, LICENSE/notices, concise release notes,
  supported-platform details and useful bug-report instructions. Upload only the final verified archives.
- [ ] After publishing, test the release downloads and update README Installation to point to the
  real assets. Review GitHub About, Help, welcome and crash-report links for the Mac Edition repository;
  retain clear thanks to Haven and xavierkeys and links to Haven's general tutorials.

## 4. Optional launch polish—does not block helper testing

- [ ] Add the final [welcome background PNG](../OptionalAssets/Welcome/README.md); inspect
  cropping, fade, readability and small-window layout in the packaged app.
- [ ] Finish the tutorial village and create the [UPS/manifest assets](../OptionalAssets/Tutorial/README.md)
  from the exact unmodified English FireRed 1.1 base; record all required hashes.
- [ ] If shipping the tutorial, play through the tour on the generated ROM, verify scripts/warps/
  encounters and save progress, then check correct-base creation, wrong-base rejection, cancellation
  and existing-file protection through the native menu. Ship no base or patched ROM.
- [ ] If final assets are absent, confirm the plain welcome screen and explanatory disabled
  tutorial action remain usable. The tutorial and background can arrive in a later release.
- [ ] Polish the user guide and release accomplishments document when the final feature set is settled.

## 5. Keep deferred unless evidence changes the priority

- Broader image-preview/compositor coverage, including multiple displays: gather native evidence
  before changing additional controls.
- Grouped-field editing and more scripting templates/guides: useful future work, without a newly
  demonstrated release blocker.
- HMA-016's deliberate Core-only negative permission state: retain the documented boundary;
  no ordinary Mac UI trigger was found.
- Expanded flag/variable range, native-code and custom-engine analysis: current bounded checks
  do not establish universal unused-ID safety; no blanket guarantee should appear in release notes.

See [FutureDevelopment](../FutureDevelopment.md) for investigation details and completed fixes.
