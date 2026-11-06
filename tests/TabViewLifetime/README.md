# Open-document control lifetime regression

Run `dotnet run --project tests/TabViewLifetime/TabViewLifetime.csproj` or the complete
`scripts/verify-mac.sh` procedure. All documents/maps are synthetic; no personal ROM or
settings are loaded. Collection is forced only by this offscreen test, never by the app.

The actual MainWindow switches between two still-open documents in Hex and Map modes.
Weak references require old TabView, HexContent, source TextEditor and column-header controls
to collect after dispatcher/render/focus work settles. Source hashes, saved state and empty
history must remain unchanged. This fails on d5630e1d on the very first Hex switch: all four
watched controls remain alive. The initial failure predates later reattachment coverage.

Reusing a TabView must reconnect once, preserve pending callbacks without settling the draft,
honor caret/focus requests, compile successfully, retain rejected source/diagnostics, and
preserve exact Undo/Redo model states. Avalonia may rebuild child source editors when a parent
reattaches, so focus is checked on the current child rather than an abandoned TextBox.
Standalone TextEditor reattachment protects native selection and a caret changed while detached.
Preview/filter controls cover never-attached panels and repeated attachment. Header tests cover
removal, collection reset/replacement, detachment and reattachment without duplicate listeners.

Reflection inspects event delegate targets for ownership assertions. A bounded diagnostic
strong-path tracer runs only when a collection assertion fails; it is not a production API
or proof that all possible native resources have been released. Native focus, Cocoa drawing,
Retina rendering and real-ROM behavior remain manual validation.
