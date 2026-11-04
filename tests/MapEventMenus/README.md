# Map event context menus

Run `dotnet run --project tests/MapEventMenus` or the Mac suite. A synthetic warp receives real secondary and Mac Control-clicks at two zoom levels. The group verifies all three shared menu commands, pointer release, the actual map-bank dialog (cancel before allocation), destination tab creation, targeted deletion with map Undo/Redo, byte/metadata integrity, and empty-cell gestures without stale menus. MapSelection separately checks complete tile-drag, stamp and Option-pan behavior.

The missing-menu regression failed before the fix. Map creation itself remains the existing shared implementation; these routing checks do not allocate or invent new map data. Native Control-click remains for user confirmation after relaunch.
