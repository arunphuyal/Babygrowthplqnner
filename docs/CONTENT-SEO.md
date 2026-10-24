# Content and SEO playbook

Goal: be the page a parent finds at 2 a.m. when potty training goes wrong, and earn trust so they come back to the app. Search traffic is also what pays for the free model (ads and optional contributions), so quality beats volume.

## What is built
- 16 starter guides (`backend/Babybunu.Api/Data/Guides.cs`) in neutral international English, each with SEO title, description, focus keyword and links to related guides. A test (`StarterGuideQualityTests`) fails the build if a guide drops below the checklist: title 30-60 characters, description 70-160, 300+ words, 2+ subheadings, focus keyword in title, description, URL and first paragraph, at least one internal link, all links valid, no orphan pages, no British spellings, no medicine doses.
- Pages are prerendered to static HTML with Article/Breadcrumb/FAQ structured data, `sitemap.xml`, `feed.xml` (RSS) and `robots.txt`. A nightly workflow (`.github/workflows/refresh-site.yml`) rebuilds them from the live API so new or edited guides are picked up automatically.
- Admin editor: focus keyword, SEO title/description, search-result preview, live body preview, and the same checklist.
- Pages for trust: About and editorial policy, Privacy, contact email in the footer.

## Style rules (international English)
- Plain words everyone understands: **pee, poop, diaper, doctor**. US spelling where regions differ (color, fiber, behavior, pediatric if unavoidable).
- Short sentences, second person ("your child"), calm tone, no blame. Never shame parents or children.
- Health topics: general information only, say when to talk to a doctor, **never** doses, medicines or diagnoses. Use "may", "often", "usually".
- Do not put the brand name inside guide bodies (the name can change); say "the app" or "a log" if you must refer to tracking.
- Currency in prose: avoid prices; where needed give USD and EUR.

## Publishing workflow
1. Admin portal > Content > New article. Write the body with `## Heading`, `- bullets`, and links like `[potty training regression](/guides/potty-training-regression)`.
2. Set a focus keyword (the phrase a parent would type) and make the checklist green. Aim for 350-800 words; longer only if it helps the reader.
3. Check "Preview body", then tick **Published** and save.
4. Run **Actions > Refresh website** (or wait for the nightly run). Check the page, then request indexing in Google Search Console for important new pages.
5. Unpublish instead of deleting a guide so URLs do not break; if a URL must change, add a redirect in the site's `.htaccess`.
6. Every guide should link to 2-4 related guides and be linked from at least one other (the test enforces this for the starter set).

## Keyword and topic backlog (ideas to validate in Search Console)
Search volumes are not included because I cannot measure them here; use Search Console and a keyword tool to prioritize.
- Readiness and timing: potty training at 18 months, at 3, boys vs girls, twins, "is it too early/late".
- Methods and routines: potty training schedule, 3-day method step by step, potty training without pull-ups, using a potty chart.
- Problems: potty training poop refusal, only pees in the potty, hides to poop, afraid of the toilet, accidents at nap time, regression after a new baby.
- Night and outings: night-time dryness checklist, car seat protection, potty training on a plane, public toilet fears.
- Health-adjacent (extra caution, review by a professional if possible): urinary tract infection signs in toddlers, constipation diet ideas, bedwetting after 6.
- Practical: best potty chair features, training pants vs underwear, potty training books (no affiliate links until disclosed).
- Printables: reward chart, daily log sheet, daycare handoff note (also drives sign-ups).

## Measuring
- **Search Console:** impressions, clicks, average position and CTR per guide; fix titles with high impressions but low CTR; add internal links to pages stuck on page 2.
- **Site:** guide page views, scroll depth, "Join the waitlist" and "Support" clicks (events `guide_view`, `waitlist_join`, `donate_start`; only recorded for visitors who allowed analytics).
- **Quality signals:** reader emails and corrections, time since a guide was last reviewed. Review every guide at least twice a year and bump its updated date only when the content really changed.

## Trust (important for health topics)
- Add a named author or editorial team and, if you can, a **review by a qualified professional** (health visitor, pediatric nurse, pediatrician) with their credentials and the review date. Do not claim a review that has not happened; the About page currently makes no such claim.
- Cite sources for factual statements where possible (national health services, pediatric associations) and link to them.
- Keep a corrections email monitored.

## Ads readiness (Google AdSense)
No approval is guaranteed. Typically helpful: 15-20+ original, useful pages (done: 16 and growing), About, Contact, Privacy, clear navigation, a custom domain, no placeholder text, and traffic. Apply after the site is live on your own domain. Do not click your own ads; do not put ads on pages with a child's data (the app, shared reports, account pages), which this site never does.

## Languages later
Guides carry a language code. To add a language, create translated articles with the **same slug** and `lang` set (for example `fr`), then add `hreflang` links and a language switcher. Translate with a human reviewer; machine translation of health content needs checking. English stays the default.
