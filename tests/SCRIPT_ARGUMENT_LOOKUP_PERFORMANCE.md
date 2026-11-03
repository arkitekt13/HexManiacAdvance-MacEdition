# Per-compilation script argument reuse — 2026-11-03

Base checkpoint: `ec58d995`; branch: `improve/script-argument-lookup`. macOS ARM64 / .NET 8.0.5,
Release, two warmups and seven measured samples per case. The original application baseline
is unchanged between `8aaa6755` and `ec58d995`; intervening commits add investigation/tests only.

## Interpretation rules remain unchanged

No numeric parsing priority was changed. ScriptArg still tries the existing enum matcher,
then its original explicit `0x`/`0X`/`$` hexadecimal and decimal fallback; offsets are applied
after resolution. The enum matcher still gives unquoted decimal integers its existing fast
path, then quoted/exact/partial/duplicate name matching. Consequently numeric-looking aliases
can still take precedence over a hexadecimal fallback. No guessed conversion is introduced.

A new opt-in LabelLibrary context remembers the original matcher's success/value (including
failed enum searches) by exact enum name and operand string within one compile. ScriptParser
creates a fresh context for each compilation. Different models bypass reuse; model-cache-scope
or read-only culture changes clear it; mutable cultures bypass reuse. Standard list invalidation
boundaries are retained. The original two-argument constructor remains uncached. Exceptions
are neither cached nor suppressed. There is no global or cross-edit cache.

Pointer/address parsing, local-label resolution, decimal/hex fallback code, enum offsets,
compiler relocation/formatting/history algorithms, researched addresses and definitions are
unchanged. Tests explicitly distinguish decimal word `1234` from hexadecimal pointer address
`<001234>`, including the pointer prefix form, and compare numeric-looking alias collisions.

## Deferred edit/update benchmark

These columns use the standalone production TextEditor attached to headless Skia. They exclude
the 500 ms debounce wait. Figures describe repeated-argument synthetic documents, not native
typing latency, every script shape, or guaranteed application-wide speedups.

| Workload | Statements | Before deferred ms | After deferred ms | Before allocated bytes | After allocated bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| commands | 100 | 148.99 | 16.12 | 218,508,168 | 11,857,304 |
| commands | 500 | 709.42 | 32.20 | 1,083,115,456 | 41,275,688 |
| commands | 2,000 | 2795.41 | 90.19 | 4,325,361,960 | 151,738,072 |
| comments | 100 | 143.19 | 6.87 | 219,509,768 | 12,812,288 |
| comments | 500 | 707.13 | 33.90 | 1,088,189,216 | 46,349,240 |
| comments | 2,000 | 2802.28 | 97.79 | 4,345,656,160 | 172,032,144 |
| dialogue | 100 | 144.61 | 7.14 | 208,738,424 | 12,480,200 |
| dialogue | 500 | 665.98 | 35.10 | 1,034,260,472 | 44,616,744 |
| dialogue | 2,000 | 2653.95 | 98.93 | 4,129,968,520 | 165,130,296 |

All 18 baseline/current six-layer hash sets match. Every measured edit must still compile
without errors, leave bytes unchanged before the deferred action, retain its address for
these same-size edits, and change exactly the intended argument byte. Dialogue is kept at
fixed synthetic addresses so this benchmark does not time new auto-stream allocation.

## Isolated equivalent-spelling compilation

Each spelling must compile to exactly the same bytes before and after; all checks pass.

| Repeated variable spelling | Before ms | After ms | Before allocated bytes | After allocated bytes |
| --- | ---: | ---: | ---: | ---: |
| hexadecimal | 702.73 | 20.58 | 1,068,447,352 | 26,623,472 |
| decimal | 7.68 | 16.75 | 24,153,352 | 24,101,752 |
| alias | 346.22 | 18.66 | 543,733,352 | 25,140,912 |

At 500 repeated hexadecimal operands, this run reduces compile time by about 97% (34x)
and temporary managed allocation by about 97.5%. The decimal control was already cheap and
shows timing variability/overhead in the current run; no decimal speedup is claimed. Paired
phase medians are not additive. JIT tiering, GC, and other host activity affect timings, so
read the complete samples rather than treating any single ratio as a universal guarantee.

## Correctness and limitations

The expanded ScriptArgumentSafety group passes 1,565 checks: 900 differential comparisons
cover 30 spellings, five declarations/offsets, two repetitions and three cultures, plus
invalidation, foreign/mutable cultures, pointer interpretation, original mutation safety and
allocation budgets. Full relocation/shrinking still preserves both callers, dialogue,
neighbors, bytes and metadata across history, with the documented free-space-cursor exclusion.

Warm successful/failed enum reuse allocates 7,424 bytes for 64 builds against a 32 KB budget.
The production 64-command compile allocates 4,832,112 bytes against an 8 MB budget, verifying
the real parser uses a fresh cache. These budgets are correctness/allocation checks rather
than elapsed-time thresholds. Allocation volumes are temporary managed objects, not retained
RAM or native/whole-process memory.

The differential corpus exposes an existing sparse-list partial-match exception on `$4001`.
Both reference and cached paths throw the same exception types; HMA-003 tracks it separately.
This optimization does not silently change precedence to avoid that pre-existing bug.

Repeated operands benefit most. Many distinct operands, repeated model writes/stream updates
that invalidate scopes, or mutable cultures can reduce the benefit. Full-document highlighting,
width measurement, rendering, autocomplete/help bindings, flag/variable allocation and native
input/compositor costs remain outside this optimization. The original Windows stutter has not
been reproduced. New auto-stream allocation and every script dialect/graph are not exhaustively
covered by these synthetic measurements.

## Evidence

Raw complete reports remain under ignored artifacts/script-performance/:
edit-pipeline-cached.json, edit-pipeline-components-cached.json, and
edit-pipeline-lookups-cached.json, alongside the original baseline reports. All original/current
layer/compiled-byte hashes match; all 1,596 benchmark invariants pass. The benchmark now reads
HMA_BENCHMARK_REVISION for honest checkpoint labeling rather than hard-coding the original
baseline identity. See tools/ScriptEditingBenchmark/README.md for reproduction commands.

Validation: all 24 Mac groups pass 4,702 assertions, with no groups skipped. First upstream
project: 2,715 cases, 1,780 passed, 0 failed, 935 skipped, 0 engine errors
(artifacts/upstream-tests/run.KeaiQp). Second upstream project: 67 cases, 26 passed, 0 failed,
41 skipped, 0 engine errors (artifacts/upstream-tests/run.qihXI0). Optional fixture skips remain
explicit; no fixture was relabeled to force coverage. The supplied source and staged FireRed
1.1 copies retain their original SHA-256 after testing; neither was modified.
