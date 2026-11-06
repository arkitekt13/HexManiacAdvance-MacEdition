# Script Map View memory review — 2026-11-06

Starting checkpoint: `cdf8835b`; branch `codex/map-reference-memory-review`. Production map
rendering/lifecycle code is unchanged. The companion dialogue-warning change does not affect
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
