# Startup update regression

Run `scripts/verify-mac.sh`. Synthetic GitHub responses and disposable cache files exercise
compatible release selection, numerical version comparisons, daily cache reuse/invalidation,
clock changes, delayed responses, timeout, shutdown cancellation, failed HTTP/network/cache
operations, invalid JSON, response size limits, and property notifications for the Update menu.
No live GitHub connection, native window, download, or installation is required.

## Release policy

The checker queries the most recent 30 releases from this fork's public GitHub REST API.
Only non-draft, non-prerelease tags `vMAJOR.MINOR.PATCH[.REVISION]` (or without `v`) qualify.
At least one uploaded, nonempty `.zip`, `.dmg`, `.pkg`, or `.tar.gz` attachment must have:

- A `mac`, `macos`, `osx`, or `darwin` filename token.
- An architecture token matching the running process (`arm64`/`aarch64`, `x64`/`amd64`/`x86_64`),
  or `universal`.
- A download URL under this fork's releases, with no `source`, `sources`, or `symbols` token.

Example: `HexManiacAdvance-osx-arm64.zip`. Source-only releases and ambiguous filenames do not
enable Update. The action opens this fork's releases page; it never installs anything.
API field reference: https://docs.github.com/en/rest/releases/releases

## Startup and cache behavior

The window starts first, then the check runs away from the UI thread with a five-second overall
cancellation budget and a one-MiB response limit. Closing the window cancels pending work.
A successful result is cached for 24 hours in `mac-update-check.json` under the application's
local-data folder, scoped to repository, current app version, and process architecture.
Old upstream flags and timestamps in editor preferences are not used by the Mac checker.

Network/API failures do not advance the successful-check timestamp or show an error dialog.
A previous matching Mac result is retained on failed refresh; absent a valid result, Update
stays disabled. Cache-write failure does not discard a valid network result. Live startup
appearance, actual network access, and the browser menu action remain manual checks.

## Mac Edition 1.0 and visible notices

Mac Edition's release identity is now 1.0.0, independent of Core's unchanged 0.6.1 metadata
version. Existing 0.6.1 clients can discover a published v1.0.0 release; Mac 1.0.0 ignores
that same release and older upstream 0.x versions, and can discover v1.0.1.
The synthetic 0.6.1 cases above remain intentional coverage for older clients.

A compatible positive result now displays a nonmodal banner on the welcome/editor window.
View Update opens this fork's releases page; Dismiss lasts for the window session without
clearing Help > Update. It never installs software or prompts on failed/offline checks.
[UpdateNotice](../UpdateNotice/README.md) covers actual input, focus and source protection.
