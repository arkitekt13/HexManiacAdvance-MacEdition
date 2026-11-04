# Short-field Tab selection

Run `dotnet run --project tests/ShortFieldTabSelection/ShortFieldTabSelection.csproj` or the
complete `scripts/verify-mac.sh --no-restore` suite from the repository root.

The actual multiple-choice table template is populated with three fabricated PCS text pointers.
Real Tab/Shift+Tab input must select a whole single-line choice, and typing must replace it.
The checks retain mouse caret placement, programmatic selection, selection during model refresh,
unrelated choice bytes/pointers and exact Undo/Redo. Traversal alone creates no ROM history.

An inactive AngleTextBox also receives actual keyboard traversal: its proxy must forward Tab's
navigation reason to the newly attached native textbox. This preserves Avalonia's normal
single-line select-all behavior without changing mouse focus.

Multiline table text and whole table/list streams are excluded. Dedicated script and dialogue
editors remain excluded even with one source line, and Tab in scripts still inserts indentation.
No text parsing, stream relocation or history rules are changed.

Manual check after relaunch: Tab and Shift+Tab through multiple-choice entries, map names and
numeric/address fields; type replacements, click to position a caret, then Undo/Redo. Verify
large editors keep their existing selection and Tab behavior. Tests use no user ROMs.
