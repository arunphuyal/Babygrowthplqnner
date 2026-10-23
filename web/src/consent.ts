import { useSyncExternalStore } from 'react'

/** Two separate choices, as privacy law expects: measurement (analytics) and advertising/marketing (ads + marketing pixels). */
export interface Consent { decided: boolean; analytics: boolean; advertising: boolean }
const KEY = 'pt_consent_v2'
const listeners = new Set<() => void>()
const NONE: Consent = { decided: false, analytics: false, advertising: false }

function read(): Consent {
  try {
    const v = JSON.parse(localStorage.getItem(KEY) ?? 'null')
    if (v && typeof v.analytics === 'boolean' && typeof v.advertising === 'boolean') return { decided: true, analytics: v.analytics, advertising: v.advertising }
  } catch { /* storage unavailable or corrupt: treat as undecided */ }
  return NONE
}

let current: Consent = read()

export function setConsent(analytics: boolean, advertising: boolean) {
  current = { decided: true, analytics, advertising }
  try { localStorage.setItem(KEY, JSON.stringify({ analytics, advertising })) } catch { /* the choice lasts for this page view only */ }
  listeners.forEach(l => l())
}

/** Shows the banner again so a visitor can change or withdraw their choice at any time (a legal requirement in the EU/UK). */
export function reopenConsent() {
  current = NONE
  try { localStorage.removeItem(KEY) } catch { /* ignore */ }
  listeners.forEach(l => l())
}

export const useConsent = (): Consent => useSyncExternalStore(cb => { listeners.add(cb); return () => listeners.delete(cb) }, () => current)
