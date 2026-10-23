#!/usr/bin/env bash
# Consistent nightly backup of the SQLite database (installed as /usr/local/bin/babybunu-backup by bootstrap-vps.sh).
# A backup on the same disk is not enough: copy /var/backups/babybunu off the server (rsync, rclone to object storage, ...).
set -euo pipefail
DB=/var/lib/babybunu/app.db
DEST=/var/backups/babybunu
mkdir -p "$DEST"
[ -f "$DB" ] || exit 0
sqlite3 "$DB" ".backup '$DEST/app-$(date +%F).db'"
find "$DEST" -name 'app-*.db' -mtime +14 -delete
