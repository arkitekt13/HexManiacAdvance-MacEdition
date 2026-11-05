# Evidence-led maintenance review

Baseline: `0ba21fc8`, November 5, 2026. Branch: `codex/evidence-led-maintenance`.
The initial investigation left application source unchanged; bounded implementations are
recorded separately below. The installed SDK, personal settings and user ROMs remain unchanged.
Evidence and implementations are separate commits.

## Runtime compatibility

Microsoft's isolated installer placed SDK **10.0.403 / runtime 10.0.14** under
`/private/tmp/hma-dotnet10-review`, leaving the normal 8.0.300 SDK intact. A `git archive`
copy under ignored artifacts was mechanically changed to target net10.0 in the Mac app and
59 regression hosts; Core remains net6.0 and no dependency versions or application algorithms
were changed. The existing runner's binary path was adjusted only in that disposable copy.

The Mac app builds successfully under SDK 10; ScriptWorkflowSafety passes **598 assertions**
on runtime 10.0.14, including rejected compilation, correction, source retention, relocation,
persistence and exact history. This is encouraging compatibility evidence, not a completed
migration or native certification. Broader checks follow the deterministic harness work.
The current working app still targets .NET 8. No installed SDK or launch setup was replaced.

Sources: [Microsoft installation/isolation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script),
[Mac requirements](https://learn.microsoft.com/en-us/dotnet/core/install/macos), and
[.NET 10 compatibility changes](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10).
Support ending is a maintenance deadline, not an application expiration mechanism.

## Intermittent focus-animation assertion

The old test manually pulses the application's global MediaContext clock and then drains the
UI dispatcher. Avalonia's MediaContext also drives that same clock with its wall-clock timer.
A deliberate 35-ms dispatcher delay reproduces the original failure at the first observed
tick: the task has already completed and cleared the transform before the test samples it.
See [global-clock trace](maintenance-review/focus-global-delayed.txt).

An investigation-only adapter assigns each outline an independent ClockBase through the
control's internal inherited ClockProperty. The same production FocusOutlineAnimation method
then passes all **13 original assertions** with the same delay, observing intermediate movement,
resizing, completion and cleanup across three animations; see
[local-clock trace](maintenance-review/focus-local-delayed.txt).

Avalonia 11.2.3 primary sources confirm the clock routes:
[MediaContext.Clock](https://github.com/AvaloniaUI/Avalonia/blob/11.2.3/src/Avalonia.Base/Media/MediaContext.Clock.cs),
[Animatable](https://github.com/AvaloniaUI/Avalonia/blob/11.2.3/src/Avalonia.Base/Animation/Animatable.cs),
[Animator](https://github.com/AvaloniaUI/Avalonia/blob/11.2.3/src/Avalonia.Base/Animation/Animators/Animator%601.cs).
The appropriate fix is a deterministic test adapter, with no animation implementation or
assertion relaxation. Native animation performance is not measured by this probe.

## Variable reference omissions

The existing read-only engine-role audit freshly reproduces **13 missed command references
per game identity** across BPRE, BPGE, BPEE, AXVE and AXPE, plus both comparison operands in
conditional map-header types 2 and 4. Summary:
[variable-before-summary.json](maintenance-review/variable-before-summary.json).
The prior [engine investigation](FLAG_USAGE_INVESTIGATION.md) and its primary-source manifest
identify the bounded operand roles, reserve rules and limitations. The known copyvar source
case remains a concrete allocator-suggestion defect, not a hypothetical optimization target.

Implement only those explicitly classified command operands and conditional-header fields,
keeping GetUsedVariables and FindVarUsages consistent. Preserve literal/function-ID operands,
existing native/session reservations, event roots, flags, command widths, numeric parsing,
compiler/ROM/history behavior and allocation ranges. Do not turn this into complete native-code
analysis, a new allocator policy or a cache-freshness rewrite. Scanning remains bounded and
cannot prove that every custom-game ID is unused.

## Helper archive preparation

The verified app bundle exists, but there is no reproducible script producing a tester-facing
ZIP with source/build identification, the original project's LICENSE, installation/reporting
instructions and a portable checksum. This is release preparation, not an asserted app defect.
A small packaging step should use the existing verified publish path, preserve earlier output,
exclude user data and explicitly label ad-hoc signing, architecture and source state. It should
create local artifacts only; publishing a release is separate from preparing one.

## Work justified by this review

1. Commit investigation evidence first.
2. Repair only the clock test harness; retain the real animation and movement assertions.
3. Repair the bounded variable-analysis omissions with failing-before/passing-after tests.
4. Add and verify the local helper ZIP/identity/license/checksum workflow.
5. Run the final Mac suite serially, then exercise the temporary .NET 10 copy and record the
   remaining migration/native-compatibility decision rather than silently changing the runtime.

## Implemented follow-ups and runtime decision

- FocusAnimation now uses independent clocks, retains crash/completion/cleanup controls and
  requires actual sampled movement/resize under both normal and deliberately delayed dispatch.
  Ten independent processes pass 250 assertions; the normal group contains 25 assertions.
- FlagUsageSafety passes 1,098 assertions. Every reviewed command/header operand is found,
  literal/function-ID exclusions and native reserve inventories are retained, and the original
  source-only copyvar case produces a different unused suggestion without changing model state.
  Allocation bounds, snapshot freshness and complete native/custom-engine analysis remain separate.
- MacPackaging passes 43 assertions, including real ZIP encoding of synthetic bundles, original
  license/credits, source identity, dirty labels, checksums, user-data exclusion and failure handling.
- A real arm64 helper archive was built from clean commit 52f917a14385; its complete ZIP CRC,
  SHA-256, expected contents and extracted app's strict codesign verification pass. It contains
  no ROM or emulator preferences and is not a published GitHub release. See
  [archive evidence](maintenance-review/helper-archive-verification.json).

The final isolated .NET 10 trial passes **59 groups / 17,917 assertions**, zero failures or skips,
using SDK 10.0.403/runtime 10.0.14 and unchanged dependency versions. Two C# 14 contextual-keyword
conflicts were reproduced in test fixture property lambdas and fixed by escaping their existing
identifiers; both fixes also pass under .NET 8. A separate experimental-runner mistake initially
hardcoded the SDK path and bypassed VerificationRunner's mock dotnet; selecting the SDK with a
process-local PATH restored that test without changing production code. Earlier stopped trials
are not presented as successful full runs. The final exact counts are in
[net10-compatibility.json](maintenance-review/net10-compatibility.json).

**Decision:** Keep the working application on net8.0 for this change. The results make a future
migration a concrete compatibility project rather than an assumed rewrite, but adoption still
needs native packaged-app and supported-OS checks, tooling/documentation updates and release
validation. This trial changes only a disposable app/test copy and a temporary SDK installation;
it does not establish Intel, older macOS, Gatekeeper, live external-emulator or gameplay coverage.
Tools outside the app/regression trial also need their target frameworks reviewed during an
actual migration. The normal installed SDK remains 8.0.300. No user ROM or HMA preferences changed.

## Final working-runtime validation

`scripts/verify-mac.sh --no-restore` completed serially on SDK 8.0.300/runtime 8.0.5:
**59 groups / 17,917 assertions / zero failures / zero skipped groups**. Every group and
assertion count matches the isolated .NET 10 run; see
[net8-verification.json](maintenance-review/net8-verification.json). This run builds fresh
binaries and uses synthetic data/offscreen rendering. The separate upstream xUnit suites
were not rerun, and native launch, Intel/older-macOS, Gatekeeper and gameplay checks are
not inferred from these results. The real archive identifies the clean implementation
checkpoint; the subsequent evidence commit only adds documentation and result records.
