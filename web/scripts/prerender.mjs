// Runs after `vite build`. Single-page apps are weak for SEO, so this writes real HTML (title, description, canonical,
// Open Graph, JSON-LD and the article text) for every public page, plus sitemap.xml and robots.txt.
// The React app replaces the static body when it loads, so visitors see the normal app and crawlers see full content.
import { readFile, writeFile, mkdir } from 'node:fs/promises'
import { existsSync } from 'node:fs'
import path from 'node:path'

const DIST = path.resolve('dist')
const SITE = (process.env.VITE_SITE_URL || 'https://example.com').replace(/\/$/, '')
const API = (process.env.PRERENDER_API_URL || 'http://localhost:5080').replace(/\/$/, '')
const NAME = 'PottyTrain'
const DESCRIPTION = "Free potty training tracker for parents: log wees, poos, accidents, drinks and food, spot your toddler's patterns and get a calm, clear plan."

const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;')
const jsonLd = o => JSON.stringify(o).replace(/</g, '\\u003c') // keeps "</script>" out of the JSON

// Keep in sync with Body() in src/pages/Guides.tsx: blank-line paragraphs, "## " headings, "- " lists.
function bodyHtml(text) {
  const list = lines => lines[0]?.startsWith('- ') ? `<ul>${lines.map(l => `<li>${esc(l.replace(/^- /, ''))}</li>`).join('')}</ul>` : `<p>${esc(lines.join(' '))}</p>`
  return text.split(/\n{2,}/).map(block => {
    if (block.startsWith('## ')) { const [head, ...rest] = block.split('\n'); return `<h2>${esc(head.slice(3))}</h2>${rest.length ? list(rest) : ''}` }
    return block.startsWith('- ') ? list(block.split('\n')) : `<p>${esc(block)}</p>`
  }).join('')
}

function page(template, { title, description, urlPath, type = 'website', ld = [], body = '' }) {
  const url = SITE + urlPath
  const head = [
    `<title>${esc(title)}</title>`,
    `<meta name="description" content="${esc(description)}" />`,
    `<meta name="robots" content="index,follow,max-image-preview:large" />`,
    `<link rel="canonical" href="${esc(url)}" />`,
    `<meta property="og:title" content="${esc(title)}" />`,
    `<meta property="og:description" content="${esc(description)}" />`,
    `<meta property="og:url" content="${esc(url)}" />`,
    `<meta property="og:type" content="${type}" />`,
    `<meta property="og:site_name" content="${NAME}" />`,
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
const urls = [{ loc: '/', priority: '1.0' }, { loc: '/guides', priority: '0.8' }, { loc: '/privacy', priority: '0.3' }]

let articles = []
try {
  articles = await getJson('/api/articles')
} catch (e) {
  console.warn(`prerender: could not read guides from ${API} (${e.message}). Writing static pages and a sitemap without guides.`)
}

await write('/', page(template, {
  title: `${NAME} – Free potty training tracker for parents`, description: DESCRIPTION, urlPath: '/',
  ld: [{ '@context': 'https://schema.org', '@type': 'WebSite', name: NAME, url: SITE, description: DESCRIPTION }],
  body: `<main><h1>Log every step. Spot the pattern. Celebrate the win.</h1><p>${esc(DESCRIPTION)}</p><p><a href="/guides">Read our potty training guides</a></p></main>`,
}))

await write('/guides', page(template, {
  title: `Potty training guides – ${NAME}`, description: 'Practical, parent-friendly potty training guides: readiness, the first week, accidents, and drinks, food and poo.', urlPath: '/guides',
  body: `<main><h1>Potty training guides</h1><ul>${articles.map(a => `<li><a href="/guides/${esc(a.slug)}">${esc(a.title)}</a> – ${esc(a.summary)}</li>`).join('')}</ul></main>`,
}))

for (const a of articles) {
  let full
  try { full = await getJson(`/api/articles/${encodeURIComponent(a.slug)}`) } catch (e) { console.warn(`prerender: skipping ${a.slug}: ${e.message}`); continue }
  if (full.locked || !full.body) continue // gated content is never written into public HTML
  const urlPath = `/guides/${a.slug}`
  const description = full.metaDescription || full.summary
  await write(urlPath, page(template, {
    title: full.metaTitle || `${full.title} – ${NAME}`, description, urlPath, type: 'article',
    ld: [
      { '@context': 'https://schema.org', '@type': 'Article', headline: full.title, description, mainEntityOfPage: SITE + urlPath, datePublished: full.createdAt, dateModified: full.updatedAt ?? full.createdAt,
        author: { '@type': 'Organization', name: NAME }, publisher: { '@type': 'Organization', name: NAME } },
      { '@context': 'https://schema.org', '@type': 'BreadcrumbList', itemListElement: [
        { '@type': 'ListItem', position: 1, name: 'Home', item: SITE + '/' }, { '@type': 'ListItem', position: 2, name: 'Guides', item: SITE + '/guides' },
        { '@type': 'ListItem', position: 3, name: full.title, item: SITE + urlPath }] },
    ],
    body: `<article><h1>${esc(full.title)}</h1>${bodyHtml(full.body)}</article>`,
  }))
  urls.push({ loc: urlPath, lastmod: (full.updatedAt ?? full.createdAt ?? '').slice(0, 10), priority: '0.7' })
}

const sitemap = `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls.map(u =>
  `  <url><loc>${esc(SITE + u.loc)}</loc>${u.lastmod ? `<lastmod>${u.lastmod}</lastmod>` : ''}<priority>${u.priority}</priority></url>`).join('\n')}\n</urlset>\n`
await writeFile(path.join(DIST, 'sitemap.xml'), sitemap)
await writeFile(path.join(DIST, 'robots.txt'), `User-agent: *\nAllow: /\nDisallow: /r/\n\nSitemap: ${SITE}/sitemap.xml\n`)
console.log(`prerender: wrote ${urls.length} pages, sitemap.xml and robots.txt for ${SITE}`)
