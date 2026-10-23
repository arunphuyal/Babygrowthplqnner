const API = import.meta.env.VITE_API_URL ?? ''
const KEY = 'pt_admin_token'

export const auth = {
  get: () => sessionStorage.getItem(KEY),
  set: (t: string) => sessionStorage.setItem(KEY, t),
  clear: () => sessionStorage.removeItem(KEY),
}

export async function api<T = unknown>(path: string, init: RequestInit = {}): Promise<T> {
  const res = await fetch(`${API}/api${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...(auth.get() ? { Authorization: `Bearer ${auth.get()}` } : {}), ...init.headers },
  })
  if (res.status === 401) { auth.clear(); window.location.href = '/login'; throw new Error('Unauthorized') }
  if (!res.ok) {
    const body = await res.json().catch(() => ({}))
    throw new Error(body.error ?? `Request failed (${res.status})`)
  }
  return res.status === 204 ? (undefined as T) : res.json()
}

export interface User { id: string; email: string; displayName: string; role: 'Parent' | 'Admin'; plan: 'Free' | 'Premium' | 'Family'; isActive: boolean; createdAt: string; lastLoginAt?: string }
export interface Article { id: string; slug: string; title: string; summary: string; body: string; isPremium: boolean; isPublished: boolean; updatedAt: string }
export interface PlanOffer { id: string; plan: string; name: string; monthlyPrice: number; yearlyPrice: number; features: string; isActive: boolean }
export interface Stats { totalUsers: number; activeLast7Days: number; payingUsers: number; estimatedMrr: number; totalChildren: number; logsLast7Days: number; waitlist: number; signupsLast30Days: { date: string; count: number }[] }
