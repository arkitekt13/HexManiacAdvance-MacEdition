# Flag/variable usage detection: investigation before implementation

Date: 2026-11-03. Application checkpoint: `29eae327`; investigation branch:
`investigate/flag-variable-usage-safety`. Production scanner, allocator, script compiler,
researched ROM addresses, and game engine behavior are unchanged.

## Conclusions

The existing helper is useful and several important paths work: direct variable operands,
three ordinary flag commands, object visibility flags, coordinate triggers, hidden-item offsets,
called scripts, conditional-header target scripts and game-family native-variable reserves.
Safety tests protect these paths before any scanner repair: 535 focused checks pass, and
all 26 Mac groups pass 5,550 assertions with no groups skipped.

The gaps are real, but a blanket "all numbers in the variable range are variables" fix would
be wrong. Command operand semantics matter. The safe first proposal is read-only analysis of
additional explicitly identified operands and header fields; changing allocation, reservations,
ID ranges or native code is outside this investigation's authorization.

## Engine reference cross-check

Reviewed the named command handlers in pret's FireRed, Emerald and Ruby engine reconstructions.
These are references for understanding binary commands, not new source code for the user's game.
All twelve byte-to-handler mappings were also checked against each engine command table.
The selected handlers agree on the following roles in all three references:

| Binary command | First halfword | Second halfword |
| --- | --- | --- |
| 16 setvar | Variable | Literal |
| 17 addvar | Variable | Literal |
| 18 subvar | Variable | Variable or literal |
| 19 copyvar | Variable | Variable |
| 1A setorcopyvar | Variable | Variable or literal |
| 21 compare | Variable | Literal |
| 22 comparevars | Variable | Variable |
| 26 specialvar/special2 | Variable | Function ID, not a variable |
| 44 additem | Variable or literal | Variable or literal |
| 47 checkitem | Variable or literal | Variable or literal |
| 8F random | Variable or literal | None |
| B3 checkcoins | Output variable | None |

