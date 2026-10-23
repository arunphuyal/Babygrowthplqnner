# Babybunu

Baby and toddler tracking for parents, starting with potty training (think "Flo", but for toddlers). This repo holds the backend API, the admin portal and the public landing page. The mobile app comes later and will consume the same API.

| Folder | What | Stack |
|---|---|---|
| `backend/Babybunu.Api` | REST API | ASP.NET Core 8, EF Core (SQLite), JWT auth, Swagger |
| `admin/` | Admin portal (users, content, plans, waitlist, stats) | React + TypeScript + Vite |
| `web/` | Public landing page + waitlist | React + TypeScript + Vite |
| `docs/PLAN.md` | Roadmap, monetisation, competitor research | |

## Business model (for now)
Babybunu is **free for everyone**. It is funded by (1) one-off **Stripe donations** and (2) **Google AdSense on the public website only** (the guides pages), never inside the tracking app. `Features:AllFree` (default `true`) switches the paywall off; set it to `false` to enforce Free/Premium/Family again (the plan code, RevenueCat webhook and admin plan tools are still in place). Sign-in is by email and password, with password reset by email.

### Stripe donations
1. Create a Stripe account. Set `Donations__StripeSecretKey` and `Donations__StripeWebhookSecret` as environment variables. Never commit keys.
2. In Stripe, add a webhook endpoint `https://<api>/api/donations/webhook/stripe` for the event `checkout.session.completed` and copy its signing secret.
3. Set `Donations__SuccessUrl` and `Donations__CancelUrl` to your website (`/?donation=thanks`), and optionally `Donations__Currency` and `Donations__PresetsCents`.

Donors pay on Stripe's hosted page. The API stores only the amount and session id (idempotent). Test with Stripe test keys and `stripe listen --forward-to localhost:5080/api/donations/webhook/stripe`. Check the local rules for accepting donations: a personal account and a registered non-profit have different tax and Stripe requirements.

