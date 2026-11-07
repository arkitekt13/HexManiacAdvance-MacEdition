# Go To keyboard focus

Headless native-control checks exercise Command-G (Control-G on other hosts),
typing without a mouse click, replacing an existing query, Escape, immediate
queued input, cancelled deferred attachment and reopening. ROM bytes/history
must remain unchanged.

The ordinary headless opening already passed before this change. Explicitly
holding the AngleTextBox out of the visual tree until queued focus work finishes
reproduces a missed focus request in the old implementation. This models delayed
presentation; it does not prove the user's exact Cocoa event sequence.

The window now waits for the actual TextBox to attach, schedules focus before
the next input-priority work and cancels stale requests on close/editor change.
The shared AngleTextBox, key bindings, Go To matching and ROM navigation are
unchanged. Native trackpad/keyboard confirmation remains useful.

Build this project and execute its DLL, or run the complete Mac regression runner.
All input/ROM fixtures are synthetic; no personal settings or ROM are used.

Follow-up coverage also opens the native menu's bound command while an attached
panel is held invisible until after the queued focus attempt. The old handler
loses this request; the new handler waits for visibility/layout or attachment
until focus succeeds, cancelling subscriptions on close or editor replacement.
All 17 assertions pass, including close-before-visibility cancellation and
unchanged ROM/history. This simulates a native presentation timing gap rather
than proving the exact Cocoa sequence on the user's screen.
