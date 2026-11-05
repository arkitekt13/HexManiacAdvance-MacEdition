# Flag/variable suggestion lifetime review

Baseline: `842bb385`, November 5, 2026. Branch: `codex/id-suggestion-lifecycle`.
All reproduction uses fabricated in-memory maps; no user ROM or preferences were used.

## Reproduced failures

The real object GenerateNewFlag -> Delete -> replacement GenerateNewFlag path produces 0021,
then 0022 even though a fresh ROM scan no longer contains 0021. The script-event trigger path
likewise produces 4034, then 4035 despite removal of the last trigger reference. The new focused
regressions fail against baseline code on both replacement suggestions. Earlier review also
reproduced manual script changes absent from EventTemplate's original used-ID snapshot.

EventTemplate currently combines two different things in its mutable used sets: the initial ROM
scan and IDs reserved by suggestions. Neither is refreshed/released by later event deletion.
Replacing this set with a fresh scan alone would lose protection for source not yet compiled.

## Bounded implementation design

- Separate live ROM usage from temporary allocation reservations and pending source.
- Reuse scanned usage only while ModelCacheScope and formatted-run identities remain unchanged;
  normal byte writes, import writes, Undo/Redo and cache refresh invalidate that identity.
- Keep known formatted XSERun scripts protected even after event roots disappear; deleting an
  event does not establish that its associated script's flag/variable is gone.
- Give immediate event tools and template creation a disposable allocation scope; multiple IDs
  requested in one operation remain distinct, and scope exit removes temporary reservations.
- Register pending/rejected overworld source by CodeTool owner, including offscreen retained
  drafts; successful compilation transfers protection to the ROM, and explicit discard releases
  the source. Battle/animation bytecode is not interpreted as overworld event bytecode.
- Analyze pending source with the existing line compiler, numeric/enum conversion and researched
  operand roles. Skip comments and resource blocks; handle valid operands in unfinished commands.
  This reads source/builds temporary bytes, without invoking ROM/resource compilation writes.
- Preserve conservative behavior for bare external/unowned investigator calls; application tools
  use explicit scopes. Keep native reservations, candidate start/range policy, trainer-ID behavior,
  numeric rules, parser/ROM encodings, compilation, pointer relocation and history algorithms.

## Required regressions and limits

Actual event creation/deletion, manual edits and shared references must work for flags and
variables. A deleted final reference is reusable; a surviving event/script reference is blocked.
Undo/Redo must preserve exact bytes and refresh protection without suggestions consuming history.
Nested/multi-ID actions must remain distinct. Pending/rejected drafts must remain blocked across
navigation and become reusable after explicit discard or corrected removal. Successful assisted
compilation must transfer protection to live references. Pending numeric forms, variable source
operands, VarGet roles, literal exclusions and text/comment blocks require preservation checks.
Known orphaned formatted scripts remain blocked until their references are actually removed.

The initial draft handoff test lacked a correctly anchored/expandable synthetic script; its
existing compiler rejection was retained safely. Correcting the fixture made the handoff test
pass without changing compiler behavior. Shared-event tests also reacquire/use current event
models after table relocation instead of deleting through a stale fixture reference.

This is not exhaustive native-code/custom-engine analysis, a new legal-range/exhaustion policy,
or a promise to discover arbitrary unformatted/disconnected code. It does not reclaim trainer
records. The separate upstream suites and real-gameplay/native UI checks remain outside the
focused reproduction. Final implementation/test results follow separately.

## Implemented lifecycle and focused validation

The repaired focused group passes 81 assertions across nine isolated synthetic scenarios.
Live usage is cached against both ModelCacheScope identity and a reference-identity snapshot of
formatted runs, including metadata-only script discovery. Run snapshots use the existing model
run lock. Native reservations and previous candidate-range/trainer policies remain intact.

Immediate tool/template actions share disposable scopes for multi-ID requests. Overworld CodeTool
state notifications register its pending and retained rejected source, clear it after successful
compilation/discard, and preserve independent owners. Standalone unowned requests retain the old
conservative distinct-ID behavior. Source analysis uses line compilation into temporary byte
arrays and compiler argument conversion for unfinished lines, with the same researched variable
operand roles and literal distinctions. It does not invoke resource-writing script compilation.

The first complete suite passed 61 groups / 18,115 assertions. Review then added the two
metadata-only invalidation checks and synchronized metadata snapshots; final full validation is
recorded below when complete. No gameplay/native UI or upstream xUnit certification is inferred.

## Final verification

The final serial Mac suite passes **61 groups / 18,117 assertions / zero failures / zero skipped
groups**, including the new 81-check group and all earlier script workflow, assistance, history,
map, clipboard and preview groups. Exact per-group counts and limits are in
[mac-verification.json](id-suggestion-lifecycle/mac-verification.json). Source builds emit no
compiler warnings; the existing MapImageExport NU1900 vulnerability-feed diagnostic remains
separate. No user ROM or preferences changed. Native gameplay/UI and separate upstream xUnit
suites were not run, and exhaustive native/custom-engine unused-ID safety is not claimed.
