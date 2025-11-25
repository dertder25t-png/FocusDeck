import { Outlet } from 'react-router-dom';
import { JarvisSidebar } from './Jarvis/JarvisSidebar';

export function AppShell() {
  return (
    <div className="flex h-screen bg-background text-foreground">
      {/* Left Sidebar */}
      <aside className="w-72 flex-shrink-0 border-r border-border bg-surface/50">
        <div className="p-4">
          <h2 className="text-lg font-semibold">Workspaces</h2>
          {/* Workspace Switcher Placeholder */}
        </div>
        <div className="p-4">
          <h3 className="text-md font-semibold">Projects</h3>
          {/* Project Tree Placeholder */}
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto p-6">
        <Outlet />
      </main>

      {/* Right Sidebar (Jarvis) */}
      <JarvisSidebar />
    </div>
  );
}
