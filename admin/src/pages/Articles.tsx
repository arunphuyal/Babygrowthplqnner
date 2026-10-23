import { FormEvent, useEffect, useState } from 'react'
import { api, Article } from '../api'

const blank = { slug: '', title: '', summary: '', body: '', isPremium: false, isPublished: false }

export default function Articles() {
  const [items, setItems] = useState<Article[]>([])
  const [editing, setEditing] = useState<(typeof blank & { id?: string }) | null>(null)
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
      <label>Title<input required value={editing.title} onChange={e => setEditing({ ...editing, title: e.target.value })} /></label>
      <label>Slug<input required pattern="[a-z0-9-]+" value={editing.slug} onChange={e => setEditing({ ...editing, slug: e.target.value })} /></label>
      <label>Summary<input value={editing.summary} onChange={e => setEditing({ ...editing, summary: e.target.value })} /></label>
      <label>Body<textarea rows={10} value={editing.body} onChange={e => setEditing({ ...editing, body: e.target.value })} /></label>
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
