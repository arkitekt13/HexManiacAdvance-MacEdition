# Script-side map event navigation

Build this project, then run its DLL; `scripts/verify-mac.sh` also discovers it.
An optional absolute PNG argument saves an offscreen control preview. All inputs
are synthetic; no personal ROMs or preferences are opened.

The 76 assertions use the actual MainWindow/TabView and headless pointer/keyboard
input. A combined NPC/script-event map has object ID 9 and independently assigned flags,
a signpost union, independent scripts, an unformatted target, null/shared pointers and a forced
relocation boundary. Coverage includes:

- Detached hit metadata, current live pointers and same-tab script navigation,
  with exact bytes/serialized metadata/saved/history preservation for pure reads.
- Read-only Object ID and live Flag (or none), plus green-event Variable Number,
  missing scripts and changed live flag/variable values.
- Retained image identity, grid and pan; no map render merely for script switching.
- Real drag versus click-jitter handling, blank tiles, canceled capture and secondary
  clicks; large sprite artwork outside its anchor tile remains clickable.
- Rejected pending dialogue source, retained diagnostics/caret/selection/focus,
  inline red notice, unchanged ROM/metadata/history and stale callback rejection.
  Inline command help is deliberately present; it must not consume the map click.
- Correction/retry, a click that both settles relocation and resolves the newly
  written destination, subsequent clicks, exact one-step Undo and Redo restoration.
- Shared scripts, actual overlapping-event chooser input and cancellation, plus
  stale menu/row/map guards after clearing/replacing the map or moving its object.
- Script signpost kinds 0–4, hidden-item kinds 5–7, secret-base/unknown kinds,
  pointer-shaped packed data, stale kind/deleted rows, pending-callback preservation
  and no select-all on Map View entry.
- Command-V routed to script source after navigation, disabled hidden hex input,
  released image and balanced script-state subscriptions when the tab detaches.

The pane only emits navigation requests. It owns no live event view models or map
editing commands. The parent settles pending source before navigating, and reads
the current event pointer again afterward. Thus a click may compile the user's
pending valid source through the existing compiler; it cannot paint tiles, create
an absent script or alter event records. Invalid source blocks script switching.

See [the investigation](../SCRIPT_MAP_EVENT_NAVIGATION_REVIEW.md), [exact suite
original results](verification.json), [latest follow-up suite](../SCRIPT_MAP_FOLLOWUP_VERIFICATION.json) and [memory measurements](lifetime-memory.json).
The existing ScriptMapReference group retains its image, context, deferred-source,
clipboard/Delete and stale-render coverage. Native trackpad/popup behavior and
real-ROM gameplay remain user checks.
