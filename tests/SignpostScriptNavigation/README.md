# Signpost arrow source focus

Synthetic map/ROM only; build this project and run its DLL. The actual MainWindow,
map signpost arrow and script controls verify first entry and reopening an existing
script tab, keyboard focus, select-all for replacement, normal click-to-deselect,
and unchanged ROM bytes/metadata. The nine assertions use real pointer input.

The arrow owns navigation and captures its window before the map detaches, then
selects only the requested script source after its destination controls are ready.
A queued focus action checks model, selected script and overlay state. Map View
navigation has separate coverage in ScriptMapNavigation and does not select all.
