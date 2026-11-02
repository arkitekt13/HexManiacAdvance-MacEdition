# Mac regression checks

From any directory, run `scripts/verify-mac.sh` using its repository path. Use `--no-restore`
when dependencies are already restored. The runner discovers every `tests/*/*.csproj`, builds
fresh binaries, executes them in separate processes, and requires one positive assertion count
from each. A build failure, nonzero process exit, missing count, duplicate count, or empty test
set fails the run. The summary reports groups and assertions rather than claiming xUnit cases ran.

These checks use synthetic data and offscreen rendering, not commercial ROMs or native windows.
The original `src/HexManiac.Tests` suite is separate and is not included or claimed as passing;
its dependency restore, platform-specific build steps, runtime, and test discovery still need work.
See individual group READMEs for any manual GUI checks that remain.

New regression groups must target net8.0, have the same project/directory/assembly name, and print
`CHECKS_PASSED:N` only after all checks have finished, with N the positive assertion count.
