#!/usr/bin/env bash
# Daily consistent backup of the SQLite database. Add to cron:  15 3 * * * /opt/api/backup-sqlite.sh
# Then copy /var/backups/babybunu somewhere OFF the server (object storage, another machine): a backup on the same disk is not a backup.
set -euo pipefail
DB=/var/lib/babybunu/app.db
DEST=/var/backups/babybunu
mkdir -p "$DEST"
sqlite3 "$DB" ".backup '$DEST/app-$(date +%F).db'"
find "$DEST" -name 'app-*.db' -mtime +14 -delete
