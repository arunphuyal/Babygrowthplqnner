// Runs after `vite build`. Single-page apps are weak for SEO, so this writes real HTML (title, description, canonical,
// Open Graph, JSON-LD and the page text) for every public page, plus sitemap.xml, feed.xml and robots.txt.
// The React app replaces the static body when it loads, so visitors see the normal app and crawlers see full content.
import { readFile, writeFile, mkdir } from 'node:fs/promises'
import { existsSync } from 'node:fs'
import path from 'node:path'
import { parseBody, readMinutes, linkedGuideSlugs } from '../shared/markup.mjs'

const DIST = path.resolve('dist')
const SITE = (process.env.VITE_SITE_URL || 'https://example.com').replace(/\/$/, '')
const API = (process.env.PRERENDER_API_URL || 'http://localhost:5080').replace(/\/$/, '')
const NAME = process.env.VITE_SITE_NAME || 'Babybunu'
const DESCRIPTION = "Free potty training tracker for parents: log pees, poops, accidents, drinks and food, spot your toddler's patterns and get a calm, clear plan."
const content = JSON.parse(await readFile(path.resolve('src/content.json'), 'utf8'))

const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;')
const jsonLd = o => JSON.stringify(o).replace(/</g, '\\u003c') // keeps "</script>" out of the JSON

const inline = parts => parts.map(p => p.t === 'text' ? esc(p.v)
  : `<a href="${esc(p.href)}"${p.external ? ' rel="noopener noreferrer"' : ''}>${esc(p.v)}</a>`).join('')
const bodyHtml = text => parseBody(text).map(b =>
  b.type === 'h2' ? `<h2>${esc(b.text)}</h2>`
  : b.type === 'p' ? `<p>${inline(b.inline)}</p>`
  : `<ul>${b.items.map(i => `<li>${inline(i)}</li>`).join('')}</ul>`).join('')

function page(template, { title, description, urlPath, type = 'website', ld = [], body = '' }) {
  const url = SITE + urlPath
  const head = [
    `<title>${esc(title)}</title>`,
    `<meta name="description" content="${esc(description)}" />`,
    `<meta name="robots" content="index,follow,max-image-preview:large" />`,
    `<link rel="canonical" href="${esc(url)}" />`,
    `<link rel="alternate" type="application/rss+xml" title="${esc(NAME)} guides" href="${SITE}/feed.xml" />`,
    `<meta property="og:title" content="${esc(title)}" />`,
    `<meta property="og:description" content="${esc(description)}" />`,
    `<meta property="og:url" content="${esc(url)}" />`,
    `<meta property="og:type" content="${type}" />`,
    `<meta property="og:site_name" content="${esc(NAME)}" />`,
    `<meta name="twitter:card" content="summary" />`,
    ...(ld.length ? [`<script id="ld-json" type="application/ld+json">${jsonLd(ld.length === 1 ? ld[0] : ld)}</script>`] : []),
  ].join('\n    ')
  return template
    .replace(/<!--seo-->.*?<!--\/seo-->\s*/s, '') // makes re-running the script on an already prerendered file safe
    .replace(/<title>.*?<\/title>/s, '')
    .replace(/<meta name="description"[^>]*>/s, '')
    .replace('</head>', `    <!--seo-->\n    ${head}\n    <!--/seo-->\n  </head>`)
    .replace(/<div id="root">.*?<\/div>(\s*<\/body>)/s, (_, tail) => `<div id="root">${body}</div>${tail}`)
}

async function write(urlPath, html) {
  const file = urlPath === '/' ? path.join(DIST, 'index.html') : path.join(DIST, urlPath, 'index.html')
  await mkdir(path.dirname(file), { recursive: true })
  await writeFile(file, html)
}

async function getJson(p) {
  const r = await fetch(API + p, { signal: AbortSignal.timeout(15000) })
  if (!r.ok) throw new Error(`${p} -> ${r.status}`)
  return r.json()
}

if (!existsSync(path.join(DIST, 'index.html'))) { console.error('prerender: run `vite build` first'); process.exit(1) }
const template = await readFile(path.join(DIST, 'index.html'), 'utf8')
const urls = [{ loc: '/', priority: '1.0' }, { loc: '/guides', priority: '0.8' }, { loc: '/about', priority: '0.4' }, { loc: '/privacy', priority: '0.3' }]

let list = []
try { list = await getJson('/api/articles?lang=en') } catch (e) {
  console.warn(`prerender: could not read guides from ${API} (${e.message}). Writing static pages and a sitemap without guides.`)
}

