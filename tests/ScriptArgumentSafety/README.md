# Script argument and mutation safety baseline

Protects the existing lookup/compiler before attempting the optimization identified in
[../SCRIPT_EDIT_PIPELINE.md](../SCRIPT_EDIT_PIPELINE.md). Application sources are unchanged.
Uses fabricated BPRE-tagged bytes and shipped definitions, without a supplied ROM or native UI.

```sh
dotnet build tests/ScriptArgumentSafety/ScriptArgumentSafety.csproj --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
python3 scripts/run-upstream-command.py 90 dotnet tests/ScriptArgumentSafety/bin/Debug/net8.0/ScriptArgumentSafety.dll
```

Restore dependencies first. The normal scripts/verify-mac.sh runner automatically discovers
this net8.0 executable. It currently passes **641 assertions**, with no timing thresholds or
claim that the expensive matching has been optimized.

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
