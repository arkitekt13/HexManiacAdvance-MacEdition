# Patch safety regression

Exercises real IPS/UPS parsing on synthetic byte arrays, including randomized forward/reverse
round trips, expansion, shrinking, empty files, zero tails, the reserved IPS EOF offset,
checksum failures, malformed input, output-size limits, and retention of pre-existing undo history
when ViewPort rejects an import. No ROM, native window, or extra test framework is required.
Run `scripts/verify-mac.sh`.

Rejected patches do not alter model bytes or request a change token. Successful UI patch imports
retain the original metadata-rebuild/history-reset behavior; this change does not promise undo
of successful whole-ROM patch imports. UPS candidates are limited to 64 MiB; IPS generation is
limited to its 24-bit address format, and larger ROMs should use UPS. Native choice dialogs and
Windows-specific rendering are not exercised by this group.
