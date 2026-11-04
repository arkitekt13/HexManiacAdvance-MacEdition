# Copied map brush and border investigation — 2026-11-04

## Scope and current state

Started clean on codex/mac-input-history-audit at e64cca67. This investigation adds tooling and
documentation only; application source, existing tests, ROM fixtures and personal settings are
unchanged. No user ROM or emulator is used. The review creates fabricated maps with mixed
source/destination permissions, real MainWindow/MapTab controls and actual routed pointer input.

Twenty-five independent current-build scenarios completed with **343 positive setup/preservation
controls** and **23 separate behavioral observations: 13 passing, 10 failing**. Failed observations
are recorded defects/latent-state findings, not included in passing regression counts. Some
cases only record raw values without claiming a semantic invariant. The executable is outside
the automatic Mac regression runner; exit zero means investigation completion, not correctness
of the recorded failed invariants.

## Confirmed user-reachable failures

### HMA-018 — Border dragging can write outside border data

A real primary press on the opened border image followed by a held leftward drag to source
position (-17,8) changes a canary at 08FE, immediately before the fabricated border at 0900:

- Single brush: 5AA5 becomes 0001.
- Copied 2x2 brush: 5AA5 becomes 4000, with additional unintended neighboring border changes.
- A single brush dragged right to (72,8) changes the canary just after the 2x2 border at 0908
  from 6BB6 to 0001; the copied multi-cell writer clips the tested right-side position correctly.

BorderEditor.Draw(int) has no coordinate bounds check. Draw(int[,]) checks upper bounds but
not negative coordinates. Both calculate addresses before ensuring the pointer lies in the
border region. Small negative pixel values also require care because integer conversion
truncates toward zero. GetBlock has the same unchecked coordinate-to-address shape for reads.

The border panel's slide transition is disabled only in the test fixture; the real image hit
target, pointer capture and production BorderMove/DrawBorder/Draw handlers are used. No direct
Core write is substituted for this pointer reproduction. Save/reload persists the incorrect
bytes, Undo restores the complete baseline, and Redo faithfully reapplies the incorrect edit.

**Historical attribution:** A read-only git archive of 03025d32, before ca25cecd's gesture fix,
was built separately under /private/tmp with the same investigation tool. The same primary drag
already changed 08FE from 5AA5 to 0001. That process completed nine positive controls and one
failed boundary observation. The baseline and current app assembly hashes differ. No checkout,
branch switch, reset or source modification was made in the working repository. The defect
predates the recent capture-lifecycle fix; the probe did not assume the historical capture owner.

### HMA-017 — Explicit nine-grid permissions miss derived inner corners

Select a placed 3x3 pattern, set the actual bound Movement selector to 01, enable Draw 9-Grid,
and paint at (12,8), (13,8), (12,9). This creates a 3x3 L region missing its bottom-right cell.
The center at (13,9) is an inner corner. Actual values, row by row:

```text
0400 0401 0400
0401 3001 0400
0400 0400 8000
```

The unpainted bottom-right 8000 is expected. The inner corner 3001 carries permission 0C,
while the other painted cells honor explicit 01. The same mismatch occurs when the user changes
the bound selector after enabling nine-grid mode, so it is not just setup ordering.

DiscoverCornersFor9Grid intentionally stores complete tile/permission words from its map
histogram. CollisionIndex updates tilesToDraw, but not innerCornersFor9Grid; Draw9Grid writes
those cached corner words unchanged. The selected explicit permission therefore fails to cover
automatically discovered corners. Ordinary copied-mode corner choice is recorded without
asserting a new game-engine rule; the confirmed inconsistency is the explicit override.

## Latent Core state rather than a demonstrated ordinary UI trigger

### HMA-016 — Negative permissions packed into an active copied pattern

Ordinary 2x2 copied stamping and 4x4 rectangle repetition preserve their mixed packed source
words correctly. Explicit Movement selection 01 also works for those two paths. Their passing
controls include every unrelated byte/pointer and the original copied source.

