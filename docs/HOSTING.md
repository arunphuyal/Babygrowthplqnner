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
You need a fresh Ubuntu 22.04/24.04 server, SSH access as root (or a sudo user), and a DNS **A record** `api.yourdomain.com` pointing at its IP (Caddy needs this to obtain the HTTPS certificate).
```bash
# 1. copy the deploy folder up and run the one-time setup (installs Caddy, creates the service user, firewall, nightly backup)
scp -r deploy root@api.yourdomain.com:/root/
ssh root@api.yourdomain.com 'cd /root/deploy && ./bootstrap-vps.sh api.yourdomain.com you@yourdomain.com'
#    -> prints the first admin login once. Then edit /etc/babybunu-api.env (domains, email, Stripe) and restart the service.

# 2. every release: from your computer
SITE_URL=https://yourdomain.com API_URL=https://api.yourdomain.com ./deploy/package.sh
./deploy/deploy-vps.sh root@api.yourdomain.com deploy/out/api-linux.zip
```
`deploy-vps.sh` uploads the zip, switches the `current` symlink to the new release, restarts the service and waits for `/health`. **If the health check fails it switches back to the previous release automatically** (the five most recent releases are kept). The first start applies the database migrations and creates the admin account from `Seed__AdminEmail` / `Seed__AdminPassword`. Generate a new `Jwt__Key` with `openssl rand -base64 48`; rotating it signs everyone out.

**Database:** SQLite (`/var/lib/babybunu/app.db`) is fine for the early stage on one server. A consistent copy is made nightly into `/var/backups/babybunu` (14 days kept). **Copy that folder off the server** (rsync or rclone to object storage) and test a restore: `systemctl stop babybunu-api; cp app-YYYY-MM-DD.db /var/lib/babybunu/app.db; chown babybunu: /var/lib/babybunu/app.db; systemctl start babybunu-api`. When traffic or reliability needs grow, move to PostgreSQL or MySQL (needs a different EF provider and regenerated migrations; cPanel's MySQL is not usable by the API today).

**Firewall:** only SSH, 80 and 443 are open; the API itself listens on `127.0.0.1:5000`. Logs: `journalctl -u babybunu-api -f`.

**On an ASP.NET-capable host instead (option C):** upload `api-windows.zip` (it has `web.config`) and set the same settings as environment variables in `web.config` or the host panel. Make the folder holding the SQLite file writable by the app pool identity.

**CI:** `.github/workflows/ci.yml` builds and tests everything on every push and checks that the EF migrations match the model. Run it manually with "package" ticked (and the repository variables `SITE_URL`, `API_URL`, optionally `SITE_NAME`, `CONTACT_EMAIL`, `GOOGLE_TAG_ID`, `ADSENSE_CLIENT`, `ADSENSE_SLOT`) to get the upload zips as a downloadable artifact.

## 4. Email (password reset)
Create a mailbox such as `no-reply@yourdomain.com` in cPanel > **Email Accounts**, then set `Email__SmtpHost=mail.yourdomain.com`, port `587` (STARTTLS), user, password and `Email__From`. Use port 587; the built-in client does not support implicit-TLS port 465. In cPanel > **Email Deliverability** make sure SPF and DKIM are valid, otherwise reset emails go to spam. Shared-host IPs have sending limits; for anything beyond a trickle use a transactional email service.

## 5. Stripe and the Google tag
- Stripe webhook endpoint: `https://api.yourdomain.com/api/donations/webhook/stripe` (event `checkout.session.completed`); put the secret in `Donations__StripeWebhookSecret`. Success/cancel URLs point at the website.
- Google tag: set `GOOGLE_TAG_ID` at build time. It only loads after a visitor allows analytics or ads, and it starts in Consent Mode with every signal denied. In Google Tag Manager/Google Ads you can verify that consent signals arrive (Tag Assistant).
- Shared report links are served by the API at `https://api.yourdomain.com/r/<token>`.

## 6. Checklist before going live
- [ ] `App__ResetUrl` and `App__VerifyUrl` point at your website (`/reset-password`, `/verify-email` pages exist)
- [ ] `Jwt__Key` set, seeded admin password changed to a long passphrase, no dev credentials
- [ ] AutoSSL green on all three hostnames, HTTP redirects to HTTPS
- [ ] `Cors__Origins__*` lists only your real domains
- [ ] Backups running **and** copied off-server, restore tested
- [ ] Stripe in live mode with the webhook verified by a real test payment
- [ ] SPF/DKIM pass; a password-reset email reaches an inbox
- [ ] Privacy page edited with your company details; consent banner works; AdSense site approved
- [ ] Uptime monitor on `https://api.yourdomain.com/health`
