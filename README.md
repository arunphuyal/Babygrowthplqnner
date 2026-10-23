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
- `Jwt__Key` (>= 32 chars, required), `Seed__AdminEmail`, `Seed__AdminPassword` (if empty, a random password is logged once on first start), `Cors__Origins__0=https://yourdomain`, `ConnectionStrings__Default`.
- Set `VITE_API_URL` when building `web/` and `admin/` if the API is on another origin.
- SQLite is used for simplicity; to move to PostgreSQL/SQL Server swap the EF provider in `Program.cs` and add migrations (the schema is created with `EnsureCreated` for now).

## API overview (for the future mobile app)

Auth: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me` -> JWT bearer.

| Endpoint | Purpose |
|---|---|
| `GET/POST /api/children`, `GET/PUT/DELETE /api/children/{id}` | Child profiles (Free plan: 1 child, else `402`) |
| `GET/POST /api/children/{id}/logs`, `DELETE .../logs/{logId}` | Log `Pee, Poop, Accident, Sit, Fluid, Reward, Note` (`inPotty`, `amount`, `notes`, `occurredAt`) |
| `GET /api/children/{id}/insights?days=7` | Success rate, dry streak, avg gap, next-visit prediction, hourly histogram, daily series |
| `GET /api/articles`, `/api/articles/{slug}` | Guides; premium bodies are locked for Free users |
| `GET /api/plans`, `POST /api/waitlist` | Public pricing and waitlist |
| `/api/admin/*` (Admin role) | stats, users, articles CRUD, plans, waitlist |

## Not built yet
Payments, email sending, refresh tokens/password reset, caregiver sharing, automated tests, EF migrations, rate limiting. See `docs/PLAN.md`.
