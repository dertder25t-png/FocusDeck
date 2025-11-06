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
let tokenPromise: Promise<string> | null = null;

async function generateToken(): Promise<string> {
  const response = await fetch('http://192.168.1.110:5000/v1/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'web-user', password: 'devmode', clientId: 'web-app' })
  });

  if (!response.ok) {
    throw new Error('Failed to generate auth token');
  }

  const data = await response.json();
  cachedToken = data.accessToken;
  localStorage.setItem('focusdeck_access_token', data.accessToken);
  localStorage.setItem('focusdeck_user', 'web-user');
  
  return data.accessToken;
}

export async function getAuthToken(): Promise<string> {
  // Return cached token if available
  if (cachedToken) return cachedToken;

  // Check localStorage
  const storedToken = localStorage.getItem('focusdeck_access_token');
  if (storedToken) {
    cachedToken = storedToken;
    return storedToken;
  }

  // Generate new token (prevent duplicate requests)
  if (!tokenPromise) {
    tokenPromise = generateToken().finally(() => {
      tokenPromise = null;
    });
  }

  return tokenPromise;
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