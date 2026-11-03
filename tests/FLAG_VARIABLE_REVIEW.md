# Existing flag/variable helpers — 2026-11-03

Scope: information and synthetic characterization, not an allocator implementation change.
Follow-up: [usage-detection investigation and safety baseline](FLAG_USAGE_INVESTIGATION.md)
adds engine-role cross-checks and read-only supplied-ROM corroboration; helpers remain unchanged.
Flags.cs, EventTemplate.cs, CodeBody.cs and insertion UI/shortcut code are unchanged by this
review. No supplied ROM was opened by the characterization tool. Do not infer that an actual
user ROM already has the demonstrated collisions.

## What HMA already provides

- Floating script-editor buttons insert an unused flag or variable when the caret is at a
  supported operand. The Mac key handler maps **Command+U** to the same operation; its tooltip
  still says Ctrl+U. Eligibility requires a recognized flag/variable argument, a preceding
  space, no embedded text/movement block, and an available investigator outside Spartan mode.
- CodeBody.InsertFlag/InsertVar writes an explicit hexadecimal operand such as `0x4034` into
  source, retains surrounding text and restores the caret. It uses the normal compiler path.
- The map/event template service supplies the investigator. The same machinery supports
  item, trainer, tutor, trade, legendary and other event templates; HMA already has templates
  rather than starting from an empty library.
- Right-click script actions can find usages of numeric flag/variable operands and jump to
  results when the action is available.

The implementation is a practical static-analysis helper. It searches known map-script roots
and follows recognized script pointers, rather than merely picking random IDs or searching
ROM bytes for a number.

## What it scans and reserves

Flags.GetUsedItemFlags includes object visibility flags, game-specific hidden-item flags,
recognized setflag/clearflag/checkflag operations, and additional Emerald Match Call reserves.
GetUsedVariables includes coordinate-script trigger variables, recognized variable opcodes,
and game-specific hard-coded variables used by native code in FRLG, Emerald and Ruby/Sapphire.
Trainer flags are scanned and managed separately.

Roots include object, coordinate-script and signpost entries and map header scripts, including
conditional header script targets. The old footer comment claiming header scripts are not
checked is stale; the actual GetAllTopLevelScripts implementation does check them. This does
not mean every header-condition variable is recorded as a used variable (see below).

EventTemplate initializes used-ID sets in background work and waits for that initialization
when needed. FindNextUnusedFlag searches upward from 0x21; FindNextUnusedVariable starts at
0x4034. Each returned ID is immediately added to the template instance's in-memory set,
preventing repeated helper calls from choosing the same ID during that instance's lifetime.

## Reproduced limitations

The tool in tools/FlagVariableReview uses real scanners/helpers with a fabricated BPRE map.
It requires the normal destination operand and the detached header root to be detected, so
these observations are not explained by a missing map/ROM fixture.

| Case | Current observation | Implication |
| --- | --- | --- |
| Connected script `copyvar 0x4036 0x4034; end` | Destination 0x4036 is found, source 0x4034 is not; the allocator suggests 0x4034. | A read-only source operand can be missed. The implementation reads only the first operand of its selected variable commands; comparevars and other additional operands merit the same review. |
| Manual edit after initialization | Starting with setflag 0x21, the helper reserves 0x22; changing the script manually to setflag 0x23 is found by a fresh scan, but the existing helper then suggests 0x23. | Used-ID snapshots are not automatically refreshed for arbitrary later edits. Normal model-cache invalidation does not refresh these separate HashSets. |
| Conditional map header variable 0x4034 | The script root is found through the header table alone, but its condition variable is absent from used variables. | Finding the target script is not the same as inspecting the header's variable/compare fields. |
| Every candidate variable 0x4034–0x40FF occupied | The helper returns 0x4100. | There is no range-exhaustion failure/upper-bound guard in this helper. |
| Every ordinary flag candidate 0x21–0x4FF occupied | The helper returns 0x500. | Search crosses into the trainer-flag region instead of reporting exhaustion; trainer reservations are separate. |
| Two requests without editing ROM bytes | IDs 0x21 and 0x22 are different, while ROM bytes remain unchanged and a fresh ROM scan does not see either reservation. | Session reservations are useful, but are not persisted, rescanned usage or undo-aware transactions. Canceled/incomplete source edits can leave conservative reservations. |

For the stock FireRed engine, persistent variable definitions end at 0x40FF; special variables
start at 0x8000. The engine's GetVarPointer does not establish that arbitrary gaps are safe
storage. These constants are version-specific evidence, not permission to rewrite a hack's
custom allocator or engine. Primary references:
[vars.h](https://github.com/pret/pokefirered/blob/master/include/constants/vars.h),
[flags.h](https://github.com/pret/pokefirered/blob/master/include/constants/flags.h), and
[event_data.c](https://github.com/pret/pokefirered/blob/master/src/event_data.c).

## Further source-level limits

- GetAllScriptSpots has a 0x1000-byte length limit per script segment and a 100-script
  traversal limit per initial root. Unknown commands stop traversal. These are sensible
  corruption guards, but an incomplete scan is not surfaced as a "cannot guarantee unused" result.
- Script argument collection uses a fixed opcode list and, for variables, reads the first
  operand. It does not perform full data-flow analysis of variables passed through other
  variables, native specials or custom assembly. The hard-coded native-variable lists help
  stock games, not arbitrary new code.
- The scan starts from map/event roots; unrelated/unattached scripts and arbitrary binary
  routines are not an exhaustive input set.
- Used sets are initialized in the EventTemplate constructor and augmented by its own
  reservations. RefreshLists refreshes presentation/options, not these usage snapshots.
  Reservations are not stored in ModelDelta, so undoing a source edit does not necessarily
  release a reservation. This is conservative for the reserved ID, but it is not live analysis.
- Find Uses is also not a complete guarantee: for example the flag-usage locator omits the
  dedicated hidden-item flag enumeration used by the allocator's used-set scan.

## Assessment for future script recipes

Reuse the existing source insertion, script formatting/compilation, pointer management and
template infrastructure. The built-in helpers are useful for ordinary supported workflows,
but I would not treat their current result as a guaranteed conflict-free allocation for
unattended recipe/AI insertion.

Before automating that step, a separate authorized hardening effort should inspect every
relevant operand and header field, invalidate usage after edits/reload/history changes, keep
explicit reservations tied to an insertion transaction, enforce game-specific legal ranges,
and report incomplete/unknown scans instead of confidently returning an ID. Reserved ranges
for custom native code should be configurable or otherwise explicit. This review implements
none of those changes and does not change researched addresses or game data.

Evidence: artifacts/script-safety/flag-variable-review.json, generated after a successful
build. The six characterization cases report current outcomes; they are not normal-suite
assertions locking those limitations into place.
