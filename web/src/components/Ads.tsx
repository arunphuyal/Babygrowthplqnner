import { useEffect, useRef } from 'react'
import { SITE } from '../site'
import { useConsent, setConsent } from '../consent'

declare global { interface Window { adsbygoogle?: Record<string, unknown>[] & { requestNonPersonalizedAds?: number } } }

let scriptLoaded = false
function loadAdsScript() {
  if (scriptLoaded || !SITE.adsenseClient) return
  scriptLoaded = true
  window.adsbygoogle = window.adsbygoogle ?? []
  window.adsbygoogle.requestNonPersonalizedAds = 1 // never personalised until a certified consent platform is added
  const s = document.createElement('script')
  s.async = true
  s.crossOrigin = 'anonymous'
  s.src = `https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=${encodeURIComponent(SITE.adsenseClient)}`
  document.head.appendChild(s)
}

/** A single display ad. Renders nothing unless AdSense is configured AND the visitor accepted. Use only on pages with real content. */
export function AdSlot() {
  const consent = useConsent()
  const pushed = useRef(false)
  const enabled = !!SITE.adsenseClient && !!SITE.adsenseSlot && consent === 'granted'

  useEffect(() => {
    if (!enabled || pushed.current) return
    loadAdsScript()
    pushed.current = true
    try { (window.adsbygoogle = window.adsbygoogle ?? []).push({}) } catch { /* ad blockers etc. */ }
  }, [enabled])

  if (!enabled) return null
  return (
    <aside className="ad" aria-label="Advertisement">
      <small>Advertisement</small>
      <ins className="adsbygoogle" style={{ display: 'block' }} data-ad-client={SITE.adsenseClient} data-ad-slot={SITE.adsenseSlot}
        data-ad-format="auto" data-full-width-responsive="true" />
    </aside>
  )
}

export function ConsentBanner() {
  const consent = useConsent()
  if (!SITE.adsenseClient || consent !== 'unset') return null
  return (
    <div className="consent" role="dialog" aria-label="Cookie choices">
      <p>We keep PottyTrain free with ads on our guide pages. Ads use cookies. Do you allow them? <a href="/privacy">Details</a></p>
      <div>
        <button className="btn small ghost" onClick={() => setConsent('denied')}>No thanks</button>
        <button className="btn small" onClick={() => setConsent('granted')}>Allow ads</button>
      </div>
    </div>
  )
}
