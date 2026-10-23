import { useCallback, useEffect, useState } from 'react'
import { api, User } from '../api'

export default function Users() {
  const [q, setQ] = useState('')
  const [data, setData] = useState<{ total: number; items: User[] }>({ total: 0, items: [] })
  const [err, setErr] = useState('')
  const load = useCallback(() => api<typeof data>(`/admin/users?q=${encodeURIComponent(q)}`).then(setData).catch(e => setErr(e.message)), [q])
  useEffect(() => { const t = setTimeout(load, 250); return () => clearTimeout(t) }, [load])

  async function patch(id: string, body: object) {
    setErr('')
    try { await api(`/admin/users/${id}`, { method: 'PATCH', body: JSON.stringify(body) }); load() } catch (e) { setErr((e as Error).message) }
  }

  return (
    <>
      <h2>Users <small>({data.total})</small></h2>
      <input className="search" placeholder="Search by name or email…" value={q} onChange={e => setQ(e.target.value)} />
      {err && <p className="err">{err}</p>}
      <table>
        <thead><tr><th>Name</th><th>Email</th><th>Plan</th><th>Joined</th><th>Last login</th><th>Status</th></tr></thead>
        <tbody>{data.items.map(u => (
          <tr key={u.id}>
            <td>{u.displayName}{u.role === 'Admin' && <span className="pill">admin</span>}</td>
            <td>{u.email}</td>
            <td><select value={u.plan} onChange={e => patch(u.id, { plan: e.target.value })}><option>Free</option><option>Premium</option><option>Family</option></select>{u.planExpiresAt && <small className="muted"> until {new Date(u.planExpiresAt).toLocaleDateString()}</small>}</td>
            <td>{new Date(u.createdAt).toLocaleDateString()}</td>
            <td>{u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString() : '—'}</td>
            <td><button className="link" onClick={() => patch(u.id, { isActive: !u.isActive })}>{u.isActive ? 'Active · deactivate' : 'Disabled · activate'}</button></td>
          </tr>
        ))}</tbody>
      </table>
    </>
  )
}
