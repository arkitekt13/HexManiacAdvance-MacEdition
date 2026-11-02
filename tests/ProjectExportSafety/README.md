# Project export regression

Uses synthetic tables and disposable folders to verify deterministic output, removal of stale
owned tables, preservation of unrelated files, import of only the declared file set, exclusive
writer locks, recovery after an interrupted publication, unsafe recovery-path rejection, and
rollback after publication fails. Run `scripts/verify-mac.sh`; no ROM or native window is required.

Exports declare their owned TSV files in the manifest. Legacy files are recognized by their
generated-file marker. Conflicting unowned names, table subfolders, and managed symbolic links
are rejected rather than overwritten. The persistent `.hma-export.lock` coordinates this app's
imports and exports; external editors do not participate in that lock.

Publication stages a complete file set and records `.hma-export.pending` before moving files.
Imports reject a pending export; exporting again recovers the previous set before retrying.
This supports process-interruption recovery, not a guarantee against physical power loss or
external changes to the recovery files. Export remains a table project, not a full ROM serializer.
