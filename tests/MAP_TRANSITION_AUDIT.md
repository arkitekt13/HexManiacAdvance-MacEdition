# Map-tool transition audit — 2026-11-04

## State and scope

Started clean on `codex/mac-input-history-audit` at `2be6d838`. Pushed that branch to
`origin/codex/mac-input-history-audit` before auditing, including the previously unpushed work.
The remote-tracking branch matched HEAD immediately afterward. No merge was requested or made.

No production source or existing regression changes are included in this audit. The new
[review executable](../tools/MapTransitionReview/README.md) uses fabricated ROM bytes and actual
MainWindow/MapTab pointer/focus routing. It opens no user ROM and launches no emulator. Each
scenario runs in an independent process; setup asserts real render readiness, independent
single clicks, the expected pointer capture owner and actual focus movement where applicable.
Known failed observations are recorded separately from passing controls.

## Results

Fourteen isolated scenarios completed: 69 positive setup/preservation controls, 25 behavioral
observations, **11 passing and 14 failing observations**. Exit zero means the audit completed;
it does not declare failed invariants correct. These are not added to the Mac suite's assertion
count. Metadata remains equal to the starting snapshot in the 13 non-navigation scenarios.
The alias-navigation scenario records a changed metadata snapshot without claiming preservation
or damage; lazy navigation/formatting and alias effects were not isolated in this probe.

| Scenario | Observed behavior | Failed observations |
| --- | --- | ---: |
| Paint → Undo → click same cell | Expected 4001, actual 4000; fresh paint silently skipped. | 1 |
| Paint → Undo → Redo → change permission → same cell | Expected 0401, actual 4001; new permission silently skipped. | 1 |
| Lose map capture while painting, release outside | Button-free movement writes bytes; a later Undo also removes the earlier interrupted stroke. Next click paints. | 2 |
| Lose capture during Option pan | Button-free movement pans; the next click does not paint. | 2 |
| Lose capture during Control selection | Button-free movement writes no bytes, but the next paint becomes a multi-block selection and writes nothing. | 1 |
| Lose block-picker capture | Next single-block click becomes a multi-block selection. | 1 |
| Lose background-pan capture | Button-free background movement pans; next map click does not paint. | 2 |
| Space reveal → focus toolbar field → release Space | ShowBeneath remains true. | 1 |
| Space reveal → ROM tab → release Space → return | ShowBeneath remains true. | 1 |
| Paint down → ROM tab → release outside → return | No ghost write on hover, next paint works, but Undo removes both the old and new strokes. | 1 |
| Paint down → navigate to another bank alias → release | No ghost write; next paint works; Undo retains the prior stroke. Metadata equality deliberately not asserted. | 0 |
| Paint down → Escape → move while held | Brush clears, but movement still changes bytes. | 1 |
| Completed Option pan with modifier released early | No ROM writes; subsequent single-block painting works. | 0 |
| Completed Control selection with modifier released early | No ROM writes; subsequent single-block painting works. | 0 |

The capture probes explicitly call IPointer.Capture(null), verify capture is actually gone,
release outside the map, then move with no pressed-button modifiers. They reproduce missing
cleanup independently of any particular native macOS interruption. Background probing verifies
MapBackground owns capture; map probing verifies MapView; picker probing verifies BlockRenderImage.
Tab switching reacquires the replacement visual rather than operating a detached MapTab.

## Confirmed causes and recommended fixes

1. **HMA-011 — Stroke deduplication survives independent edits/history.**
   BlockMapViewModel.DrawBlock returns early when the coordinates equal lastDrawX/lastDrawY.
   Those fields are not scoped to a stroke, and ClearCaches/Undo refresh does not reset them.
   Reset duplicate-coordinate suppression at appropriate fresh-stroke boundaries. Preserve
   lastDrawVal across the first click/double-click fill sequence: it identifies the original
   full-word region for both ordinary and bag fill. Do not simply remove the within-stroke
   suppression or reroll random bag members on redundant movement.

2. **HMA-012 — Interrupted gestures lack a common finish path.**
   MapTab tracks withinMapInteraction plus per-surface capture flags, but registers no
   PointerCaptureLost cleanup. Detachment hides the tooltip without finishing the Core gesture;
   DataContext replacement also lacks old-context gesture completion. This explains ghost
   paint/pan, stale selection/picker state and merged history after a tab interruption.
   Introduce explicit, idempotent interaction completion for the owning old context, covering
   capture loss, detachment and context replacement. Retain valid edits already made, finish
   their history boundary exactly once, and clear capture, modifiers, previews and interaction
   state. Normal release itself drops capture before dispatching Core Up: guard against
   reentrant capture-loss handling and preserve the actual final release position.

3. **HMA-013 — Escape clears the brush but not the live stroke.**
   MapEditorViewModel.Cancel resets brush/selection fields but leaves interactionType active.
   Subsequent DrawMove sees both brush and collision unset and can enter block-swapping logic.
   End the active operation before clearing brush fields; test rectangles, events, selection,
   bag painting and cancellation of already-applied edits without accidental rollback or swaps.

4. **HMA-014 — Reveal mode can outlive its key owner.**
   MapTab.OnLostFocus clears HideEvents and waitingForControlUp, but not ShowBeneath. Space KeyUp
   is ignored when a text entry owns focus, or goes to the other tab. Clear transient reveal
   state when the map loses its owning context/focus/lifetime, while preserving legitimate
   text entry and Command hide-events behavior.

Prioritize interrupted-gesture cleanup and Escape, then stroke deduplication and reveal state.
Each implementation should first promote its failed observation to an enforced regression.
Use separate focused commits and keep the existing map/script protections passing.

## Preservation requirements for implementation

- Exact bytes, permission/elevation bits, unrelated regions, pointers and metadata remain intact.
- A completed or interrupted valid stroke is one Undo step; the following independent stroke
  is another. Redo restores exact random outcomes. Repeat around Save/reload and saved-state
  boundaries; no empty or duplicate history entries from repeated completion notifications.
- Preserve first-click/double-click region matching, HMA-010 bag fill, rectangle preview/backup,
  multi-block stamping, early modifier release, Option pan and Control-click event/warp menus.
- Test normal release, lost capture, Escape, map/tab/context replacement and detach while each
  relevant tool is active. Close stale previews and menus without applying them to another map.
- Retain text-field Delete/clipboard routing and HMA-006/007 script context/persistence behavior.

## Existing regression baseline and limits

Six existing groups were freshly built and run during this audit, all passing, zero skips:

| Group | Assertions |
| --- | ---: |
| MapSelection | 394 |
| MapHistoryRefresh | 23 |
| MapHoverPreview | 134 |
| MapEventEditing | 63 |
| MapEventMenus | 68 |
| BlockBagFill | 2,081 |
| Total | 2,763 |

Those passing groups cover completed operations and previous protections; the new interruption
failures illustrate missing transition coverage. The complete suite was not repeated for
investigation-only changes; its preceding freshly verified baseline is 51 groups / 15,209
assertions at 2be6d838. The upstream xUnit suite was not rerun. Fresh dependency builds retain
eight existing Core warnings.

Unmeasured: native app deactivation, Mission Control, trackpad hardware event delivery, closing
real unsaved documents mid-drag, independent-map/document byte isolation, active event creation/
drag interruption, border editing and connection-arrow dragging, 9-grid/wave-function painting,
and every combination of tools. The map-navigation probe uses two bank aliases to one fabricated
map, not two independent ROM regions. Existing event/menu groups are baseline checks, not proof
that every combined event transition passes. Native verification should follow the fixes.
