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
[ITEM_PERFORMANCE.md](ITEM_PERFORMANCE.md).
