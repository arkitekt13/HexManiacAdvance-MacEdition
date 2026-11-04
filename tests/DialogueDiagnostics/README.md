# Visible dialogue diagnostics

Run `dotnet run --project tests/DialogueDiagnostics/DialogueDiagnostics.csproj` or the complete
`scripts/verify-mac.sh --no-restore` suite from the repository root.

The user's exact `msgbox.npc <auto>` example is compiled in a synthetic ROM and rendered
through the real Mac TextEditor. Tests inspect actual Skia pixels for a warning underline
below the overflowing suffix, rather than just counting diagnostic records or decorated Runs.
They cover LF/CRLF, shortening and manually breaking dialogue, horizontal/vertical scrolling,
whitespace-only spans, red compilation-error underlines, unsorted/stale diagnostic ranges,
context changes, clearing, retained focus and unchanged source/ROM bytes/metadata.

Existing PCS encoding is characterized too: one Enter creates a line break (`FE`), subsequent
single breaks use scrolling (`FA`), and a blank line creates a paragraph/page break (`FB`).
The fix adds no prompts, automatic wrapping or changes to those controls.

The original renderer applied TextDecorations to blank syntax placeholders. Avalonia omits
their trailing-whitespace decorations, so computed warnings could remain invisible. A separate
non-interactive layer now draws paths using the actual TextBox layout and viewport.

Manual check after relaunch: enter the screenshot's long NPC dialogue, confirm the warning
underline, use Enter to shorten the rows, and confirm it clears while typing/selection and
scrolling work normally. Pixel tests use a synthetic fallback-font ROM, not a real-ROM font
or an in-game emulator preview; the existing width estimator and its limits are unchanged.
