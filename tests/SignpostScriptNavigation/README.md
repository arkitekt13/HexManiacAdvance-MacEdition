# Signpost arrow dialogue focus

Synthetic map/ROM only; build this project and run its DLL. Actual MainWindow,
map signpost arrow and script controls verify first entry and reopening an existing
script tab, keyboard focus and selection of only the first text stream's contents.
Commands, braces and preceding movement streams are excluded. A normal click
clears the selection and places the caret. Scripts with no dialogue receive focus
without selecting their commands. The group passes 14 assertions.

Selection uses the decompiler's stream classifications, including text referenced
by loadpointer's generic pointer argument. Multiline text is selected as a single
replacement range; actual typing replaces it while surrounding commands remain compilable. Opening/selecting is checked against unchanged ROM bytes and
metadata, with real pointer input and a synthetic compiled movement/text script.

The queued focus action retains model, selected script and overlay guards. Only
the signpost arrow requests dialogue selection; Map View navigation and other
script arrows keep their separate behavior, covered by ScriptMapNavigation and
ScriptArrowFocus.
