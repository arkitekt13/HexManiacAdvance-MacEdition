# Upstream verification runner checks

Exercises the actual shell runner, result validator, and process watchdog with disposable fake
dotnet processes and synthetic TRX/xUnit XML. No upstream assertions, ROMs, native windows, or
network are involved. Run `scripts/verify-mac.sh` to include this group.

Checks ensure that empty discovery, entirely skipped runs, absent or inconsistent results,
unknown outcomes, aborted hosts, engine errors, and process failures cannot pass. Valid results
retain JSON summaries, failures remain failures in both runner modes, and the watchdog stops a
deliberately stalled subprocess. Upstream xUnit case counts remain separate from these assertions.
