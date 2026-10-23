import { useEffect, useState } from 'react'
import { API } from '../site'
import { track } from '../trackers'

interface Config { enabled: boolean; currency: string; presetsCents: number[]; minCents: number; maxCents: number }

export default function Donate() {
  const [cfg, setCfg] = useState<Config>()
  const [loaded, setLoaded] = useState(false)
  const [custom, setCustom] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const status = new URLSearchParams(window.location.search).get('donation')

  useEffect(() => { if (status === 'thanks') track('donate_thanks') }, [status])
  useEffect(() => { fetch(`${API}/api/donations/config`).then(r => r.json()).then(setCfg).catch(() => setCfg(undefined)).finally(() => setLoaded(true)) }, [])

  const money = (cents: number) => new Intl.NumberFormat(undefined, { style: 'currency', currency: cfg?.currency ?? 'usd', maximumFractionDigits: cents % 100 ? 2 : 0 }).format(cents / 100)

  async function give(cents: number) {
    setError('')
    if (!cfg || !Number.isFinite(cents) || cents < cfg.minCents || cents > cfg.maxCents) { setError(`Please choose between ${money(cfg?.minCents ?? 100)} and ${money(cfg?.maxCents ?? 50000)}.`); return }
    setBusy(true)
    track('donate_start', { value: cents / 100 })
    try {
      const r = await fetch(`${API}/api/donations/checkout`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ amountCents: cents }) })
      const body = await r.json()
      if (!r.ok) throw new Error(body.error ?? 'Something went wrong')
      window.location.href = body.url
    } catch (e) { setError((e as Error).message); setBusy(false) }
  }

  return (
    <section id="support" className="section alt">
      <h2>Free for every family</h2>
      <div className="support">
        <p>PottyTrain is free while we grow. If it helped you, a small one-off donation covers our servers and keeps it free for the next parent. Payments are handled securely by Stripe.</p>
        {status === 'thanks' && <p className="ok">💚 Thank you so much! Your support keeps PottyTrain free.</p>}
        {status === 'cancelled' && <p className="muted">No problem, nothing was charged.</p>}
        {!loaded ? null : !cfg?.enabled ? <p className="muted">Donations will open soon.</p> : (
          <>
            <div className="amounts">{cfg.presetsCents.map(c => <button key={c} className="btn ghost" disabled={busy} onClick={() => give(c)}>{money(c)}</button>)}</div>
            <form className="custom" onSubmit={e => { e.preventDefault(); give(Math.round(parseFloat(custom) * 100)) }}>
              <input inputMode="decimal" placeholder="Other amount" value={custom} onChange={e => setCustom(e.target.value)} aria-label="Custom donation amount" />
              <button className="btn" disabled={busy || !custom}>Donate</button>
            </form>
            {error && <p className="err">{error}</p>}
            <small>Donations are not tax-deductible unless we tell you otherwise.</small>
          </>
        )}
      </div>
    </section>
  )
}
