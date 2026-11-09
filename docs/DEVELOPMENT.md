# Building and verifying Mac Edition

For a first source launch, follow [Installation](../README.md#installation).
The commands below are for developers and maintainers working in the repository folder.
Use the Avalonia project for Mac builds. The Windows WPF solution is a separate target.

## Build an application bundle

To build a self-contained app:

```sh
./publish-mac.sh osx-arm64    # Apple Silicon
./publish-mac.sh osx-x64      # Intel
```

Output is `artifacts/mac/<architecture>/HexManiacAdvance.app`. The script can be invoked by
absolute path from another directory; it stages and verifies a new bundle before replacing
the previous build for that architecture. For example, launch an arm64 build with:

```sh
open "artifacts/mac/osx-arm64/HexManiacAdvance.app"
```

## Prepare a download for testers

```sh
./package-mac.sh osx-arm64   # or osx-x64
```

This rebuilds/verifies the app and prepares a local ZIP plus a portable `.sha256` file under
`artifacts/releases`. The archive includes the app, source/build identification, the original
LICENSE, credits and standalone helper instructions. Existing archives are preserved; tracked
working-tree changes are labeled `dirty`. This command does not upload a release or notarize
anything. From the archive directory, verify a received copy with `shasum -a 256 -c <file>.zip.sha256`.

## Run regression checks

Run the complete synthetic/headless Mac regressions from the repository root:

```sh
scripts/verify-mac.sh
```

Use `--no-restore` when dependencies are already restored. The runner reports groups and
assertions; it is separate from the original unit/integration suites described in
[tests/README.md](../tests/README.md). Native input, Finder, signing and gameplay still need
real-machine testing. [PORT_STATUS.md](../PORT_STATUS.md) and [RELEASE_NOTES.md](../RELEASE_NOTES.md)
preserve historical port/release notes rather than current setup instructions.

## Mac release versions and project metadata

The Mac app uses `src/HexManiac.Avalonia/MacAssemblyInfo.cs` and is currently **1.0.0**.
The welcome/About labels, crash reports, update checker, bundle and package names use that
app identity. Update both assembly/file version declarations there for a new Mac release.

Core keeps `src/SharedAssemblyInfo.cs` and its existing **0.6.1** metadata/migration version.
Do not bump Core simply to label a Mac release: those values participate in project TOML
upgrades. The Mac app no longer links the shared version declarations.

Publish numbered stable tags such as `v1.0.0`, then `v1.0.1`, with a compatible uploaded Mac
asset such as `HexManiacAdvance-osx-arm64.zip`. A branch push or version edit alone does not
publish an update. Pre-releases and source-only assets do not trigger the stable checker.
The notice opens the releases page for a manual download; there is no automatic installer.

## Release preparation

A staged [release checklist](MAC_RELEASE_CHECKLIST.md) separates private helper preparation,
native verification, public-release gates and optional tutorial/background polish.

A supported-runtime upgrade remains a separate pre-public-release task. See
[FutureDevelopment.md](../FutureDevelopment.md) and the
[distribution checklist](MAC_HELPER_TESTING.md). Local packaging does not publish a GitHub release.
