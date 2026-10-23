import { useEffect, useState, FormEvent } from 'react'

const API = import.meta.env.VITE_API_URL ?? ''

interface Plan { id: string; name: string; monthlyPrice: number; yearlyPrice: number; features: string }

const fallbackPlans: Plan[] = [
  { id: 'free', name: 'Free', monthlyPrice: 0, yearlyPrice: 0, features: '1 child\nOne-tap logging\n7-day history' },
  { id: 'premium', name: 'Premium', monthlyPrice: 4.99, yearlyPrice: 39.99, features: 'Smart potty-time predictions\nFull history & insights\nReward charts\nExpert guides' },
  { id: 'family', name: 'Family', monthlyPrice: 7.99, yearlyPrice: 59.99, features: 'Everything in Premium\nUnlimited children\nShare with caregivers\nDaycare report export' },
]

const features = [
  { icon: '🚽', title: 'One-tap logging', text: 'Pee, poop, accidents, sits and fluids — logged in a second, even with one hand full.' },
  { icon: '🔮', title: 'Smart predictions', text: 'We learn your child\'s rhythm and nudge you before the next likely potty time.' },
  { icon: '📈', title: 'Clear progress', text: 'Success rate, dry streaks and busiest hours, so you can see what is working.' },
  { icon: '⭐', title: 'Reward charts', text: 'Stickers and milestones that keep little ones motivated and celebrating.' },
  { icon: '👨‍👩‍👧', title: 'Share with caregivers', text: 'Parents, grandparents and daycare stay in sync on the same log.' },
  { icon: '📚', title: 'Expert guides', text: 'Readiness checklists and step-by-step methods, from regression to night training.' },
]

const faqs = [
  ['When should I start potty training?', 'Most children show signs of readiness between 18 and 36 months. Every child is different — the app\'s readiness guide helps you decide.'],
  ['Is my child\'s data private?', 'Yes. We only store what you log, never sell it, and you can delete everything at any time.'],
  ['Is it free?', 'The core logging is free. Premium adds predictions, full history and guides.'],
  ['When is the app launching?', 'Join the waitlist and we\'ll email you as soon as the iOS and Android apps are ready.'],
]

export default function App() {
  const [plans, setPlans] = useState<Plan[]>(fallbackPlans)
  const [yearly, setYearly] = useState(false)

  useEffect(() => {
    fetch(`${API}/api/plans`).then(r => r.ok ? r.json() : Promise.reject()).then((p: Plan[]) => p.length && setPlans(p)).catch(() => {})
  }, [])

  return (
    <>
      <header className="nav">
        <a className="brand" href="#top">🌱 PottyTrain</a>
        <nav>
          <a href="#features">Features</a><a href="#pricing">Pricing</a><a href="#faq">FAQ</a>
          <a className="btn small" href="#join">Join waitlist</a>
        </nav>
      </header>

      <main id="top">
        <section className="hero">
          <div>
            <p className="eyebrow">Potty training, made calm</p>
            <h1>Log every step. <span>Spot the pattern.</span> Celebrate the win.</h1>
            <p className="lead">PottyTrain helps parents track pee, poop and accidents, predicts the next potty time and turns a messy phase into clear progress.</p>
            <WaitlistForm source="hero" />
            <p className="hint">Free to start · iOS &amp; Android · No spam</p>
          </div>
          <div className="phone" aria-hidden>
            <div className="card"><b>Leo</b><small>Day 4 of training</small></div>
            <div className="stats"><div><strong>82%</strong><small>success rate</small></div><div><strong>6h</strong><small>dry streak</small></div></div>
            <div className="next">Next likely potty time <strong>3:20 pm</strong></div>
            <div className="buttons"><span>💧 Pee</span><span>💩 Poop</span><span>😅 Accident</span></div>
          </div>
        </section>

        <section id="features" className="section">
          <h2>Everything you need to get out of diapers</h2>
          <div className="grid">
            {features.map(f => (
              <article key={f.title} className="feature"><span className="icon">{f.icon}</span><h3>{f.title}</h3><p>{f.text}</p></article>
            ))}
          </div>
        </section>

        <section className="section alt">
          <h2>How it works</h2>
          <ol className="steps">
            <li><b>1. Add your child</b><p>Set their age and the day you start.</p></li>
            <li><b>2. Log in one tap</b><p>Record each potty visit or accident as it happens.</p></li>
            <li><b>3. Follow the insights</b><p>Get predictions, streaks and a plan that adapts.</p></li>
          </ol>
        </section>

        <section id="pricing" className="section">
          <h2>Simple pricing</h2>
          <div className="toggle">
            <button className={!yearly ? 'on' : ''} onClick={() => setYearly(false)}>Monthly</button>
            <button className={yearly ? 'on' : ''} onClick={() => setYearly(true)}>Yearly</button>
          </div>
          <div className="grid plans">
            {plans.map(p => {
              const price = yearly ? p.yearlyPrice : p.monthlyPrice
              return (
                <article key={p.id} className={`plan ${p.name === 'Premium' ? 'popular' : ''}`}>
                  <h3>{p.name}</h3>
                  <p className="price">{price === 0 ? 'Free' : `$${price.toFixed(2)}`}<small>{price === 0 ? '' : yearly ? '/year' : '/month'}</small></p>
                  <ul>{p.features.split('\n').filter(Boolean).map(f => <li key={f}>{f}</li>)}</ul>
                  <a className="btn" href="#join">Get early access</a>
                </article>
              )
            })}
          </div>
        </section>

        <section id="faq" className="section alt">
          <h2>Questions</h2>
          <div className="faq">{faqs.map(([q, a]) => <details key={q}><summary>{q}</summary><p>{a}</p></details>)}</div>
        </section>

        <section id="join" className="cta">
          <h2>Be first to know when we launch</h2>
          <WaitlistForm source="footer" />
        </section>
      </main>

      <footer>© {new Date().getFullYear()} PottyTrain. PottyTrain is a tracking tool, not medical advice — talk to your paediatrician about concerns.</footer>
    </>
  )
}

function WaitlistForm({ source }: { source: string }) {
  const [email, setEmail] = useState('')
  const [state, setState] = useState<'idle' | 'loading' | 'done' | 'error'>('idle')

  async function submit(e: FormEvent) {
    e.preventDefault()
    setState('loading')
    try {
      const r = await fetch(`${API}/api/waitlist`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, source }) })
      setState(r.ok ? 'done' : 'error')
    } catch { setState('error') }
  }

  if (state === 'done') return <p className="ok">🎉 You're on the list! We'll be in touch.</p>
  return (
    <form className="waitlist" onSubmit={submit}>
      <input type="email" required placeholder="you@example.com" value={email} onChange={e => setEmail(e.target.value)} aria-label="Email address" />
      <button className="btn" disabled={state === 'loading'}>{state === 'loading' ? 'Joining…' : 'Join the waitlist'}</button>
      {state === 'error' && <p className="err">Something went wrong. Please try again.</p>}
    </form>
  )
}
