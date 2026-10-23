import { useSyncExternalStore } from 'react'

export type Consent = 'granted' | 'denied' | 'unset'
const KEY = 'pt_ad_consent'
const listeners = new Set<() => void>()

function read(): Consent {
  try { const v = localStorage.getItem(KEY); return v === 'granted' || v === 'denied' ? v : 'unset' } catch { return 'unset' }
}

export function setConsent(v: 'granted' | 'denied') {
  try { localStorage.setItem(KEY, v) } catch { /* private mode: the choice just lasts for this page view */ }
  cached = v
  listeners.forEach(l => l())
}

let cached: Consent = read()
export const useConsent = () => useSyncExternalStore(cb => { listeners.add(cb); return () => listeners.delete(cb) }, () => cached)
