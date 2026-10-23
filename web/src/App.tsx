import { Link, Route, Routes } from 'react-router-dom'
import { SITE } from './site'
import { ConsentBanner } from './components/Ads'
import Home from './pages/Home'
import Privacy from './pages/Privacy'
import { GuideList, GuidePage } from './pages/Guides'

export default function App() {
  return (
    <>
      <header className="nav">
        <Link className="brand" to="/">🌱 {SITE.name}</Link>
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
          <Route path="*" element={<div className="page"><h1>Page not found</h1><Link to="/">Go home</Link></div>} />
        </Routes>
      </main>
      <footer>
        © {new Date().getFullYear()} {SITE.name} · <Link to="/privacy">Privacy</Link> · <a href={`mailto:${SITE.contactEmail}`}>Contact</a>
        <br />{SITE.name} is a tracking tool, not medical advice. Talk to your paediatrician about concerns.
      </footer>
      <ConsentBanner />
    </>
  )
}
