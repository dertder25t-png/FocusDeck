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

function isBrowser(): boolean {
  return typeof window !== 'undefined' && typeof document !== 'undefined'
}

export async function logout() {
  try {
    // Call the backend to clear the cookie
    await fetch('/v1/auth/logout', { method: 'POST' });
  } catch (error) {
    console.warn('Logout request failed', error)
  }

  // Clear user info
  if (isBrowser()) {
    try {
      localStorage.removeItem('focusdeck_user')
    } catch { /* ignore */ }
  }

  // Only redirect if not already on login/register pages to prevent infinite loops
  if (isBrowser()) {
    const currentPath = window.location.pathname
    if (currentPath !== '/login' && currentPath !== '/register') {
      window.location.href = '/login'
    }
  }
}

export async function apiFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const isAbsolute = /^https?:\/\//i.test(url);
  // If the URL is absolute and pointing to a different domain, we might not want to send credentials.
  // However, for this app, we assume backend is same origin or CORS allowed with credentials.

  if (isAbsolute) {
      // Just to silence unused variable warning in case we need it later
  }

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  // Always include credentials so auth cookies are sent with requests
  const response = await fetch(url, { ...options, headers, credentials: 'include' });

  // If we get a 401 on any endpoint, it means the cookie is invalid/expired
  if (response.status === 401) {
    // Only logout if not already on auth pages to prevent infinite loops
    if (isBrowser()) {
       const currentPath = window.location.pathname;
       if (currentPath !== '/login' && currentPath !== '/register') {
         await logout();
       }
    }
    throw new Error('Session expired');
  }

  return response;
}

// Backward compatibility for existing code.
// In Cookie Auth, we don't need to manage tokens, but we might still need to store user ID.
// Arguments are kept to match signature but tokens are ignored.
export function storeTokens(accessToken: string, refreshToken?: string, userId?: string) {
  if (accessToken || refreshToken) {
      // Ignore tokens
  }
  if (userId) {
    try {
      localStorage.setItem('focusdeck_user', userId)
    } catch (error) {
      console.warn('Unable to persist user info to localStorage', error)
    }
  }
}

// Stub for getting auth token. Returns a dummy value.
export async function getAuthToken(): Promise<string> {
  return Promise.resolve("");
}

// For SignalR which calls this.
export async function getOrRefreshAuthToken(): Promise<string | null> {
    return Promise.resolve(null);
}

// Stub for refresh token
export async function refreshAuthToken(): Promise<string | null> {
    return Promise.resolve(null);
}

export function getTenantIdFromToken(): string | null {
  return null;
}
