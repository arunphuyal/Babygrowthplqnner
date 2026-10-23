#!/usr/bin/env bash
# One-time setup of a fresh Ubuntu 22.04/24.04 VPS for the Babybunu API. Run as root:
#   ./bootstrap-vps.sh api.yourdomain.com you@yourdomain.com
# Safe to re-run. It never overwrites an existing /etc/babybunu-api.env.
set -euo pipefail
DOMAIN="${1:?usage: $0 api.yourdomain.com admin-email}"
ADMIN_EMAIL="${2:?usage: $0 api.yourdomain.com admin-email}"
HERE="$(cd "$(dirname "$0")" && pwd)"
[ "$(id -u)" = 0 ] || { echo "run as root"; exit 1; }

export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq caddy sqlite3 unzip ufw curl ca-certificates libicu-dev

id babybunu >/dev/null 2>&1 || useradd --system --home /opt/babybunu --shell /usr/sbin/nologin babybunu
mkdir -p /opt/babybunu/releases /var/lib/babybunu /var/backups/babybunu
chown -R babybunu:babybunu /opt/babybunu /var/lib/babybunu
chmod 750 /var/lib/babybunu

if [ ! -f /etc/babybunu-api.env ]; then
  JWT="$(openssl rand -base64 48 | tr -d '\n')"
  ADMIN_PW="$(openssl rand -base64 18 | tr -d '\n=+/')"
  install -m 600 -o root -g root "$HERE/api.env.example" /etc/babybunu-api.env
  sed -i "s|^Jwt__Key=.*|Jwt__Key=$JWT|; s|^Seed__AdminEmail=.*|Seed__AdminEmail=$ADMIN_EMAIL|; s|^Seed__AdminPassword=.*|Seed__AdminPassword=$ADMIN_PW|" /etc/babybunu-api.env
  echo
  echo "Created /etc/babybunu-api.env. First admin login:  $ADMIN_EMAIL  /  $ADMIN_PW"
  echo "Change that password, then edit the file (domains, email, Stripe) and run: systemctl restart babybunu-api"
else
  echo "/etc/babybunu-api.env already exists, left unchanged."
fi

install -m 644 "$HERE/babybunu-api.service" /etc/systemd/system/babybunu-api.service
install -m 755 "$HERE/backup-sqlite.sh" /usr/local/bin/babybunu-backup
cat > /etc/caddy/Caddyfile <<CADDY
$DOMAIN {
	encode gzip
	reverse_proxy 127.0.0.1:5000
}
CADDY
echo '15 3 * * * root /usr/local/bin/babybunu-backup' > /etc/cron.d/babybunu-backup

ufw allow OpenSSH >/dev/null; ufw allow 80,443/tcp >/dev/null; ufw --force enable >/dev/null
systemctl daemon-reload
systemctl enable babybunu-api >/dev/null
systemctl reload caddy || systemctl restart caddy
echo
echo "Done. Next: ./deploy-vps.sh root@$DOMAIN api-linux.zip   (DNS A record for $DOMAIN must point here for HTTPS to be issued)"
echo "Remember to copy /var/backups/babybunu off this server (the nightly backup is on the same disk)."
