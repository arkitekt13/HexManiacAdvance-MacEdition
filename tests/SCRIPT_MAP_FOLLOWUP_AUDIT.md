# Script/map follow-up audit — 2026-11-07

The requested Map View context changes and four follow-up audits are complete.
All investigation fixtures use synthetic ROMs and temporary profiles; no personal
ROM, fixture game or user settings were opened or changed.

## Checkpoint and scope

Starting branch: `codex/map-reference-event-navigation`, clean at `753f7886`.
Rollback tag: `checkpoint/pre-script-map-audit-2026-11-07`.
Final implementation tested: `a25c0742`. Evidence and repairs are separate commits.
The tag preserves the starting state; reverting individual repair commits also
allows targeted review without discarding unrelated work. This branch is not merged
as part of this audit.

## Requested Map View details

- `71532746`: object descriptions and overlap menus show **Object ID** and the
  current live **Flag**, formatted as hexadecimal or `none` when unassigned.
- `7ef1738c`: green script events show their live trigger as **Variable Number**.
  Both values are reread when resolving the selected event rather than trusting a
  stale image snapshot. These additions are read-only and do not enable map editing.

## Confirmed defects and repairs

| Area | Reproduced behavior | Repair and validation | Evidence → fix |
| --- | --- | --- | --- |
| Signpost script growth | Successful root-script relocation leaves raw signpost script arguments pointing at the erased old entry. | Update matching script-kind signpost entries inside the existing compilation transaction, only when the XSE entry moves; item/base/unknown union values remain untouched. | `1592c7ad` → `3fc61c58` |
| Map-header condition tables | Adding a row changes bytes without adding the visible row; deleted-row shifts leave stale row addresses, and repopulating an empty table can clone its terminator. | Rebuild child models after add/delete, initialize the first active condition, and complete individual edit history boundaries. | `ec3e6975` → `8c95a0e5` |
| Script navigation arrows | Several arrows open the intended script without focusing its source editor. | Queue guarded source focus after existing bound navigation; only the signpost arrow selects all. Invalid green-event targets cannot navigate. | `cf0f82d2` → `0a141bdf` |
| Recent-file persistence | Brackets/commas inside valid filenames are split as profile syntax; embedded quotes cannot round-trip reliably. | Write a marked escaped array and retain a quote-aware literal reader for legacy profiles. | `8961e6f7` → `9b9de924` |
| Search preference | The search-result limit is restored from the diff-limit key. | Read the search key while retaining the existing numeric parser, bounds and defaults. | `ff2a7d3c` → `a25c0742` |

### Signpost safety coverage

[SignpostScriptGrowth](SignpostScriptGrowth/README.md) passes 48 assertions.
Actual relocation is forced with an occupied neighbor; script kinds 0–4 follow the
new entry, while kinds 5–8 and 255 remain bit-identical even with pointer-shaped
payloads. Tests exercise both a signpost-only entry and an entry shared with an
ordinary object pointer. They compare exact ROM bytes, structural metadata and
one-step Undo/Redo, plus rejected source, retained diagnostics and correction/retry.

A fixture-only injected compiler failure occurs during the second compilation
pass after signpost arguments have changed. The transaction restores bytes,
metadata, allocation and model history; rejected source remains available, and a
corrected retry succeeds. Separate dialogue growth moves its resource while
preserving the root and signpost entry. The repair covers relocated root entries;
it does not redesign arbitrary interior-label remapping or compiler allocation.

### Header and arrow coverage

[MapHeaderScripts](MapHeaderScripts/README.md) passes 69 assertions, including
in-place and forced table relocation, deletion/shifting, editing the survivor,
empty-table repopulation, regular/table type transitions, parent deletion and
Undo/Redo. Structural-metadata comparisons exclude the existing `FreeSpaceSearch`
allocation hint, which normal history does not rewind; allocator semantics were
not changed to satisfy a test.

[ScriptArrowFocus](ScriptArrowFocus/README.md) passes 26 assertions using actual
headless MainWindow/MapTab pointer input. Object, green-event, regular-header and
conditional-child arrows focus source without selecting all. The conditional
parent arrow still opens its table. Existing command bindings and enablement stay
in place. The separate SignpostScriptNavigation group passes nine assertions,
including arrow-only select-all and ordinary click-to-deselect. Map View selection
continues through its independent protected navigation path.

### Startup/profile findings

[ProfilePersistence](ProfilePersistence/README.md) passes 32 assertions. Fresh and
reloaded isolated Mac profiles retain Map View/grid/assistance/zoom preferences.
Five disposable recent files with punctuation, embedded quotes and Unicode reopen
through their exact menu commands without byte changes. Legacy path migration,
missing/malformed recent values and independent search/diff limits are covered.

The saved emulator preference survives a new launcher instance; injected process
starts prevent any native application launch. Restart does not automatically open
a ROM. Existing default window dimensions are verified as 1200×800. Explicit
window-geometry persistence is not a pre-existing feature, so no new geometry
behavior was added merely for this audit. No other defect was established in
these audited preference paths.

## Fresh complete Mac regression result

Procedure: `bash scripts/verify-mac.sh --no-restore`.

**78 groups / 22,148 assertions / zero failures / zero skipped groups.**
The [machine-readable result](SCRIPT_MAP_FOLLOWUP_VERIFICATION.json) lists every
group and assertion count. It records the final completed run, not the earlier
interrupted run used while investigating the additional preference defect.

There is one build-warning category: MapImageExport reports NU1900 because the
NuGet vulnerability-data service index was unavailable. All groups built and ran;
this warning is not a regression failure or a clean vulnerability-scan result.

The run includes all existing pending-source, failure-atomicity, assistance,
clipboard, navigation, scrolling, map lifetime and history groups. It does not
replace native macOS interaction or real-ROM gameplay checks, and the full
upstream xUnit suite was not rerun. Parsing rules, game-engine behavior and the
existing compilation/allocation/history algorithms were preserved; the targeted
signpost reference updates use the existing compilation transaction.
