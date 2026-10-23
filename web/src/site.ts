// Edit these before launch. They appear on the privacy page, in the footer and in search-engine metadata.
export const SITE = {
  // Working name; set VITE_SITE_NAME to the final brand and every page, title and email-facing text follows.
  name: (import.meta.env.VITE_SITE_NAME ?? 'Babybunu') as string,
  url: ((import.meta.env.VITE_SITE_URL ?? 'https://example.com') as string).replace(/\/$/, ''),
  contactEmail: import.meta.env.VITE_CONTACT_EMAIL ?? 'hello@example.com',
  description: 'Free potty training tracker for parents: log wees, poos, accidents, drinks and food, spot your toddler\'s patterns and get a calm, clear plan.',
  // Google AdSense. Leave the client empty to show no ads at all.
  adsenseClient: (import.meta.env.VITE_ADSENSE_CLIENT ?? '') as string, // e.g. ca-pub-1234567890123456
  adsenseSlot: (import.meta.env.VITE_ADSENSE_SLOT ?? '') as string,     // an ad unit id from your AdSense account
  // Optional Google tag (gtag.js) and Meta Pixel. Empty = never loaded. Public website only, never in the tracking app.
  googleTagId: (import.meta.env.VITE_GOOGLE_TAG_ID ?? import.meta.env.VITE_GA_MEASUREMENT_ID ?? '') as string, // G-XXXXXXXXXX, GT-XXXXXXX or AW-XXXXXXXXX
  metaPixelId: (import.meta.env.VITE_META_PIXEL_ID ?? '') as string,         // e.g. 1234567890
}
export const API = import.meta.env.VITE_API_URL ?? ''
export const hasOptionalTracking = !!(SITE.adsenseClient || SITE.googleTagId || SITE.metaPixelId)
