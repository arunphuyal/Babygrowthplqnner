import { useEffect } from 'react'
import { Link, Route, Routes, useLocation } from 'react-router-dom'
import { useConsent, reopenConsent } from './consent'
import { hasOptionalTracking } from './site'
import { applyConsent, trackPageView } from './trackers'
import { useSeo } from './seo'
import { SITE } from './site'
import { ConsentBanner } from './components/Ads'
import Home from './pages/Home'
import Privacy from './pages/Privacy'
import { GuideList, GuidePage } from './pages/Guides'
import { VerifyEmail, ResetPassword } from './pages/AccountLinks'

function NotFound() {
  useSeo({ title: `Page not found – ${SITE.name}`, path: '/404', noindex: true })
  return <div className="page"><h1>Page not found</h1><Link to="/">Go home</Link></div>
}

export default function App() {
  const consent = useConsent()
  const { pathname } = useLocation()
  useEffect(() => { applyConsent(consent.analytics, consent.advertising) }, [consent.analytics, consent.advertising])
  useEffect(() => { trackPageView(pathname) }, [pathname, consent.analytics, consent.advertising])
  return (
    <>
      <header className="nav">
        <Link className="brand" to="/">🐰 {SITE.name}</Link>
        <nav>
          <Link to="/guides">Guides</Link>
          <a href="/#features">Features</a>
          <a href="/#support">Support us</a>
          <a className="btn small" href="/#join">Join waitlist</a>
        </nav>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/guides" element={<GuideList />} />
          <Route path="/guides/:slug" element={<GuidePage />} />
          <Route path="/privacy" element={<Privacy />} />
          <Route path="/verify-email" element={<VerifyEmail />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
      <footer>
        © {new Date().getFullYear()} {SITE.name} · <Link to="/privacy">Privacy</Link> · {hasOptionalTracking && <><button className="link" onClick={reopenConsent}>Ad &amp; cookie choices</button> · </>} <a href={`mailto:${SITE.contactEmail}`}>Contact</a>
        <br />{SITE.name} is a tracking tool, not medical advice. Talk to your paediatrician about concerns.
      </footer>
      <ConsentBanner />
    </>
  )
}
