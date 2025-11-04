import { BrowserRouter, Routes, Route, Link, useLocation } from 'react-router-dom'
import { ToastProvider, ToastViewport } from './components/Toast'
import { DashboardPage } from './pages/DashboardPage'
import { LecturesPage } from './pages/LecturesPage'
import { FocusPage } from './pages/FocusPage'
import { NotesPage } from './pages/NotesPage'
import { DesignPage } from './pages/DesignPage'
import { AnalyticsPage } from './pages/AnalyticsPage'
import { SettingsPage } from './pages/SettingsPage'
import { cn } from './lib/utils'

function AppShell() {
  const location = useLocation()
  const isOnline = true

  const navigation = [
    { name: 'Dashboard', path: '/app', icon: '📊', exact: true },
    { name: 'Lectures', path: '/app/lectures', icon: '🎓' },
    { name: 'Focus', path: '/app/focus', icon: '⚡' },
    { name: 'Notes', path: '/app/notes', icon: '📝' },
    { name: 'Design', path: '/app/design', icon: '🎨' },
    { name: 'Analytics', path: '/app/analytics', icon: '📈' },
  ]

  const isActive = (path: string, exact?: boolean) => {
    if (exact) {
      return location.pathname === path
    }
    return location.pathname.startsWith(path)
  }

  return (
    <ToastProvider>
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
              <Link
                key={item.path}
                to={item.path}
                className={cn(
                  'w-full flex items-center gap-3 px-4 py-3 rounded-md font-medium transition-colors',
                  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary',
                  isActive(item.path, item.exact)
                    ? 'bg-primary/10 text-primary'
                    : 'text-gray-300 hover:bg-gray-800/50'
                )}
                aria-current={isActive(item.path, item.exact) ? 'page' : undefined}
              >
                <span className="text-xl" aria-hidden="true">{item.icon}</span>
                <span>{item.name}</span>
              </Link>
            ))}
          </nav>

          <div className="p-4 border-t border-gray-800">
            <Link
              to="/app/settings"
              className={cn(
                'w-full flex items-center gap-3 px-4 py-3 rounded-md font-medium transition-colors',
                'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary',
                isActive('/app/settings')
                  ? 'bg-primary/10 text-primary'
                  : 'text-gray-300 hover:bg-gray-800/50'
              )}
            >
              <span className="text-xl" aria-hidden="true">⚙️</span>
              <span>Settings</span>
            </Link>
          </div>
        </aside>

        {/* Main Content Area */}
        <main className="flex-1 flex flex-col overflow-hidden">
          {/* Top Bar */}
          <header className="h-16 border-b border-gray-800 flex items-center justify-between px-6" role="banner">
            <div className="flex items-center gap-4">
              <h1 className="text-lg font-semibold sr-only">
                {navigation.find(n => isActive(n.path, n.exact))?.name || 'FocusDeck'}
              </h1>
            </div>
            <div className="flex items-center gap-4">
              <button
                className="p-2 hover:bg-gray-800 rounded-md transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
                aria-label="Search"
              >
                <span className="text-xl">🔍</span>
              </button>
              <button
                className="p-2 hover:bg-gray-800 rounded-md transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
                aria-label="User menu"
              >
                <span className="text-xl">👤</span>
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
              <Routes>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/lectures" element={<LecturesPage />} />
                <Route path="/focus" element={<FocusPage />} />
                <Route path="/notes" element={<NotesPage />} />
                <Route path="/design" element={<DesignPage />} />
                <Route path="/analytics" element={<AnalyticsPage />} />
                <Route path="/settings" element={<SettingsPage />} />
              </Routes>
            </div>
          </div>
        </main>
      </div>
      <ToastViewport />
    </ToastProvider>
  )
}

function App() {
  return (
    <BrowserRouter basename="/app">
      <AppShell />
    </BrowserRouter>
  )
}

export default App
