#!/usr/bin/env python3
"""Bound an upstream discovery/execution process and clean up its own child process group."""
import math
import os
import signal
import subprocess
import sys

if len(sys.argv) < 3:
    sys.exit("Usage: run-upstream-command.py SECONDS COMMAND [ARG ...]")
seconds = float(sys.argv[1])
if not math.isfinite(seconds) or seconds <= 0:
    sys.exit("Timeout must be positive and finite.")
with subprocess.Popen(sys.argv[2:], start_new_session=True) as process:
    try:
        sys.exit(process.wait(timeout=seconds))
    except (subprocess.TimeoutExpired, KeyboardInterrupt) as error:
        reason = "timed out" if isinstance(error, subprocess.TimeoutExpired) else "was interrupted"
        print(f"FAIL: upstream command {reason}; terminating its process group.", file=sys.stderr, flush=True)
        os.killpg(process.pid, signal.SIGTERM)
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            os.killpg(process.pid, signal.SIGKILL)
            process.wait()
        sys.exit(124 if isinstance(error, subprocess.TimeoutExpired) else 130)
