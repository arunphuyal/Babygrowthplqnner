#!/usr/bin/env bash
# Builds everything and writes upload-ready zips to deploy/out:
#   web.zip           -> public_html of the main domain (cPanel)
#   admin.zip         -> document root of the admin subdomain (cPanel)
#   api-linux.zip     -> a Linux server / VPS (self-contained, no .NET install needed)
#   api-windows.zip   -> Windows/IIS hosting (self-contained, includes web.config)
#
# Required: SITE_URL (https://yourdomain.com) and API_URL (https://api.yourdomain.com).
# Optional: SITE_NAME, CONTACT_EMAIL, GOOGLE_TAG_ID, ADSENSE_CLIENT, ADSENSE_SLOT, META_PIXEL_ID, SKIP_API=1
# The API must be reachable at PRERENDER_API_URL (default API_URL) so guides can be prerendered; otherwise only static pages are built.
set -euo pipefail
: "${SITE_URL:?set SITE_URL, e.g. https://yourdomain.com}"
: "${API_URL:?set API_URL, e.g. https://api.yourdomain.com}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/deploy/out"
rm -rf "$OUT" && mkdir -p "$OUT"

export VITE_SITE_URL="$SITE_URL" VITE_API_URL="$API_URL"
export VITE_SITE_NAME="${SITE_NAME:-Babybunu}" VITE_APP_NAME="${SITE_NAME:-Babybunu}"
export VITE_CONTACT_EMAIL="${CONTACT_EMAIL:-hello@example.com}"
export VITE_GOOGLE_TAG_ID="${GOOGLE_TAG_ID:-}" VITE_ADSENSE_CLIENT="${ADSENSE_CLIENT:-}" VITE_ADSENSE_SLOT="${ADSENSE_SLOT:-}" VITE_META_PIXEL_ID="${META_PIXEL_ID:-}"
export PRERENDER_API_URL="${PRERENDER_API_URL:-$API_URL}"

echo "==> website"
(cd "$ROOT/web" && npm ci --silent && npm run build --silent)
(cd "$ROOT/web/dist" && zip -qr "$OUT/web.zip" . -x '.DS_Store')

echo "==> admin portal"
(cd "$ROOT/admin" && npm ci --silent && npm run build --silent)
(cd "$ROOT/admin/dist" && zip -qr "$OUT/admin.zip" . -x '.DS_Store')

if [ "${SKIP_API:-0}" != "1" ]; then
  for rid in linux-x64 win-x64; do
    name=$([ "$rid" = linux-x64 ] && echo linux || echo windows)
    echo "==> API ($rid)"
    dotnet publish "$ROOT/backend/Babybunu.Api" -c Release -r "$rid" --self-contained true -o "$OUT/api-$name" --nologo -v q
    rm -f "$OUT/api-$name"/*.pdb "$OUT/api-$name"/appsettings.Development.json "$OUT"/api-$name/*.db*
    (cd "$OUT/api-$name" && zip -qr "$OUT/api-$name.zip" .)
    rm -rf "$OUT/api-$name"
  done
fi
echo; ls -lh "$OUT"
