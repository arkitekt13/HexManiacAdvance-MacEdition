# Map-header script editing

Synthetic map/ROM only. Build this project and run its DLL. The fixture uses a
real mapscripts table, EventTemplate, MapScriptCollection and shared model history.

The baseline fails immediately: Add Table Script grows the raw table from one row
to two, but the view-model collection stays at one. Later checks cover row offsets,
editing after deletion, separate Undo boundaries, exact byte/metadata Undo/Redo,
all regular/table type transitions and shifting/deleting parent entries.

The repaired group passes 69 assertions, including forced table relocation and
repopulation after deleting all rows. Byte and structural-metadata comparisons
exclude the existing FreeSpaceSearch allocation hint; ordinary history does not
rewind that cache, and this repair does not alter allocator/history algorithms.
