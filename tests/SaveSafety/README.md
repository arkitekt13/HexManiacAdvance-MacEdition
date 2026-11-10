# Save safety regression

Uses the original shared ViewPort with a stub filesystem to exercise ROM-write failure,
metadata-write failure, successful retry, Save As, and closing on cancel/discard/success/error.
Uses disposable real files to test staged-write failure, shrink replacement, UTF-8 metadata,
Unix permission preservation, symbolic links, and read-only protection. No real ROM is used.

Run `scripts/verify-mac.sh`. Native dialogs and the Windows WPF build are not exercised here.
ROM and metadata remain two separate files: metadata failure leaves the document open/unsaved
for retry, but this is not a power-loss transaction covering both files. Replacement can fail
when another program opens a file without allowing deletion; the old file is retained.

The [intermittent metadata-save investigation](INTERMITTENT_METADATA_SAVE.md)
records read-only project verification, the live-session evidence and remaining
uncertainty. Operation context now accompanies the original staged I/O exception;
the group passes 19 assertions without changing the write strategy or retry behavior.
