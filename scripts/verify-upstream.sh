#!/usr/bin/env bash
# Build upstream xUnit tests, verify discovery, and retain structured results.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
restore=true
configuration=Debug
mode=vstest
suite=HexManiac.Tests
restore_args=(--nologo -p:NuGetAudit=false)
while [[ $# -gt 0 ]]; do
  case "$1" in
    --integration) suite=HexManiac.Integration; shift ;;
    --in-process) mode=in-process; shift ;;
    --no-restore) restore=false; shift ;;
    --configuration)
      [[ $# -ge 2 && ( "$2" == Debug || "$2" == Release ) ]] || { printf 'Expected Debug or Release.\n' >&2; exit 2; }
      configuration="$2"; shift 2 ;;
    --source)
      [[ $# -ge 2 ]] || { printf 'Expected a NuGet source.\n' >&2; exit 2; }
      restore_args+=(--source "$2"); shift 2 ;;
    *) printf 'Usage: %s [--integration] [--in-process] [--no-restore] [--configuration Debug|Release] [--source NUGET_SOURCE]\n' "$0" >&2; exit 2 ;;
  esac
done
command -v python3 >/dev/null || { printf 'Python 3 is required to validate test results.\n' >&2; exit 2; }
export DOTNET_CLI_UI_LANGUAGE=en
export VSTEST_CONNECTION_TIMEOUT="${VSTEST_CONNECTION_TIMEOUT:-15}"
# Keep this harness's restored dependencies isolated and writable; respect an explicit override.
export NUGET_PACKAGES="${NUGET_PACKAGES:-$ROOT/artifacts/upstream-packages}"
cd "$ROOT"
mkdir -p artifacts/upstream-tests
results="$(mktemp -d "$ROOT/artifacts/upstream-tests/run.XXXXXX")"
printf 'Upstream results: %s\n' "$results"
project="$ROOT/src/$suite/$suite.csproj"
runner_args=("$results")
if [[ "$suite" == HexManiac.Integration ]]; then runner_args+=("$suite"); fi
if [[ "$mode" == in-process ]]; then project="$ROOT/tools/UpstreamTestRunner/UpstreamTestRunner.csproj"; fi
if $restore; then
  python3 "$ROOT/scripts/run-upstream-command.py" 120 dotnet restore "$project" --disable-parallel -m:1 -nr:false -p:Configuration="$configuration" "${restore_args[@]}"
fi
dotnet build "$project" --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false -c "$configuration"
# The in-process option uses the same xUnit engine without VSTest's local socket connection.
if [[ "$mode" == in-process ]]; then
  set +e
  python3 "$ROOT/scripts/run-upstream-command.py" 360 dotnet "$ROOT/tools/UpstreamTestRunner/bin/$configuration/net8.0/UpstreamTestRunner.dll" "${runner_args[@]}" > "$results/execution.log" 2>&1
  test_exit=$?
  python3 "$ROOT/scripts/summarize-upstream.py" --format xunit "$results/discovery.log" "$results/results.xunit.xml" "$results/summary.json"
  summary_exit=$?
  set -e
  if [[ $test_exit -ne 0 || $summary_exit -ne 0 ]]; then
    printf 'FAIL: in-process upstream verification; see %s/execution.log and results.xunit.xml.\n' "$results" >&2
    exit 1
  fi
  printf 'PASS: in-process upstream verification; results retained at %s\n' "$results"
  exit 0
fi
# Never execute stale binaries; list-tests must also complete successfully.
if ! python3 "$ROOT/scripts/run-upstream-command.py" 120 dotnet test "$project" --no-build --no-restore --nologo -c "$configuration" --list-tests > "$results/discovery.log" 2>&1; then
  cat "$results/discovery.log"; exit 1
fi
if ! python3 - "$results/discovery.log" <<'PY'
from pathlib import Path
import sys
lines = Path(sys.argv[1]).read_text().splitlines()
if not any(line.startswith("    HavenSoft.HexManiac.") for line in lines):
    sys.exit("FAIL: no upstream tests were discovered.")
PY
then
  cat "$results/discovery.log"; exit 1
fi
set +e
python3 "$ROOT/scripts/run-upstream-command.py" 360 dotnet test "$project" --no-build --no-restore --nologo -c "$configuration" \
  --results-directory "$results" --logger 'trx;LogFileName=results.trx' > "$results/execution.log" 2>&1
test_exit=$?
python3 "$ROOT/scripts/summarize-upstream.py" "$results/discovery.log" "$results/results.trx" "$results/summary.json"
summary_exit=$?
set -e
if [[ $test_exit -ne 0 || $summary_exit -ne 0 ]]; then
  printf 'FAIL: upstream verification; see %s/execution.log and results.trx.\n' "$results" >&2
  exit 1
fi
printf 'PASS: upstream verification; results retained at %s\n' "$results"
