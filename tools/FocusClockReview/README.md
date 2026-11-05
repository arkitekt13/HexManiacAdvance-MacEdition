# Focus clock investigation

Run with `--delay` to reproduce global-clock/dispatcher interference, then with
`--local --delay` to use an independent clock for the same real production animation.
All 13 original assertions are retained. Output samples position, width and completion.
The deliberate 35-ms delay is investigation stress, not a native interaction benchmark.
No user ROM, settings or native window is used. See tests/MAINTENANCE_REVIEW.md.
