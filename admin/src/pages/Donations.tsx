import { useEffect, useState } from 'react'
import { api } from '../api'

interface Data { count: number; totalCents: number; items: { id: string; amountCents: number; currency: string; createdAt: string }[] }

export default function Donations() {
  const [d, setD] = useState<Data>()
  useEffect(() => { api<Data>('/admin/donations').then(setD) }, [])
  if (!d) return <p>Loading…</p>
  const fmt = (cents: number, cur: string) => new Intl.NumberFormat(undefined, { style: 'currency', currency: cur }).format(cents / 100)
  return (
    <>
      <h2>Donations</h2>
      <div className="tiles">
        <div className="tile"><small>Donations</small><strong>{d.count}</strong></div>
        <div className="tile"><small>Total (before Stripe fees)</small><strong>{(d.totalCents / 100).toFixed(2)}</strong></div>
      </div>
      <p className="muted">Recorded from Stripe's webhook. Donor details stay in your Stripe dashboard.</p>
      <table>
        <thead><tr><th>Date</th><th>Amount</th></tr></thead>
        <tbody>{d.items.map(i => <tr key={i.id}><td>{new Date(i.createdAt).toLocaleString()}</td><td>{fmt(i.amountCents, i.currency)}</td></tr>)}</tbody>
      </table>
    </>
  )
}
