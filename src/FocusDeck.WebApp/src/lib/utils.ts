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

// Cached token is intentionally NOT persisted across page reloads
// Each page load should re-read from localStorage/cookies
let cachedToken: string | null = null
const ACCESS_COOKIE_NAME = 'focusdeck_access_token'
const REFRESH_COOKIE_NAME = 'focusdeck_refresh_token'

// Clear cached token on module load to force fresh read after login reload
if (typeof window !== 'undefined') {
  cachedToken = null
}

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

  // No token found - just throw error, don't redirect (let React Router handle that)
  throw new Error('Not authenticated')
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
  
  // Only redirect if not already on login/register pages to prevent infinite loops
  if (isBrowser()) {
    const currentPath = window.location.pathname
    if (currentPath !== '/login' && currentPath !== '/register') {
      window.location.href = '/login'
    }
  }
}

interface RefreshResponse {
  accessToken: string;
  refreshToken: string;
}

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason?: any) => void;
}> = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

export async function apiFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const isAbsolute = /^https?:\/\//i.test(url);
  const path = isAbsolute ? new URL(url).pathname : url;
  const isProtected = path.startsWith('/v1/') && !path.startsWith('/v1/auth/');

  let token = await getAuthToken().catch(() => null);

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (token && isProtected) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  // Always include credentials so auth cookies (if any) are sent with requests
  let response = await fetch(url, { ...options, headers, credentials: 'include' });

  // If we get a 401 on a protected endpoint, try to refresh
  if (response.status === 401 && isProtected) {
    if (isRefreshing) {
      // If already refreshing, queue this request to retry later
      return new Promise((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      })
        .then((newToken) => {
          (headers as Record<string, string>)['Authorization'] = `Bearer ${newToken}`;
          return fetch(url, { ...options, headers, credentials: 'include' });
        })
        .catch((err) => {
          return Promise.reject(err);
        });
    }

    const refreshToken = localStorage.getItem('focusdeck_refresh_token');
    const accessToken = localStorage.getItem('focusdeck_access_token');

    if (!refreshToken) {
      // No refresh token available, logout truly required
      await logout();
      throw new Error('Session expired');
    }

    isRefreshing = true;

    try {
      // Attempt to refresh tokens - send both access and refresh tokens
      const refreshRes = await fetch('/v1/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
          accessToken: accessToken || '',
          refreshToken,
          clientId: navigator.userAgent,
          deviceName: navigator.userAgent,
          devicePlatform: 'web'
        }),
        credentials: 'include'
      });

      if (!refreshRes.ok) {
        throw new Error('Refresh failed');
      }

      const data: RefreshResponse = await refreshRes.json();

      // Store new tokens
      storeTokens(data.accessToken, data.refreshToken);

      // Mark refresh as complete
      isRefreshing = false;

      // Process queued requests with new token
      processQueue(null, data.accessToken);

      // Retry original request
      (headers as Record<string, string>)['Authorization'] = `Bearer ${data.accessToken}`;
      return fetch(url, { ...options, headers, credentials: 'include' });

    } catch (err) {
      // Refresh failed (token revoked or expired), force logout
      processQueue(err, null);
      isRefreshing = false;
      
      // Only logout if not already on auth pages to prevent infinite loops
      const currentPath = window.location.pathname;
      if (currentPath !== '/login' && currentPath !== '/register') {
        await logout();
      }
      throw err;
    }
  }

  return response;
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
