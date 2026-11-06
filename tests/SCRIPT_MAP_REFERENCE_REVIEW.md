# Script-side map reference feasibility — November 5, 2026

Baseline: `68ed0f918d826c59ad3b9e19006081f765081c2c`, branch
`codex/goto-keyboard-focus`. Investigation only; no tracked application/test/document changes.
No personal ROMs/settings used. Runtime migration and map-export UI work are outside scope.

## Findings

1. `Controls/TabView.axaml` already separates CodeToolPanel (left) from the complete
   hex editor DockPanel (right) around a persistent splitter. The code-mode row can
   accommodate compact Hex/Map controls by limiting the mode selector's width.
   Replace/hide the entire hex section, including anchor/headers/scrollbars, rather
   than just overlay its byte surface. Preserve its instance, selection and scroll.
2. Map context is not currently explicit script-tab state. `ViewPort.GotoScript`
   reuses/deduplicates script tabs; `CreateDuplicate` shares Model/history and templates
   but does not transfer the originating map ID. Mapper construction defaults to bank
   3/map 0. Existing MapEditor/PrimaryMap therefore cannot reliably identify the intended
   reference map of every script tab. Event double-click, pointer navigation, context-menu
   new-tab opening and map scripts must carry deliberate source context.
3. A script may have multiple map callers, or be opened directly by address. Capture
   the launching map's model identity and bank/map ID; do not guess ownership from the
   script address, assume the default map or follow an unrelated ROM's last active map.
   Show map name/ID. Without context, explain the absence and retain Hex access.
4. BlockMapViewModel and PixelImage provide reusable graphics/layout/cache infrastructure,
   but PixelImage explicitly documents lazy event discovery changing model metadata/history.
   BlockMapViewModel uses the owning ViewPort's CurrentChange token; event view models can
   discover/format scripts and text. Embedding MapTab or merely removing mouse handlers
   does not establish a read-only path. Sharing its PrimaryMap also shares selection,
   collision highlights, pan/zoom and cache state with the actual map editor.
5. Input routing needs explicit coverage. MainWindow normally restores focus to HexContent;
   global clipboard/delete commands fall back to the selected ROM when no TextBox owns focus.
   A preview must not expose hidden hex editing through panning, toggles, focus restoration,
   Command-V or Delete. Preserve script caret/selection, completion dismissal and draft lifecycle.
6. Persist view preference in application settings; keep map identity and pan per live tab/map.
   Switching Hex/Map must not change CodeTool.Mode, selection, address, history or compile timing.
   Restrict the initial feature to the overworld Script tool; other tools/modes retain Hex.

## Synthetic probe

The ignored Probe.csproj links the existing SyntheticMapFixture and actual application
project. It draws an 80 × 50 map with and without one lazily discovered NPC, with fabricated
reference lists supplied before the baseline. Initial incomplete-fixture attempts were not
used as product failure evidence. Successful raw observations are in results.jsonl.

- 1,024,000 pixels in each case; first draw about 12.2 ms without NPC / 15.4 ms with NPC.
- Repeated PixelData access reuses the same array; about 0.0002 ms in this single run.
- Both preserve ROM bytes and serialized metadata.
- With the NPC, IsSaved changes true → false and Undo becomes available. Without the NPC,
  saved/history state remains unchanged. This is evidence against unguarded reuse in a
  new read-only pane, not a claim that ordinary map editing has a newly diagnosed defect.
- These are tiny synthetic block/sprite inputs, not real-ROM, large-map or compositor benchmarks.
- One RGB555 map array at that size is 2,048,000 bytes; two full BGRA buffers would add
  8,192,000 bytes. This arithmetic excludes sprite/tile/model caches and any detached ROM copy.

## Recommended implementation sequence — not implemented

1. Add minimal source-map context transfer to script opening/reuse/duplicate paths, with
   independent ROM identity and explicit unavailable/shared-script behavior. Regression-test
   context before adding the pane.
2. Establish a render-only snapshot boundary. Investigate lower-level graphics reuse versus
   detached-model rendering; prove unchanged live bytes, metadata, saved state, free-space state,
   pending/rejected source, diagnostics and Undo/Redo before selecting the mechanism. Do not
   refactor the whole map editor or invoke its repair/edit initialization just for a preview.
3. Display a clipped cached snapshot through a lightweight control. Pan locally by drag and
   trackpad scrolling; no map selection, tile/event editing, context menus or script execution.
   Show placed NPCs at stored map positions; do not imply a live player/camera/movement simulation.
4. Add compact Hex/Map selectors to the existing code-mode row and persist the preference.
   Preserve splitter size, hidden hex state, script caret/scroll and normal editor shortcuts.
5. Invalidate snapshots for relevant committed map/event/tileset/history/reload changes;
   coalesce refresh, bound retained snapshots, dispose resources, cancel stale queued work and
   avoid rebuilding maps per keystroke or pan. Identify map by stable bank/map plus model,
   not script ROM address, so script relocation cannot change map context.

## Acceptance checks

- Event double-click, pointer link, map script, reused/duplicate tab, two maps sharing a script,
  two ROMs, map rename/removal/close and direct address opening with no known map.
- Repeated Hex/Map toggles preserve hex selection/scroll and script caret/selection/source.
- Pending debounce, rejected draft/diagnostics, compile relocation, correction/retry,
  Save/Save As/Close/reload and HMA-006/007/008 protections remain intact.
- Panning/toggles, Command-C/V/X, Delete, Undo/Redo, Go To and focus restoration cannot edit
  hidden hex bytes or map events. Capture loss/tab change/Escape/deactivation end panning.
- Map/tileset/event edits, Undo/Redo and external reload refresh only the current valid snapshot;
  closed/replaced contexts cannot receive old frames or steal focus.
- Small windows/splitter widths/Retina dimensions/large NPCs and bounded cache allocations;
  cached panning does not redraw the map. Persistent-setting reload and invalid defaults.
- Real tutorial-village check on the native Mac after synthetic coverage passes.

Assessment: feasible and worthwhile as a bounded reference pane. The panel swap is small;
correct map context, read-only rendering and keyboard isolation are the substantive work.

## Implementation outcome

The trial branch adds explicit per-script map IDs, copied into duplicate tabs and updated
on shared-script reuse through event and map-script routes. Rendering captures live bytes
and metadata on the UI thread, then uses an independent model/history off-thread; only
immutable pixels reach the pane. It does not embed MapTab or call live repair/render paths.

Hex/Map controls apply only to the overworld Script tool. The app remembers the preference;
weakly keyed per-tab map offsets survive view recreation within the session. The complete
hex pane is hidden and disabled while Map View is active. Source focus is retained/restored,
and clipboard/delete routing cannot fall back to hidden hex editing. Escape, capture loss,
tab changes, detachment and window deactivation end panning.

Only the active pane holds an image; queued results are guarded against replaced/closed
contexts. Map edits refresh on returning to the script tab, with additional Undo/Redo,
reload and loading-completion notifications. Script typing and panning reuse the displayed
snapshot; there is no polling or per-keystroke map rendering. Pending source is not flushed
by toggles or preview preparation. Native trackpad feel, real-ROM cost and Retina appearance
remain user verification, separate from the synthetic checks.

The implementation's focused coverage and final full-suite counts are recorded in
[ScriptMapReference](ScriptMapReference/README.md) and
[mac-verification.json](script-map-reference/mac-verification.json).
