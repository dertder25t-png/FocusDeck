import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
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

let cachedToken: string | null = null
const ACCESS_COOKIE_NAME = 'focusdeck_access_token'
const REFRESH_COOKIE_NAME = 'focusdeck_refresh_token'

function isBrowser(): boolean {
  return typeof window !== 'undefined' && typeof document !== 'undefined'
}

function isSecureOrigin(): boolean {
  if (!isBrowser()) return false
  return window.location.protocol === 'https:'
}

function setCookie(name: string, value: string, expires?: Date | null) {
  if (!isBrowser()) return
  let cookie = `${name}=${encodeURIComponent(value)}; Path=/; SameSite=Lax`
  if (expires) {
    cookie += `; Expires=${expires.toUTCString()}`
  }
  if (isSecureOrigin()) {
    cookie += '; Secure'
  }
  document.cookie = cookie
}

function deleteCookie(name: string) {
  if (!isBrowser()) return
  document.cookie = `${name}=; Path=/; Expires=Thu, 01 Jan 1970 00:00:00 GMT; SameSite=Lax`
}

function getCookie(name: string): string | null {
  if (!isBrowser()) return null
  const cookies = document.cookie ? document.cookie.split('; ') : []
  for (const cookie of cookies) {
    if (cookie.startsWith(`${name}=`)) {
      return decodeURIComponent(cookie.substring(name.length + 1))
    }
  }
  return null
}

function getTokenExpiryDate(token: string): Date | null {
  const payload = parseJwtPayload(token)
  const exp = payload?.['exp']
  if (typeof exp === 'number') {
    return new Date(exp * 1000)
  }
  if (typeof exp === 'string') {
    const parsed = Number(exp)
    if (!Number.isNaN(parsed)) {
      return new Date(parsed * 1000)
    }
  }
  return null
}

function persistAccessToken(token: string) {
  try {
    localStorage.setItem('focusdeck_access_token', token)
  } catch (error) {
    console.warn('Unable to persist access token to localStorage', error)
  }
  setCookie(ACCESS_COOKIE_NAME, token, getTokenExpiryDate(token))
}

function persistRefreshToken(token?: string) {
  if (!token) {
    try {
      localStorage.removeItem('focusdeck_refresh_token')
    } catch (error) {
      console.warn('Unable to remove refresh token from localStorage', error)
    }
    deleteCookie(REFRESH_COOKIE_NAME)
    return
  }

  try {
    localStorage.setItem('focusdeck_refresh_token', token)
  } catch (error) {
    console.warn('Unable to persist refresh token to localStorage', error)
  }
  setCookie(REFRESH_COOKIE_NAME, token, getTokenExpiryDate(token))
}

export async function getAuthToken(): Promise<string> {
  if (cachedToken) return cachedToken

  let storedToken: string | null = null
  try {
    storedToken = localStorage.getItem('focusdeck_access_token')
  } catch {
    storedToken = null
  }

  if (storedToken) {
    cachedToken = storedToken
    return storedToken
  }

  const cookieToken = getCookie(ACCESS_COOKIE_NAME)
  if (cookieToken) {
    cachedToken = cookieToken
    try {
      localStorage.setItem('focusdeck_access_token', cookieToken)
    } catch (error) {
      console.warn('Unable to persist cookie token to localStorage', error)
    }
    return cookieToken
  }

  // No token — redirect to login
  if (isBrowser()) {
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
  persistAccessToken(accessToken)
  persistRefreshToken(refreshToken)
  if (userId) {
    try {
      localStorage.setItem('focusdeck_user', userId)
    } catch (error) {
      console.warn('Unable to persist user info to localStorage', error)
    }
  }
}

export async function logout() {
  try {
    await fetch('/v1/auth/logout', { method: 'POST', headers: { 'Authorization': `Bearer ${await getAuthToken()}` } })
  } catch (error) {
    console.warn('Logout request failed', error)
  }
  cachedToken = null
  try {
    localStorage.removeItem('focusdeck_access_token')
    localStorage.removeItem('focusdeck_refresh_token')
    localStorage.removeItem('focusdeck_user')
  } catch (error) {
    console.warn('Unable to clear auth localStorage entries', error)
  }
  deleteCookie(ACCESS_COOKIE_NAME)
  deleteCookie(REFRESH_COOKIE_NAME)
  if (isBrowser()) window.location.href = '/login'
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