import { Outlet, useNavigate, NavLink } from 'react-router-dom';
import { JarvisSidebar } from './Jarvis/JarvisSidebar';
import { useCurrentTenant } from '../hooks/useCurrentTenant';
import { logout } from '../lib/utils';
import { cn } from '../lib/utils';
import { useState } from 'react';

const workspaces = [
  { id: '1', name: 'Personal' },
  { id: '2', name: 'Work' },
];

const projects = {
  '1': [{ id: '1', name: 'Project A' }, { id: '2', name: 'Project B' }],
  '2': [{ id: '3', name: 'Project C' }, { id: '4', name: 'Project D' }],
};

export function AppShell() {
  const navigate = useNavigate();
  const { tenant, loading: tenantLoading, error: tenantError } = useCurrentTenant();
  const [selectedWorkspace, setSelectedWorkspace] = useState(workspaces[0]);

  return (
    <div className="flex h-screen bg-background text-foreground">
      {/* Left Sidebar */}
      <aside className="w-72 flex-shrink-0 border-r border-border bg-surface/50">
        <div className="p-4">
          <h2 className="text-lg font-semibold">Workspaces</h2>
          <select
            value={selectedWorkspace.id}
            onChange={(e) => setSelectedWorkspace(workspaces.find(w => w.id === e.target.value))}
            className="w-full mt-2 p-2 rounded-md bg-surface-100 border border-border"
          >
            {workspaces.map(workspace => (
              <option key={workspace.id} value={workspace.id}>{workspace.name}</option>
            ))}
          </select>
        </div>
        <div className="p-4">
          <h3 className="text-md font-semibold">Projects</h3>
          <nav className="mt-2 space-y-1">
            {projects[selectedWorkspace.id].map(project => (
              <NavLink
                key={project.id}
                to={`/projects/${project.id}/board`}
                className={({ isActive }) =>
                  cn(
                    'block px-4 py-2 rounded-md',
                    isActive ? 'bg-primary/10 text-primary' : 'hover:bg-surface-100'
                  )
                }
              >
                {project.name}
              </NavLink>
            ))}
          </nav>
        </div>
        <div className="p-4 mt-auto">
          <NavLink
            to="/customize"
            className={({ isActive }) =>
              cn(
                'block px-4 py-2 rounded-md',
                isActive ? 'bg-primary/10 text-primary' : 'hover:bg-surface-100'
              )
            }
          >
            Customize
          </NavLink>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 flex flex-col overflow-hidden">
        {/* Top Bar */}
        <header className="h-16 border-b border-border flex items-center justify-between px-6" role="banner">
          <div className="flex items-center gap-4">
            <h1 className="text-lg font-semibold sr-only">FocusDeck</h1>
          </div>
          <div className="flex items-center gap-4">
            <button
              className="p-2 hover:bg-surface-100 rounded-md transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
              aria-label="Search"
            >
              <span className="text-xl">🔍</span>
            </button>
            <button
              onClick={() => navigate('/tenants')}
              className="flex items-center gap-2 rounded-full border border-border bg-surface/70 px-3 py-1.5 text-sm text-foreground hover:bg-surface-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
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
              className="px-3 py-2 hover:bg-surface-100 rounded-md transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
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

      {/* Right Sidebar (Jarvis) */}
      <JarvisSidebar />
    </div>
  );
}
