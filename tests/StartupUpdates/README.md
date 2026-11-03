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
