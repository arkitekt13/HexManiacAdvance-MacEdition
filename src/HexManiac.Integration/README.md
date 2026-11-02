# Upstream integration tests on Mac

This is the second upstream xUnit project, separate from `HexManiac.Tests`.
Its host targets .NET 8 with the existing package versions. Project-relative references,
transitive portable resource items, and the shared test-environment initializer replace
Windows post-build copy/delete commands. Core and game definitions are unchanged.

Run from the repository root:

```sh
scripts/verify-upstream.sh --integration --in-process --no-restore
```

Omit `--no-restore` to restore dependencies; `--source SOURCE` and
`--configuration Debug|Release` work as for the first project. Without `--in-process`,
the selected integration project runs through VSTest (not verified in this restricted session).
Both projects share the in-process host's build output but are discovered and run separately.
The default command without `--integration` continues to select the first project.

Optional game fixtures are `sampleFiles/Pokemon FireRed.gba` and
`sampleFiles/Pokemon Emerald.gba` below the selected host output directory. The harness
does not find or copy personal ROMs. Skips are missing coverage, not passing game workflows.
Resource-only checks run without ROMs. All-skipped, incomplete, or erroneous runs fail
the same evidence validator as the first project.

See [the baseline](../../tests/INTEGRATION_BASELINE.md) for counts and known failures.
