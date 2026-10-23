# Babybunu – plan, features and market research

## Similar services (found via web search, Oct 2026)
Mostly iOS-first indie apps; I could not verify per-country availability from the listings, so check each store page in your target markets.

| App | What it does | Monetisation |
|---|---|---|
| [Potty Whiz](https://apps.apple.com/us/app/-/id1493987452) | Logs sits, successes, accidents, fluids; reminder timer; Apple Watch; works offline | Free + IAP |
| [totpath](https://apps.apple.com/app/id6760669007) | Pee/poop/accidents, dry streaks, rewards, caregiver sharing | AI coaching & analytics behind Pro |
| [PottyPurr](https://apps.apple.com/us/app/-/id6744728332) | Predictive reminders and charts | Subscription |
| [POTTR](https://mwm.ai/apps/potty-training-pottr/6757593828) | Co-parent sync, lock-screen timer, weekly expert guides | Subscription |
| [Sprout](https://mwm.ai/apps/potty-training-app-sprout/6777609371) | Predictive reminders, iCloud sync, on-device privacy | Subscription |
| [Potty Time from Lalo](https://mwm.ai/apps/potty-time-from-lalo/6765751276) | One-tap logging, AI coach "Paddy" | Free/IAP |
| [Potty Toilet Training](https://apps.apple.com/app/id1099995211), [Potty Training Time](https://apps.apple.com/app/id473203652) | Sticker/reward charts for kids, multi-child profiles | Free / one-off |
| [Pooply](https://apps.apple.com/app/id6754704319) | Bowel tracking with Bristol stool chart | Free/IAP |
| Oh Crap! Potty Training (book/method, Jamie Glowacki) | Not an app, but the most popular method – a content partnership opportunity | – |

**Takeaways:** the space is crowded with thin, iOS-only trackers; common paid hooks are predictions, AI coaching and caregiver sharing. Gaps to exploit: Android + web, multilingual/regional content (non-English markets), daycare/nanny sharing and reports, and method-aware plans (3-day, child-led, Oh-Crap-style) instead of just a log.

## Where we are (Oct 2026)
**Built and tested (50 automated tests):** ASP.NET Core API (accounts with refresh tokens and password reset, children, logs of wees/poos/accidents/drinks/food with size, stool type, colour and tags, insights, intake-to-output lag, health notes, two prediction models behind an interface, correlations, caregiver sharing, time zones, rate limiting, EF migrations), admin portal (stats, users, content, plans, donations, waitlist), public website (landing, guides, privacy, donate, consent-gated AdSense slots), Stripe donations, a dormant RevenueCat billing path.
**Business model now:** everything free (`Features:AllFree`), funded by donations and website ads.
**Not started:** deployment, mobile app, email verification, account export/delete, push reminders, analytics/crash reporting, legal review, store listings.

## Roadmap
Estimates assume one developer working with an AI pair and are rough; each phase ends with something you can demo or ship. Do the phases in order; items inside a phase can be reordered.

### Phase 0 – Company, brand and accounts (start now, runs in parallel)
Several launch steps depend on a legal entity, so begin early (registration time varies by country).
- **Brand (decided): Babybunu.** Still to do: check the domain (and common variants), trademark databases in your target markets, the App Store and Google Play name, and social handles before spending on branding; I cannot verify availability from here. The name is a setting (`VITE_SITE_NAME`, `VITE_APP_NAME`, `App:Name`) and the code, projects and services have been renamed to Babybunu. Potty training is the first module; plan the wider product (growth, sleep, feeding, milestones) as separate "programs" attached to a child when the second one starts.
- **Company:** register it, open a business bank account, get tax IDs (and VAT/GST if applicable), and get a D-U-N-S number (Apple and Google need one to publish under an organisation account).
- **Accounts in the company's name:** domain, hosting, Stripe, Google AdSense and Google Search Console/Tag, Apple Developer ($99/yr) and Google Play ($25 once) developer accounts, an email provider, an error-tracking account.
- **Legal:** privacy policy and terms with the company's name and address (replace the draft text in `web/src/pages/Privacy.tsx`), a cookie/consent tool, a data-processing record, and an accountant's view on how contributions and ad income are taxed. Because the company is not a charity, the app says "support" and "contribution", not "donation", and Stripe's button says "Pay".
- **Exit criteria:** entity registered, brand and domain locked, store and Stripe accounts opened.

### Phase 1 – Launch-ready web and API (about 2 weeks)
Goal: a public site you can safely point real parents at.
- **Hosting:** decided to self-host. The website and admin portal go on cPanel as static files (rules and packaging are done: `docs/HOSTING.md`, `deploy/package.sh`, tested on Apache). Shared cPanel usually cannot run the ASP.NET Core API, so confirm with the host and otherwise run the API on a small Linux VPS (systemd + Caddy templates in `deploy/`) or on Windows/IIS hosting. A Dockerfile is no longer a priority.
- **Database:** SQLite is acceptable on a single server at the start, with daily backups copied off-server and a tested restore (`deploy/backup-sqlite.sh`). Move to PostgreSQL when traffic or reliability needs grow.
- **CI/CD:** GitHub Actions to build, run `dotnet test`, build both frontends, and deploy from the main branch. Secrets only from the platform's secret store.
- **Config hardening:** no dev defaults in production (JWT key, admin seed), CORS to your domains, forwarded headers for the rate limiter, security headers (HSTS, CSP for the website that allows AdSense and Stripe), request size limits.
- **Accounts:** email verification at sign-up, a web "reset password" page, account **export** and **delete** endpoints (GDPR/CCPA; currently by email only), access-token revocation list or a shorter lifetime for admins, admin 2FA.
- **Email:** transactional provider (e.g. Postmark/SendGrid/SES) with SPF/DKIM set up; replace the SMTP stub if needed.
- **Observability:** error tracking (Sentry or Application Insights), structured logs, uptime check on `/health`, basic dashboards.
- **Money and legal:** go live with Stripe (test mode first, then live keys and the webhook), apply for AdSense once there are 10+ guides, have the privacy policy and a short terms page reviewed, add a certified consent tool for EEA/UK, trademark and domain check on the name.
- **SEO and tracking:** deploy the prerendered site with the directory-index hosting rule, submit the sitemap, set `VITE_SITE_URL`; decide whether to enable GA4 and the Meta Pixel (both consent-gated; keep them off the app and shared reports); add a certified consent platform for the EEA/UK.
- **Content:** write at least 10 solid guides (readiness, methods, night training, regression, constipation basics, daycare, travel, siblings, special needs resources, FAQs), reviewed by someone qualified (health visitor or paediatric nurse) if you can.
- **Exit criteria:** all CI green, a staging environment, restore drill done, Stripe test donation recorded in admin, privacy page live, Lighthouse score above 90 on the landing page.

### Phase 2 – Mobile app MVP (about 6 weeks)
Goal: parents can actually track. This is the product; the site and API exist to support it.
- **Stack (decided):** Expo/React Native with TypeScript (reuses your React skills and the API client). English is the default language and the content model already carries a language per account and per guide; use `i18next` with `expo-localization` from day one so strings are never hard-coded. Generate the TypeScript client from the API's OpenAPI document so types never drift.
- **Core screens:** sign-up/login (email), add child, today timeline, big one-tap buttons for wee/poo/accident/sit/drink/food with optional details (size, Bristol type, colour, tags), edit/delete, insights (success rate, dry streak, hourly chart, intake vs output), next-potty prediction, guides (offline cache), settings (time zone, export, delete account), caregiver invite/accept.
- **Offline-first:** local queue with client-generated IDs so retries never create duplicate logs. **Backend change needed:** accept a client-supplied log id (idempotent create) and return `409`/the existing row on repeat; add a `since` cursor for efficient sync.
- **Push reminders:** device-token registration endpoint, a background service that schedules "try the potty now" notifications from predictions with quiet hours and a per-child on/off, via Expo push / FCM / APNs.
- **Quality:** crash reporting, accessibility (large tap targets, screen reader labels), dark mode, English first with strings ready for translation.
- **Store rules to settle before building in-app support options:** Apple and Google generally require their own billing for digital purchases and tips by non-charities, which makes a Stripe contribution button inside the app likely to be rejected. Plan to keep contributions on the website (link out only if the current store policy for your regions allows it) or to offer a store-billing "tip" product. Rules change often: read the current App Store Review Guidelines and Google Play payments policy before designing this.
- **Ads in the app:** the website asks parents to allow ads (optional). If you ever want ads in the app, treat it as a separate project: the consent SDK for each store, no ads on screens that show a child's data, and legal review. The default plan stays ad-free in the app.
- **Privacy in the app:** no advertising SDKs, analytics only first-party or privacy-friendly and opt-out, secure token storage, optional app lock (PIN/biometric).
- **Exit criteria:** a parent can sign up, log a full day offline and online, see insights and a prediction, and share with a partner. Internal testers use it for a week.

### Phase 3 – Closed beta (about 3 weeks)
- Release through TestFlight and Google Play internal/closed testing to the waitlist (invite in waves of 50-100).
- In-app feedback button and a short survey after day 7; weekly review of crashes and feedback.
- Measure: sign-up to first log within 24 hours (activation), logs per active day, day-7 and day-30 retention, how many users reach 3 consecutive dry days, prediction accuracy (see Phase 5).
- Store preparation: listing copy and screenshots, privacy labels (Apple) and Data safety form (Google), an age rating and audience declaration that the app is for **parents**, health-claim wording reviewed ("tracking tool, not medical advice"), support page and contact.
- Exit criteria: crash-free sessions above 99%, day-7 retention above your target (set it from beta data), no open data-loss bugs.

### Phase 4 – Public launch and growth (ongoing, first 8 weeks)
- Launch on both stores; email the waitlist; post in parenting communities honestly and with permission; a short product video.
- SEO: publish 2-4 guides a month targeting real questions (for example "potty training at 2", "potty training regression", "poop withholding"), internal links to the app, structured data. Consider prerendering the guide pages so search engines get full HTML.
- App store optimisation: keywords, localised listings, ratings prompt after a success milestone (not after an accident).
- Referral: "share with a friend" and sibling/caregiver invites; a certificate-sharing card when a child graduates.
- Partnerships: health visitors, nurseries, parenting bloggers; affiliate disclosure rules apply.
- Support: a help centre built from the FAQ, response-time target, a public changelog.

### Phase 5 – Product depth (months 3-6)
Order by what beta data shows parents want; likely candidates:
- **Phase-aware journey:** readiness, intensive days, consolidation, night training, outings, regression, with a daily checklist per phase and gentle content.
- **Rewards:** sticker charts, milestones, certificates, child-facing screens.
- **Reports (first version built):** JSON/HTML/CSV reports and expiring share links exist. Next: in-app report screen and share sheet, server-side PDF if needed, weekly email summary (opt-in), and a clinician-friendly layout reviewed with a health professional.
- **Smarter predictions:** store every prediction and its error (`PredictionRecord`), compare models on mean absolute error and the share within 15 minutes, add a time-of-day model and an exponentially weighted gap model, then consider an offline-trained ML model served through the same interface. Keep a plain-language "why" on every prediction.
- **Reading the data better:** nap and meal context tags, constipation trend charts, week-over-week summaries.
- **Convenience:** home-screen widgets, lock-screen quick log, Apple Watch / Wear OS, voice logging.
- **Reach:** localisation (start with the languages of your top traffic countries), regional content, right-to-left support if relevant.

### Phase 6 – Sustainability and monetisation review (month 6 onward)
- Track monthly: infrastructure cost, donation income, ad income, active users. Compare income to costs.
- **If donations plus ads cover costs:** stay free and put effort into growth and content.
- **If not:** switch `Features:AllFree` to `false` for new features only (keep core logging free), or add a low-priced optional Premium (predictions, long history, sharing, reports) through RevenueCat; keep donations. Other options: daycare/nursery licences, one-off method packs, labelled affiliate shelves, sponsorship that is privacy-safe.
- Do not add ad SDKs to the app. If you ever do, get legal advice first.

### Continuous work (every phase)
- **Security:** dependency updates monthly, a penetration-style review before launch, secrets rotation, audit log for admin actions, role-based admin access (viewer/editor/owner).
- **Privacy:** data inventory, retention policy (delete inactive accounts after a stated period), data-processing agreements with Stripe, email provider and hosting; a breach response checklist.
- **Quality:** keep tests growing with each feature, load test the log and insights endpoints before launch, add end-to-end tests for the main mobile flows.
- **Performance:** index review as logs grow, cache insights for a few minutes, paginate logs.

## Decisions needed from you
| Decision | Options | My recommendation |
|---|---|---|
| Hosting | **Decided:** self-hosted, static sites on cPanel. **Still open:** where the API runs (Linux VPS, Windows/IIS, or managed) | A small Linux VPS for the API; see `docs/HOSTING.md` |
| Mobile framework | **Decided:** React Native (Expo) | - |
| Legal entity | **Decided:** register a company | Phase 0; open the accounts in its name |
| Languages and markets | **Decided:** English by default | Add other languages from beta and SEO data |
| Product name and domain | **Decided:** a generic name, not potty-specific | Shortlist, then check domain, trademarks, store names |
| Analytics | None, self-hosted, or privacy-friendly SaaS | Privacy-friendly, first-party, opt-out |
| Clinical review of content | Self-written, or reviewed by a health professional | Reviewed if budget allows; otherwise keep content general and cautious |

## Rough costs (assumptions to verify)
Domain about $15/year; Apple Developer $99/year; Google Play $25 once; managed hosting plus a small Postgres roughly $20-60/month at the start; email and error tracking have free tiers at low volume; Stripe charges a per-payment fee (about 2.9% + $0.30 for typical US cards, varies by country). Ad income depends entirely on traffic and region and is not guaranteed; AdSense approval is also not guaranteed. Do not budget on ads or donations until you have real traffic data.

## Risks and how we handle them
| Risk | Mitigation |
|---|---|
| Ads and donations do not cover costs | Cost-light hosting, the free-mode switch can be flipped, Phase 6 review |
| AdSense rejects the site | More original content, a clear privacy policy, about/contact pages; apply again |
| Sensitive child data leaks or is misused | Minimal data, encryption in transit, strict access control, no ad SDKs in the app, no admin viewing of individual logs |
| Store rejection for health claims or privacy | Plain "tracking tool, not medical advice" wording, accurate privacy labels, no diagnostic claims |
| Predictions are wrong and frustrate parents | Show confidence and reasons, make them optional, measure error and improve |
| Low retention because training ends | Phase journey, sibling reuse, post-graduation night/travel modes, referrals |
| Solo-maintainer risk | Good docs, CI, tests, simple infrastructure |

## Suggested order for the next four weeks
1. Week 1: Dockerfile, CI, Postgres migration, staging deploy, error tracking.
2. Week 2: email verification, account export/delete, security headers, live Stripe test, 10 guides, legal review started.
3. Week 3: API changes for the app (idempotent log ids, sync cursor, device tokens), OpenAPI client generation, Expo project skeleton with login.
4. Week 4: core logging screens and the timeline working end to end against staging.

## Revenue ideas (for later, see Phase 6)
The app is currently free, funded by Stripe donations and ads on the website only. The ideas below are options to revisit once there is real usage data.
- **Subscriptions (core):** Free (1 child, 7-day history) / Premium (predictions, full history, rewards, guides) / Family (unlimited children, caregiver sharing, daycare export). Prices seeded: $4.99/mo, $39.99/yr, $7.99/mo.
- **One-off purchases:** method packs (3-day plan, night-training, regression rescue), printable reward charts and certificates, themed sticker packs.
- **Affiliate / marketplace:** potties, training pants, books, reward toys – contextual in-app shelf, clearly labelled.
- **B2B:** daycare/nursery licence with a multi-child dashboard and parent reports; paediatrician/health-visitor referral programme.
- **Sponsorship:** vetted brand content (diaper/pull-up brands) – only if privacy-safe and clearly marked; avoid ad SDKs in a children's context.
- **Premium services:** 1:1 potty coach chat or live Q&A add-on.

## Additional feature ideas
**Tracking:** fluid & meal timing, stool type (Bristol scale) and colour, night dryness, wetness before/after naps, regression flags, photos of reward charts, mood/notes, sickness/travel markers.
**Smart:** next-visit prediction (basic version included), AI coach for troubleshooting, personalised daily plan, auto-detected readiness score, constipation/health alerts (with "see your doctor" disclaimers).
**Engagement:** reward charts & stickers, milestones and certificates, kid-facing mini-games, streaks for parents, a "next potty" widget/lock-screen timer, Apple Watch/Wear OS quick log, voice logging ("log a pee").
**Family:** multi-caregiver live sync, daycare/grandparent invite links, shareable PDF report for daycare or paediatrician.
**Content:** readiness quiz, method library, video lessons, community Q&A, multilingual guides.
**Platform:** data export & delete (GDPR/COPPA-friendly – data is about a child but owned by the parent), offline-first, dark mode, home-screen widgets.

## Compliance notes
Child data: collect the minimum (first name or nickname, birth month/year are enough), no third-party ad trackers, clear privacy policy, parental-only accounts, deletion on request. Present health content as informational, not medical advice.
