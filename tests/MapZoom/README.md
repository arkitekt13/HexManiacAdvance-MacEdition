# Full-editor wheel zoom regression and unresolved freeze

Added after a reported macOS beachball immediately after opening a map and zooming.
The same user could zoom normally after restarting. **The reported freeze has not been
reproduced or fixed. No application code was changed during this investigation.**

This executable mounts the production MainWindow and MapTab with the Mac work dispatcher,
opens three fresh sessions, and sends 120 routed wheel events per session. It checks direction,
the border threshold at 3x, a subsequent UI-dispatch heartbeat, a final map bitmap, and unchanged
model bytes, metadata, and undo availability. It does not wait for the first map bitmap before
starting the zoom sequence. This extends the older PixelImage-only scaling checks.

The default fabricated fixture needs no ROM. The optional local-ROM scenario uses the normal
Editor.Open background initialization path and Pallet Town (bank 3, map 0); its deeper-neighbor
assertion is intended for the supplied FireRed fixture. File-system stubs prevent settings,
sidecar, and file-watcher writes. ROM bytes are copied into memory and the source is checked
again afterward. No commercial data is included in this project.

After restoring dependencies:

```sh
dotnet build tests/MapZoom/MapZoom.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 60 dotnet tests/MapZoom/bin/Debug/net8.0/MapZoom.dll
python3 scripts/run-upstream-command.py 90 dotnet tests/MapZoom/bin/Debug/net8.0/MapZoom.dll 'sampleFiles/Pokemon FireRed.gba'
```

Use the external watchdog for hang investigations: an in-thread timeout cannot interrupt a
blocked UI thread. The normal Mac verification runner also discovers the synthetic group.

## Investigation results

- Synthetic: three fresh sessions, 360 routed wheel events, 1,098 checks passed.
- Supplied FireRed 1.1: three normal-open sessions, 360 routed wheel events, 1,102 checks passed;
  zoom ranged from 4x to 1/8x and back, with up to 11 visible maps.
- Earlier exploratory runs each completed 1,200 direct zoom calls in an isolated MapTab and in
  the full MainWindow. These exploratory calls were not native mouse/compositor tests.
- Native launch from the restricted session failed to initialize Avalonia's native render timer
  (error -6661). Native process sampling was also denied. Neither failure establishes the cause
  of the user's original freeze. The original frozen process was no longer available when the
  managed diagnostic tool was ready.
- Source-ROM SHA-256 remained
  `729041b940afe031302d630fdbe57c0c145f3f7b6d9b8eca5e98678d0ca4d059`.

These passing headless checks do not rule out an intermittent deadlock, native rendering/input
issue, or initialization race. No causal diagnosis or performance improvement is claimed.

If it recurs, before force-quitting, select HexManiacAdvance in Activity Monitor and use
**Sample Process** to save a stack sample. Alternatively, from the user's Terminal while it is
still frozen:

```sh
sample HexManiacAdvance 5 -file "$HOME/Desktop/HMA-freeze.txt"
```

Record the map, action, whether it happened on the first zoom, and mouse versus trackpad.
Sampling does not save unsaved editor changes. Inspect diagnostics before publishing them,
since they can contain local paths.
