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

## Exact-match syntax lookup checkpoint

Syntax highlighting now caches ordinal keyword/constant sets and invalidates them when the
corresponding observable dictionary changes. The existing tokenizer, keyword precedence,
comments, numeric formatting, six layer strings, and source/caret behavior are retained.
Twenty-four new tests cover these semantics, dictionary edits, duplicates, multiline input,
and quoted EOF input. The full suite caught a new substring bounds regression in an
unchanged pointer-table test; the highlighter now rejects the same out-of-range candidate
that the old Match method rejected, and four focused EOF cases guard that behavior.

The benchmark uses 256 keywords, 4,096 constants, two warmups, and seven measured edits per
size on macOS ARM64 / .NET 8, Release. Times are medians for Core highlighting only:

| Lines | Before ms | Lookup checkpoint ms | Before allocated bytes/edit | Lookup allocated bytes/edit |
| --- | --- | --- | --- | --- |
| 100 | 7.975 | 2.313 | 1,610,888 | 1,627,688 |
| 500 | 34.601 | 13.492 | 5,631,672 | 6,187,824 |
| 2000 | 197.034 | 94.978 | 21,558,024 | 21,902,200 |

All six layer SHA-256 hashes match at every size. Candidate substring allocation increases
memory per edit modestly; the improvement is CPU work, not an allocation reduction. JSON
samples and output hashes are retained under ignored `artifacts/script-performance/` as
`highlight-before.json` and `highlight-lookup.json`. No timing threshold gates correctness.
These figures do not measure compiler work, UI layout, total typing latency, Windows, or real
ROM workloads; they do not establish a fix for the user's Windows stutter.

Validation: first upstream project **2,656 cases: 1,608 passed, 0 failed, 1,048 skipped**
(`artifacts/upstream-tests/run.xrXoPm`); second project **67 cases: 26 passed, 0 failed,
41 skipped** (`artifacts/upstream-tests/run.52qfE3`); zero engine errors in both. All fourteen
Mac groups passed **1,112 assertions** (`/tmp/hma-script-optimizations-final-mac.log`).
Only presentation highlighting changes in Core; compilation, repointing, allocation, flags,
variables, model caches, and ROM/reference definitions remain unchanged.
