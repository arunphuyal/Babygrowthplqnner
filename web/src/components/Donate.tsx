import { useEffect, useState } from 'react'
import { API, SITE } from '../site'
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
      <h2>Free for every family. Support us if you like.</h2>
      <div className="support">
        <p>{SITE.name} is free while we grow. If it has helped you, you can make a small one-off contribution towards our running costs, or simply allow ads on this website. Both are optional. Payments are handled securely by Stripe.</p>
        {status === 'thanks' && <p className="ok">💚 Thank you so much! Your support helps keep {SITE.name} free.</p>}
        {status === 'cancelled' && <p className="muted">No problem, nothing was charged.</p>}
        {!loaded ? null : !cfg?.enabled ? <p className="muted">Contributions will open soon.</p> : (
          <>
            <div className="amounts">{cfg.presetsCents.map(c => <button key={c} className="btn ghost" disabled={busy} onClick={() => give(c)}>{money(c)}</button>)}</div>
            <form className="custom" onSubmit={e => { e.preventDefault(); give(Math.round(parseFloat(custom) * 100)) }}>
              <input inputMode="decimal" placeholder="Other amount" value={custom} onChange={e => setCustom(e.target.value)} aria-label="Custom amount" />
              <button className="btn" disabled={busy || !custom}>Contribute</button>
            </form>
            {error && <p className="err">{error}</p>}
            <small>Contributions help fund a company and are not tax-deductible donations to a charity.</small>
          </>
        )}
      </div>
    </section>
  )
}
