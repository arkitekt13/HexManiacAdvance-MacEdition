# Script argument and mutation safety baseline

Protects the existing lookup/compiler before attempting the optimization identified in
[../SCRIPT_EDIT_PIPELINE.md](../SCRIPT_EDIT_PIPELINE.md). Application sources are unchanged.
Uses fabricated BPRE-tagged bytes and shipped definitions, without a supplied ROM or native UI.

```sh
dotnet build tests/ScriptArgumentSafety/ScriptArgumentSafety.csproj --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 90 dotnet tests/ScriptArgumentSafety/bin/Debug/net8.0/ScriptArgumentSafety.dll
```

Restore dependencies first. The normal scripts/verify-mac.sh runner automatically discovers
this net8.0 executable. The original baseline passed 641 assertions; the lookup-reuse expansion
originally passed 1,565 assertions; the HMA-003 fix expands this to **1,596 assertions** with no elapsed-time thresholds.

## Matching contracts

- Exact, case-insensitive, quoted/spaced, partial, and duplicate `~n` names.
- Decimal priority versus quoted numeric names and implicit numeric names in empty slots.
- Sparse high-ID aliases, including the distinction between a named slot and an empty slot.
- Aliases that look like hex/`$` numbers, including partial-name priority over numeric fallback.
- Decimal, lowercase/uppercase hex prefixes, `$`, negative values, byte/short/word widths,
  boundary values, positive/negative enum offsets, and `|h` numeric arguments.
- Rejected arguments emit no partial bytes; numeric argument building never changes ROM bytes.
- Replaced/reordered/grown/shrunk option lists after the normal model-cache invalidation
  boundary, and isolation between models with the same list name.

SetList is a low-level metadata API and does not itself clear ModelCacheScope. Tests explicitly
call ClearCacheScope, as normal Refresh does, after replacing a previously queried list. They
do not assert immediate freshness inside an unchanged cache scope or introduce an application
cache-invalidation fix. Quoted numbers also retain existing matching rules: a quoted numeric
index is an implicit name only for an empty slot; an unquoted decimal remains numeric.

## Script mutation contracts

For each of `0x4001`, `16385`, and `temp1`, the real CodeTool edits a script with two callers,
an anchored dialogue stream included in the editor's source braces, and protected neighbors.
Growth from one to 24 setvar commands must actually relocate the script rather than overwrite
a blocked neighbor. Both caller pointers, source metadata, the named root, text pointer/content/PCS
format, and neighboring bytes/anchor are checked. Shrinking to one command keeps the new
address and preserves those relationships. The setvar opcodes/operands are checked against
literal expected bytes, independently of the compiler.

Undo must recover every original byte and serialized metadata; redo must exactly retrace
each intermediate captured history state and finish at the known shrunken result. The
fixture's two edits currently create five history records because existing relocation inserts
custom formatting records around data movement. Tests verify the complete round-trip and
do not impose a one-edit/one-undo contract. The sole metadata exclusion is FreeSpaceSearch:
FindFreeSpace deliberately updates that search/navigation cursor outside ModelDelta, so it
is not restored by undo. All other serialized metadata is compared, with explicit checks of
caller-pointer sources, root and neighboring anchors, and dialogue format.

Invalid variable input must report an error without changing the last valid ROM/metadata;
correcting the input must recover without corrupting that state.

Limits: this is shared-Core correctness coverage, not native typing or rendering validation.
It does not cover every script dialect, every graph shape, unused flag/variable allocation,
new `<auto>` stream allocation, or dialogue text expansion; the supplied dialogue remains
unchanged while its owning script relocates. Existing upstream script tests remain separate.

## Per-compilation argument lookup reuse

The two-argument LabelLibrary constructor remains uncached and supplies the reference path.
The opt-in constructor uses the same original matcher, and the production parser opts in for
each fresh compilation. Differential tests compare emitted bytes and error strings for 30
spellings, five widths/offset declarations, two repetitions, and invariant/en-US/tr-TR cultures.
They also cover read-only culture changes, mutable number-format changes, successful/failed
matches after list replacement and scope invalidation, foreign models, and unchanged pointer
address parsing. No alias-versus-number precedence is reordered.

Warm repeated alias and failed-enum/hex-fallback matches allocate 7,424 bytes for 64 builds
against a 32 KB budget. A production 64-command compile allocates 4,832,112 bytes against an
8 MB budget, guarding the actual parser-to-context wiring as well as the isolated helper.
Budgets exclude setup/warmup, have substantial headroom, and are managed allocation checks,
not retained RAM or speed assertions. See [../SCRIPT_ARGUMENT_LOOKUP_PERFORMANCE.md](../SCRIPT_ARGUMENT_LOOKUP_PERFORMANCE.md).

The original differential corpus exposed HMA-003: `$4001` could sort a null option and throw.
It was deliberately not repaired by lookup reuse. The separate fix below now requires the
valid corpus to throw no exceptions while cached and uncached results still agree.

## HMA-003: sparse aliases and explicit hexadecimal input

The original default variable list reproduces the null-entry sorting exception for `$4001`.
A smaller 124-slot unnamed list also exposes silent misinterpretation: `$123` used to match
the synthesized decimal name at index 123 instead of reaching the hexadecimal fallback for
0x123 (291). A 1,024-slot list has multiple such candidates and reproduces the sort exception.
The original source fails the added regression (`/tmp/hma003-before.log`).

The matcher now excludes unnamed slots from unquoted valid explicit-hex name searches,
leaving actual aliases in their established exact/partial/duplicate order. If no real name
matches, ScriptArg's unchanged numeric fallback handles the value. Decimal priority, genuine
numeric-looking aliases, offsets, pointer interpretation, and quoted numeric-name lookup
remain covered. Quoted `$123` remains a name query rather than becoming a numeric literal;
its implicit-name candidates now sort using their decimal display names instead of null.

The null-safe ranking changes no non-null comparison. Tests preserve the previously successful
quoted partial `123` -> unnamed slot 1023 when slot 123 has a different name, and cover real
`$123` aliases, `~2` duplicate selection, real named partials, default-variable `$4001`, a
single-hole `$123` fallback, multi-hole fallback, and offsets. The same 900 cached/uncached
differential comparisons and full script mutation/history checks continue to pass.

An end-to-end parser check additionally compiles `setvar $4001 $123; end` to the explicit
expected bytes `16 01 40 23 01 02`, without writing the model during compilation.
