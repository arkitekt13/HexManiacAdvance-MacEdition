# Implementation design review

Reviewed debf793b on November 5, 2026, before merging codex/id-suggestion-lifecycle.
No user ROM/preferences were opened or modified. This review makes a bounded scan refinement,
not a replacement of the reservation architecture.

## Verdict

The state separation is justified. The previous initial-scan HashSet combined ROM references
with session suggestions, causing both stale usage and unreleased IDs after deletion. Immediate
operation scopes, live usage and retained pending source have different lifetimes and need separate
tracking. No correctness failure in those ownership transitions was reproduced during this review.
The earlier focused group remains intact; the graph follow-up raises it from 81 to 87 assertions.

| Part | Assessment |
| --- | --- |
| Operation scopes | Necessary to keep multi-ID template requests distinct before bytes are written and release temporary ownership afterward. |
| Pending-source ownership | Necessary to preserve uncompiled/rejected drafts across navigation and release them on commit/discard without changing script history. |
| Existing compiler conversion | Justified reuse of numeric/alias/macro semantics; a separate ad hoc number parser would create another correctness surface. |
| Metadata snapshots | Needed for metadata-only script discovery without a Core-wide revision/event redesign; unchanged request cost was small in the synthetic measurement. |
| Optional investigator interface/unowned calls | Preserves existing simple investigator implementations and conservative bare-call behavior while application tools use explicit ownership. |
| Retained formatted scripts | Correctly keeps known orphaned scripts protected; event deletion alone does not prove a flag/variable disappeared. |
| Repeated shared-tail traversal | Demonstrated unnecessary work and repaired in the follow-up. |

Ownership/call-site inspection covers object flags, script triggers, conditional map-script variables,
item/tutor/trade/legendary templates and CodeBody insertions. Trainer records and ID exhaustion/range
policy retain their earlier behavior. Pending-source registration copies source references on state
notifications; parsing/rescanning occurs on an allocation request, not on every keystroke. Retained
source entries are removed when the owner has no drafts, and replacing the investigator removes
its old registration. Background run snapshots use the existing model run lock.

## Demonstrated scan cost and bounded repair

The new retained-script scan called the existing root walker separately for every known script.
That walker intentionally follows calls/gotos, so many entry scripts repeated the same formatted
tail. The 1,000-entry synthetic graph with a 25-script shared tail measured:

| Operation | Before | After |
| --- | ---: | ---: |
| Initialization | 1,342 ms | 4.6 ms |
| Refresh after one byte edit | 607 ms | 2.2 ms |
| Unchanged request | 0.037 ms | 0.045 ms |
| Unchanged-request managed allocation | 25,816 bytes | 25,816 bytes |

These are single-run synthetic observations, not real-ROM or native UI claims. Cached-query timing
difference is too small for a performance conclusion. Raw records are in id-suggestion-review;
the reproducible probe is tools/IdSuggestionBenchmark. The same candidate flag is returned.

The normal public map-root walker still uses its existing traversal. The retained-script path
now treats each known formatted entry as an independent bounded scan, leaving calls/fallthroughs
to other known entries for their own scan. Unknown targets still use the original bounded walk;
cycles, game parser, command lengths and literal/variable classification are preserved. On very
large graphs, treating known entries separately can expose additional references beyond an older
per-root budget, conservatively blocking more IDs. The new path does not make a completeness claim.

A differential regression compares its reference set against the original walker on shared tails,
cycles and unformatted callees, checks reduced repeated visits, verifies public flag/variable
suggestions and preserves every model byte/metadata record. It adds no timing assertion.

## Limits and decision

Keep the architecture and the measured traversal refinement. A wholesale rewrite or Core-wide
cache-version framework is not justified by this review. Full game-ROM/native performance remains
unmeasured; cached requests still copy the metadata-reference snapshot, and pending-source analysis
is proportional to retained source size. Existing native/custom-engine, unformatted disconnected
code and legal-range/exhaustion limits remain explicit. No compiler, ROM write, relocation, history,
reservation policy or user-facing numeric grammar is changed by this follow-up.

Final serial Mac suite: **61 groups / 18,123 assertions / zero failures / zero skipped groups**.
Exact counts are in [mac-verification.json](id-suggestion-review/mac-verification.json). Source
builds emit no compiler warnings; the existing MapImageExport NU1900 feed diagnostic remains
separate. Upstream xUnit/native/gameplay and actual game-ROM performance were not tested.
The checked-in benchmark was built/run from its final location and reproduced a 2.3-ms refresh
for the 1,000-entry graph; this confirms reproducibility without establishing statistical bounds.
