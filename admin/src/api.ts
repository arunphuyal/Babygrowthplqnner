const API = import.meta.env.VITE_API_URL ?? ''
const KEY = 'pt_admin_token'
const REFRESH_KEY = 'pt_admin_refresh'

export const auth = {
  get: () => sessionStorage.getItem(KEY),
  set: (token: string, refresh?: string) => { sessionStorage.setItem(KEY, token); if (refresh) sessionStorage.setItem(REFRESH_KEY, refresh) },
  refreshToken: () => sessionStorage.getItem(REFRESH_KEY),
  clear: () => { sessionStorage.removeItem(KEY); sessionStorage.removeItem(REFRESH_KEY) },
}

// One in-flight refresh at a time: refresh tokens are single-use, so parallel refreshes would trip reuse detection.
let refreshing: Promise<boolean> | null = null
function refreshSession(): Promise<boolean> {
  refreshing ??= (async () => {
    const refresh = auth.refreshToken()
    if (!refresh) return false
    const res = await fetch(`${API}/api/auth/refresh`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ refreshToken: refresh }) })
    if (!res.ok) return false
    const body = await res.json()
    auth.set(body.token, body.refreshToken)
    return true
  })().finally(() => { refreshing = null })
  return refreshing
}

export async function api<T = unknown>(path: string, init: RequestInit = {}, retried = false): Promise<T> {
  const res = await fetch(`${API}/api${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...(auth.get() ? { Authorization: `Bearer ${auth.get()}` } : {}), ...init.headers },
  })
  if (res.status === 401 && !path.startsWith('/auth/')) {
    if (!retried && await refreshSession()) return api<T>(path, init, true)
    auth.clear(); window.location.href = '/login'; throw new Error('Unauthorized')
  }
  if (!res.ok) {
    const body = await res.json().catch(() => ({}))
    throw new Error(body.error ?? `Request failed (${res.status})`)
  }
  return res.status === 204 ? (undefined as T) : res.json()
}

export interface User { id: string; email: string; displayName: string; role: 'Parent' | 'Admin'; plan: 'Free' | 'Premium' | 'Family'; planExpiresAt?: string; isActive: boolean; createdAt: string; lastLoginAt?: string }
export interface Article { id: string; slug: string; title: string; summary: string; body: string; isPremium: boolean; isPublished: boolean; updatedAt: string }
export interface PlanOffer { id: string; plan: string; name: string; monthlyPrice: number; yearlyPrice: number; features: string; isActive: boolean }
export interface Stats { totalUsers: number; activeLast7Days: number; payingUsers: number; estimatedMrr: number; totalChildren: number; logsLast7Days: number; waitlist: number; donationsCount: number; donationsTotalCents: number; activeChildren7d: number; sharedChildren: number; logsByType30d: { type: string; count: number }[]; signupsLast30Days: { date: string; count: number }[] }
