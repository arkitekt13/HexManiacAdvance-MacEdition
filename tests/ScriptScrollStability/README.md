# Long script click/scroll stability

The default plain-text fixture preserves the command structure, blank lines and
dialogue line lengths of the reported two-section tutorial script. Dialogue is
replaced with neutral characters; the user's story text and ROM are not included.
An optional absolute plain-text path exercises the same checks with another script.
No ROM is loaded from that path and no script is compiled into a personal game.

Build this project and run its DLL, or use scripts/verify-mac.sh.

The regression uses the actual CodeToolMultiTextBox ScrollViewer and TextEditor
controls, plus a complete MainWindow/script tab backed by a synthetic ROM. Checks
cover unfocused/already-focused visible clicks, exact caret hit positions, two text
sections, LF/CRLF, two font/window sizes, manual scroll, drag selection, arrow-key
caret reveal, typing, unchanged pending source/debounce work and unchanged synthetic
ROM bytes/metadata/history. Future tests must retain keyboard caret reveal, rather
than suppressing every BringIntoView request.

On checkpoint 554b16db, the baseline fails immediately: a click on visible source
moves the outer scroll offset from 55 to 5. A separate exploratory test reproduces
long-box focus jumps, including 900/1500-position cases. Disabling only the outer
viewer's automatic focus reveal stabilizes initial clicks. The repair disables only that focus behavior; native caret requests
remain enabled. The final regression passes 195 assertions, including keyboard and
model-driven moves to both ends of the long source. The exact pasted script also
passes those 195 assertions through the optional local-file route; its story text
is not committed. [verification.json](verification.json) records the fresh 13-group /
3,266-assertion focused run and its validation boundaries.

Native trackpad interaction and real-ROM UI confirmation remain user checks.
