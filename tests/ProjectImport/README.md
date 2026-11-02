# Project import regression

Uses synthetic tables and disposable files to check numeric widths (including signed fields),
text encoding/length and escapes, row/column/manifest validation, compatible layouts, unchanged
round trips, one-step undo, and preservation of redo after rejection. An injected second-field
write failure verifies rollback of earlier bytes and metadata without losing existing redo.
Run `scripts/verify-mac.sh`; no ROM or native window is required.

New exports carry a version and table-layout fingerprint. Legacy exports with matching game code
and ROM size remain accepted with a note and field-by-field validation. Pointer, calculated, and
unsupported fields remain context only; this is not a complete ROM project serializer.
