import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useEffect, useState } from 'react'

export function ProtectedRoute() {
  const location = useLocation()
  const [checking, setChecking] = useState(true)
  const [isAuthed, setIsAuthed] = useState(false)

  useEffect(() => {
    const token = localStorage.getItem('focusdeck_access_token')
    setIsAuthed(!!token)
    setChecking(false)
  }, [location.pathname, location.search, location.hash])

  if (checking) {
    return (
      <div className="flex h-screen items-center justify-center bg-surface text-gray-400">
        Checking session…
      </div>
    )
  }

  if (!isAuthed) {
    const redirectTo = location.pathname + location.search + location.hash
    return <Navigate to="/login" state={{ from: redirectTo }} replace />
  }

  return <Outlet />
}

