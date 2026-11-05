#!/usr/bin/env bash
# Prepare a local helper ZIP; this does not upload a release or notarize the app.
# Usage: ./package-mac.sh [osx-arm64|osx-x64]
set -euo pipefail
if [[ $# -gt 1 ]]; then echo 'Usage: package-mac.sh [osx-arm64|osx-x64]' >&2; exit 2; fi
RID="${1:-osx-arm64}"
case "$RID" in osx-arm64|osx-x64) ;; *) echo "Unsupported Mac architecture: $RID" >&2; exit 2;; esac
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
for tool in ditto plutil shasum; do command -v "$tool" >/dev/null || { echo "Missing packaging tool: $tool" >&2; exit 1; }; done
bash "$ROOT/publish-mac.sh" "$RID"
APP="$ROOT/artifacts/mac/$RID/HexManiacAdvance.app"
VERSION="$(plutil -extract CFBundleShortVersionString raw -o - "$APP/Contents/Info.plist")"
REVISION="$(git -C "$ROOT" rev-parse HEAD 2>/dev/null || echo source-archive)"
DIRTY=false
if [[ "$REVISION" != source-archive && -n "$(git -C "$ROOT" status --porcelain --untracked-files=no)" ]]; then DIRTY=true; fi
SUFFIX="${REVISION:0:12}"
if [[ "$DIRTY" == true ]]; then SUFFIX="$SUFFIX-dirty"; fi
NAME="HexManiacAdvance-MacEdition-$VERSION-$RID-$SUFFIX"
OUT="$ROOT/artifacts/releases"
ZIP="$OUT/$NAME.zip"
CHECKSUM="$ZIP.sha256"
mkdir -p "$OUT"
if [[ -e "$ZIP" || -L "$ZIP" || -e "$CHECKSUM" || -L "$CHECKSUM" ]]; then
  echo "Archive already exists; existing files were preserved: $ZIP" >&2; exit 1
fi
STAGE="$(mktemp -d "$OUT/.package.XXXXXX")"
ZIP_CREATED=false
CHECKSUM_CREATED=false
COMPLETE=false
cleanup() {
  if [[ "$COMPLETE" != true ]]; then
    if [[ "$ZIP_CREATED" == true ]]; then rm -f "$ZIP"; fi
    if [[ "$CHECKSUM_CREATED" == true ]]; then rm -f "$CHECKSUM"; fi
  fi
  rm -rf "$STAGE"
}
trap cleanup EXIT
PAYLOAD="$STAGE/$NAME"
mkdir -p "$PAYLOAD"
cp -R "$APP" "$PAYLOAD/HexManiacAdvance.app"
cp "$ROOT/LICENSE" "$PAYLOAD/LICENSE"
cp "$ROOT/docs/MAC_HELPER_TESTING.md" "$PAYLOAD/HELPER-TESTING.md"
APP_SHA="$(shasum -a 256 "$APP/Contents/MacOS/HexManiacAdvance.dll" | awk '{print $1}')"
cat > "$PAYLOAD/BUILD-INFO.txt" <<INFO
HexManiacAdvance Mac Edition
App version: $VERSION
Architecture: $RID
Source revision: $REVISION
Tracked working tree modified: $DIRTY
Managed application SHA-256: $APP_SHA
Signing: ad-hoc; not Developer ID signed or notarized
Runtime details: HexManiacAdvance.app/Contents/MacOS/HexManiacAdvance.runtimeconfig.json
INFO
cat > "$PAYLOAD/START-HERE.txt" <<'INFO'
HexManiacAdvance Mac Edition - helper build

Move HexManiacAdvance.app to Applications, then launch it and choose Open ROM.
The app includes its runtime; you do not need to install .NET or Visual Studio.
Use a disposable ROM copy and keep its matching .toml metadata beside it.

This helper build is ad-hoc signed, not notarized. For a trusted build blocked by
macOS, the targeted command after placing it in Applications is:
xattr -dr com.apple.quarantine "/Applications/HexManiacAdvance.app"

The green Play button selects an external emulator such as mGBA.app. Use Browse
if it is absent from the initial list; the choice is remembered. Unsaved edits
require Save and Launch. No ROM, emulator or personal settings are included.

See BUILD-INFO.txt for identification and HELPER-TESTING.md for checks/reporting.
Mac setup and bug reports: https://github.com/arkitekt13/HexManiacAdvance-MacEdition
General HMA tutorials: https://github.com/haven1433/HexManiacAdvance/wiki

Thank you to Haven for HexManiacAdvance and to xavierkeys for establishing the
original working Mac port: https://github.com/Xkeys-bot/HexManiacAdvance-mac
Maintainer of this fork: arkitekt13. The original MIT license is included.
INFO
ditto -c -k --sequesterRsrc --keepParent "$PAYLOAD" "$STAGE/$NAME.zip"
(cd "$STAGE" && shasum -a 256 "$NAME.zip" > "$NAME.zip.sha256")
# Hard-link publication refuses an existing destination rather than overwriting it.
ln "$STAGE/$NAME.zip" "$ZIP"
ZIP_CREATED=true
ln "$STAGE/$NAME.zip.sha256" "$CHECKSUM"
CHECKSUM_CREATED=true
COMPLETE=true
echo "Prepared $ZIP"
echo "Checksum $CHECKSUM"
