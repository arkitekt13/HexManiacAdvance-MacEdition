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

## Per-character whitespace allocation checkpoint

The two whitespace-preservation loops in MutableString.Clear and CopyWhitespace now use
direct comparisons for the same CR, LF, and tab characters. Their generic params-array
membership calls allocated temporary objects for every character on repeated layer passes.
The tokenizer, numeric/comment parsing, layer construction, and compiler are unchanged.

A new allocation regression warmed these helpers and then executed twenty paired passes on
a 6,000-character buffer. Before the change it allocated **23,040,040 bytes**, failing the
4,096-byte budget (`artifacts/upstream-tests/run.SiPQkt`). Afterward it passed that budget,
preserved the source, and produced the exact expected spaces, tabs, and line endings.
This is an allocation budget, not a hardware-dependent elapsed-time assertion.

Final combined Release highlighting results compared with the original baseline:

| Lines | Original ms | Final ms | Original allocated bytes/edit | Final allocated bytes/edit |
| --- | --- | --- | --- | --- |
| 100 | 7.975 | 1.349 | 1,610,888 | 169,448 |
| 500 | 34.601 | 10.473 | 5,631,672 | 834,072 |
| 2000 | 197.034 | 89.860 | 21,558,024 | 3,332,424 |

All six layer hashes match the original baseline at all three sizes. The 2,000-line case
uses about 85% less temporary allocation and about 54% less highlighting time in this run.
Raw medians, all seven timing samples, runtime identity, and output hashes are in
`artifacts/script-performance/highlight-final.json`; earlier checkpoint reports are retained.
This remains a synthetic highlighting measurement, not proof of improved end-to-end Mac or
Windows editing latency. Full-document highlighting and other measured work remain possible
future targets; no changes to their algorithms are included here.

Final validation:

- First upstream project: **2,657 cases; 1,609 passed, 0 failed, 1,048 skipped**, zero engine
  errors (`artifacts/upstream-tests/run.51QCaK`), including 25 new highlighting checks.
- Second upstream project: **67 cases; 26 passed, 0 failed, 41 skipped**, zero engine errors
  (`artifacts/upstream-tests/run.Mu7p1J`).
- Mac regression suite: **14 groups, 1,112 assertions**, no groups skipped
  (`/tmp/hma-script-performance-final-mac.log`).
- Diff inspection against `26244704` confirms no changes to Core Models, CodeTool, CodeBody,
  Flags, EventTemplate, or ChangeHistory: relocation, repointing, free-space search, unused
  flags/variables, undo, and compiler behavior are outside this work's code changes.

ROM-dependent tests remain skipped without fixtures. Native interaction with a long script
has not been profiled, and the user's observed Windows stutter is not claimed resolved.
