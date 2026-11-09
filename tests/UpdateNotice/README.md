# Dismissible update notice

Headless MainWindow tests exercise actual pointer clicks, active script focus/caret/selection,
positive update results, fixed GitHub release routing, session dismissal, Help > Update,
new-window reminders, editor replacement, the welcome screen, and retained rejected drafts.
No browser, network request, download or installation is performed; process launches use a stub.
Set HMA_UPDATE_NOTICE_PREVIEW to an absolute PNG path to render the real notice.
The existing StartupUpdates group covers release selection, architecture/version gating,
caching, offline startup, cancellation and failed network responses.
