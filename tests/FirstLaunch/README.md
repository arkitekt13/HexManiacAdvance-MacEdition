# First launch and explicit ROM opening

Run `dotnet run --project tests/FirstLaunch` or the complete Mac regression suite.
The 33 assertions use temporary synthetic files, stub preferences and an actual headless
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
