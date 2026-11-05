# First launch review probe

Run `dotnet run --project tools/FirstLaunchReview` from the repository root.
The read-only application probe prints JSON for startup filenames/address syntax, rejected
ROM metadata, app-settings read failure, default preferences and missing recent files.
It uses temporary files and stub settings, never personal preferences or user ROMs.
Baseline output and the implementation plan are in tests/FIRST_LAUNCH_READINESS_REVIEW.md.
Private startup methods are inspected deliberately for this investigation; automated
preservation regressions belong in the tests suite after fixing confirmed failures.
