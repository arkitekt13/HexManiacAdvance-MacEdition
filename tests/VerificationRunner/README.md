# Verification runner checks

Exercises the real verification script in disposable directories with a fake dotnet executable.
Confirms that empty discovery, silent success, missing/zero/duplicate assertion counts, failed
restore/build/process execution all fail, and successful groups are counted correctly.
Run with `scripts/verify-mac.sh`; no ROM, GUI, or additional test framework is needed.
