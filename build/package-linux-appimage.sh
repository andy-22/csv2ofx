#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 3 ]; then
  echo "Usage: $0 <publish-dir> <output-dir> <version>" >&2
  exit 1
fi

PUBLISH_DIR="$(cd "$1" && pwd)"
OUTPUT_DIR="$(mkdir -p "$2" && cd "$2" && pwd)"
VERSION="$3"

APP_NAME="Csv2Ofx"
EXECUTABLE="Csv2Ofx.Gui"
APP_ID="com.csv2ofx.gui"
APPDIR="$OUTPUT_DIR/$APP_NAME.AppDir"
APPIMAGE_TOOL="$OUTPUT_DIR/appimagetool-x86_64.AppImage"
APPIMAGE_PATH="$OUTPUT_DIR/$APP_NAME-$VERSION-linux-x64.AppImage"

rm -rf "$APPDIR"
mkdir -p \
  "$APPDIR/usr/bin" \
  "$APPDIR/usr/share/applications" \
  "$APPDIR/usr/share/icons/hicolor/scalable/apps"

cp -a "$PUBLISH_DIR/." "$APPDIR/usr/bin/"
chmod +x "$APPDIR/usr/bin/$EXECUTABLE"

cat > "$APPDIR/AppRun" <<EOF
#!/usr/bin/env bash
HERE="\$(dirname "\$(readlink -f "\${0}")")"
exec "\$HERE/usr/bin/$EXECUTABLE" "\$@"
EOF
chmod +x "$APPDIR/AppRun"

cat > "$APPDIR/$APP_ID.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=$APP_NAME
Comment=Convert CSV exports to OFX files
Exec=$EXECUTABLE
Icon=csv2ofx
Categories=Office;Finance;
Terminal=false
StartupWMClass=$EXECUTABLE
EOF
cp "$APPDIR/$APP_ID.desktop" "$APPDIR/usr/share/applications/$APP_ID.desktop"

cat > "$APPDIR/csv2ofx.svg" <<'EOF'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128">
  <rect width="128" height="128" rx="24" fill="#1f7a5c"/>
  <path d="M30 26h50l18 18v58H30z" fill="#ffffff"/>
  <path d="M80 26v19h18z" fill="#cfeee5"/>
  <path d="M43 57h42M43 72h42M43 87h28" stroke="#1f7a5c" stroke-width="8" stroke-linecap="round"/>
</svg>
EOF
cp "$APPDIR/csv2ofx.svg" "$APPDIR/usr/share/icons/hicolor/scalable/apps/csv2ofx.svg"
ln -s "csv2ofx.svg" "$APPDIR/.DirIcon"

if [ ! -x "$APPIMAGE_TOOL" ]; then
  curl -L \
    "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage" \
    -o "$APPIMAGE_TOOL"
  chmod +x "$APPIMAGE_TOOL"
fi

rm -f "$APPIMAGE_PATH"
ARCH=x86_64 APPIMAGE_EXTRACT_AND_RUN=1 "$APPIMAGE_TOOL" "$APPDIR" "$APPIMAGE_PATH"
chmod +x "$APPIMAGE_PATH"
