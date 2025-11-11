import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useEffect, useState } from 'react'

export function ProtectedRoute() {
  const location = useLocation()
  const [checking, setChecking] = useState(true)
  const [isAuthed, setIsAuthed] = useState(false)

  useEffect(() => {
    // Check for valid JWT token
    const token = localStorage.getItem('focusdeck_access_token')
    const isValid = !!(token && token.length > 0 && !isTokenExpired(token))
    setIsAuthed(isValid)
    setChecking(false)
    
    if (!isValid) {
      console.info('No valid authentication token found, will redirect to login')
    }
  }, [])

  if (checking) {
    return (
      <div className="flex h-screen items-center justify-center bg-gradient-to-br from-surface via-surface-100 to-surface text-gray-400">
        <div className="text-center">
          <div className="text-4xl mb-4">🎯</div>
          <div className="mb-4 font-medium">Verifying your session…</div>
          <div className="w-8 h-8 border-2 border-primary/30 border-t-primary rounded-full animate-spin mx-auto" />
        </div>
      </div>
    )
  }

  if (!isAuthed) {
    // Redirect to login with the requested path as a query parameter
    const redirectTo = location.pathname + location.search + location.hash
    
    // Only redirect if not already on a public page
    if (location.pathname !== '/login' && location.pathname !== '/register') {
      console.info(`Unauthenticated. Redirecting to /login from ${location.pathname}`)
      return <Navigate to={`/login?redirectUrl=${encodeURIComponent(redirectTo)}`} replace />
    }
  }

  return <Outlet />
}

/**
 * Helper to check if JWT token is expired
 * Verifies the token structure and compares expiration time
 */
function isTokenExpired(token: string): boolean {
  try {
    // JWT format: header.payload.signature
    const parts = token.split('.')
    if (parts.length !== 3) {
      console.warn('Invalid token format')
      return true
    }
    
    const payload = JSON.parse(atob(parts[1]))
    
    // Check if exp claim exists
    if (!payload.exp) {
      console.warn('Token missing exp claim')
      return true
    }
    
    const expiryTime = payload.exp * 1000 // exp is in seconds, convert to ms
    const isExpired = Date.now() >= expiryTime
    
    if (isExpired) {
      console.info('Token has expired')
    }
    
    return isExpired
  } catch (error) {
    console.error('Error checking token expiration:', error)
    return true
  }
}