When the review deliberately assigns CollisionIndex=-1 in Core while keeping the copied brush
active, stamping writes FC00,FC01,FC01,FC00 and rectangle repetition writes the same FC-prefixed
pattern. CollisionIndex packs the signed sentinel into every tilesToDraw cell; DrawBlocks and
RepeatBlocks then write its low 16 bits as ROM data. Nine-grid also fails destination-preserve
expectations in this forced state. In-bounds border stamping records FC-prefixed values too,
but no border-specific movement policy is assumed or counted as a passing/failing observation.

**UI reachability matters:** The real Mac Movement selector contains 64 entries, 00 through 3F;
it has no selectable Preserve item. IgnoreEmptySelectionConverter ignores SelectedIndex=-1
rather than updating Core. Emptying that UI selection leaves the copied brush's original
permission value, and copied stamping/repetition continue to work. No ordinary native UI route
was found that intentionally sets -1 while retaining the copied multi-cell pattern. Current
negative-reset callers also clear that pattern. This is a reproducible latent Core defect and
a design hazard for future preservation controls, not proof that normal copying is broken.

Switching from explicit 01 to forced Core preserve reproduces the bad pattern. Re-copying the
original source repairs the brush. Undo repairs ROM bytes but does not turn a bad brush state
into a safe permission policy; Save/Redo do not sanitize the bad values.

## Recommended implementation order and regressions

1. **Border bounds first.** Promote left/right failures into enforced tests, then reject or
   safely clip out-of-range coordinates before calculating addresses in both write overloads
   and the read path. Cover top/bottom/left/right, exact edges, fractional negative pixels,
   one/multi-cell brushes, default 2x2 and explicit border dimensions, normal release and
   capture interruption. Compare every ROM byte, neighboring canaries, pointers, metadata,
   save/reload and one-step exact Undo/Redo. Keep valid strokes and partial-edge behavior deliberate.
2. **Nine-grid explicit overrides.** Preserve corner-art discovery but apply an explicitly
   selected movement policy to every written cell, including cached/fallback corners. Cover
   all four inner corners, selection before/after mode activation, mixed source permissions,
   successive strokes and exact history/save behavior. Retain ordinary copied-pattern semantics.
3. **Define copied-brush permission modes before exposing preserve.** Distinguish copying
   source permissions, overriding all cells and preserving destination permissions; do not
   infer the distinction from signed packed ROM words. Keep mixed copied patterns intact by
   default, isolate scalar permission policy from tile IDs, and explicitly test correction,
   re-copy, stamping/repetition/nine-grid/border routes and original HMA-010/015 protections.

No implementation is included. The first two failures are reachable through normal controls;
the negative-preserve case is characterized through a deliberate Core assignment.

## Baseline verification and limits

MapSelection and BlockBagFill were freshly built and pass **394 + 2,081 = 2,475 assertions**,
zero failures or skipped groups. They protect normal mixed-permission copying and the existing
single-block/bag fixes; they did not cover outside border dragging, explicit derived corners or
an active copied brush forced to negative permissions. The full Mac suite was not repeated for
investigation-only changes; its prior verified baseline remains 52 groups / 15,854 assertions
at e64cca67. The upstream suites were not rerun. Fresh builds retain eight existing Core warnings.

Each current probe preserves metadata and checks Save/reload and exact Undo/Redo, including
faithful persistence of bad results. All non-border-drag cases also compare every byte outside
the intended operation's region. In-bounds border semantics are recorded without asserting
whether copied upper bits should be retained for every engine. Native trackpad delivery,
animations, game-specific border dimensions, gameplay and every nine-grid corner topology
remain outside this synthetic review. The historical snapshot is separate evidence, excluded
from the 25 current scenarios and their 343 controls.

Reproduction and scenario definitions: [CopiedBrushReview](../tools/CopiedBrushReview/README.md).


## HMA-018 implementation follow-up

Entry 113 fixes border bounds before pixel conversion/address calculation for both writes and
sampling. The original checkpoint results above remain historical. BorderBounds passes 190
assertions over default/custom dimensions, invalid origins and retained Redo, valid clipping
and Save/reload/history. Two enforced real-pointer scenarios add 38 assertions for single and
copied border drags, all sides, invalid sampling and capture interruption. HMA-016 and HMA-017
remain separate work in this commit.
