# Mac distribution packaging

Run `dotnet run --project tests/MacPackaging` or the complete Mac suite.
Forty-three assertions execute a disposable copy of the real publish script with fake dotnet and
codesign tools; no actual app is built, signed or launched by this regression group.

The matrix covers invalid architecture/extra arguments, publish failure, missing resources,
missing self-contained runtime, signing failure, verification failure, successful default
arm64 output, Intel output coexistence, repeat failure after success and staging cleanup.
It runs from an unrelated directory with spaces and checks version metadata against a fake
SharedAssemblyInfo, preserving previous bundles on all failing paths. This guards script
control flow; actual architecture, native dependencies and codesigning require the separate
real publish inspection recorded in FIRST_LAUNCH_READINESS_REVIEW.md.

The helper-archive cases exercise package-mac.sh with the same fake build/sign toolchain
and the system ditto ZIP encoder: archive failure cleanup, source/version/architecture identity,
original LICENSE and credits, standalone instructions, bundled resources/runtime, exclusion of
ROM/preferences, portable SHA-256, existing-output preservation and dirty-source labeling.
These cases prepare local files only and never publish a GitHub release.
