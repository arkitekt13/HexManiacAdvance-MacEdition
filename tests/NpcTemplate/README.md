# NPC creation template sprite selection

Run `dotnet run --project tests/NpcTemplate` or `scripts/verify-mac.sh --no-restore`.
The regression uses a real headless MainWindow, map settings popup and synthetic sprites;
no user ROM, settings or sidecar is written.

Before the fix, the visibility assertion fails because the panel checks `NPC` while the
enum is `Npc`. The former shared binding also used the trainer preference path.
The NPC picker now has its own setting and publishes refreshed choices together so
collection rebuilding cannot leave the picker blank.

The 44 assertions cover pointer selection through actual popup containers, small and
oversized sprites (including ID 108), bounded toolbar/picker layout, template switching,
popup reopen, map navigation, option refresh, invalid indices and removed-choice fallback.
They verify that selecting a sprite changes no ROM bytes, metadata or history, and that
Trainer/Legendary selection stays independent. Calling the existing NPC creation routine
checks the selected graphics ID, exact existing script/dialogue/pointer bytes and all
other defaults, in-memory Save/reload, and complete byte/metadata Undo plus byte Redo.

The preference lasts within the open ROM/session; it is not saved across app relaunch.
Native macOS trackpad appearance and dragging a chosen sprite onto a real map remain
manual checks. The automated creation check calls the same NPC factory used by placement.
