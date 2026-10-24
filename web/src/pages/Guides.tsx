import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { API, SITE } from '../site'
import { AdSlot } from '../components/Ads'
import { useSeo, articleLd } from '../seo'
import { track } from '../trackers'
import { parseBody, readMinutes, linkedGuideSlugs, type Inline } from '../../shared/markup.mjs'

interface Summary { slug: string; title: string; summary: string }
interface Full extends Summary { body: string | null; locked: boolean; metaTitle?: string; metaDescription?: string; createdAt?: string; updatedAt?: string }

async function loadList(): Promise<Summary[]> {
  const r = await fetch(`${API}/api/articles?lang=en`)
  return r.ok ? r.json() : []
}

export function GuideList() {
  const [items, setItems] = useState<Summary[] | null>(null)
  useSeo({ title: `Potty training guides – ${SITE.name}`, description: 'Practical, parent-friendly potty training guides: readiness, methods, accidents, regression, night-time dryness, constipation, daycare, travel and more.', path: '/guides' })
  useEffect(() => { loadList().then(setItems).catch(() => setItems([])) }, [])
  return (
    <div className="page">
      <h1>Potty training guides</h1>
      <p className="lead">Practical, parent-friendly guidance. General information only, not medical advice.</p>
      {items === null ? <p>Loading…</p> : items.length === 0 ? <p>No guides yet.</p> : (
        <div className="grid">{items.map(g => <Link key={g.slug} to={`/guides/${g.slug}`} className="feature"><h3>{g.title}</h3><p>{g.summary}</p></Link>)}</div>
      )}
      <AdSlot />
    </div>
  )
}

function Inlines({ parts }: { parts: Inline[] }) {
  return <>{parts.map((p, i) => p.t === 'text' ? <span key={i}>{p.v}</span>
    : p.external ? <a key={i} href={p.href} rel="noopener noreferrer" target="_blank">{p.v}</a>
    : <Link key={i} to={p.href}>{p.v}</Link>)}</>
}

/** Renders the guide text format (see shared/markup.mjs). All text is rendered through React, so it is escaped. */
function Body({ text }: { text: string }) {
  return <>{parseBody(text).map((b, i) =>
    b.type === 'h2' ? <h2 key={i}>{b.text}</h2>
    : b.type === 'p' ? <p key={i}><Inlines parts={b.inline} /></p>
    : <ul key={i}>{b.items.map((it, j) => <li key={j}><Inlines parts={it} /></li>)}</ul>)}</>
}

function SupportCallout() {
  return (
    <aside className="callout">
      <b>Found this helpful?</b> {SITE.name} is free for every family. You can <a href="/#support">make a one-off contribution</a> or allow ads on this website to help cover our running costs. Both are optional.
    </aside>
  )
}

export function GuidePage() {
  const { slug } = useParams()
  const [g, setG] = useState<Full | null | undefined>(undefined)
  const [all, setAll] = useState<Summary[]>([])
  useEffect(() => {
    setG(undefined)
    fetch(`${API}/api/articles/${slug}?lang=en`).then(r => r.ok ? r.json() : null).then((x: Full | null) => { setG(x); if (x) track('guide_view', { slug: x.slug }) }).catch(() => setG(null))
    loadList().then(setAll).catch(() => setAll([]))
  }, [slug])
  useSeo({
    title: g ? (g.metaTitle || `${g.title} – ${SITE.name}`) : `Guide – ${SITE.name}`, description: g ? (g.metaDescription || g.summary) : undefined,
    path: `/guides/${slug}`, type: 'article', noindex: g === null, publishedTime: g?.createdAt, modifiedTime: g?.updatedAt,
    jsonLd: g ? articleLd({ title: g.title, description: g.metaDescription || g.summary, slug: g.slug, created: g.createdAt, updated: g.updatedAt }) : undefined,
  })

  // "Keep reading": the guides this one links to first, then other guides, three in total.
  const related = useMemo(() => {
    if (!g?.body) return []
    const others = all.filter(a => a.slug !== g.slug)
    const linked = linkedGuideSlugs(g.body).map(s => others.find(a => a.slug === s)).filter((a): a is Summary => !!a)
    return [...linked, ...others.filter(a => !linked.includes(a))].slice(0, 3)
  }, [g, all])

  if (g === undefined) return <div className="page"><p>Loading…</p></div>
  if (g === null) return <div className="page"><h1>Guide not found</h1><Link to="/guides">← All guides</Link></div>
  return (
    <article className="page prose">
      <Link to="/guides">← All guides</Link>
      <h1>{g.title}</h1>
      {g.body && <p className="muted"><small>{readMinutes(g.body)} min read{g.updatedAt && ` · Updated ${new Date(g.updatedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' })}`}</small></p>}
      {g.locked || !g.body ? <p>This guide is not available.</p> : <Body text={g.body} />}
      <SupportCallout />
      <AdSlot />
      {related.length > 0 && (
        <section className="related">
          <h2>Keep reading</h2>
          <ul>{related.map(r => <li key={r.slug}><Link to={`/guides/${r.slug}`}>{r.title}</Link><br /><small className="muted">{r.summary}</small></li>)}</ul>
        </section>
      )}
      <p className="muted"><small>This guide is general information and not medical advice. Talk to your doctor about any concerns.</small></p>
    </article>
  )
}
