// Edit these before launch. They appear on the privacy page and in the footer.
export const SITE = {
  name: 'PottyTrain',
  contactEmail: import.meta.env.VITE_CONTACT_EMAIL ?? 'hello@example.com',
  // Google AdSense. Leave the client empty to show no ads at all.
  adsenseClient: (import.meta.env.VITE_ADSENSE_CLIENT ?? '') as string, // e.g. ca-pub-1234567890123456
  adsenseSlot: (import.meta.env.VITE_ADSENSE_SLOT ?? '') as string,     // an ad unit id from your AdSense account
}
export const API = import.meta.env.VITE_API_URL ?? ''
