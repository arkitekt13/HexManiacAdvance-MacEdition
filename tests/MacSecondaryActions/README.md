# Remaining Mac secondary actions

Run `dotnet run --project tests/MacSecondaryActions` (or `-- --block-only` to isolate block tiles).
The 31 checks exercise physical secondary-click and Control-left-click, including releasing
Control first. A real hex view must show identical context actions without model changes; its
selected-byte summary must copy the exact existing multi-byte/trailing-space representation.
Capture loss cancels a context gesture and cannot contaminate the next primary click.

A rendered, initialized block editor backed by a formatted synthetic blockmap must sample tile 0
without painting tile 1. The old Control-click route failed this assertion on an actual PixelImage.
Normal primary block painting and complete-byte Undo/Redo remain covered. The fixture explicitly
selects a block before clicking, matching the editor's block-selection initialization path.

The Code-created summary menu is inspected through its owning field because it lives in a popup
rather than the XAML name scope. No user ROM is used. Existing command lists, clipboard format,
block encoding, pointer relocation and history algorithms are unchanged. Upstream stub-only
grouped-field menus are outside this regression's implementation scope.
