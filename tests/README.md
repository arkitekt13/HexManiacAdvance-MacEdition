# Mac regression checks

From any directory, run `scripts/verify-mac.sh` using its repository path. Use `--no-restore`
when dependencies are already restored. The runner discovers every `tests/*/*.csproj`, builds
fresh binaries, executes them in separate processes, and requires one positive assertion count
from each. A build failure, nonzero process exit, missing count, duplicate count, or empty test
set fails the run. The summary reports groups and assertions rather than claiming xUnit cases ran.

These checks use synthetic data and offscreen rendering, not commercial ROMs or native windows.
The original `src/HexManiac.Tests` xUnit suite is separate and is not included in these assertion
counts. Run `scripts/verify-upstream.sh` for that suite, or add `--in-process` when VSTest's
local test-host socket is unavailable. It builds on .NET 8, and the latest in-process baseline
has zero failures with explicit optional-ROM skips; see [UPSTREAM_BASELINE.md](UPSTREAM_BASELINE.md).
The second upstream project, `src/HexManiac.Integration`, now has a portable host; select it
with `--integration` and see [INTEGRATION_BASELINE.md](INTEGRATION_BASELINE.md) for its separate results.
See individual group READMEs for any manual GUI checks that remain.
Production trainer, sprite, palette, and ordinary text-stream template checks are documented
in [StreamTemplates/README.md](StreamTemplates/README.md).

New regression groups must target net8.0, have the same project/directory/assembly name, and print
`CHECKS_PASSED:N` only after all checks have finished, with N the positive assertion count.

Map presentation measurements and synthetic-fixture limits are recorded separately in
[MAP_PERFORMANCE.md](MAP_PERFORMANCE.md); script highlighting results remain in
[SCRIPT_PERFORMANCE.md](SCRIPT_PERFORMANCE.md). Pokémon names/base-stats table preparation
and headless binding/layout findings are in [TABLE_PERFORMANCE.md](TABLE_PERFORMANCE.md).
Trainer stream, portrait, validation, and template-selection findings are in
[TRAINER_PERFORMANCE.md](TRAINER_PERFORMANCE.md).
The portrait-specific cost and image-freshness review is in
[PORTRAIT_PERFORMANCE.md](PORTRAIT_PERFORMANCE.md); live portrait regression coverage is in
[PortraitFreshness/README.md](PortraitFreshness/README.md).
These are component-level measurements,
not native-input or full-editor latency claims.

Item-table preparation, actual-ROM limits, and icon notification findings are recorded in
[ITEM_PERFORMANCE.md](ITEM_PERFORMANCE.md); bound editing, relocation/history, and dimension/scale
regressions are documented in [ItemTablePresentation/README.md](ItemTablePresentation/README.md).

The normal delayed item/map-use search, UI-thread cost, and query-local reuse experiment are
reviewed separately in [ITEM_USAGE_PERFORMANCE.md](ITEM_USAGE_PERFORMANCE.md).

Synthetic query-local search freshness, cancellation, and allocation regressions are in
[MapUsageSearch/README.md](MapUsageSearch/README.md).

Full-editor routed wheel zoom, fresh-tab startup, optional local FireRed checks, and the
unresolved intermittent freeze investigation are in [MapZoom/README.md](MapZoom/README.md).

Local file drag/drop routing and HMA imports from map or ROM tabs are checked in
[FileDrop/README.md](FileDrop/README.md).

Control-drag map selection, repeated stamping, collision-bit preservation, and Command rectangle
fill are checked in [MapSelection/README.md](MapSelection/README.md).

Backward text selection, name replacement, and the reported text-tool crash are checked in
[TextSelection/README.md](TextSelection/README.md).

The script-edit pipeline, deferred compiler/component probes, and sparse variable-alias lookup
investigation are recorded in [SCRIPT_EDIT_PIPELINE.md](SCRIPT_EDIT_PIPELINE.md); reproduction
commands and scope limits are in [../tools/ScriptEditingBenchmark/README.md](../tools/ScriptEditingBenchmark/README.md).

Existing argument precedence, sparse alias lookup freshness, and script expansion/relocation,
shrinking, caller/dialogue preservation, and complete history round-trips are protected in
[ScriptArgumentSafety/README.md](ScriptArgumentSafety/README.md).

Per-compilation argument reuse, unchanged numeric/name interpretation, before/after metrics,
and invalidation/coverage limits are recorded in [SCRIPT_ARGUMENT_LOOKUP_PERFORMANCE.md](SCRIPT_ARGUMENT_LOOKUP_PERFORMANCE.md).

NPC-style 100–500-source-line editing, dialogue/comment changes, core completion cost, and the
separate Mac autocomplete mapping finding are recorded in [NPC_SCRIPT_BENCHMARK.md](NPC_SCRIPT_BENCHMARK.md).

The autocomplete mapping fix, LF/CRLF/mixed offered/applied options, keyboard/mouse dispatch,
caret/selection boundaries, and live compiled-script history are protected in
[AutocompleteMapping/README.md](AutocompleteMapping/README.md).

The existing unused-flag/variable insertion helpers and their synthetic characterization
are documented in [FLAG_VARIABLE_REVIEW.md](FLAG_VARIABLE_REVIEW.md); this is a review tool,
not a regression group requiring the observed limitations to remain.

The follow-up [usage-detection investigation](FLAG_USAGE_INVESTIGATION.md) records primary-engine
cross-checks and read-only FireRed fixture findings; [FlagUsageSafety](FlagUsageSafety/README.md)
protects currently supported detection and literal distinctions without changing the helpers.

[ScriptWorkflowSafety](ScriptWorkflowSafety/README.md) protects settled script correction,
relocation/history and save/reopen; the separate [workflow investigation](SCRIPT_WORKFLOW_INVESTIGATION.md)
records pending-context/persistence and late-resource-error findings without changing the editor.
