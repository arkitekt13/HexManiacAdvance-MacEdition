# Tour-guide scripting review — 2026-11-06

Starting checkpoint: `a7b60850`, branch `codex/script-map-reference`. No personal ROM,
settings or emulator is used. The branch remains unmerged.

## Investigation evidence

Existing groups separately cover command assistance, single movement resources, draft
atomicity and the read-only map pane. The new TourGuideWorkflow group combines two dialogue
blocks, NPC/player/camera movement, a second asynchronous NPC and an explicit wait. It checks
actual byte operands, six independent resources, forced relocation, named anchors, late failure,
correction, navigation, stale callbacks, Undo/Redo and save/reopen across LF/CRLF/CR source.

The initial baseline fails the first dialogue-format assertion: bytes for `Welcome!` are correct,
but its destination is a `NoInfoRun`, not a `PCSRun`. A smaller probe reproduces this with just
`msgbox.default <auto>`, its block and `end`: text at `000189` is unformatted after successful
editor compilation. Calling FormatScript again without a requested minimum span restores PCS.

Root cause: the compiled span includes inline `<auto>` resources after the ending opcode.
FormatScript continues through the entire requested span and interprets already discovered
resource bytes as script instructions. It can clear the text format it just created. The relevant
traversal predates this port; this is not evidence of a map-reference regression.

Bounded repair: while traversing a minimum span, skip exact, already formatted stream
destinations referenced by the script being formatted. Continue processing actual script
sections and preserve existing compiler, allocation, matching and numeric rules. Do not blindly
stop at the first ending opcode, because valid following script sections still need formatting.

## Separate compatibility finding

Follow-up: the [movement-validation repair](MovementValidation/README.md) now rejects unknown
rows atomically, and autocomplete's ellipsis indicator cannot be accepted. The original
observations below describe the investigation checkpoint, not the repaired behavior.

`TourGuideWorkflow --probe-invalid-movement` reports that `NoSuchMovementInstruction`
does not reject compilation. With a fresh `<auto>` movement block it is omitted (`01 03 FE`);
with an existing movement stream the shared table writer substitutes zero (`01 00 03 FE`).
Both leave no rejected draft or error. This permissive behavior also predates the port.

This finding is recorded separately rather than changing numeric/enum/resource parsing during
the formatting repair. A follow-up should define consistent invalid-row diagnostics across
new/existing movement blocks while preserving every currently supported numeric and alias
form, partial-name matching, comments and terminator behavior. No claim is made that malformed
movement rows are currently protected by the late-invalid-*script-command* atomicity tests.

## Reproduction

```sh
dotnet restore tests/TourGuideWorkflow/TourGuideWorkflow.csproj --ignore-failed-sources -p:NuGetAudit=false
dotnet build tests/TourGuideWorkflow/TourGuideWorkflow.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
dotnet tests/TourGuideWorkflow/bin/Debug/net8.0/TourGuideWorkflow.dll
dotnet tests/TourGuideWorkflow/bin/Debug/net8.0/TourGuideWorkflow.dll --probe-format
dotnet tests/TourGuideWorkflow/bin/Debug/net8.0/TourGuideWorkflow.dll --probe-invalid-movement
```

The optional probes print observations and are not assertion-bearing regression runs.
Native Cocoa input, real tutorial ROMs, camera setup/teardown, collision/pathing and game timing
remain emulator/manual checks. This harness is not a movement simulator.

## Formatting repair verification

The bounded FormatScript repair passes 321 mixed-tour assertions. Named-anchor comparisons
canonicalize complete records because rollback reinsertion can alter dictionary order; no
production serialization or history logic was changed. A reachable following script section
and idempotent repeated formatting are also protected. Actual UI coverage now includes eleven
commands/352 assistance assertions and a mixed movement-suggestion/pan/Undo case among the
63 map-reference assertions.

The full Mac run passes **65 groups / 18,673 assertions / zero failures or skipped groups**;
see [the exact group record](TourGuideWorkflow/mac-verification.json). MapImageExport emits the
existing NU1900 vulnerability-feed warning; builds still succeed.

The first upstream run reports 2,715 cases: 1,779 passed, one failed, 935 skipped and zero
engine errors. The sole failure, `ErrorInScript_ClickOff_ResetScriptEditor`, expects rejected
source to disappear on navigation, contrary to the deliberate HMA-006 retained-draft contract.
The pre-repair commit `16904dd7` produces the identical failure and totals; summaries are
preserved beside the new group. The test contract will be aligned in a separate commit.

Baseline execution detail: the fresh archive's runner build omitted its transitive xUnit
execution dependency from its dependency manifest and could not discover tests. For a controlled
comparison, its freshly built Core/test/integration assemblies were placed into a copied working
runner host with the existing identical-version dependency manifest/libraries. That matched-host
baseline discovers all 2,715 cases and reproduces the exact failure; it is not a claim that the
fresh runner's dependency packaging passed. Those initial upstream runs picked up an existing
cached optional FireRed 1.1 fixture in the working runner directory; the fixture-free follow-up
below explicitly excludes it. Neither ROM nor settings files were edited. The cached ROM's
SHA-256 still matches the repository's existing sample fixture.

## Separate legacy-test alignment and isolated final result

The original navigation test is renamed and strengthened to require retained rejected source,
unchanged compiled ROM bytes across navigation, and restoration of compiled source without
byte changes when the user explicitly invokes Undo. No production code changes accompany it.

Both hosts were rerun with `sampleFiles` explicitly excluded. The baseline at `16904dd7`
reports **2,715 cases: 1,666 passed, one identical legacy assertion failure, 1,048 skipped,
zero engine errors**. The final test-aligned tree reports **2,715 cases: 1,667 passed,
zero failed, 1,048 skipped, zero engine errors**. The skips are absent optional-ROM fixtures;
they are not gameplay passes. Exact summaries are preserved as `upstream-romfree-*-summary.json`
beside the group. These controlled hosts reuse the working dependency manifest as described
above; the fresh runner's dependency omission remains a test-infrastructure follow-up, not an
app-launch defect. The original fixture-bearing summaries are retained with their distinct totals.
