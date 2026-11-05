# Image-preview and border-panel lifecycle repair

Investigated at `bf271865` on November 5, 2026, using generated probes and synthetic models.
The border repair is commit `7ba9fdf3`; image-hint changes are a separate implementation.

## Confirmed border failure

BorderChanged requested a cache refresh that replaced BorderEditor, restored its open state,
then closed the retired editor while the panel still observed it. The live `shown` class was
removed/re-added and its real 300-ms slide transition restarted. Seven changed tiles produced
seven hide/show cycles. Three fresh processes reproduced a four-cell mouse stroke reaching only
two cells as the border image moved; disabling animation only in the diagnostic probe allowed
all four cells. No-op writes caused no cycles, and exact Undo/Redo preserved the actual edits.

The new `border-visible-single` regression fails before the fix on its first transient hide.
Refreshing the existing editor's dimensions/render data retains visibility, subscriptions and
pointer geometry without changing cache-invalidation scope or drawing/history algorithms.
All 38 gesture scenarios pass 1,343 assertions, and BorderBounds passes 195, including refreshed
custom dimensions, normal animated close/reopen, no-ops and exact Undo/Redo.

## Confirmed image-hint failures

The shared HexContentToolTip image template has natural image dimensions. Native popup bounds
constrained a 128x1024 hex image to a 400-pixel window while its image remained 1024 pixels tall.
It covered the pointer and triggered HexContent's exit/close handler; stale cached visibility
also prevented same-pointer re-entry for ordinary small hints. A 128x2048 Go To image likewise
remained 2048 pixels tall inside a 400-pixel popup and intercepted its source button click;
the 128x64 control stayed clickable. A continuous oscillation in every host is not claimed.

ImageHoverPreviews fails against the old code separately for oversized hex hover and the
compiled Go To image-token binding. Its repaired 56 assertions protect real image bounds,
input routes, text hints, keyboard/mouse token selection and source/context/window lifetimes.

ImagePreviewLayer now fits the complete text/image content in a noninteractive, window-owned
Viewbox. HexContent explicitly owns cell hints and resets stale hover state on real dismissal;
unformatted-cell movement keeps its cheap cell cache. Go To routes image-bearing tips to the
same layer and keeps native text-only tips. Collection/context changes, pointer exit, scroll,
key presses, source detachment, Go To closure, window deactivation and window detachment clear
image hints. No image popup or global tooltip-style change is introduced.

Avalonia 11.2.3 source cross-check:
[ToolTip](https://github.com/AvaloniaUI/Avalonia/blob/11.2.3/src/Avalonia.Controls/ToolTip.cs),
[ToolTipService](https://github.com/AvaloniaUI/Avalonia/blob/11.2.3/src/Avalonia.Controls/ToolTipService.cs).
These sources distinguish the automatic popup service from the app's explicit cell-exit logic.

## Scope and limits

NPC/script summaries and blockset selectors already use bounded noninteractive map overlays;
object/NPC graphic rows already reserve a fixed-size image slot. The audit reviewed those paths,
shared PixelImage buffering/context guards, inline sprite/tile displays, selected-block hover,
image-editor rendering and deliberate palette/map-list popups. No additional flicker reproduction
justified changing those controls. The Core renderer, script parser/compiler, numeric parsing,
pointer relocation, game-engine data and edit-history algorithms remain intact.

Early generated-probe setup/constructor mistakes were corrected before collecting successful
results. During implementation, an artificial root-Editor swap twice in one MainWindow reached
a quick-edit event-list mutation on shutdown; that separate root-replacement path has not been
assessed or repaired here. Document switching is tested through actual tabs instead, and no
event-dispatch behavior was altered to make this pass.
Native macOS trackpad, popup/compositor and multi-display behavior remains a live verification
step; headless bounds/input assertions do not certify native rendering. The separate upstream
xUnit suites and runtime migration are outside this repair.

## Final verification

The complete serial `scripts/verify-mac.sh --no-restore` run passes **60 groups / 18,036
assertions / zero failures / zero skipped groups**, including all earlier script workflow,
clipboard, map, emulator-launch and history checks. Exact counts are in
[mac-verification.json](preview-lifecycle/mac-verification.json). Source builds emit no compiler
warnings. MapImageExport retains its NU1900 package-vulnerability-feed diagnostic; successful
dependency security-data retrieval is not claimed. No user ROM/preferences changed, and native
compositor/trackpad and separate upstream xUnit coverage are not inferred from this run.
