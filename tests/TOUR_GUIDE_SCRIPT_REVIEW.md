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
