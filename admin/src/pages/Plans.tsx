import { useEffect, useState } from 'react'
import { api, PlanOffer } from '../api'

export default function Plans() {
  const [plans, setPlans] = useState<PlanOffer[]>([])
  const [msg, setMsg] = useState('')
  useEffect(() => { api<PlanOffer[]>('/admin/plans').then(setPlans).catch(e => setMsg(e.message)) }, [])
  const edit = (id: string, patch: Partial<PlanOffer>) => setPlans(ps => ps.map(p => p.id === id ? { ...p, ...patch } : p))

  async function save(p: PlanOffer) {
    setMsg('')
    try { await api(`/admin/plans/${p.id}`, { method: 'PUT', body: JSON.stringify({ ...p }) }); setMsg(`Saved ${p.name}`) } catch (e) { setMsg((e as Error).message) }
  }

  return (
    <>
      <h2>Plans &amp; pricing</h2>
      <p className="muted">Changes appear on the public landing page immediately. Payment processing (Stripe / App Store / Play Billing) is a later step.</p>
      {msg && <p className="ok">{msg}</p>}
      <div className="cards">{plans.map(p => (
        <div className="panel" key={p.id}>
          <label>Name<input value={p.name} onChange={e => edit(p.id, { name: e.target.value })} /></label>
          <label>Monthly price<input type="number" step="0.01" min="0" value={p.monthlyPrice} onChange={e => edit(p.id, { monthlyPrice: +e.target.value })} /></label>
          <label>Yearly price<input type="number" step="0.01" min="0" value={p.yearlyPrice} onChange={e => edit(p.id, { yearlyPrice: +e.target.value })} /></label>
          <label>Features (one per line)<textarea rows={5} value={p.features} onChange={e => edit(p.id, { features: e.target.value })} /></label>
          <label className="check"><input type="checkbox" checked={p.isActive} onChange={e => edit(p.id, { isActive: e.target.checked })} /> Visible on website</label>
          <button className="btn" onClick={() => save(p)}>Save</button>
        </div>
      ))}</div>
    </>
  )
}
