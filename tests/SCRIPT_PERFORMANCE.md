# Script editor responsiveness checkpoints

Branch: `improve/script-editor-responsiveness`, based on `26244704`.
The reported long-script stutter was observed in the Windows editor; equivalent Mac stutter
has not been reproduced or measured. These checkpoints target confirmed repeated work,
not a claimed root cause or a promised end-to-end latency improvement.

## Diagnostic presentation coalescing

The actual Avalonia/Skia offscreen editor rebuilt its comment/underline inline collection
33 times when one validation pass cleared diagnostics and published 32 errors/warnings.
The regression failed before the change (`/tmp/hma-diagnostics-before.log`).
The editor now requests one render-priority update and reads the current model when it runs.
The same burst rebuilds the collection once, retaining all 32 decorated spans, all text and
diagnostic records. Tests also cover clearing underlines, switching scripts while work is
queued, and unsubscribing the old script. Validation/compilation timing is unchanged.

Validation: `ScriptEditorLayout` passed 102 assertions; all 14 Mac groups passed 1,112
assertions (`/tmp/hma-diagnostic-coalescing-mac.log`). This is a deterministic reduction in
presentation work, not a measurement of typing latency. Compiler, ROM models, relocation,
pointer updates, flag/variable allocation, and game definitions are unchanged.
