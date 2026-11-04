# Clipboard focus routing

Run `dotnet run --project tests/ClipboardRouting/ClipboardRouting.csproj` from the repository root,
or run `scripts/verify-mac.sh --no-restore` for the complete Mac suite.

The full MainWindow is hosted in Avalonia.Headless with its in-memory clipboard and fabricated
ROMs. Routed Command shortcuts (Control on other platforms) and the installed native Edit menu
commands must operate on the focused TextBox. The checks cover selection copy/cut, backward
selection replacement, LF/CRLF source paste, focus/editor retention, read-only fields, Select All,
legacy Cmd+B, Deep Copy, rejected source/diagnostics and blocked saves, correction/compilation,
and actual Cmd+Z/Shift+Cmd+Z byte/metadata round trips. Dialogue, name-table text and grass,
surf, tree and fishing stream fields exercise their actual templates inside MainWindow.
Hex focus retains binary paste through the native menu and its existing Undo behavior.

Before the fix, routed Cmd+C copied hex data and routed Cmd+V with script focus wrote the
clipboard's `AA BB` into the synthetic script's ROM bytes, replacing opcode `6A` with `AA`.
The tests now require text replacement before compilation and complete model preservation on
rejected source. Existing ScriptWorkflowSafety checks retain broader navigation, stale callback,
atomic compilation, save/close and external-reload protections.

Manual confirmation after rebuilding: paste a multiline script with Cmd+V and Edit > Paste,
check that focus stays in source, then correct/compile and Undo/Redo. Also check native menu
selection and clipboard exchange with another application. Headless tests invoke the installed
native menu commands; they do not emulate AppKit's menu-bar event delivery.
