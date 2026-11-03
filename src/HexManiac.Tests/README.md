# Upstream engine tests on Mac

The existing test assertions and package versions are retained. The host targets .NET 8; Core
still targets .NET 6. Portable content items copy reference resources, scripts, assembly fixtures,
and historical metadata. TestEnvironment sets the working directory to the test output before
legacy static fixtures initialize and rejects absent/empty required reference files.

From any directory, invoke the repository's `scripts/verify-upstream.sh`. It requires the .NET 8
SDK and Python 3, restores into `artifacts/upstream-packages` unless NUGET_PACKAGES is explicitly
set, builds fresh binaries, verifies discovery, runs tests, and retains evidence in a unique
`artifacts/upstream-tests/run.*` directory. Test failures return a nonzero exit code.

Options:

- `--no-restore`: reuse already restored dependencies for the selected runner/configuration.
- `--configuration Debug|Release`: default Debug, applied consistently to restore/build/test.
- `--source SOURCE`: optionally repeat for specific NuGet feeds.
- `--in-process`: use xUnit's pinned 2.4.1 runner utility to discover and execute the same assembly
  without VSTest's socket connection; results use standard xUnit XML rather than TRX.

Examples:

```sh
scripts/verify-upstream.sh
scripts/verify-upstream.sh --in-process
scripts/verify-upstream.sh --in-process --no-restore
```

The default VSTest mode keeps discovery/execution logs, TRX, and validated summary JSON. The
in-process mode keeps discovery/execution logs, xUnit XML, and validated summary JSON. Discovery
has a deadline; execution has a six-minute process watchdog (and a five-minute engine deadline
in-process). Empty, entirely skipped, incomplete, aborted, or inconsistent runs fail verification.
Discovery entries and executed parameterized cases are reported separately; case totals can differ
when data is generated at execution time. A failed host/process cannot be hidden by an XML report.

This session could build the VSTest host but its local socket connection was denied. The explicit
in-process mode produced the recorded baseline; the default VSTest execution is not claimed as
verified here. Normal NuGet access also failed through dotnet; pinned packages were retrieved
over HTTPS into an ignored local feed and restored with existing cached Core packages as a second
read-only feed. No TLS verification was disabled or package versions changed.

No ROMs are copied or searched for outside `sampleFiles` under the selected host output directory.
BasicLoad discovers `.gba` files case-insensitively in a deterministic order. If the directory is
missing or contains no ROMs, it supplies one explicit skipped case explaining the absent optional
fixture. A supplied path still executes the original load operation; read/load failures are not
converted into skips. The other optional tests retain their existing filename conventions and
skip policies. Missing fixture skips are not compatibility evidence. Historical metadata fixtures
load from the copied `toml_versions` directory instead of a fixed parent-directory traversal.

See [the baseline](../../tests/UPSTREAM_BASELINE.md) for counts and triage. The separate
`HexManiac.Integration` project now has a portable host selected with `--integration`; see its
[baseline](../../tests/INTEGRATION_BASELINE.md). No performance optimization is included.

AutoSearch and Upgrade now include the expected path in missing-ROM skip messages.
AutoSearch retains exact filenames such as `sampleFiles/Pokemon FireRed.gba` and
`sampleFiles/Pokemon FireRed v1.1.gba`; case and revision labels matter on case-sensitive
filesystems. Filenames identify the expected game/revision, so renaming a different ROM
is not sufficient validation. No automatic search or copying of personal files is performed.
Requests for independent models load an existing file even if it was not initially preloaded,
then copy its bytes and exported metadata without modifying the source file.

Three old AutoSearch expansion methods have commented-out test attributes and are absent
from discovery, rather than counted among skips. TM and item expansion tests explicitly say
"test removed until feature is complete"; those features are also absent from the editor's
expansion menu, and TM implementation comments identify outstanding research. Move expansion
is still available in the editor, but its old test is disabled without an explanation. That
coverage gap needs a matching ROM fixture and a separate feature review before re-enabling;
this work does not change expansion code, researched addresses, or disabled attributes.
