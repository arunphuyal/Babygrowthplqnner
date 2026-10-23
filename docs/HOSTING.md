# Self-hosting on cPanel (and what to do about the API)

## The one thing to check first
cPanel shared hosting is built for **PHP and static sites on Apache**. It normally **cannot run an ASP.NET Core API** (no .NET runtime, and long-running processes are not allowed). Ask your host: *"Do you support ASP.NET Core 8, or can I run a long-lived Linux process (systemd/Passenger)?"* If the answer is no, use one of these layouts. The website and admin portal are static files and always work on cPanel.

| Option | Where the API runs | Notes |
|---|---|---|
| **A. Static on cPanel + API on a small VPS (recommended)** | A cheap Linux VPS (any provider) | Simple, cheap, full control. Needs basic Linux admin. Use the files in `deploy/` (systemd unit, Caddy, backup script). |
| B. cPanel/WHM **VPS or dedicated** with root | Same server, behind Apache `ProxyPass` + systemd | Same as A but one bill. Requires root; not available on shared plans. |
| C. **Windows hosting with ASP.NET Core** (IIS) | The Windows host | Not cPanel (usually Plesk or a custom panel). Upload `api-windows.zip`; it contains `web.config`. Put settings in `web.config` `<environmentVariables>` or the host's panel. |
| D. Managed platform (Azure App Service, Fly.io, Render, Railway) | The platform | Least admin work, pricier. Same environment variables. |

Layout used below: `yourdomain.com` (website), `admin.yourdomain.com` (admin portal), `api.yourdomain.com` (API). The browser calls the API across domains, so set `Cors__Origins__*` (see `deploy/api.env.example`).

## 1. Build the upload packages
On your own computer (needs Node 20+, .NET 8 SDK, `zip`):
```bash
SITE_URL=https://yourdomain.com API_URL=https://api.yourdomain.com \
SITE_NAME="Your Brand" CONTACT_EMAIL=hello@yourdomain.com \
GOOGLE_TAG_ID=G-XXXXXXXXXX ./deploy/package.sh
```
Output in `deploy/out/`: `web.zip`, `admin.zip`, `api-linux.zip`, `api-windows.zip`. Optional: `ADSENSE_CLIENT`, `ADSENSE_SLOT`, `META_PIXEL_ID`, `SKIP_API=1`. If the API is already live when you build, the guides are prerendered into static HTML and `sitemap.xml` (best for SEO); otherwise only the static pages are, so build again once the API is up and the guides exist.

## 2. Website and admin on cPanel
1. cPanel > **Domains**: create `admin.yourdomain.com` (its own document root) and `api.yourdomain.com` (DNS only, if the API is elsewhere, point its A record there instead).
2. cPanel > **SSL/TLS Status**: run **AutoSSL** for all domains.
3. cPanel > **File Manager**: upload `web.zip` to `public_html`, **Extract**; upload `admin.zip` to the admin subdomain's folder, **Extract**. Make sure hidden files are shown: `.htaccess` must be there (it handles HTTPS, single-page routes, prerendered guide URLs, caching and security headers; tested on Apache 2.4 with `mod_rewrite` and `mod_headers`).
4. Put your AdSense line in `ads.txt` (document root of the main domain) once approved.
5. Visit the site, a guide URL, and `/sitemap.xml`. In Google Search Console add the property and submit the sitemap.
The admin `.htaccess` also sends `noindex` and blocks framing. For extra safety restrict the admin subdomain by IP in cPanel > **IP Blocker** or add `Require ip` rules if you have a fixed address.

## 3. The API on a Linux VPS (option A)
```bash
# once, as root: create a user and folders
useradd -r -m apiuser; mkdir -p /opt/api /var/lib/babybunu; chown apiuser /var/lib/babybunu
unzip api-linux.zip -d /opt/api; chmod +x /opt/api/Babybunu.Api
cp deploy/api.env.example /etc/babybunu-api.env   # edit it; chmod 600
cp deploy/babybunu-api.service /etc/systemd/system/
systemctl daemon-reload && systemctl enable --now babybunu-api
# HTTPS reverse proxy (Caddy): put deploy/Caddyfile.example in /etc/caddy/Caddyfile, then: systemctl reload caddy
curl https://api.yourdomain.com/health        # {"status":"ok"}
```
The first start applies the database migrations and creates the admin account from `Seed__AdminEmail` / `Seed__AdminPassword`. Generate `Jwt__Key` with `openssl rand -base64 48`; rotating it signs everyone out. **Updating:** upload the new zip, replace the files in `/opt/api` (keep the database folder), `systemctl restart babybunu-api`.

**Database:** SQLite is fine for the early stage on one server. Back it up daily with `deploy/backup-sqlite.sh` and copy backups **off the server**; test a restore. When traffic or reliability needs grow, move to PostgreSQL or MySQL (needs a different EF provider and regenerated migrations; cPanel's MySQL is not usable by the API today).

**Firewall:** allow only 80/443 and SSH; the API listens on `127.0.0.1:5000`.

## 4. Email (password reset)
Create a mailbox such as `no-reply@yourdomain.com` in cPanel > **Email Accounts**, then set `Email__SmtpHost=mail.yourdomain.com`, port `587` (STARTTLS), user, password and `Email__From`. Use port 587; the built-in client does not support implicit-TLS port 465. In cPanel > **Email Deliverability** make sure SPF and DKIM are valid, otherwise reset emails go to spam. Shared-host IPs have sending limits; for anything beyond a trickle use a transactional email service.

## 5. Stripe and the Google tag
- Stripe webhook endpoint: `https://api.yourdomain.com/api/donations/webhook/stripe` (event `checkout.session.completed`); put the secret in `Donations__StripeWebhookSecret`. Success/cancel URLs point at the website.
- Google tag: set `GOOGLE_TAG_ID` at build time. It only loads after a visitor allows analytics or ads, and it starts in Consent Mode with every signal denied. In Google Tag Manager/Google Ads you can verify that consent signals arrive (Tag Assistant).
- Shared report links are served by the API at `https://api.yourdomain.com/r/<token>`.

## 6. Checklist before going live
- [ ] `Jwt__Key` set, seeded admin password changed to a long passphrase, no dev credentials
- [ ] AutoSSL green on all three hostnames, HTTP redirects to HTTPS
- [ ] `Cors__Origins__*` lists only your real domains
- [ ] Backups running **and** copied off-server, restore tested
- [ ] Stripe in live mode with the webhook verified by a real test payment
- [ ] SPF/DKIM pass; a password-reset email reaches an inbox
- [ ] Privacy page edited with your company details; consent banner works; AdSense site approved
- [ ] Uptime monitor on `https://api.yourdomain.com/health`
