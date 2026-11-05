# Synthetic ID-suggestion scan benchmark

Build this project and run its DLL from any directory. It uses the real scanner/helper and
fabricated BPRE metadata/bytes only, with 100 or 1,000 formatted entry scripts sharing a tail
of 25 scripts. It measures initialization, 100 unchanged scoped requests and one refresh after
a byte change. It reports per-request managed allocations for unchanged requests.

The before/after records in tests/id-suggestion-review were collected with identical generated
probe code at debf793b and with the known-entry scan refinement. They are single-run observations,
not statistical medians or representative-ROM timings. The first smaller case includes more
JIT warmup. Both variants suggest the same flag. The cyclic/shared/unformatted-target regression
checks reference preservation independently of wall-clock timing.

This stresses the newly added retained-script scan. Existing reachable-map scans remain in place;
actual ROM speedups depend on their share of the workload and script graph shape. No native UI,
gameplay, commercial ROM, settings or emulator is used, and no speed threshold is a test assertion.
