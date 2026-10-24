import { FormEvent, useEffect, useState } from 'react'
import { api, Article } from '../api'
import { seoChecks } from '../seo'
import { parseBody, readMinutes, type Inline } from '../markup.mjs'

interface Draft { language?: string; slug: string; title: string; summary: string; body: string; isPremium: boolean; isPublished: boolean; metaTitle?: string; metaDescription?: string; focusKeyword?: string }
const blank: Draft = { language: 'en', slug: '', title: '', summary: '', body: '', isPremium: false, isPublished: false, metaTitle: '', metaDescription: '', focusKeyword: '' }

function Inlines({ parts }: { parts: Inline[] }) {
  return <>{parts.map((p, i) => p.t === 'text' ? <span key={i}>{p.v}</span> : <a key={i} href="#preview" onClick={e => e.preventDefault()} title={p.href}>{p.v}</a>)}</>
}

/** Shows the body exactly as the website will render it (same parser), so formatting mistakes are visible before publishing. */
function Preview({ body }: { body: string }) {
  const blocks = parseBody(body)
  if (blocks.length === 0) return <p className="muted">Nothing to preview yet.</p>
  return (
    <div className="preview">
      <small className="muted">{readMinutes(body)} min read</small>
      {blocks.map((b, i) => b.type === 'h2' ? <h3 key={i}>{b.text}</h3> : b.type === 'p' ? <p key={i}><Inlines parts={b.inline} /></p>
        : <ul key={i}>{b.items.map((it, j) => <li key={j}><Inlines parts={it} /></li>)}</ul>)}
    </div>
  )
}

function SeoPanel({ a }: { a: Draft }) {
  const checks = seoChecks(a)
  const passed = checks.filter(c => c.ok).length
  return (
    <div className="seo">
      <b>SEO checklist: {passed} of {checks.length}</b>
      <div className="serp"><span className="serp-url">/guides/{a.slug || 'your-slug'}</span><span className="serp-title">{a.metaTitle || a.title || 'Title'}</span><span className="serp-desc">{a.metaDescription || a.summary || 'Description'}</span></div>
      <ul>{checks.map(c => <li key={c.label} className={c.ok ? 'ok' : ''}>{c.ok ? '✅' : '⚠️'} {c.label}{!c.ok && <small className="muted"> ({c.hint})</small>}</li>)}</ul>
    </div>
  )
}

export default function Articles() {
  const [items, setItems] = useState<Article[]>([])
  const [editing, setEditing] = useState<(Draft & { id?: string }) | null>(null)
  const [err, setErr] = useState('')
  const load = () => api<Article[]>('/admin/articles').then(setItems).catch(e => setErr(e.message))
  useEffect(() => { load() }, [])

  async function save(e: FormEvent) {
    e.preventDefault()
    if (!editing) return
    setErr('')
    try {
      const { id, ...body } = editing
      await api(id ? `/admin/articles/${id}` : '/admin/articles', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) })
      setEditing(null); load()
    } catch (e) { setErr((e as Error).message) }
  }
  async function remove(a: Article) {
    if (!confirm(`Delete "${a.title}"?`)) return
    await api(`/admin/articles/${a.id}`, { method: 'DELETE' }); load()
  }

  if (editing) return (
    <form className="panel" onSubmit={save}>
      <h2>{editing.id ? 'Edit' : 'New'} article</h2>
      <label>Language code<input pattern="[a-z]{2}(-[A-Z]{2})?" title="e.g. en, fr, pt-BR" value={editing.language ?? 'en'} onChange={e => setEditing({ ...editing, language: e.target.value })} /></label>
      <label>Title<input required value={editing.title} onChange={e => setEditing({ ...editing, title: e.target.value })} /></label>
      <label>Slug<input required pattern="[a-z0-9-]+" value={editing.slug} onChange={e => setEditing({ ...editing, slug: e.target.value })} /></label>
      <label>Summary<input value={editing.summary} onChange={e => setEditing({ ...editing, summary: e.target.value })} /></label>
      <label>Body (blank line = new paragraph, "## " = heading, "- " = bullet, [text](/guides/slug) = link)<textarea rows={14} value={editing.body} onChange={e => setEditing({ ...editing, body: e.target.value })} /></label>
      <label>Focus keyword<input placeholder="e.g. potty training regression" value={editing.focusKeyword ?? ''} onChange={e => setEditing({ ...editing, focusKeyword: e.target.value })} /></label>
      <label>SEO title (optional, defaults to the title)<input maxLength={120} value={editing.metaTitle ?? ''} onChange={e => setEditing({ ...editing, metaTitle: e.target.value })} /></label>
      <label>SEO description (optional, defaults to the summary)<textarea rows={2} maxLength={320} value={editing.metaDescription ?? ''} onChange={e => setEditing({ ...editing, metaDescription: e.target.value })} /></label>
      <details><summary>Preview body</summary><Preview body={editing.body} /></details>
      <SeoPanel a={editing} />
      <label className="check"><input type="checkbox" checked={editing.isPremium} onChange={e => setEditing({ ...editing, isPremium: e.target.checked })} /> Premium only</label>
      <label className="check"><input type="checkbox" checked={editing.isPublished} onChange={e => setEditing({ ...editing, isPublished: e.target.checked })} /> Published</label>
      {err && <p className="err">{err}</p>}
      <div className="row"><button className="btn">Save</button><button type="button" className="link" onClick={() => setEditing(null)}>Cancel</button></div>
    </form>
  )

  return (
    <>
      <div className="row between"><h2>Content</h2><button className="btn" onClick={() => setEditing(blank)}>New article</button></div>
      {err && <p className="err">{err}</p>}
      <table>
        <thead><tr><th>Title</th><th>Slug</th><th>Access</th><th>Status</th><th /></tr></thead>
        <tbody>{items.map(a => (
          <tr key={a.id}>
            <td>{a.title}</td><td>{a.slug}</td>
            <td>{a.isPremium ? <span className="pill">premium</span> : 'free'}</td>
            <td>{a.isPublished ? 'Published' : 'Draft'}</td>
            <td><button className="link" onClick={() => setEditing(a)}>Edit</button> <button className="link danger" onClick={() => remove(a)}>Delete</button></td>
          </tr>
        ))}</tbody>
      </table>
    </>
  )
}
