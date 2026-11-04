# Script responsiveness and stale suggestions — 2026-11-04

Checkpoint: ad86bb30, clean application source. This follow-up adds investigation tooling and
documentation only. No game ROM is opened and no compiler/editor behavior is changed.

## Confirmed suggestion lifecycle gaps

The production AutocompleteOverlay uses a separate Popup, with IsOpen bound to the overlay's
local IsVisible. LostFocus clears it. TargetTextChanged can show it again without checking
whether its target is focused, attached or effectively visible. Changing the overlay's
DataContext does not clear previously offered options.

The standalone headless probe uses the actual TextEditor, bound CodeBody and popup controls:

| Scenario | Editor focused | Popup.IsOpen | Options |
| --- | --- | --- | ---: |
| Focused positive control | Yes | Yes | 8 |
| Focus a different control | No | No | 0 |
| Then update the bound script source | No | Yes | 8 |
| Replace overlay context with an empty CodeBody | Yes | Yes | 8 stale |
| Hide the parent containing editor and overlay | Yes in headless state | Yes | 8 |
| Restore that parent without typing | Yes | Yes | 8 retained |
| Replace target editor context | Yes | No | 0 |
| Replace Target | Old editor remains focused | No | 0 |
| Detach editor and overlay | No | No | 0 |
| Reattach without typing | No | No | 0 |

The unfocused-source-update case uses a real bound Content update, not a synthetic LostFocus
event. Six preparations require an open focused popup before probing. Initial offering uses a
routed TextChanged positive control, consistent with AutocompleteMapping. Synthetic bytes and
metadata must remain identical after all scenarios. Popup state is recorded rather than
incorrectly treating known stale states as passing behavior.

This reproduces a plausible source of the reported intermittent stale lists; it does not
establish the user's exact native trigger. Popup.IsOpen while its owner is hidden is a lifecycle
finding, not proof of what Cocoa visibly displays. The probe does not cover every native combo
dropdown, window deactivation sequence or list type.

Reproduce from the repository root:

```sh
dotnet build tools/AutocompleteLifecycleProbe/AutocompleteLifecycleProbe.csproj -p:NuGetAudit=false
mkdir -p artifacts/script-performance
dotnet tools/AutocompleteLifecycleProbe/bin/Debug/net8.0/AutocompleteLifecycleProbe.dll > artifacts/script-performance/popup-lifecycle-current.json
```

Recommended fix: allow automatic opening only for the current attached, focused, effectively
visible target; clear options on context/target changes and when the owner becomes hidden or
detached. Track owner/window lifetime correctly and check deactivation behavior. In particular,
do not gate opening on the overlay's own effective visibility, because it starts hidden.
Test queued updates after dismissal and preserve the non-focusable suggestion buttons so
mouse-down/release acceptance, keyboard navigation, Enter/Escape and continuous typing still
work. Apply these checks to script, encounter/stream, trainer/mart and other shared-overlay
hosts. Keep compiler debounce and draft/history handling unchanged.

## Fresh scripting measurements

Existing ScriptEditingBenchmark --npc completed all 18 fabricated 100/250/500-line cases,
passing 2,892 measurement/invariant checks. All 24 overlay completion probes agree with Core;
the previously repaired later-line mapping has not regressed. Measurements use Release/.NET
8.0.5/arm64, ten warmups and seven measured edits per case. The editor is standalone headless
Skia; edits are controlled bound text updates, not native keyboard input.

At 500 actual lines (about 9,294 characters, 49 branch calls and 49 dialogue blocks):

| Edit with attached editor | Immediate ms | Deferred compile ms | Presentation ms |
| --- | ---: | ---: | ---: |
| Argument | 9.97 | 33.23 | 38.95 |
| Dialogue character | 9.94 | 33.61 | 39.71 |
| Comment digit | 9.89 | 46.03 | 22.65 |

These are medians of separate phases; do not add them as a native keystroke latency claim.
Deferred time excludes the existing 500 ms idle wait. The same-size synthetic edits must compile
to exactly the expected changed byte (or none for a comment), defer ROM writes until compilation,
preserve the settled address and retain paired UI/Core source and six-layer hashes. Allocation
is temporary managed memory on the measured thread, not retained process RAM.

A fresh isolated ScriptHighlightBenchmark run gives:

| Lines | Core highlighting ms | Allocated bytes/edit |
| --- | ---: | ---: |
| 100 | 1.366 | 169,448 |
| 500 | 10.403 | 834,072 |
| 2,000 | 89.439 | 3,332,424 |

This separate workload has 256 keywords, 4,096 constants and a comment on every line. It cannot
be numerically attributed to the NPC pipeline above. It verifies unchanged source and aligned
layer lengths and exports all six hashes. It measures no compiler or UI layout work.

## Priorities

1. Fix the reproduced suggestion lifecycle gap with focused real-control regressions.
2. Profile the complete TabView edit/presentation path before another map-wide optimization:
   full-document highlighting, TextEditor FormattedText width measurement, bindings and layout
   all remain relevant. Current evidence does not assign all presentation time to width sizing.
3. Independently test the repeated comment search in TextEditorViewModel.UpdateLayers:
   each iteration searches from the start after blanking the previous comment. Its scaling is
   a candidate for a narrow progress-cursor optimization, with exact layer/source equality.
4. Expand compiler profiling only with representative varied operands, growing/shrinking
   scripts and new auto streams. The earlier per-compilation argument reuse is already present;
   these measurements do not justify compiler rewrites or global caches.

Preserve numeric/name matching priority, pointer relocation, compilation/resource atomicity,
rejected source/diagnostics, correction/retry, Save/reload and Undo/Redo protections. Do not
move mutable model/compiler work to a background thread as a shortcut.

Raw outputs are under ignored artifacts/script-performance: npc-current-review.json,
highlight-current-review.json and popup-lifecycle-current.json. Existing AutocompleteMapping
still passes 294 assertions, demonstrating that acceptance/mapping coverage missed these
lifecycle cases. The complete Mac suite was not rerun for investigation-only changes; the
preceding implementation checkpoint passed 47 groups/7,933 assertions. Native popup behavior,
the user's exact list/trigger, real-ROM latency and full TabView profiling remain unclaimed.
