# Downloaded-source installation rehearsal — 2026-11-05

Baseline: `avalonia-port`, `6c64f826558bf833243b6b45477ee6c22cd5b394`.

Downloaded the actual public default-branch ZIP and extracted it into an owned
directory containing spaces. Source ZIP SHA-256:
`1cd8f6366c2d8039e167f7137becc36fcb20a50a921f191a92bc678eea8f19e9`.
The extracted folder and project match README Installation Option 2.

## Results

- Downloaded source builds with SDK 8.0.300: zero errors. The initial restricted
  restore emitted four NU1900 vulnerability-feed warnings; network-enabled
  packaging restored successfully without those warnings.
- Ran the downloaded `package-mac.sh osx-arm64` from an unrelated directory.
  Publish, ad-hoc signing, strict signature verification and ZIP creation passed.
- ZIP CRC validation passed for all 2,101 entries. The package contains its
  startup instructions and no `.gba` file. ZIP size: 60,216,758 bytes; application
  file contents: 150,969,018 bytes. Bundled runtime: .NET 8.0.5.
- Extracted the ZIP using macOS `ditto`; deep, strict signature verification
  passed again. Python ZIP extraction does not restore the resource-fork signing
  information and is unsuitable for this signature check.
- The existing `tools/PublishedBundleReview` probe passed all seven checks
  against the freshly published assembly: empty/error-free startup, resource
  discovery from an unrelated directory, script references, isolated settings
  write/reload and bundled runtime libraries. It found 61 resource files.
- Personal ROMs/preferences were not used. No application code changed.

## Limits

This is an existing Apple Silicon development Mac with its installed SDK and
NuGet cache, not a clean machine. The README's native `dotnet run` launch was
not executed: native startup uses the normal personal settings directory.
The isolated probe exercises the existing startup loader, not a native window.
Finder launch, Gatekeeper/quarantine, no-SDK launch, Intel hardware and older
supported macOS still need helper verification. The package is ad-hoc signed,
not notarized. No release was uploaded and the full regression suite was not
rerun for this evidence-only change.

The downloaded copy was nested beneath this Git checkout, so package revision
discovery inherited the parent repository's matching baseline SHA. This does
not exercise the `source-archive` label used outside a Git checkout.

Generated logs, startup JSON and package review are under the ignored
`artifacts/install-rehearsal/` directory. No installer defect was reproduced
within the tested scope.

## Future asset feasibility — no implementation yet

The welcome screen can display an optional PNG behind its existing content,
with a dark fade and a plain-background fallback for missing/invalid assets.
Use a documented fixed source asset path, included in published builds; users
need not edit XAML when the final map image is ready.

Utilities already supports conditionally enabled actions. A tutorial action can
remain disabled until an optional patch and base-ROM manifest are bundled.
The existing UPS engine validates checksums and stages candidate bytes before
committing. A tutorial wrapper should additionally require the intended base
revision/hash and forward direction, and create a separate output ROM rather
than overwrite the user's working ROM. Never bypass checksum verification or
reuse the CFRU workflow's unrelated metadata handling. The user confirmed English FireRed 1.1 as the required base. Its exact hash and
the final patch format will be recorded when the tutorial is ready.
