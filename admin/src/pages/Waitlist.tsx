import { useEffect, useState } from 'react'
import { api } from '../api'

interface Entry { id: string; email: string; source?: string; createdAt: string }

export default function Waitlist() {
  const [items, setItems] = useState<Entry[]>([])
  useEffect(() => { api<Entry[]>('/admin/waitlist').then(setItems) }, [])
  const csv = () => {
    const rows = ['email,source,joined', ...items.map(i => `${i.email},${i.source ?? ''},${i.createdAt}`)].join('\n')
    const a = document.createElement('a')
    a.href = URL.createObjectURL(new Blob([rows], { type: 'text/csv' }))
    a.download = 'waitlist.csv'; a.click()
  }
  return (
    <>
      <div className="row between"><h2>Waitlist <small>({items.length})</small></h2><button className="btn" onClick={csv} disabled={!items.length}>Export CSV</button></div>
      <table>
        <thead><tr><th>Email</th><th>Source</th><th>Joined</th></tr></thead>
        <tbody>{items.map(i => <tr key={i.id}><td>{i.email}</td><td>{i.source ?? '—'}</td><td>{new Date(i.createdAt).toLocaleString()}</td></tr>)}</tbody>
      </table>
    </>
  )
}
