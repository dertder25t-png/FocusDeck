import { BrowserRouter, Routes, Route, NavLink, Navigate, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useEffect, useMemo } from 'react'
import { LoginPage } from './pages/Auth/LoginPage'
import { RegisterPage } from './pages/Auth/RegisterPage'
import { ToastProvider, ToastViewport } from './components/Toast'
import { DashboardPage } from './pages/DashboardPage'
import { LecturesPage } from './pages/LecturesPage'
import { FocusPage } from './pages/FocusPage'
import { NotesPage } from './pages/NotesPage'
import { DesignPage } from './pages/DesignPage'
import { AnalyticsPage } from './pages/AnalyticsPage'
import { SettingsPage } from './pages/SettingsPage'
import { TenantsPage } from './pages/TenantsPage'
import { JobsPage } from './pages/JobsPage'
import { cn } from './lib/utils'
import { DevicesPage } from './pages/DevicesPage'
import { logout } from './lib/utils'
import { PairingPage } from './pages/Auth/PairingPage'
import ProvisioningPage from './pages/ProvisioningPage'
import { useCurrentTenant } from './hooks/useCurrentTenant'
import { ProtectedRoute } from './pages/Auth/ProtectedRoute'

type NavigationItem = { name: string; path: string; icon: string; exact?: boolean }

function AppLayout() {
  const location = useLocation()
  const navigate = useNavigate()
  const isOnline = true
  const { tenant, loading: tenantLoading, error: tenantError, refresh: refreshTenant } = useCurrentTenant()

  const navigation: NavigationItem[] = useMemo(() => [
    { name: 'Dashboard', path: '/', icon: '📊', exact: true },
    { name: 'Lectures', path: '/lectures', icon: '🎓' },
    { name: 'Focus', path: '/focus', icon: '⚡' },
    { name: 'Notes', path: '/notes', icon: '📝' },
    { name: 'Design', path: '/design', icon: '🎨' },
    { name: 'Analytics', path: '/analytics', icon: '📈' },
    { name: 'Devices', path: '/devices', icon: '💻' },
    { name: 'Pairing', path: '/pairing', icon: '🔗' },
    { name: 'Provisioning', path: '/provisioning', icon: '📱' },
  ], [])

  const activeItem = useMemo(() => {
    return navigation.find(item =>
      item.exact ? location.pathname === item.path : location.pathname.startsWith(item.path)
    )
  }, [location.pathname, navigation])

  useEffect(() => {
    refreshTenant()
  }, [location.pathname, refreshTenant])

  return (
    <div className="flex h-screen bg-surface text-white">
      {/* Sidebar Navigation */}
      <aside className="w-64 bg-surface-100 border-r border-gray-800 flex flex-col" role="navigation" aria-label="Main navigation">
        <div className="p-4 border-b border-gray-800 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="text-2xl" aria-hidden="true">🎯</div>
            <span className="text-lg font-semibold">FocusDeck</span>
          </div>
          <div
            className={cn(
              'w-2 h-2 rounded-full',
              isOnline ? 'bg-green-500' : 'bg-red-500'
            )}
            title={isOnline ? 'Server Online' : 'Server Offline'}
            role="status"
            aria-label={isOnline ? 'Server Online' : 'Server Offline'}
          />
        </div>

        <nav className="flex-1 p-4 space-y-1">
          {/* Skip to content link for accessibility */}
          <a
            href="#main-content"
            className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 focus:z-50 focus:p-4 focus:bg-primary focus:text-white focus:rounded-md"
          >
            Skip to content
          </a>

          {navigation.map((item) => (
            <NavLink
              key={item.path}
              to={item.path}
              className={({ isActive }) =>
                cn(
                  'w-full flex items-center gap-3 px-4 py-3 rounded-md font-medium transition-colors',
                  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary',
                  isActive ? 'bg-primary/10 text-primary' : 'text-gray-300 hover:bg-gray-800/50'
                )
              }
            >
              <span className="text-xl" aria-hidden="true">{item.icon}</span>
              <span>{item.name}</span>
            </NavLink>
          ))}
        </nav>

        <div className="p-4 border-t border-gray-800">
          <NavLink
            to="/settings"
            className={({ isActive }) =>
              cn(
                'w-full flex items-center gap-3 px-4 py-3 rounded-md font-medium transition-colors',
                'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary',
                isActive ? 'bg-primary/10 text-primary' : 'text-gray-300 hover:bg-gray-800/50'
              )
            }
          >
            <span className="text-xl" aria-hidden="true">⚙️</span>
            <span>Settings</span>
          </NavLink>
        </div>
      </aside>

      {/* Main Content Area */}
      <main className="flex-1 flex flex-col overflow-hidden">
        {/* Top Bar */}
        <header className="h-16 border-b border-gray-800 flex items-center justify-between px-6" role="banner">
          <div className="flex items-center gap-4">
            <h1 className="text-lg font-semibold sr-only">{activeItem?.name ?? 'FocusDeck'}</h1>
          </div>
          <div className="flex items-center gap-4">
            <button
              className="p-2 hover:bg-gray-800 rounded-md transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
              aria-label="Search"
            >
              <span className="text-xl">🔍</span>
            </button>
            <button
              onClick={() => navigate('/tenants')}
              className="flex items-center gap-2 rounded-full border border-gray-700 bg-gray-900/70 px-3 py-1.5 text-sm text-gray-200 hover:bg-gray-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
              title={tenant?.name ?? 'Select tenant'}
            >
              {tenant ? (
                <>
                  <span className="font-medium">{tenant.name}</span>
                  <span className="text-xs text-gray-400">/{tenant.slug}</span>
                </>
              ) : tenantLoading ? (
                <span className="text-xs text-gray-400">Resolving tenant…</span>
              ) : tenantError ? (
                <span className="text-xs text-red-300">Tenant error</span>
              ) : (
                <span className="text-xs text-gray-400">Select tenant</span>
              )}
            </button>
            <button
              onClick={() => logout()}
              className="px-3 py-2 hover:bg-gray-800 rounded-md transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
              aria-label="Logout"
              title="Logout"
            >
              Logout
            </button>
          </div>
        </header>

        {/* Content */}
        <div
          id="main-content"
          className="flex-1 overflow-auto p-6"
          role="main"
          tabIndex={-1}
        >
          <div className="max-w-7xl mx-auto">
            <Outlet />
          </div>
        </div>
      </main>
    </div>
  )
}

function App() {
  return (
    <ToastProvider>
      <BrowserRouter>
        <Routes>
          {/* Public Routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          
          {/* Protected Routes - All require authentication */}
          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              {/* Dashboard */}
              <Route index element={<DashboardPage />} />
              
              {/* Main Features */}
              <Route path="lectures" element={<LecturesPage />} />
              <Route path="focus" element={<FocusPage />} />
              <Route path="notes" element={<NotesPage />} />
              <Route path="design" element={<DesignPage />} />
              <Route path="analytics" element={<AnalyticsPage />} />
              
              {/* Settings & Management */}
              <Route path="settings" element={<SettingsPage />} />
              <Route path="devices" element={<DevicesPage />} />
              <Route path="pairing" element={<PairingPage />} />
              <Route path="provisioning" element={<ProvisioningPage />} />
              
              {/* Admin Routes */}
              <Route path="tenants" element={<TenantsPage />} />
              <Route path="jobs" element={<JobsPage />} />
            </Route>
          </Route>
          
          {/* Fallback: Redirect unknown paths to home */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
        <ToastViewport />
      </BrowserRouter>
    </ToastProvider>
  )
}

export default App
