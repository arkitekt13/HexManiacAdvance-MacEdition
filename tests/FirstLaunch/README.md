# First launch and explicit ROM opening

Run `dotnet run --project tests/FirstLaunch` or the complete Mac regression suite.
The 47 assertions use temporary synthetic files, stub preferences and an actual headless
Avalonia dispatcher; no personal settings, user ROM or native file picker is used.

Baseline evidence is recorded in FIRST_LAUNCH_READINESS_REVIEW.md. Existing filenames with
spaces, colons or hexadecimal-looking suffixes take precedence over optional address syntax;
legacy colon/space hexadecimal addresses and startup options remain supported. Invalid
suffixes do not truncate filenames.

Opening returns its actual new tab (or no tab for cancellation/import/failure). Checks cover
missing anchor fields, malformed and overflowing numeric metadata, retained errors, zero
metadata writes on failure, correction/retry, existing-tab byte/history preservation, explicit
and picker cancellation, last-tab close, stale recent paths, and closing a model during delayed
initialization. Real queued address navigation works for its new live tab and cannot target
another selected tab or revive a closed ROM. Native Finder cold-start delivery remains manual.

Settings coverage injects a temporary app-support directory into the real Mac filesystem.
Checks cover fresh defaults, preference save/restart, an invalid directory, chmod-denied
reads, read-only writes, nonmodal startup warnings and protection against overwriting unread
preferences even after permissions recover. Unreadable ROM sidecars reject opening and stay
unchanged. No HOME override or personal profile reads/writes are used.
