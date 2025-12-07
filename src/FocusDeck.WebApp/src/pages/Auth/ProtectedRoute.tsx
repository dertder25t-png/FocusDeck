import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useEffect, useState, useCallback } from 'react'
import { useCurrentTenant } from '../../hooks/useCurrentTenant'

export function ProtectedRoute() {
  const location = useLocation()
  const { tenant, loading: tenantLoading, refresh } = useCurrentTenant()
  const [checking, setChecking] = useState(true)
  const [isAuthed, setIsAuthed] = useState(false)

  const verifySession = useCallback(async () => {
      try {
          await refresh(); // This calls /v1/tenants/current
          // If refresh succeeds (no throw) and tenant is set, we are good.
          // However, refresh sets 'tenant' state asynchronously.
          // We can check the tenantLoading state in the render.
          setChecking(false);
      } catch (e) {
          console.warn('Session verification failed', e);
          setChecking(false);
      }
  }, [refresh]);

  useEffect(() => {
    // Only verify once on mount
    verifySession()
  }, [verifySession])

  // React to tenant loading/state changes
  useEffect(() => {
      if (!tenantLoading) {
          if (tenant) {
              setIsAuthed(true);
          } else {
              setIsAuthed(false);
          }
          setChecking(false);
      }
  }, [tenant, tenantLoading]);


  if (checking || tenantLoading) {
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
