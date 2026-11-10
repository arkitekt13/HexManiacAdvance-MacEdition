#!/usr/bin/env bash
# Review copied packaged code/runtime, not the user's native session. Requires a clean FR 1.1 base.
set -euo pipefail
if [[ $# -ne 3 ]]; then echo 'Usage: run.sh /path/to/HexManiacAdvance.app /path/to/clean-FireRed-1.1.gba /empty/output/directory' >&2; exit 2; fi
SOURCE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP="$(cd "$1" && pwd)"
BASE_ROM="$(python3 -c 'from pathlib import Path; import sys; print(Path(sys.argv[1]).resolve())' "$2")"
OUTPUT="$3"
python3 - "$APP" "$BASE_ROM" "$OUTPUT" <<'PY'
from pathlib import Path
import sys
app,base,out=map(lambda p:Path(p).resolve(),sys.argv[1:])
if not (app/'Contents/MacOS/HexManiacAdvance.dll').is_file() or not base.is_file(): raise SystemExit('App or input ROM is missing.')
if out==app or app in out.parents or out==base.parent: raise SystemExit('Use a separate empty output folder; input files are read-only.')
if out.exists() and any(out.iterdir()): raise SystemExit('Output must be empty; existing files are preserved.')
out.mkdir(parents=True,exist_ok=True)
PY
codesign --verify --deep --strict "$APP"
dotnet build "$SOURCE/PackagedProjectReview.csproj" -p:BundleBinaries="$APP/Contents/MacOS" -p:NuGetAudit=false --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false -v:q
python3 - "$APP" "$SOURCE" "$OUTPUT" <<'PY'
from pathlib import Path
import shutil,json,sys,hashlib
app,source,out=map(lambda p:Path(p).resolve(),sys.argv[1:])
driver=out/'driver.app/Contents/MacOS';shutil.copytree(app/'Contents/MacOS',driver)
built=source/'bin/Debug/net8.0/osx-arm64'
for name in ['PackagedProjectReview','PackagedProjectReview.dll']:shutil.copy2(built/name,driver/name)
# Preserve the packaged runtime graph, adding only the driver entry; do not resolve a host SDK runtime.
base=json.loads((driver/'HexManiacAdvance.deps.json').read_text());tool=json.loads((built/'PackagedProjectReview.deps.json').read_text());base['libraries'].update(tool['libraries'])
for target,libs in tool['targets'].items():base['targets'].setdefault(target,{}).update(libs)
(driver/'PackagedProjectReview.deps.json').write_text(json.dumps(base))
shutil.copy2(driver/'HexManiacAdvance.runtimeconfig.json',driver/'PackagedProjectReview.runtimeconfig.json')
profile=Path.home()/'Library/Application Support/HexManiacAdvance'
canary={'exists':profile.exists(),'files':{str(p.relative_to(profile)):hashlib.sha256(p.read_bytes()).hexdigest() for p in profile.rglob('*') if p.is_file()} if profile.exists() else {}}
(out/'normal-profile-before.json').write_text(json.dumps(canary))
(out/'assemblies.json').write_text(json.dumps({name:hashlib.sha256((driver/name).read_bytes()).hexdigest() for name in ['HexManiacAdvance.dll','HexManiac.Core.dll','Avalonia.Base.dll','Avalonia.Controls.dll','libcoreclr.dylib']},indent=2))
PY
OUTPUT="$(cd "$OUTPUT" && pwd)"
# Launch from an unrelated directory, through the apphost and bundled runtime.
cd /private/tmp
"$OUTPUT/driver.app/Contents/MacOS/PackagedProjectReview" "$OUTPUT/driver.app" "$OUTPUT/results" "$BASE_ROM"
python3 - "$OUTPUT" <<'PY'
from pathlib import Path
import sys,json,hashlib
out=Path(sys.argv[1]);profile=Path.home()/'Library/Application Support/HexManiacAdvance'
now={'exists':profile.exists(),'files':{str(p.relative_to(profile)):hashlib.sha256(p.read_bytes()).hexdigest() for p in profile.rglob('*') if p.is_file()} if profile.exists() else {}}
unchanged=now==json.loads((out/'normal-profile-before.json').read_text())
if not unchanged:raise SystemExit('Normal preferences changed during this run; investigate before claiming isolation.')
r=json.loads((out/'results/result.json').read_text());r['normalProfileUnchanged']=True;(out/'results/result.json').write_text(json.dumps(r,indent=2)+'\n')
print('Normal application preferences are byte-identical to their pre-run state.')
PY
