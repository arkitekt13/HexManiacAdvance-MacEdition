# Script typing assistance

Run `dotnet run --project tests/ScriptAssistance/ScriptAssistance.csproj` or the complete Mac
suite. The real MainWindow/TabView editor receives character, Space, Enter, pointer suggestion acceptance and Cmd+U input with a
synthetic ROM. Supported text-pointer commands must visibly insert `<auto>` with the correct
caret inside a complete dialogue or movement block; move.player/move.camera receive real keyboard
input and compile movement instructions. Setflag/checkflag and setvar/addvar must expose the matching helper button and insert an
explicit operand through both keyboard and button paths. Completed commands retain exact
Undo/Redo byte and metadata behavior.

The synthetic investigator returns predictable IDs to isolate UI routing; these checks do
not establish real-ROM unused-ID scan completeness. See `tests/FLAG_VARIABLE_REVIEW.md` and
`tests/FLAG_USAGE_INVESTIGATION.md` for the separate allocator/scanner limits.

The fixture queues UI work but keeps compilation debounce deterministic, catching reentrant
two-way binding problems that direct helper calls or synchronous dispatch can miss.

Manual check after relaunch: type a supported command and a space, inspect the pointer/caret,
then try the unused-ID button and Cmd+U in a map-backed script. Spartan mode, the auto-insert
setting and availability of an investigator retain their existing eligibility rules.

Command-list regressions cover nine commands through typed Space, keyboard acceptance and an actual suggestion click (258 assertions). Accepting a command publishes its intended caret with the source before script assistance runs; suffix/line-ending mapping remains covered by AutocompleteMapping.
