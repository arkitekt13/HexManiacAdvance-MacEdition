# Script Map View memory review — 2026-11-06

Starting checkpoint: `cdf8835b`; branch `codex/map-reference-memory-review`. The initial investigation left production map
rendering/lifecycle code unchanged; the follow-up below repairs shared control subscriptions. The companion dialogue-warning change does not affect
these map fixtures, which contain only an ending script. No user ROM or settings are used.

## Snapshot ownership is healthy in these measurements

Release/offscreen Skia runs use synthetic 16/32-MiB ROMs and 80×52, 128×128 and 256×256 maps.
After eight render/upload/clear cycles per size, **zero watched snapshots, pixel arrays or
bitmap wrappers remain alive**. Final post-GC heap differs from the warmed baseline by less
than 0.02 MiB in the final run. Live source bytes/metadata, saved state and Undo remain intact.

Bitmap storage implied by dimensions (not total app RAM):

| Map size | Display bitmap (32-bit pixels) |
| --- | ---: |
| 80×52 tiles | 4.06 MiB |
| 128×128 tiles | 16 MiB |
| 256×256 tiles (current preview maximum) | 64 MiB |

Snapshot preparation also clones the full ROM and builds temporary 16-bit pixel buffers.
Four overlapping renders in the final run produce sampled *managed heap increases* of about
96 MiB, 188 MiB and 400 MiB respectively, then release every watched result/pixel payload.
Those are bounded stress observations, not ordinary retained memory or a native RAM limit.
Cancellation/coalescing could reduce transient work later; these measurements alone do not
justify changing the rendering pipeline now.

## General open-document tab retention is reproduced

The actual MainWindow alternates between two documents twelve times, toggles completed
previews, then performs twelve rapid switches and waits/collects repeatedly. A Hex-only
control runs the same selection pattern without creating map snapshots.

| Mode | Warmed managed heap | Settled managed heap | Watched distinct detached TabViews still alive |
| --- | ---: | ---: | ---: |
| Hex only, baseline run | 95.7 MiB | 255.2 MiB | 12 |
| Map View, baseline run | 93.8 MiB | 248.9 MiB | 12 |
| Hex only, follow-up run | 95.6 MiB | 255.2 MiB | 12 |
| Map View, follow-up run | 93.9 MiB | 249.0 MiB | 12 |

The source document's PropertyChanged delegate contains 24 TabView handlers after switching.
TabView's general subscription code unhooks on DataContext changes, but not on visual detach;
that path predates Map View. The new map-specific lifecycle does unbind on detach. Exact
event-target inspection proves a stale retention path, while the paired Hex control shows
this is not specific to map images. No old bitmap wrappers remain in the final probe.

An owned-control experiment clears detached DataContexts, HexContent ViewPorts and TextEditor
contexts. It removes the identified TabView subscriptions, but **the watched controls and
heap remain retained**. Other ownership paths still need isolating; do not claim that one
unsubscribe or clearing DataContext is a sufficient fix. All watched objects become
collectible after the benchmark closes its window and releases the documents.

## Closing documents releases their data

A separate probe opens/renders/closes four synthetic documents while a different document
and window stay open. After settling and collection: **zero closed document ViewPorts,
models, TabViews or bitmap wrappers remain alive**. The remaining ROM bytes are unchanged.
Heap rises from about 38.4 to 61.1 MiB in this probe, consistent with additional views for
the still-open document; it is not evidence that closed ROM copies are retained.

## Safe follow-up plan

1. Add a failing lifecycle regression requiring detached controls to become collectible
   while their document remains open, after dispatcher work has settled. Cover Map and Hex.
2. Trace the remaining roots before patching: general TabView handlers, HexContent's
   collection/header/request-menu hooks, TextEditor model/diagnostic listeners, command
   subscriptions and cursor timers. Match every owner to detach/reattach/context changes.
3. Implement symmetric subscription lifetimes without clearing active source or forcing GC.
   Preserve scroll/caret/focus, tab reuse, pending/rejected drafts, history and keyboard routing.
4. Repeat the same managed/weak-reference probes before/after. Then verify native behavior;
   do not treat offscreen RSS or a cleanup experiment as a validated app fix.

