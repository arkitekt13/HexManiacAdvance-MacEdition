#!/usr/bin/env bash
# Regenerate the committed macOS icon from the reviewed square PNG artwork.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE="$ROOT/assets/macos/AppIcon.png"
DESTINATION="$ROOT/assets/macos/AppIcon.icns"
for tool in sips iconutil; do
  command -v "$tool" >/dev/null || { echo "Missing icon tool: $tool" >&2; exit 1; }
done
[[ -s "$SOURCE" ]] || { echo "Missing source artwork: $SOURCE" >&2; exit 1; }
STAGE="$(mktemp -d "${TMPDIR:-/tmp}/hma-icon.XXXXXX")"
trap 'rm -rf "$STAGE"' EXIT
mkdir "$STAGE/AppIcon.iconset"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$SOURCE" --out "$STAGE/AppIcon.iconset/icon_${size}x${size}.png" >/dev/null
  double=$((size * 2))
  sips -z "$double" "$double" "$SOURCE" --out "$STAGE/AppIcon.iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$STAGE/AppIcon.iconset" -o "$STAGE/AppIcon.icns"
cp "$STAGE/AppIcon.icns" "$DESTINATION"
echo "Built $DESTINATION"
