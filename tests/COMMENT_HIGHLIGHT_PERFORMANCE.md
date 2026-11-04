# Comment-search progress — 2026-11-04

The highlighter previously searched from character zero after each comment was cleared.
Normal comment markers cannot occur inside that cleared text, so dense scripts repeatedly
scanned a growing blank prefix. The production change resumes at the end of the previous
comment, resetting to zero between the line-comment and block-comment phases.

Clearing replaces non-CR/LF/tab characters with spaces. A marker containing a literal space can
therefore form a new match earlier in the buffer: `/*/* tail` with line marker `/* ` is one
example. Such markers retain the legacy restart-at-zero behavior. This is a presentation-only
optimization, not a change in recognized comments or script parsing.

## Measurements

macOS arm64/.NET 8.0.5, Release. The unchanged ScriptHighlightBenchmark uses 256 keywords,
4,096 constants, two warmups and seven measured content changes per size. These are Core-only
medians, excluding compilation, native typing and UI layout:

| Lines | Before ms | After ms | Before bytes/edit | After bytes/edit |
| --- | ---: | ---: | ---: | ---: |
| 100 | 1.341 | 0.660 | 169,448 | 169,448 |
| 500 | 10.468 | 2.505 | 834,072 | 834,072 |
| 2,000 | 90.044 | 7.785 | 3,332,424 | 3,332,512 |

All six layer hashes match at every size. The 2,000-line case is about 91% faster in this run;
allocation is essentially unchanged (88 bytes of measurement variation at that size). This is
not a native keystroke latency promise or a compiler speedup.

An isolated legacy-versus-cursor scan benchmark attributes the repeated work independently of
tokenization, compilation or UI. At 2,000 shorter lines, line-comment scanning falls from 45.259
to 0.217 ms and block-comment scanning from 52.514 to 0.155 ms. Basic/comment output pairs and
hashes agree in all six size/type cases. These prototype phase medians use three warmups,
seven samples and disabled tiered compilation; they cannot be added to or subtracted from the
whole-highlighter medians above, whose document text and JIT conditions differ.

Raw reports are in ignored artifacts/script-performance: comment-highlight-before.json,
comment-highlight-after.json and comment-scan-isolated.json. Baseline application checkpoint
is 769825a9. No user ROM or model service is used.

## Reproduce and preserve

```sh
dotnet build tools/CommentScanBenchmark/CommentScanBenchmark.csproj -c Release -p:NuGetAudit=false
DOTNET_TieredCompilation=0 dotnet tools/CommentScanBenchmark/bin/Release/net8.0/CommentScanBenchmark.dll artifacts/script-performance/comment-scan-isolated.json
dotnet run --project tools/ScriptHighlightBenchmark/ScriptHighlightBenchmark.csproj -c Release -- artifacts/script-performance/comment-highlight-current.json
```

CommentHighlighting compares all six actual layers against an independent legacy oracle over
2,320 source/delimiter/syntax configurations and six frozen preformatter cases, with 4,652
assertions. Source length, CR/LF/tab positions, preformatting and diagnostics remain intact.
No compiler, tokenizer, numeric precedence, resource allocation/relocation, history, source
retention, caret or game-engine code is changed. Width/layout investigation is a separate step.

Full verification: scripts/verify-mac.sh --no-restore freshly builds and passes 49 groups and
12,670 assertions, with zero failures and zero skipped groups. This includes script resource/
history/source-retention/save protections and the recent suggestion dismissal/acceptance tests.
The separate upstream xUnit suite was not rerun; fresh dependency builds retain eight existing
Core warnings. Timing is descriptive; no elapsed-time threshold gates the regression suite.
