# PottyTrain

Potty-training tracker for parents (think "Flo", but for toddlers). This repo holds the backend API, the admin portal and the public landing page. The mobile app comes later and will consume the same API.

| Folder | What | Stack |
|---|---|---|
| `backend/PottyTrain.Api` | REST API | ASP.NET Core 8, EF Core (SQLite), JWT auth, Swagger |
| `admin/` | Admin portal (users, content, plans, waitlist, stats) | React + TypeScript + Vite |
| `web/` | Public landing page + waitlist | React + TypeScript + Vite |
| `docs/PLAN.md` | Roadmap, monetisation, competitor research | |

## Run locally

```bash
# 1. API  -> http://localhost:5080  (Swagger at /swagger)
cd backend/PottyTrain.Api
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5080 dotnet run --no-launch-profile

# 2. Landing page -> http://localhost:5173
cd web && npm install && npm run dev

# 3. Admin portal -> http://localhost:5174
cd admin && npm install && npm run dev
```

Dev admin login: `admin@pottytrain.local` / `Admin123!dev` (from `appsettings.Development.json`).

### Production configuration
- Required: `Jwt__Key` (>= 32 chars). Admin seed: `Seed__AdminEmail`, `Seed__AdminPassword` (if empty, a random password is logged once on first start).
- `Cors__Origins__0=https://yourdomain`; `ConnectionStrings__Default`; `VITE_API_URL` when building `web/` and `admin/` against a different origin.
- Email (password reset): `Email__SmtpHost`, `Email__SmtpPort`, `Email__User`, `Email__Password`, `Email__From`, and `App__ResetUrl` (the page in your app that accepts `?token=`). Without `SmtpHost`, emails are only logged.
- Billing: create a RevenueCat webhook pointing at `/api/billing/webhook/revenuecat` with Authorization header `Bearer <secret>`, set `Billing__RevenueCatWebhookSecret` to `<secret>` (empty = webhook disabled), map store products to plans in `Billing:ProductPlans`, and set the RevenueCat App User ID to the PottyTrain user id. This is implemented from RevenueCat's documented payload and tested with sample events only; try it with RevenueCat's test events before launch.
- Behind a reverse proxy set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so rate limiting sees real client IPs.
- SQLite is the default. Schema changes use EF migrations (applied automatically on start): `dotnet ef migrations add <Name> -o Data/Migrations` from `backend/PottyTrain.Api`. To use PostgreSQL/SQL Server, swap the provider in `Program.cs` and regenerate the migrations.

## API overview (for the future mobile app)

Auth: `POST /api/auth/register`, `/login` return a short-lived (30 min) JWT plus a single-use **refresh token**; `POST /api/auth/refresh` rotates it (replaying an old one signs the user out everywhere), `/logout` revokes it. `POST /api/auth/forgot-password` (always 200) and `/reset-password` handle resets; a reset signs out all sessions. Credential endpoints are rate limited per IP (`RateLimit:AuthPerMinute`, default 10).

| Endpoint | Purpose |
|---|---|
| `GET/POST /api/children`, `GET/PUT/DELETE /api/children/{id}` | Child profiles (Free plan: 1 child, else `402`) |
| `GET/POST /api/children/{id}/logs`, `DELETE .../logs/{logId}` | Log `Pee, Poop, Accident, Sit, Fluid, Food, Reward, Note`. Common: `occurredAt`, `inPotty`, `notes`. Intake/output detail: `amount` (ml; Fluid/Pee), `size` (`Tiny/Small/Medium/Large`; Pee/Poop/Fluid/Food), `stoolType` (Bristol 1-7; Poop), `colour` (Pee/Poop), `category` and `tags` (Fluid/Food). Inconsistent combinations return `400`. |
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
