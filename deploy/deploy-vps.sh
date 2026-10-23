#!/usr/bin/env bash
# Deploys api-linux.zip (from deploy/package.sh) to the VPS prepared by bootstrap-vps.sh, with automatic rollback.
#   ./deploy-vps.sh root@api.yourdomain.com deploy/out/api-linux.zip [https://api.yourdomain.com/health]
set -euo pipefail
TARGET="${1:?usage: $0 user@host path/to/api-linux.zip [health-url]}"
ZIP="${2:?usage: $0 user@host path/to/api-linux.zip [health-url]}"
HOST="${TARGET#*@}"
HEALTH="${3:-https://$HOST/health}"
[ -f "$ZIP" ] || { echo "no such file: $ZIP"; exit 1; }
REL="$(date +%Y%m%d%H%M%S)"

scp -q "$ZIP" "$TARGET:/tmp/babybunu-$REL.zip"
ssh "$TARGET" bash -s -- "$REL" <<'REMOTE'
set -euo pipefail
REL="$1"; ROOT=/opt/babybunu
mkdir -p "$ROOT/releases/$REL"
unzip -q "/tmp/babybunu-$REL.zip" -d "$ROOT/releases/$REL"
rm -f "/tmp/babybunu-$REL.zip"
chmod +x "$ROOT/releases/$REL/Babybunu.Api"
chown -R babybunu:babybunu "$ROOT/releases/$REL"
PREV=""
[ -L "$ROOT/current" ] && PREV="$(readlink -f "$ROOT/current")" # empty on the very first deploy
echo "$PREV" > "$ROOT/.previous"
ln -sfn "$ROOT/releases/$REL" "$ROOT/current"
systemctl restart babybunu-api
# keep the five newest releases
ls -1dt "$ROOT"/releases/* | tail -n +6 | xargs -r rm -rf
REMOTE

echo "Waiting for $HEALTH ..."
for _ in $(seq 1 30); do
  if curl -fsS --max-time 5 "$HEALTH" >/dev/null 2>&1; then echo "Healthy. Deployed release $REL."; exit 0; fi
  sleep 2
done

echo "Health check failed. Rolling back." >&2
ssh "$TARGET" 'set -e; PREV="$(cat /opt/babybunu/.previous)"; if [ -n "$PREV" ] && [ -d "$PREV" ]; then ln -sfn "$PREV" /opt/babybunu/current; systemctl restart babybunu-api; echo "Rolled back to $PREV"; else echo "No previous release to roll back to"; fi; journalctl -u babybunu-api -n 30 --no-pager'
exit 1
