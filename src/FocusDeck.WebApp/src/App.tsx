import { useState } from 'react'

function App() {
  const [isOnline] = useState(true)

  return (
    <div className="flex h-screen bg-surface text-white">
      {/* Sidebar Navigation */}
      <aside className="w-64 bg-surface-100 border-r border-gray-800 flex flex-col">
        <div className="p-4 border-b border-gray-800 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="text-2xl">🎯</div>
            <span className="text-lg font-semibold">FocusDeck</span>
          </div>
          <div
            className={`w-2 h-2 rounded-full ${
              isOnline ? 'bg-green-500' : 'bg-red-500'
            }`}
            title={isOnline ? 'Server Online' : 'Server Offline'}
          />
        </div>

        <nav className="flex-1 p-4 space-y-1">
          <button className="w-full flex items-center gap-3 px-4 py-3 rounded-md bg-primary/10 text-primary hover:bg-primary/20 animation-ease-out">
            <span className="text-xl">📊</span>
            <span className="font-medium">Dashboard</span>
          </button>
          <button className="w-full flex items-center gap-3 px-4 py-3 rounded-md text-gray-300 hover:bg-gray-800/50 animation-ease-out">
            <span className="text-xl">🎓</span>
            <span className="font-medium">Lectures</span>
          </button>
          <button className="w-full flex items-center gap-3 px-4 py-3 rounded-md text-gray-300 hover:bg-gray-800/50 animation-ease-out">
            <span className="text-xl">⚡</span>
            <span className="font-medium">Focus</span>
          </button>
          <button className="w-full flex items-center gap-3 px-4 py-3 rounded-md text-gray-300 hover:bg-gray-800/50 animation-ease-out">
            <span className="text-xl">📝</span>
            <span className="font-medium">Notes</span>
          </button>
          <button className="w-full flex items-center gap-3 px-4 py-3 rounded-md text-gray-300 hover:bg-gray-800/50 animation-ease-out">
            <span className="text-xl">🎨</span>
            <span className="font-medium">Design</span>
          </button>
          <button className="w-full flex items-center gap-3 px-4 py-3 rounded-md text-gray-300 hover:bg-gray-800/50 animation-ease-out">
            <span className="text-xl">📈</span>
            <span className="font-medium">Analytics</span>
          </button>
        </nav>

        <div className="p-4 border-t border-gray-800">
          <button className="w-full flex items-center gap-3 px-4 py-3 rounded-md text-gray-300 hover:bg-gray-800/50 animation-ease-out">
            <span className="text-xl">⚙️</span>
            <span className="font-medium">Settings</span>
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <main className="flex-1 flex flex-col overflow-hidden">
        {/* Top Bar */}
        <header className="h-16 border-b border-gray-800 flex items-center justify-between px-6">
          <div className="flex items-center gap-4">
            <h1 className="text-lg font-semibold">Dashboard</h1>
          </div>
          <div className="flex items-center gap-4">
            <button className="p-2 hover:bg-gray-800 rounded-md animation-ease-out">
              <span className="text-xl">🔍</span>
            </button>
            <button className="p-2 hover:bg-gray-800 rounded-md animation-ease-out">
              <span className="text-xl">👤</span>
            </button>
          </div>
        </header>

        {/* Content */}
        <div className="flex-1 overflow-auto p-6">
          <div className="max-w-6xl mx-auto space-y-6">
            {/* Welcome Card */}
            <div className="bg-gradient-to-r from-primary/20 to-purple-900/20 rounded-lg p-6 border border-primary/20">
              <h2 className="text-2xl font-semibold mb-2">
                Welcome to FocusDeck
              </h2>
              <p className="text-gray-300">
                Your productivity companion for lectures, focus sessions, notes, and design work.
              </p>
            </div>

            {/* Stats Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="bg-surface-100 rounded-lg p-4 border border-gray-800">
                <div className="text-gray-400 text-sm mb-1">Total Lectures</div>
                <div className="text-2xl font-semibold">0</div>
              </div>
              <div className="bg-surface-100 rounded-lg p-4 border border-gray-800">
                <div className="text-gray-400 text-sm mb-1">Focus Time</div>
                <div className="text-2xl font-semibold">0h</div>
              </div>
              <div className="bg-surface-100 rounded-lg p-4 border border-gray-800">
                <div className="text-gray-400 text-sm mb-1">Notes Verified</div>
                <div className="text-2xl font-semibold">0</div>
              </div>
              <div className="bg-surface-100 rounded-lg p-4 border border-gray-800">
                <div className="text-gray-400 text-sm mb-1">Design Projects</div>
                <div className="text-2xl font-semibold">0</div>
              </div>
            </div>

            {/* Recent Activity */}
            <div className="bg-surface-100 rounded-lg p-6 border border-gray-800">
              <h3 className="text-lg font-semibold mb-4">Recent Activity</h3>
              <div className="text-center text-gray-400 py-8">
                No recent activity yet. Start by creating your first lecture or focus session!
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}

export default App
