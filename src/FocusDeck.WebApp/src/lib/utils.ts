import { type ClassValue, clsx } from 'clsx'

export function cn(...inputs: ClassValue[]) {
  return clsx(inputs)
}

export function formatDate(date: Date): string {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(date)
}

export function formatTime(date: Date): string {
  return new Intl.DateTimeFormat('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  }).format(date)
}

export function formatDuration(seconds: number): string {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)

  if (hours > 0) {
    return `${hours}h ${minutes}m`
  }
  return `${minutes}m`
}

// ========================================
// AUTH UTILITIES
// ========================================

let cachedToken: string | null = null;

export async function getAuthToken(): Promise<string> {
  if (cachedToken) return cachedToken
  const storedToken = localStorage.getItem('focusdeck_access_token')
  if (storedToken) {
    cachedToken = storedToken
    return storedToken
  }
  // No token — redirect to login
  if (typeof window !== 'undefined') {
    const loc = window.location
    if (!loc.pathname.includes('/login')) {
      window.location.href = '/login'
    }
  }
  throw new Error('Not authenticated')
}

export async function apiFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const token = await getAuthToken();
  
  return fetch(url, {
    ...options,
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });
}

export function storeTokens(accessToken: string, refreshToken?: string, userId?: string) {
  cachedToken = accessToken
  localStorage.setItem('focusdeck_access_token', accessToken)
  if (refreshToken) localStorage.setItem('focusdeck_refresh_token', refreshToken)
  if (userId) localStorage.setItem('focusdeck_user', userId)
}

export async function logout() {
  try {
    await fetch('/v1/auth/logout', { method: 'POST', headers: { 'Authorization': `Bearer ${await getAuthToken()}` } })
  } catch {}
  cachedToken = null
  localStorage.removeItem('focusdeck_access_token')
  localStorage.removeItem('focusdeck_refresh_token')
  localStorage.removeItem('focusdeck_user')
  if (typeof window !== 'undefined') window.location.href = '/login'
}

export function parseJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const payload = token.split('.')[1]
    if (!payload) return null
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(decodeURIComponent(encodeURIComponent(decoded)))
  } catch {
    return null
  }
}

export function getTenantIdFromToken(): string | null {
  const token = localStorage.getItem('focusdeck_access_token')
  if (!token) {
    return null
  }
  const payload = parseJwtPayload(token)
  const tenantId = payload?.['app_tenant_id'] ?? payload?.['tenant_id']
  return typeof tenantId === 'string' ? tenantId : null
}
