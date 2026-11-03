# Mac autocomplete line mapping regression

Uses the production TextEditor/AutocompleteOverlay in a headless Skia window and fabricated
BPRE-tagged models. No supplied ROM, network or native desktop session is needed. The
original handler fails the exact Core/overlay suggestion comparison on a later line
(`/tmp/hma-autocomplete-original.log`); the expanded group passes **292 checks**.

```sh
dotnet build tests/AutocompleteMapping/AutocompleteMapping.csproj --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 90 dotnet tests/AutocompleteMapping/bin/Debug/net8.0/AutocompleteMapping.dll
```

Restore dependencies first. scripts/verify-mac.sh discovers this group automatically.

Coverage:

- Exact offered commands/variable names at first, second, middle and final source lines,
  including blank lines and LF, CRLF and mixed separators.
- Enter acceptance changes exactly the active line, retains suffixes/repeated spaces, and
  preserves all surrounding text and original separators; it positions the caret correctly.
- The popup follows the active caret line even when a selection starts on a different line;
  Up/Down wrap, Escape dismisses, and clicking an actual generated button accepts its option.
- Document start, line end/start, CRLF interior and empty final lines clear stale suggestions
  without a negative column or another line's menu.
- Space inside an open quote does not accept early; quote acceptance edits the correct line.
- A real headless TextInput notification typing `b` after `lock\nmsg` offers completions.
- Live CodeTool acceptance of `temp1` from `setvar tem 1` preserves surrounding LF/CRLF script
  text, changes exactly the intended variable byte after delayed compilation, preserves the
  caller pointer/source metadata, and round-trips bytes and metadata through undo/redo.
- The live CodeTool fixture uses the actual TabView template, types the variable prefix one key
  at a time and checks focus/caret progression plus retained editor identity after compilation.

Most scenarios use an independent CodeBody without a compilation subscription, establishing
that querying/accepting suggestions alone does not change ROM bytes. The final two fixtures
use the actual CodeTool and manually controlled debounce timer; incomplete text and menu
navigation must not create data changes or undo records. Quoted key behavior uses a prepared
menu item to isolate quote/space dispatch from enum matching.

Implementation scope: one shared line/span locator in the Avalonia overlay replaces all three
fixed `length + 2` traversals. Queries exclude line terminators, replacements splice the source
span instead of joining lines with the host newline, and absent suggestions clear old options.
The original matchers, completion ranking, numeric/pointer interpretation and shared-Core
compiler/repointing/history algorithms are unchanged.

These checks validate input routing, options, source edits, caret placement and model/history
behavior, not native screen appearance or popup geometry under every scroll/font configuration.
The companion 100–500-line benchmark compares all prepared Core/overlay option arrays separately.