### Google AdSense (website)
1. Publish the site on your own domain and apply at adsense.google.com. AdSense wants real content and a privacy policy. The guides pages and `/privacy` are included; add more guides in the admin portal ("Content").
2. After approval set `VITE_ADSENSE_CLIENT` (ca-pub-...) and `VITE_ADSENSE_SLOT` at build time (see `web/.env.example`) and put the line from AdSense into `web/public/ads.txt`.
3. Ads only load after a visitor clicks "Allow ads", and are requested as non-personalised. **Before serving visitors in the EEA/UK you must use a Google-certified consent platform** (Google's free "Privacy & messaging" tool works); the built-in banner is not one.
4. Keep `VITE_CONTACT_EMAIL` and the wording in `web/src/pages/Privacy.tsx` up to date and have the privacy policy reviewed for your jurisdiction.

The site is a single-page app: configure your host to serve `<path>/index.html` if it exists, otherwise the root `index.html` (nginx: `try_files $uri $uri/index.html /index.html;`; Netlify and Cloudflare Pages do this by default).

### Analytics and marketing pixels (website only)
`VITE_GOOGLE_TAG_ID` (a Google tag: `G-...`, `GT-...` or `AW-...`) and `VITE_META_PIXEL_ID` (Meta Pixel) are optional. Nothing loads until the visitor opts in, in two separate categories: **analytics** (Google tag measurement) and **ads** (AdSense, ad signals of the Google tag, Meta Pixel). The Google tag is not fetched until the visitor allows at least one category, then starts in Consent Mode v2 with every signal denied and grants only what was chosen; the Meta Pixel starts with consent revoked. The banner asks parents, optionally, to allow ads or make a contribution to keep the app free; declining changes nothing. Events: `page_view`, `waitlist_join`, `donate_start`, `donate_thanks`, `guide_view`, with no personal data in any parameter. Waitlist sign-ups also store `utm_source/medium/campaign` so you can see which campaign worked. These tags are deliberately **not** used in the tracking app, on shared report pages, or anywhere a child's data appears. Keep it that way when the mobile app is built, because health-related tracker leaks have led to regulatory action against family and health apps. Check your region's consent rules, as the banner here is not a certified consent platform.

### SEO
- Every page sets title, description, canonical URL, Open Graph/Twitter tags and JSON-LD (Organization, WebSite, FAQPage, Article, BreadcrumbList) at runtime.
- `npm run build` in `web/` also runs `scripts/prerender.mjs`: it reads the published guides from `PRERENDER_API_URL`, writes real HTML for each guide (so crawlers do not depend on JavaScript), and generates `sitemap.xml` and `robots.txt`. Set `VITE_SITE_URL` to your domain first. Premium/locked guides are never written to public HTML. Rebuild (or run `npm run build` in CI on a schedule or on publish) after adding guides.
- The admin article editor has focus keyword, SEO title/description fields, a search-result preview and an on-page checklist (title and description length, 300+ words, subheadings, keyword placement).
- Submit `sitemap.xml` in Google Search Console and Bing Webmaster Tools after launch.

### Reports
- `GET /api/children/{id}/report?from=2026-10-01&to=2026-10-07&format=json|html|csv&includeLogs=true`: any caregiver. Dates are the child's local days (max 90 days). HTML is print-friendly ("Save as PDF" from the browser or the app); CSV lists every entry (spreadsheet-formula-safe).
- Share links, owner only: `POST /api/children/{id}/report-links` (`from`, `to`, `includeLogs`, `expiresInDays` 1-30) returns a token and `/r/{token}`; `GET` lists active links; `DELETE .../report-links/{linkId}` revokes. The public page is unguessable, expiring, revocable, `noindex`, `no-store`, `no-referrer`, and has no scripts, ads or trackers.
- Native PDF generation is not included; add a PDF library (check its licence terms) if you need server-side PDFs.

## Hosting
Self-hosted: static site and admin on cPanel, API on an ASP.NET-capable host or a small VPS. See **`docs/HOSTING.md`** (what runs where, step-by-step cPanel upload, API on a VPS, email, Stripe, go-live checklist) and `deploy/package.sh` (one command builds the upload zips), `deploy/bootstrap-vps.sh` + `deploy/deploy-vps.sh` (VPS setup and deploys with automatic rollback) and `.github/workflows/ci.yml`.

## Branding and language
The product name (**Babybunu**) is a setting, not hard-coded: `VITE_SITE_NAME` (website), `VITE_APP_NAME` (admin), `App__Name` (API emails and the Stripe product name). English (`en`) is the default; accounts and guides carry a language code (`/api/articles?lang=fr`), so translated guides can live beside English ones with the same slug.

## Run locally

## Run locally

```bash
# 1. API  -> http://localhost:5080  (Swagger at /swagger)
cd backend/Babybunu.Api
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5080 dotnet run --no-launch-profile

# 2. Landing page -> http://localhost:5173
cd web && npm install && npm run dev

# 3. Admin portal -> http://localhost:5174
cd admin && npm install && npm run dev
```

Dev admin login: `admin@babybunu.local` / `Admin123!dev` (from `appsettings.Development.json`).

### Production configuration
- Required: `Jwt__Key` (>= 32 chars). Admin seed: `Seed__AdminEmail`, `Seed__AdminPassword` (if empty, a random password is logged once on first start).
- `Cors__Origins__0=https://yourdomain`; `ConnectionStrings__Default`; `VITE_API_URL` when building `web/` and `admin/` against a different origin.
- Email (password reset): `Email__SmtpHost`, `Email__SmtpPort`, `Email__User`, `Email__Password`, `Email__From`, and `App__ResetUrl` (the page in your app that accepts `?token=`). Without `SmtpHost`, emails are only logged.
- Billing: create a RevenueCat webhook pointing at `/api/billing/webhook/revenuecat` with Authorization header `Bearer <secret>`, set `Billing__RevenueCatWebhookSecret` to `<secret>` (empty = webhook disabled), map store products to plans in `Billing:ProductPlans`, and set the RevenueCat App User ID to the Babybunu user id. This is implemented from RevenueCat's documented payload and tested with sample events only; try it with RevenueCat's test events before launch.
- Behind a reverse proxy set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so rate limiting sees real client IPs.
- SQLite is the default. Schema changes use EF migrations (applied automatically on start): `dotnet ef migrations add <Name> -o Data/Migrations` from `backend/Babybunu.Api`. To use PostgreSQL/SQL Server, swap the provider in `Program.cs` and regenerate the migrations.

## API overview (for the future mobile app)

Auth: `POST /api/auth/register`, `/login` return a short-lived (30 min) JWT plus a single-use **refresh token**; `POST /api/auth/refresh` rotates it (replaying an old one signs the user out everywhere), `/logout` revokes it. `POST /api/auth/forgot-password` (always 200) and `/reset-password` handle resets; `POST /api/auth/verify-email` confirms an address (a link is emailed at sign-up; it does not block use) and `/resend-verification` sends a new one; `GET /api/auth/me/export` downloads all of a user's data as JSON and `DELETE /api/auth/me` (with the password) permanently deletes the account and everything it owns (both are store and GDPR requirements); a reset signs out all sessions. Credential endpoints are rate limited per IP (`RateLimit:AuthPerMinute`, default 10).

| Endpoint | Purpose |
|---|---|
| `GET/POST /api/children`, `GET/PUT/DELETE /api/children/{id}` | Child profiles (Free plan: 1 child, else `402`) |
| `GET/POST /api/children/{id}/logs`, `DELETE .../logs/{logId}` | Log `Pee, Poop, Accident, Sit, Fluid, Food, Reward, Note`. Common: `occurredAt`, `inPotty`, `notes`, and an optional client-generated `id` so an offline retry returns the existing entry (`200`) instead of creating a duplicate. Intake/output detail: `amount` (ml; Fluid/Pee), `size` (`Tiny/Small/Medium/Large`; Pee/Poop/Fluid/Food), `stoolType` (Bristol 1-7; Poop), `colour` (Pee/Poop), `category` and `tags` (Fluid/Food). Inconsistent combinations return `400`. |
| `GET /api/children/{id}/insights?days=7` | (Free: max 7 days; Premium/Family: up to 90 days and food/fluid-to-stool correlations.) Days and hours use the child's `timeZoneId` (IANA). Success rate, dry streak, hourly histogram, daily series, **daily intake/output balance**, **drink-to-wee lag**, avg wee/stool intervals, non-diagnostic **notes** (no stool for 3+ days, hard stools, unusual colour, fewer wees than usual) |
| `GET /api/children/{id}/prediction` | Next likely potty visit: best model plus alternatives, each with confidence and reason |
| `POST /api/children/{id}/invites`, `POST /api/invites/accept`, `GET /api/children/{id}/caregivers`, `DELETE .../caregivers/{userId}` | Share a child with caregivers (owner needs the **Family** plan; invites are single-use, 7 days, view-only or can-edit). Logs record who logged them (`loggedBy`). |
| `POST /api/billing/webhook/revenuecat` | RevenueCat subscription webhook that sets the user's plan and expiry |
| `GET /api/articles`, `/api/articles/{slug}` | Guides; premium bodies are locked for Free users |
| `GET /api/plans`, `POST /api/waitlist` | Public pricing and waitlist |
| `/api/admin/*` (Admin role) | stats, users, articles CRUD, plans, waitlist |

## Prediction models
Predictors implement `IPottyPredictor` (`Services/Prediction.cs`) and are registered in `Program.cs`; `PredictionService` returns the most confident one. Included: `median-gap` (baseline, free) and `fluid-aware` (Premium: uses the child's own drink-to-wee delay; needs 5+ samples and a recent drink with no wee since). Set `RequiresPremium => true` on a predictor to make it a paid feature; paid features follow the child's **owner**, so a Family plan benefits every caregiver. To add a model, implement the interface, return `null` when data is insufficient, and register it as a singleton.

## Tests
`cd backend && dotnet test` (unit tests for predictors/insights, plus API tests against a temporary SQLite file).

## Not built yet
- A payment flow in the mobile app (the backend side is the webhook above), Stripe for web payments, and the mobile app itself.
- Revocation of already-issued access tokens (they live 30 minutes) and email verification at sign-up.
- A viewer for individual children's logs in the admin portal (deliberately omitted for privacy; the dashboard shows aggregates).
- Production hosting config (Dockerfile, CI).

See `docs/PLAN.md`.
