import { useEffect, useRef } from 'react'
import { SITE, hasOptionalTracking } from '../site'
import { useConsent, setConsent } from '../consent'
import { useState } from 'react'

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
  const enabled = !!SITE.adsenseClient && !!SITE.adsenseSlot && consent.advertising

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
  const [custom, setCustom] = useState(false)
  const [analytics, setAnalytics] = useState(false)
  const [advertising, setAdvertising] = useState(false)
  if (!hasOptionalTracking || consent.decided) return null
  return (
    <div className="consent" role="dialog" aria-label="Cookie choices">
      <p>We keep PottyTrain free with ads and use light analytics on this website (never inside your child's tracking). They use cookies or similar technology only if you agree. <a href="/privacy">Details</a></p>
      {custom && (
        <div className="choices">
          <label><input type="checkbox" checked disabled /> Essential (always on)</label>
          {(SITE.gaMeasurementId) && <label><input type="checkbox" checked={analytics} onChange={e => setAnalytics(e.target.checked)} /> Analytics: how the site is used</label>}
          {(SITE.adsenseClient || SITE.metaPixelId) && <label><input type="checkbox" checked={advertising} onChange={e => setAdvertising(e.target.checked)} /> Advertising &amp; marketing</label>}
        </div>
      )}
      <div>
        <button className="btn small ghost" onClick={() => setConsent(false, false)}>Essential only</button>
        {custom ? <button className="btn small ghost" onClick={() => setConsent(analytics, advertising)}>Save choices</button>
          : <button className="btn small ghost" onClick={() => setCustom(true)}>Choose</button>}
        <button className="btn small" onClick={() => setConsent(true, true)}>Allow all</button>
      </div>
    </div>
  )
}
