import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { API } from '../site'
import { AdSlot } from '../components/Ads'
import { useSeo, articleLd } from '../seo'
import { track } from '../trackers'

interface Summary { slug: string; title: string; summary: string }
interface Full extends Summary { body: string | null; locked: boolean; metaTitle?: string; metaDescription?: string; createdAt?: string; updatedAt?: string }

export function GuideList() {
  const [items, setItems] = useState<Summary[] | null>(null)
  useSeo({ title: 'Potty training guides – PottyTrain', description: 'Practical, parent-friendly potty training guides: readiness, the first week, accidents, and drinks, food and poo.', path: '/guides' })
  useEffect(() => { fetch(`${API}/api/articles`).then(r => r.json()).then(setItems).catch(() => setItems([])) }, [])
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

/** Minimal renderer for the admin-authored bodies: blank-line paragraphs, "## " headings and "- " lists. */
function Body({ text }: { text: string }) {
  return <>{text.split(/\n{2,}/).map((block, i) => {
    if (block.startsWith('## ')) {
      const [head, ...rest] = block.split('\n')
      return <div key={i}><h2>{head.slice(3)}</h2>{rest.length > 0 && <List lines={rest} />}</div>
    }
    return block.startsWith('- ') ? <List key={i} lines={block.split('\n')} /> : <p key={i}>{block}</p>
  })}</>
}
const List = ({ lines }: { lines: string[] }) => lines[0]?.startsWith('- ') ? <ul>{lines.map((l, i) => <li key={i}>{l.replace(/^- /, '')}</li>)}</ul> : <p>{lines.join(' ')}</p>

export function GuidePage() {
  const { slug } = useParams()
  const [g, setG] = useState<Full | null | undefined>(undefined)
  useEffect(() => {
    setG(undefined)
    fetch(`${API}/api/articles/${slug}`).then(r => r.ok ? r.json() : null).then((x: Full | null) => { setG(x); if (x) track('guide_view', { slug: x.slug }) }).catch(() => setG(null))
  }, [slug])
  useSeo({
    title: g ? (g.metaTitle || `${g.title} – PottyTrain`) : 'Guide – PottyTrain', description: g ? (g.metaDescription || g.summary) : undefined,
    path: `/guides/${slug}`, type: 'article', noindex: g === null, publishedTime: g?.createdAt, modifiedTime: g?.updatedAt,
    jsonLd: g ? articleLd({ title: g.title, description: g.metaDescription || g.summary, slug: g.slug, created: g.createdAt, updated: g.updatedAt }) : undefined,
  })
  if (g === undefined) return <div className="page"><p>Loading…</p></div>
  if (g === null) return <div className="page"><h1>Guide not found</h1><Link to="/guides">← All guides</Link></div>
  return (
    <article className="page prose">
      <Link to="/guides">← All guides</Link>
      <h1>{g.title}</h1>
      {g.locked || !g.body ? <p>This guide is not available.</p> : <Body text={g.body} />}
      <AdSlot />
      <p className="muted"><small>This guide is general information and not medical advice. Talk to your paediatrician about any concerns.</small></p>
    </article>
  )
}
