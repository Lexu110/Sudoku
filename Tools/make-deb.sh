#!/usr/bin/env bash
# Packages a Unity Linux build into a .deb.
# Usage: Tools/make-deb.sh <linux-build-folder> [version]
# Needs dpkg-deb (on macOS: brew install dpkg).
set -euo pipefail

BUILD_DIR="${1:?Usage: $0 <linux-build-folder> [version]}"
VERSION="${2:-1.0.0}"
MAINTAINER="${MAINTAINER:-Sudoku Maintainer <you@example.com>}"

PKG=sudoku
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ICON="$SCRIPT_DIR/../Assets/Art/AppIcon.png"
OUT="$PWD/${PKG}_${VERSION}_amd64.deb"

command -v dpkg-deb >/dev/null || { echo "dpkg-deb not found. On macOS run: brew install dpkg" >&2; exit 1; }

EXE_PATH="$(ls "$BUILD_DIR"/*.x86_64 2>/dev/null | head -n1 || true)"
[ -n "$EXE_PATH" ] || { echo "No .x86_64 executable found in $BUILD_DIR" >&2; exit 1; }
EXE="$(basename "$EXE_PATH")"

ROOT="$(mktemp -d)/${PKG}_${VERSION}_amd64"
mkdir -p "$ROOT/DEBIAN" "$ROOT/opt/$PKG" "$ROOT/usr/bin" "$ROOT/usr/share/applications" "$ROOT/usr/share/pixmaps"

# Copy the game, leaving out Unity's debug-symbols folder (it says not to ship it).
(cd "$BUILD_DIR" && COPYFILE_DISABLE=1 tar \
    --exclude='*_BackUpThisFolder_ButDontShipItWithYourGame' --exclude='.DS_Store' -cf - .) \
  | (cd "$ROOT/opt/$PKG" && tar -xf -)

# Games are read-only under /opt; only the executable needs the execute bit.
find "$ROOT" -type d -exec chmod 755 {} +
find "$ROOT/opt/$PKG" -type f -exec chmod 644 {} +
chmod 755 "$ROOT/opt/$PKG/$EXE"

cat > "$ROOT/usr/bin/$PKG" <<EOF
#!/bin/sh
exec /opt/$PKG/$EXE "\$@"
EOF
chmod 755 "$ROOT/usr/bin/$PKG"

cp "$ICON" "$ROOT/usr/share/pixmaps/$PKG.png"
chmod 644 "$ROOT/usr/share/pixmaps/$PKG.png"

cat > "$ROOT/usr/share/applications/$PKG.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Sudoku
Comment=Neon-styled Sudoku with an online leaderboard
Exec=$PKG
Icon=$PKG
Terminal=false
Categories=Game;LogicGame;
EOF
chmod 644 "$ROOT/usr/share/applications/$PKG.desktop"

cat > "$ROOT/DEBIAN/control" <<EOF
Package: $PKG
Version: $VERSION
Section: games
Priority: optional
Architecture: amd64
Maintainer: $MAINTAINER
Installed-Size: $(du -sk "$ROOT/opt" | cut -f1)
Depends: libc6, libgl1, libx11-6
Recommends: libasound2 | libasound2t64
Description: Sudoku
 A neon-styled Sudoku game with accounts and an online leaderboard.
EOF

# xz keeps the package readable by older Debian and Ubuntu releases.
dpkg-deb --root-owner-group -Zxz --build "$ROOT" "$OUT"
echo "Created $OUT"
