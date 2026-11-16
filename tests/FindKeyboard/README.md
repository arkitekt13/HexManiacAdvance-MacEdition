# Find submission and results display

Build this project and run its DLL, or use `scripts/verify-mac.sh`.
The group uses actual headless Command-F and Return input through MainWindow,
with two fabricated ROMs; it never opens personal ROMs or preferences.

Before the fix it fails at `Return executes Find and closes the field`:
FindBox had no Enter binding, unlike the original WPF field. The scoped binding
now submits the control's current text through the existing Find command.
This does not make Find a live table filter or alter matching rules.

The group passes 25 assertions covering case-insensitive Pokémon-name search,
hex bytes, exact-case and no-result errors, current/all-files scope, multiple
results, F3/Shift-F3, Escape, and normal Enter input in another field. Both ROMs,
serialized metadata, saved/Undo state and on-disk files remain unchanged.

The multiple-result case also reproduced a SCADE menu-refresh null-model crash;
SCADE and tutorial eligibility now reject results viewports, which represent
several locations rather than one ROM model. Their dedicated groups cover both
checks, including the optional tutorial path even without shipped assets.

Focused verification: FindKeyboard 25, GotoFocus 27, ClipboardRouting 78,
UpdateNotice 14, ScadeInstaller 138, TutorialPatch 59: six groups / 341 assertions,
zero failures or skipped groups. This is a focused run, not a new complete-suite
count. Native Cocoa keyboard confirmation remains useful.

## November 16: multiple-match results

The earlier 25 assertions verified submission and the search model, but did not
inspect the rendered results tab. A new visual-tree regression fails on the old
code with `multiple matches display a real results viewer instead of the unported
placeholder`. The repair registers SearchResultsViewPort with TabView, matching
the original WPF template. No matching, compiler, model or write behavior changes.

The expanded group passes 50 assertions. It drives real headless window input to
verify highlighted previews and source/address headings, single-click behavior,
Command-click and double-click navigation, selection of the matched range,
two-file results with identical addresses, wheel scrolling through 40 matches,
last-match navigation, smaller windows, tab switching and closure. Typing,
Backspace, Delete and paste preserve both fabricated ROMs, metadata, history
and on-disk files. Existing single-hit, no-hit and keyboard behavior stays covered.

Pass a PNG path as the sole argument to render the fabricated results window for
visual inspection. No personal ROMs, application preferences or native windows
are used. Full-suite and app-publication evidence is recorded separately.
