#!/usr/bin/env bash
# Build and execute every headless regression group; never trust an empty dotnet test run.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
restore=true
if [[ "${1:-}" == "--no-restore" ]]; then restore=false; shift; fi
if [[ $# -ne 0 ]]; then printf 'Usage: %s [--no-restore]\n' "$0" >&2; exit 2; fi
projects=(tests/*/*.csproj)
if [[ ! -f "${projects[0]}" ]]; then printf 'FAIL: no regression projects found.\n' >&2; exit 1; fi
log="$(mktemp -t hma-verification.XXXXXX)"
trap 'rm -f "$log"' EXIT
groups=0
assertions=0
for project in "${projects[@]}"; do
  name="$(basename "$project" .csproj)"
  printf '\nVerifying %s\n' "$name"
  if $restore; then dotnet restore "$project" --ignore-failed-sources --nologo -p:NuGetAudit=false; fi
  dotnet build "$project" --no-restore --nologo --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false -v quiet
  # --no-build alone can execute stale binaries, so the build above is mandatory.
  dotnet "$(dirname "$project")/bin/Debug/net8.0/$name.dll" | tee "$log"
  count="$(awk '/^CHECKS_PASSED:[0-9]+$/ { n++; split($0,a,":"); value=a[2] } END { if(n==1 && value>0) print value; else exit 1 }' "$log")" || {
    printf 'FAIL: %s did not report exactly one positive assertion count.\n' "$name" >&2
    exit 1
  }
  groups=$((groups + 1))
  assertions=$((assertions + count))
done
printf '\nPASS: %s regression groups, %s assertions; no groups skipped.\n' "$groups" "$assertions"
