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
      console.warn('No valid authentication token found')
    }
  }, [location.pathname, location.search, location.hash])

  if (checking) {
    return (
      <div className="flex h-screen items-center justify-center bg-gray-950 text-gray-400">
        <div className="text-center">
          <div className="mb-4">Verifying your session…</div>
          <div className="w-8 h-8 border-2 border-primary/30 border-t-primary rounded-full animate-spin mx-auto" />
        </div>
      </div>
    )
  }

  if (!isAuthed) {
    // Redirect to login with the requested path so we can return them after auth
    const redirectTo = location.pathname + location.search + location.hash
    console.log(`Not authenticated. Redirecting from ${location.pathname} to /login`)
    return <Navigate to="/login" state={{ from: redirectTo }} replace />
  }

  return <Outlet />
}

// Helper to check if JWT token is expired
function isTokenExpired(token: string): boolean {
  try {
    // JWT format: header.payload.signature
    const parts = token.split('.')
    if (parts.length !== 3) return true
    
    const payload = JSON.parse(atob(parts[1]))
    const expiryTime = payload.exp * 1000 // exp is in seconds, convert to ms
    return Date.now() >= expiryTime
  } catch {
    return true
  }
}