Evidence: [baseline](script-map-memory/baseline.json),
[cleanup follow-up](script-map-memory/cleanup-probe.json),
[individual document close](script-map-memory/close-documents.json).
The [benchmark](../tools/ScriptMapMemoryBenchmark/README.md) reproduces the final protocol.
Baseline JSON predates the explicit cleanup experiment and source-byte check in the final
tool; these are observations, not a paired optimization benchmark. Additional exploratory
logs are under ignored `artifacts/script-map-memory`. Synthetic graphics/metadata are simpler
than a real tutorial ROM; Cocoa, Retina, real-ROM complexity and new-Mac behavior are unverified.

## Lifecycle follow-up: failing evidence and isolated owners

Starting production checkpoint: `d5630e1d`. The new TabViewLifetime regression fails on the
first actual Hex-only tab switch: the detached tab, hex, source-editor and header controls
are all alive while their documents remain open. This is independent of Map View rendering.

Tracing established multiple strong event owners, requiring more than one unsubscribe:

- Column header items retain anonymous callbacks capturing HorizontalSlantedTextControl;
  removed/reset rows also leave item subscriptions behind.
- TableTool.CurrentElementSelector retains AngleComboBox through HandleVMTextChanged.
- Image panels retain PixelImage and SelectionRender through model notifications, while
  Colors.Elements retains PaletteControl through PaletteContentsChanged.
- TabView, HexContent and source TextEditor also need balanced document/model listeners.

Hidden panels can acquire DataContexts without ever attaching visually. They must start with
manual model observation disabled, refresh from current state on attachment, unsubscribe on
removal, and restore exactly once on reuse. Clearing old DataContexts or adding app GC is
neither necessary nor used. The snapshot renderer/compiler/history algorithms are outside
this repair.

The narrower paired protocol omits large-map warmup and preserves its own evidence in
[before.json](tab-lifecycle-memory/before.json) and [after.json](tab-lifecycle-memory/after.json).
Both perform 12 completed and 12 rapid tab switches in each mode, then settle/collect:

| Mode | Before warmed → settled heap | After warmed → settled heap | Detached watched tabs before → after |
| --- | ---: | ---: | ---: |
| Hex | 60.5 → 225.7 MiB | 60.5 → 61.2 MiB | 12 → 0 |
| Map | 58.9 → 212.1 MiB | 58.8 → 62.3 MiB | 12 → 0 |

This avoids about 165 MiB (Hex) / 150 MiB (Map) of retained managed heap in this repeated-switch
workload. It is not a claim that initial map rendering became faster or native RAM fell by
the same amount. Stale source TabView handlers drop from 24 to zero; source bytes match and
all watched objects collect after window teardown. The original full-protocol data above
remains historical evidence with its different warmup, not an interchangeable baseline.

The [complete post-repair corpus](tab-lifecycle-memory/full-after.json) also reports zero
retained snapshots, pixel arrays and bitmap wrappers at all three sizes, zero detached watched
tabs in both modes, zero watched objects after window close, and zero closed documents/models
while another document remains open. Its warmed settled managed heaps are about 96.4 MiB
(Hex) / 97.5 MiB (Map), versus 255.2 / 248.9 MiB in the original full-corpus baseline.
Transient overlapping snapshot work still exists; no renderer optimization is claimed.

The first full regression run stopped at MapRendering: its live-zoom checks used never-attached
PixelImage controls. Production uses these controls in attached XAML views. MapRendering and
ScriptEditorLayout now attach their subjects to real headless windows while retaining every
pixel/layout/diagnostic assertion. This tests the repaired subscription lifecycle rather than
restoring the hidden-control ownership defect. The initial timeout log is preserved in ignored
artifacts; fresh complete verification is required after fixture alignment.

Final verification: **67 groups / 20,727 assertions, zero failures and zero skipped groups**
using `scripts/verify-mac.sh --no-restore`; [exact group counts](tab-lifecycle-memory/mac-verification.json).
TabViewLifetime contributes 41 assertions. Existing MapImageExport NU1900 reports the unavailable
package-vulnerability index; no build errors occurred. The benchmark Release build has zero
warnings/errors. Upstream xUnit was not rerun for the Avalonia-only repair. Native Cocoa/Retina,
real-ROM use and new-Mac behavior remain manual checks.
