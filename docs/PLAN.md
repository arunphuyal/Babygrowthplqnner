# PottyTrain – plan, features and market research

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

## Roadmap
1. **Foundation (done in this repo):** API (auth, children, logs, insights, articles, plans, waitlist, admin), admin portal, landing page + waitlist.
2. **Hardening (next 1–2 wks):** EF migrations + PostgreSQL, tests, password reset & email (SendGrid/Postmark), refresh tokens, rate limiting, Dockerfile + CI, deploy (Azure App Service / Fly.io + static hosting).
3. **Mobile MVP (4–6 wks):** React Native (Expo) or .NET MAUI app on the same API: child setup, one-tap log buttons, timeline, insights, push reminders, offline queue with sync. Launch to waitlist.
4. **Monetisation (with launch):** RevenueCat for App Store/Play subscriptions, server-side entitlement sync with `User.Plan`; Stripe for the web. Free-trial paywall at the moment predictions/insights are first valuable.
5. **Growth:** referral codes, SEO articles on the landing site, localisation, partnerships with paediatric/daycare brands.

## Revenue ideas
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
