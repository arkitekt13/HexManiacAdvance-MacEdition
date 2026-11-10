# Intermittent metadata save investigation — 2026-11-09

A live Mac Edition 1.0.0 session intermittently refused access to the sibling
`.toml.<guid>.tmp` staging file while saving the user's tutorial project. Save As
to a new name succeeded; subsequent saves sometimes failed and sometimes
succeeded. Thus a persistent corrupt-TOML interpretation and an every-overwrite
failure are not supported. The exact failing filesystem operation remains unknown.

Read-only checks on the first recovery pair confirmed 547 identical named anchor
definitions and 73 identical lists compared with the original TOML. The original
TOML was byte-identical to its post-failure snapshot. The recovery ROM/TOML loaded
in an isolated in-memory PokemonModel without byte changes and retained all 547
anchor names, addresses and formats. The second recovery pair also loaded as
BPRE1, 16 MiB, with 547 definitions and 73 lists; all definitions were retained
and source disk files remained unchanged. These checks do not certify gameplay
or recover edits that exist only in the running UI.

Repeated atomic overwrites of temporary copies of both TOMLs succeeded. Narrow
macOS log queries returned no matching denial records. Short read-only EventPipe
captures from the still-running application did not capture a save-access
exception. They did capture GetCwd/FileNotFoundException in relative resource
checks after the on-disk app bundle had been replaced beneath the running
process. This proves a separate invalid working-directory condition; it does
not establish that condition as the cause of the temporary-file access denial.
Caught icon-conversion exceptions also appeared and are not evidence of save failure.

No project file, file permission, ACL, extended attribute or preference was
changed by the investigation. On-disk snapshots and diagnostic traces are local
private temporary outputs, excluded from the repository. The newest project
must be saved successfully before the user quits; avoid republishing the app
bundle while the old instance is running.

## Narrow diagnostic change

AtomicFileWriter now attaches the current operation to the original I/O/access
exception, then rethrows the same exception. The Mac metadata-save dialog shows
that operation on final retry failure. This distinguishes copying the old file,
opening/writing/flushing its temporary copy and renaming the completed result.
Creation, copy, permissions, write/flush, replacement, retries, cleanup and
saved/dirty-state behavior are unchanged. This is instrumentation, not a claimed
repair of the intermittent access failure. A new failure report from the updated
app is still needed if a clean restarted session reproduces it.

SaveSafety passes 19 assertions, including injected write failure, original
exception identity and operation context, old-file preservation, temp cleanup,
UTF-8, Unix permissions, symbolic links, read-only protection, retry and retained
unsaved/close state. No commercial ROM or native dialog is used by that group.
