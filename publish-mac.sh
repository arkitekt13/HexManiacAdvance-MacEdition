#!/usr/bin/env bash
# Build a self-contained, ad-hoc-signed Mac app without replacing a previous failed build.
# Usage: /path/to/publish-mac.sh [osx-arm64|osx-x64]
set -euo pipefail
if [[ $# -gt 1 ]]; then echo 'Usage: publish-mac.sh [osx-arm64|osx-x64]' >&2; exit 2; fi
RID="${1:-osx-arm64}"
case "$RID" in osx-arm64|osx-x64) ;; *) echo "Unsupported Mac architecture: $RID" >&2; exit 2;; esac
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NAME="HexManiacAdvance"
OUT="$ROOT/artifacts/mac/$RID"
FINAL="$OUT/$NAME.app"
VERSION="$(sed -n 's/^\[assembly: AssemblyFileVersion("\([0-9.]*\)")\].*/\1/p' "$ROOT/src/SharedAssemblyInfo.cs" | tr -d '\r')"
if [[ ! "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?$ ]]; then echo 'Could not read the application version.' >&2; exit 1; fi
command -v dotnet >/dev/null || { echo 'Install the .NET 8 SDK to build this application.' >&2; exit 1; }
command -v codesign >/dev/null || { echo 'Mac packaging requires codesign on macOS.' >&2; exit 1; }
mkdir -p "$OUT"
STAGE="$(mktemp -d "$OUT/.stage.XXXXXX")"
BACKUP=""
cleanup() {
  if [[ -n "$BACKUP" && -d "$BACKUP" && ! -e "$FINAL" ]]; then mv "$BACKUP" "$FINAL"; fi
  rm -rf "$STAGE"
}
trap cleanup EXIT

dotnet publish "$ROOT/src/HexManiac.Avalonia/HexManiac.Avalonia.csproj" \
  -c Release -r "$RID" --self-contained true -p:UseAppHost=true \
  --disable-build-servers -p:UseSharedCompilation=false -m:1 -o "$STAGE/publish"

for file in "$NAME" "$NAME.dll" "$NAME.runtimeconfig.json" libhostfxr.dylib libcoreclr.dylib; do
  [[ -s "$STAGE/publish/$file" ]] || { echo "Missing self-contained publish file: $file" >&2; exit 1; }
done
for file in hma.py armReference.txt scriptReference.txt pcsReference.txt tableReference.txt default.toml; do
  [[ -s "$STAGE/publish/resources/$file" ]] || { echo "Missing application resource: $file" >&2; exit 1; }
done
APP="$STAGE/$NAME.app"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$STAGE/publish/." "$APP/Contents/MacOS/"
cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleName</key><string>$NAME</string>
  <key>CFBundleDisplayName</key><string>HexManiacAdvance Mac Edition</string>
  <key>CFBundleIdentifier</key><string>io.github.hexmaniacadvance.mac</string>
  <key>CFBundleExecutable</key><string>$NAME</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>CFBundleDocumentTypes</key><array><dict>
    <key>CFBundleTypeName</key><string>Game Boy Advance ROM</string>
    <key>CFBundleTypeExtensions</key><array><string>gba</string></array>
    <key>CFBundleTypeRole</key><string>Editor</string>
  </dict></array>
</dict></plist>
PLIST
# Local ad-hoc signature only; this is not Developer ID signing or notarization.
codesign --force --deep --sign - "$APP"
codesign --verify --deep --strict "$APP"
# Build, resources and signing must all succeed before the previous bundle is moved.
if [[ -e "$FINAL" ]]; then
  BACKUP="$OUT/.previous-$NAME-$(basename "$STAGE")"
  mv "$FINAL" "$BACKUP"
fi
mv "$APP" "$FINAL"
if [[ -n "$BACKUP" ]]; then rm -rf "$BACKUP"; BACKUP=""; fi
echo "Built $FINAL ($VERSION, $RID; ad-hoc signed, not notarized)"
