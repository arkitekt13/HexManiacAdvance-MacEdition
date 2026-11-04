# Coalesced script width measurements

Run `dotnet run --project tests/ScriptWidthUpdates` or the complete Mac regression suite.

The 448 assertions use real production TextEditor controls in a headless Skia window. A subclass
counts actual UpdateDocumentWidth calls; an immediate-scheduling reference uses the same
unchanged measurement routine. Before coalescing, ten queued edits fail the one-measurement
assertion with ten actual calls. Afterward one measurement reads the latest source.

The attached controls are compared for exact editable and six-layer displayed text, all layer
widths, scroll transforms, native extents/offsets, scrollbar corner dimensions, caret and
selection. Cases cover empty/growing/shrinking documents, LF/CRLF/tabs/trailing spaces, Unicode/
emoji/CJK, long lines, both scrollbars, resize, proportional/fallback fonts, font size and culture.
Interleaved requests/scroll changes retain the existing ordering of source width versus native
scroll-extent widths.

Queued context and template replacements cannot measure abandoned source/templates. Detachment
cancels pending work and reattachment measures current source once. Diagnostics-only changes
request no widths. These controls contain fabricated strings only, with no compiler, ROM or
history model; the complete Mac suite separately protects live script compilation, source
retention, suggestion interaction, save/reload and Undo/Redo.

Timing is never a test gate. See [SCRIPT_WIDTH_COALESCING.md](../SCRIPT_WIDTH_COALESCING.md)
for paired actual-measurement benchmarks and limits of synthetic/offscreen measurements.