// ---- Home ----
await write('/', page(template, {
  title: `${NAME} – Free potty training tracker for parents`, description: DESCRIPTION, urlPath: '/',
  ld: [
    { '@context': 'https://schema.org', '@type': 'Organization', name: NAME, url: SITE },
    { '@context': 'https://schema.org', '@type': 'WebSite', name: NAME, url: SITE, description: DESCRIPTION },
    { '@context': 'https://schema.org', '@type': 'FAQPage', mainEntity: content.faqs.map(([q, a]) => ({ '@type': 'Question', name: q, acceptedAnswer: { '@type': 'Answer', text: a } })) },
  ],
  body: `<main><h1>${esc(content.headline)}</h1><p>${esc(DESCRIPTION)}</p>`
    + `<h2>Everything you need to get out of diapers</h2><ul>${content.features.map(f => `<li><b>${esc(f.title)}.</b> ${esc(f.text)}</li>`).join('')}</ul>`
    + `<h2>How it works</h2><ol>${content.steps.map(([t, d]) => `<li><b>${esc(t)}</b> ${esc(d)}</li>`).join('')}</ol>`
    + `<h2>Questions</h2>${content.faqs.map(([q, a]) => `<h3>${esc(q)}</h3><p>${esc(a)}</p>`).join('')}`
    + `<p><a href="/guides">Read our potty training guides</a></p></main>`,
}))

// ---- Guides index ----
await write('/guides', page(template, {
  title: `Potty training guides – ${NAME}`, description: 'Practical, parent-friendly potty training guides: readiness, methods, accidents, regression, night-time dryness, constipation, daycare, travel and more.', urlPath: '/guides',
  body: `<main><h1>Potty training guides</h1><ul>${list.map(a => `<li><a href="/guides/${esc(a.slug)}">${esc(a.title)}</a> – ${esc(a.summary)}</li>`).join('')}</ul></main>`,
}))

// ---- Each guide ----
const feed = []
for (const a of list) {
  let full
  try { full = await getJson(`/api/articles/${encodeURIComponent(a.slug)}?lang=en`) } catch (e) { console.warn(`prerender: skipping ${a.slug}: ${e.message}`); continue }
  if (full.locked || !full.body) continue // gated content is never written into public HTML
  const urlPath = `/guides/${a.slug}`
  const description = full.metaDescription || full.summary

  const linked = linkedGuideSlugs(full.body).map(s => list.find(x => x.slug === s)).filter(Boolean)
  const related = [...linked, ...list.filter(x => x.slug !== a.slug && !linked.includes(x))].slice(0, 3)

  await write(urlPath, page(template, {
    title: full.metaTitle || `${full.title} – ${NAME}`, description, urlPath, type: 'article',
    ld: [
      { '@context': 'https://schema.org', '@type': 'Article', headline: full.title, description, mainEntityOfPage: SITE + urlPath, datePublished: full.createdAt, dateModified: full.updatedAt ?? full.createdAt,
        author: { '@type': 'Organization', name: NAME }, publisher: { '@type': 'Organization', name: NAME } },
      { '@context': 'https://schema.org', '@type': 'BreadcrumbList', itemListElement: [
        { '@type': 'ListItem', position: 1, name: 'Home', item: SITE + '/' }, { '@type': 'ListItem', position: 2, name: 'Guides', item: SITE + '/guides' },
        { '@type': 'ListItem', position: 3, name: full.title, item: SITE + urlPath }] },
    ],
    body: `<article><p><a href="/guides">← All guides</a></p><h1>${esc(full.title)}</h1><p>${readMinutes(full.body)} min read${full.updatedAt ? ` · Updated ${new Date(full.updatedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' })}` : ''}</p>${bodyHtml(full.body)}`
      + (related.length ? `<h2>Keep reading</h2><ul>${related.map(r => `<li><a href="/guides/${esc(r.slug)}">${esc(r.title)}</a></li>`).join('')}</ul>` : '')
      + `<p>This guide is general information and not medical advice. Talk to your doctor about any concerns.</p></article>`,
  }))
  urls.push({ loc: urlPath, lastmod: (full.updatedAt ?? full.createdAt ?? '').slice(0, 10), priority: '0.7' })
  feed.push({ title: full.title, link: SITE + urlPath, summary: full.summary, date: new Date(full.createdAt ?? Date.now()) })
}

const sitemap = `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls.map(u =>
  `  <url><loc>${esc(SITE + u.loc)}</loc>${u.lastmod ? `<lastmod>${u.lastmod}</lastmod>` : ''}<priority>${u.priority}</priority></url>`).join('\n')}\n</urlset>\n`
await writeFile(path.join(DIST, 'sitemap.xml'), sitemap)

feed.sort((x, y) => y.date - x.date)
await writeFile(path.join(DIST, 'feed.xml'), `<?xml version="1.0" encoding="UTF-8"?>\n<rss version="2.0"><channel>\n  <title>${esc(NAME)} guides</title>\n  <link>${SITE}/guides</link>\n  <description>${esc(DESCRIPTION)}</description>\n  <language>en</language>\n${feed.map(i =>
  `  <item><title>${esc(i.title)}</title><link>${esc(i.link)}</link><guid isPermaLink="true">${esc(i.link)}</guid><pubDate>${i.date.toUTCString()}</pubDate><description>${esc(i.summary)}</description></item>`).join('\n')}\n</channel></rss>\n`)
await writeFile(path.join(DIST, 'robots.txt'), `User-agent: *\nAllow: /\nDisallow: /r/\n\nSitemap: ${SITE}/sitemap.xml\n`)
console.log(`prerender: wrote ${urls.length} pages, sitemap.xml, feed.xml (${feed.length} items) and robots.txt for ${SITE}`)