Primary sources: [FireRed scrcmd.c](https://github.com/pret/pokefirered/blob/master/src/scrcmd.c),
[Emerald scrcmd.c](https://github.com/pret/pokeemerald/blob/master/src/scrcmd.c),
[Ruby scrcmd.c](https://github.com/pret/pokeruby/blob/master/src/scrcmd.c).
Exact downloaded-content hashes and URLs are retained in
[primary-source-manifest.json](../tools/FlagVariableReview/primary-source-manifest.json).

FireRed's VarGet treats values below 0x4000 as literals and higher values as variable references.
This does **not** validate every higher value as legal storage. Conditional header types 2/4
compare two VarGet operands, so either field may refer to a variable; a literal comparison value
must remain a literal. Flags use a separate ID namespace, not the variable-value convention.
See [event_data.c](https://github.com/pret/pokefirered/blob/master/src/event_data.c) and
[script.c](https://github.com/pret/pokefirered/blob/master/src/script.c).

The role table is deliberately limited to twelve commands; many other commands and native
specials also access variables. Tests neither execute engine functions nor validate gameplay.
Contrived high literal operands, including function IDs, test classification rather than
asserting that every handcrafted command could run successfully in-game.

## Existing reserve policy, recorded rather than changed

The helper begins at flag 0x21 or variable 0x4034, implicitly avoiding earlier IDs. It does not
mean every later ID is safe. Game definitions distinguish temporary, persistent, special,
trainer and system storage. None of these boundaries has been enforced or rewritten here.

| Reference family | Persistent variable definitions | Special variable definitions | Hidden-item flag offset used by HMA |
| --- | --- | --- | --- |
| FireRed/LeafGreen | 0x4000–0x40FF | 0x8000–0x8014 | 1000 / 0x3E8 |
| Emerald | 0x4000–0x40FF | 0x8000–0x8015 | 500 / 0x1F4 |
| Ruby/Sapphire | 0x4000–0x40FF | 0x8000–0x8015 | 600 / 0x258 |

The constants include temporary variables within the persistent array; this is a storage-range
description, not a list of IDs available for a new game. Emerald also reserves flag IDs
0x15C–0x1A9 in HMA's used set for Match Call. Ordinary flags are discovered from supported
scripts and events; HMA has no comparable complete native-flag reserve inventory here.
A named constant or alias is not, by itself, proof either of live usage or of availability.

Primary [FireRed](https://github.com/pret/pokefirered/blob/master/include/constants/vars.h),
[Emerald](https://github.com/pret/pokeemerald/blob/master/include/constants/vars.h), and
[Ruby](https://github.com/pret/pokeruby/blob/master/include/constants/vars.h) variable definitions;
flag-definition URLs/hashes are also in the source manifest. LeafGreen and Sapphire reserve
routing/offset behavior is tested synthetically; their engines were not separately downloaded.

HMA's current native-variable sets are listed below. These are a baseline of the current
application's explicit reserves, **not** a completed audit of all native reads/writes:

### FireRed/LeafGreen (41 IDs)

`0x4035`, `0x4037`, `0x4038`, `0x4039`, `0x403B`, `0x403C`, `0x403D`, `0x4040`, `0x4042`, `0x4043`, `0x4044`, `0x4045`, `0x4046`, `0x4047`, `0x4048`, `0x404C`, `0x404D`, `0x404E`, `0x404F`, `0x40AA`, `0x40AB`, `0x40AC`, `0x40AD`, `0x40AE`, `0x40B4`, `0x40B5`, `0x40B6`, `0x40B7`, `0x40B8`, `0x40B9`, `0x40BA`, `0x40BB`, `0x40BC`, `0x40CF`, `0x40E6`, `0x40E7`, `0x40E8`, `0x40E9`, `0x40EA`, `0x40EB`, `0x40F1`.

### Emerald (38 IDs)

`0x4036`, `0x4038`, `0x403E`, `0x4040`, `0x4041`, `0x4042`, `0x4046`, `0x4047`, `0x404A`, `0x404B`, `0x404C`, `0x404F`, `0x4097`, `0x40C2`, `0x40CD`, `0x40D0`, `0x40D4`, `0x40DD`, `0x40DE`, `0x40DF`, `0x40E0`, `0x40E1`, `0x40E2`, `0x40E3`, `0x40E4`, `0x40E6`, `0x40E7`, `0x40E8`, `0x40E9`, `0x40EA`, `0x40EB`, `0x40EC`, `0x40EE`, `0x40EF`, `0x40F0`, `0x40F1`, `0x40F5`, `0x40F6`.

### Ruby/Sapphire (12 IDs)

`0x4040`, `0x4041`, `0x4042`, `0x4046`, `0x4047`, `0x404A`, `0x404B`, `0x404C`, `0x404F`, `0x4054`, `0x4097`, `0x40C2`.

## Synthetic detection findings

Twenty command vectors run for BPRE, BPGE, BPEE, AXVE and AXPE (100 cases). The original
seventeen-case run and expanded boundary run agree on the same substantive gaps. Each
conditional-header case detaches normal roots so the header is the only route to its target.

- Second-variable operands are missed in subvar, copyvar, setorcopyvar and comparevars.
- additem/checkitem variable arguments, random's variable bound and checkcoins' output are
  outside the production variable scanner's opcode list.
- Both conditional-header comparison fields can be missed, even when their target script is found.
- A special variable used only as copyvar's source is also missed. Special variables are not
  interchangeable with ordinary persistent IDs for new story state.
- High literal operands in setvar/addvar/compare and a specialvar function ID are correctly not
  treated as additional variables. This distinction must survive a future implementation.

Missing references are observations, not assertions that the regression suite requires HMA to
keep missing. Additional native-variable completeness, dynamic/native usage, unsaved source,
unknown custom commands and orphan scripts remain open analysis limits.

## Supplied FireRed 1.1: read-only corroboration

The supplied fixture identifies as BPRE1 and is 16,777,216 bytes, SHA-256
`729041b940afe031302d630fdbe57c0c145f3f7b6d9b8eca5e98678d0ca4d059`.
No external clean-ROM identity certification is claimed; no modified working game was tested.

- HMA finds 2,143 top-level script roots and reports 133 used variable IDs / 710 flag IDs.
- The bounded raw-command review visits 3,979 distinct script segments and examines 4,762
  selected command/header references, with no encountered unknown-command or traversal-budget stops.
- Within that coverage, no additional ordinary persistent variable ID is absent from HMA's set.
  A missed operand at one location can still be found at another location or covered by a native reserve.
- One globally missing ID is special variable 0x8013: at 0x1A66ED the actual bytes are
  `19 12 80 13 80 03`, i.e. copyvar 0x8012, 0x8013 followed by return. This corroborates
  the source-operand finding without demonstrating an ordinary-variable allocation collision.
- Model bytes are unchanged by scanning after initialization, and the source ROM's disk hash
  is unchanged. The staged upstream fixture is hash-checked separately as well.

The review shares HMA's decoder and map-root discovery and covers only the selected roles.
It is not an independent exhaustive ROM disassembly, proof of native-reserve completeness,
a gameplay test, or evidence that a modified game cannot contain conflicts. Ruby/Sapphire,
Emerald, LeafGreen and FireRed 1.0 have no supplied real-ROM validation in this investigation.
The JSON report is local at artifacts/script-safety/usage-detection-review.json.

## Safety gates for a later proposal

1. Retain exact command widths, existing first-operand detection, flag/event/root coverage,
   native reserves and the literal-versus-variable distinction; scanners must not mutate bytes.
2. Add engine-confirmed missing operands and both conditional header fields only after approval,
   with expected-detection tests that initially fail against current production code.
3. Keep native reserves intact; any changes require a separate game/version-specific audit.
4. Treat freshness, reservation lifetime, legal ranges and incomplete-scan reporting as separate
   design changes, with undo/reload/custom-engine behavior reviewed before implementation.

The supported behavior baseline is [FlagUsageSafety](FlagUsageSafety/README.md); it does not
turn a partial scan into a guarantee. No allocator or engine changes are implemented.
