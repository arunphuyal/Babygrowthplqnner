import { SITE } from './site'

/**
 * Optional third-party measurement and marketing tags (Google tag, Meta Pixel). Nothing here loads until the visitor has opted in to the matching category,
 * and this module is only used by the public website. Never send child names, health data or email addresses in event parameters.
 */
declare global { interface Window { dataLayer?: unknown[]; gtag?: (...a: unknown[]) => void; fbq?: ((...a: unknown[]) => void) & { callMethod?: unknown; queue?: unknown[]; loaded?: boolean; version?: string; push?: unknown } } }

let googleLoaded = false
let analyticsAllowed = false
let metaLoaded = false

function script(src: string) {
  const s = document.createElement('script')
  s.async = true
  s.src = src
  document.head.appendChild(s)
}

export function applyConsent(analytics: boolean, advertising: boolean) {
  analyticsAllowed = analytics

  // Google tag with Consent Mode v2. The script is not fetched until the visitor allows at least one category; every signal starts
  // "denied" and is then granted per category: analytics -> analytics_storage, advertising -> ad_storage / ad_user_data / ad_personalization.
  if (SITE.googleTagId) {
    window.dataLayer = window.dataLayer ?? []
    window.gtag = window.gtag ?? function () { window.dataLayer!.push(arguments) }
    if ((analytics || advertising) && !googleLoaded) {
      googleLoaded = true
      window.gtag('consent', 'default', { analytics_storage: 'denied', ad_storage: 'denied', ad_user_data: 'denied', ad_personalization: 'denied' })
      script(`https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(SITE.googleTagId)}`)
      window.gtag('js', new Date())
      window.gtag('config', SITE.googleTagId, { send_page_view: false, anonymize_ip: true })
    }
    if (googleLoaded) {
      const a = advertising ? 'granted' : 'denied'
      window.gtag('consent', 'update', { analytics_storage: analytics ? 'granted' : 'denied', ad_storage: a, ad_user_data: a, ad_personalization: a })
    }
  }

  // Meta Pixel, advertising/marketing category.
  if (SITE.metaPixelId) {
    if (advertising && !metaLoaded) {
      metaLoaded = true
      const fbq = function (...a: unknown[]) { (fbq as unknown as { queue: unknown[] }).queue.push(a) } as unknown as NonNullable<Window['fbq']>
      fbq.queue = []; fbq.loaded = true; fbq.version = '2.0'; fbq.push = fbq
      window.fbq = fbq
      script('https://connect.facebook.net/en_US/fbevents.js')
      window.fbq('consent', 'grant')
      window.fbq('init', SITE.metaPixelId)
    }
    if (metaLoaded) window.fbq?.('consent', advertising ? 'grant' : 'revoke')
  }
}

/** Page views for the single-page app (call on every route change). Silent unless the matching tag is loaded and allowed. */
export function trackPageView(path: string) {
  if (googleLoaded && analyticsAllowed) window.gtag?.('event', 'page_view', { page_path: path, page_location: SITE.url + path })
  if (metaLoaded) window.fbq?.('track', 'PageView')
}

export type EventName = 'waitlist_join' | 'donate_start' | 'donate_thanks' | 'guide_view'

export function track(name: EventName, params: Record<string, string | number> = {}) {
  if (googleLoaded && analyticsAllowed) window.gtag?.('event', name, params)
  if (metaLoaded) {
    if (name === 'waitlist_join') window.fbq?.('track', 'Lead')
    else window.fbq?.('trackCustom', name, params)
  }
}

/** Remembers UTM parameters for the session so a waitlist sign-up can be attributed to its campaign. No cookies involved. */
export function campaignSuffix(): string {
  try {
    const key = 'bb_utm'
    const q = new URLSearchParams(window.location.search)
    const found = ['utm_source', 'utm_medium', 'utm_campaign'].filter(k => q.get(k)).map(k => `${k}=${q.get(k)!.slice(0, 40)}`)
    if (found.length) sessionStorage.setItem(key, found.join('&'))
    const saved = sessionStorage.getItem(key)
    return saved ? `?${saved}` : ''
  } catch { return '' }
}
