import { useState } from 'react';
import { cn } from '../../lib/utils';

export function JarvisSidebar() {
  const [isPinned, setIsPinned] = useState(false);
  const [isHovered, setIsHovered] = useState(false);

  const isOpen = isPinned || isHovered;

  return (
    <>
      {/* Hover Trigger Area */}
      <div
        className="w-4 h-full fixed right-0 top-0 z-40"
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      />

      {/* Sidebar */}
      <aside
        className={cn(
          'fixed top-0 right-0 h-full w-96 bg-surface/80 backdrop-blur-lg border-l border-border shadow-2xl transition-transform duration-300 ease-in-out z-50 transform',
          isOpen ? 'translate-x-0' : 'translate-x-full'
        )}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        <div className="p-4 h-full flex flex-col">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold">Jarvis</h2>
            <button
              onClick={() => setIsPinned(!isPinned)}
              className="p-2 rounded-md hover:bg-surface-100"
              title={isPinned ? 'Unpin' : 'Pin'}
            >
              {isPinned ? '📌' : '📍'}
            </button>
          </div>
          <div className="flex-1 overflow-y-auto">
            {/* Jarvis content placeholder */}
            <p className="text-sm text-gray-400">Context-aware AI assistant.</p>
          </div>
        </div>
      </aside>
    </>
  );
}
