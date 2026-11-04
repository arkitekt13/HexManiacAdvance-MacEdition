# Mac input and history refresh audit

Date: 2026-11-04. Starting state: clean `codex/npc-hover-preview`, `550d4504`.
Implementation branch: `codex/mac-input-history-audit`.
This is a technical first-pass audit, not the planned end-user Mac guide.

The priority was to prevent focus/secondary gestures from becoming ROM edits and to prevent
editable fields or previews from displaying an abandoned history state. Fixes have separate
commits and CodexChanges entries 088–094. All new tests use fabricated models and offscreen
Skia windows; no user ROM, emulator preference, real image clipboard, theme setting or account
is modified. The headless text clipboard is used only for text-focus routing checks.

## Confirmed findings and fixes

| Area | Failing-before evidence | Result / commit | Regression |
| --- | --- | --- | --- |
| Image canvas Control-click | Selected draw color 7 replaced sprite pixel 1 instead of sampling it. | Existing eyedropper path; physical release retained. `c48f24d6`, entry 088. | ImageEditorInput: 38 |
| Palette named colors | Entering `7FFF` produced Avalonia's `White` string; the hex-only swatch decoder silently skipped the edit. | UI color names and existing theme hex both propagate; Core encoding unchanged. `f322b3b2`, entry 089. | ColorEditorInput: 93 |
| Palette editor after Undo | ROM returned to the original color, but the open field and its local delta baseline retained `1234`. | External content replacement closes stale editors; own synchronous writes preserve focus. `439ce5af`, entry 090. | PaletteHistory: 36 |
| Palette/image clipboard initialization and focus | Cmd+C crashed with a filesystem captured as null before attachment; native Edit Copy targeted the image; disabled palette Paste could reach an ancestor image binding. | Live parameters, palette/text focus ownership and disabled-operation consumption. `0d8b4bd1`, entry 091. | PaletteClipboard: 40; existing ClipboardRouting: 78 |
| Hex and Copy Bytes Control-click | Physical secondary-click worked; Control-left-click did not open the same actions. | Existing menus reachable with early modifier release/cancellation handled. `4ab23f49`, entry 092. | MacSecondaryActions |
| Block tile Control-click | An initialized rendered block tile took the painting route, retaining tile 1 instead of sampling tile 0. | Existing tile sampling precedes painting; normal painting/history preserved. `4ab23f49`, entry 092. | MacSecondaryActions |
| NPC summary after Undo/Redo | Bytes/metadata restored correctly while the edited dialogue remained visible in a same-cell hover. | Explicit presentation-cache revision and same-cell rebuild on map refresh/data/visibility changes. `60e9a934`, entry 093. | MapHistoryRefresh: 23; MapHoverPreview: 134 |
| Palette Control-click | Opened the primary swatch editor instead of the action menu. | Existing secondary actions, clicked-color selection and physical release retained. `c27ed695`, entry 094. | MacSecondaryActions: 43 total |

The color and block reproductions were isolated from test-harness issues before recording them:
raw-ROM and rendered color channel orders are checked separately; block tiles are initialized
through block selection and backed by a formatted blockmap so primary drawing reaches the
actual write path. No Core parsing rules or fixture assertions were changed to require bugs.

## Coverage and preservation

| Input/history surface | Evidence and limits |
| --- | --- |
| Script and ordinary text clipboard | ClipboardRouting uses actual keyboard/native Edit commands, retained rejected source, focus and exact model/history snapshots. It still passes with palette routing added. |
| Dialogue/movement assistance | Existing ScriptAssistance, DialogueScaffolding, AutocompleteMapping and ScriptWorkflowSafety remain in the complete suite, including pending/stale callbacks, rejected drafts, correction, pointers and history. |
| Palette clipboard and input | New real popup/keyboard/native/context-menu cases preserve established `r:g:b` clipboard text, paste-selection advance, raw color channels, unrelated sprite/pointer bytes and metadata. Null/invalid text with a valid image payload cannot become image paste. Popup text retains native text behavior. |
| Image editor | Compressed/uncompressed sprites; actual drawing, sampling, Cmd+Z/Cmd+Shift+Z and bound bitmap hashes. Canvas shortcuts decline palette/text/combo/slider focus; tests directly exercise palette/text ownership. |
| Palette history | Complete bytes/metadata and displayed bitmap snapshots; redo availability after inspection; correction after Undo; model replacement; palette context switching; detached notifications and reattachment. Own field refresh must not dismiss the editor. |
| NPC map history | Actual NPC selection and dialogue editing, same-cell preview after Undo/Redo, refreshed child text, and real flag/script-address fields. Exact model/metadata and preserved Redo are required. Hiding/revealing events invalidates old summaries. |
| Secondary and trackpad-oriented gestures | Image, block, palette, hex, byte summary, existing map/event/connection/tab paths; physical right and Control-left where appropriate. Early Control release, captured-context cancellation, ordinary primary actions and unchanged selection/paint algorithms are checked. Hardware trackpad/native Cocoa delivery remains for confirmation. |
| Other specialized editors | Existing PortraitFreshness, StreamTemplates, WildEncounterEditing, TextSelection, MapEventEditing and MapSpriteSelection verify their documented selectors/streams, names/encounters, pointers, pixels, metadata and history. Their individual READMEs define exact coverage. |
| Save/reload/close | Existing SaveSafety, ExternalFileChanges and ScriptWorkflowSafety remain unchanged and are included in the complete Mac run. No new persistence or undo-stack algorithm is introduced. |

## Deliberately unclaimed coverage / next targets

- `TableGroupPanel` has custom-rendered grouped-field input paths, placeholder text context
  actions and no-op key handlers also present in the upstream-derived implementation. They
  need a dedicated rendered grouped-field fixture before changing behavior. This pass does
  not claim that enabling every grouped custom control gives full native text/menu parity.
- `EditableComboBox` has no current production XAML call site; its legacy selection mapper
  was reviewed but not changed speculatively.
- Multi-frame/tile-palette combinations and the tileset-width slider need dedicated fixtures
  beyond the compressed/uncompressed image cases. Their focused-input guards are present;
  this is not a claim that every image mode, import or export is covered.
- Native desktop color sampling, real PNG clipboard interchange, Cocoa menu/trackpad sequences,
  hardware scaling/accessibility, external file dialogs and emulator launch are not exercised
  by these new groups. Existing emulator/file tests retain their documented stub/native limits.
- The separate upstream xUnit/optional-ROM and integration suites are not included in Mac
  assertion counts and were not rerun as part of this pass. Prior documented baselines and
  known retained-draft expectation differences remain separate.

Shared Core changes are limited to map-summary cache invalidation. ROM/script numeric parsing,
color encoding/quantization, script compilation, resource allocation, pointer relocation,
shared history algorithms and game-engine behavior are preserved. The unused flag/variable
scanner investigation is a separate scope and was not altered.

## Validation

Run the complete Mac suite with `scripts/verify-mac.sh --no-restore`; it rebuilds each group
before execution and rejects missing/duplicate/empty assertion counts. Focused group counts
above are actual assertions, not xUnit case counts. The complete fresh run passes **46 groups / 7,883 assertions**, with zero failures and zero
skipped groups. The six new groups contribute 273 assertions. The log for this run is
`/private/tmp/hma-input-history-audit-mac.log`; CodexChanges entry 095 records the same result.
