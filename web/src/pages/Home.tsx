import { useEffect, useState, FormEvent } from 'react'
import { API, SITE } from '../site'
import { useSeo, organizationLd, websiteLd, faqLd } from '../seo'
import { track, campaignSuffix } from '../trackers'
import Donate from '../components/Donate'

const features = [
  { icon: '🚽', title: 'One-tap logging', text: 'Pee, poop, accidents, sits, drinks and food, logged in a second, even with one hand full.' },
  { icon: '🔮', title: 'Smart predictions', text: 'We learn your child\'s rhythm, including how long after a drink they usually need to go, and nudge you before the next likely potty time.' },
  { icon: '📈', title: 'Clear progress', text: 'Success rate, dry streaks, intake vs output and busiest hours, so you can see what is working.' },
  { icon: '👨‍👩‍👧', title: 'Share with caregivers', text: 'Parents, grandparents and daycare stay in sync on the same log.' },
  { icon: '🥦', title: 'Food, fluids and poo', text: 'Spot patterns between what your child eats and drinks and how easily they go.' },
  { icon: '📚', title: 'Expert-style guides', text: 'Readiness checklists, first-week routines and what to do about accidents. Free to read.' },
]

const faqs = [
  ['Is PottyTrain really free?', 'Yes. Every feature is free for now. We cover the running costs with small donations and ads on our website guides (never inside your child\'s log).'],
  ['When should I start potty training?', 'Most children show signs of readiness between 18 and 36 months. Every child is different. Our readiness guide helps you decide.'],
  ['Is my child\'s data private?', 'Yes. We only store what you log, never sell it, never show ads next to it, and you can delete everything at any time.'],
  ['How do I sign in?', 'With your email address and a password. You can reset your password by email if you forget it.'],
  ['When is the app launching?', 'Join the waitlist and we\'ll email you as soon as the iOS and Android apps are ready.'],
]

export default function Home() {
  useSeo({ title: 'PottyTrain – Free potty training tracker for parents', description: SITE.description, path: '/', jsonLd: [organizationLd(), websiteLd(), faqLd(faqs)] })
  useEffect(() => {
    if (window.location.hash) document.querySelector(window.location.hash)?.scrollIntoView() // arrived via /#support from another page
  }, [])
  return (
    <>
      <section className="hero">
        <div>
          <p className="eyebrow">Free potty training tracker</p>
          <h1>Log every step. <span>Spot the pattern.</span> Celebrate the win.</h1>
          <p className="lead">PottyTrain helps parents track pee, poop, accidents, drinks and food, predicts the next potty time and turns a messy phase into clear progress. Free for every family.</p>
          <WaitlistForm source="hero" />
          <p className="hint">Free · iOS &amp; Android · No spam</p>
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
        <div className="grid">{features.map(f => <article key={f.title} className="feature"><span className="icon">{f.icon}</span><h3>{f.title}</h3><p>{f.text}</p></article>)}</div>
      </section>

      <section className="section alt">
        <h2>How it works</h2>
        <ol className="steps">
          <li><b>1. Add your child</b><p>Set their age and the day you start.</p></li>
          <li><b>2. Log in one tap</b><p>Record each potty visit, accident, drink and snack as it happens.</p></li>
          <li><b>3. Follow the insights</b><p>Get predictions, streaks and a plan that adapts.</p></li>
        </ol>
      </section>

      <Donate />

      <section id="faq" className="section">
        <h2>Questions</h2>
        <div className="faq">{faqs.map(([q, a]) => <details key={q}><summary>{q}</summary><p>{a}</p></details>)}</div>
      </section>

      <section id="join" className="cta">
        <h2>Be first to know when we launch</h2>
        <WaitlistForm source="footer" />
      </section>
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
      const r = await fetch(`${API}/api/waitlist`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, source: (source + campaignSuffix()).slice(0, 200) }) })
      if (r.ok) track('waitlist_join', { source })
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
