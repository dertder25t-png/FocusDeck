import React from 'react';
import { useWindowManager, APPS } from '../../contexts/WindowManagerContext';

interface TaskbarProps {
  onToggleStart: () => void;
}

export const Taskbar: React.FC<TaskbarProps> = ({ onToggleStart }) => {
  const { openApps, minimizedApps, activeApp, splitMode, splitApps, focusApp, isDarkMode, toggleDarkMode } = useWindowManager();

  const processedIds = new Set<string>();

  return (
    <div className="h-14 bg-subtle dark:bg-gray-900 border-t-2 border-border dark:border-gray-700 flex items-center px-2 md:px-4 gap-2 md:gap-4 z-50 shrink-0 relative shadow-[0_-4px_10px_rgba(0,0,0,0.02)] transition-colors duration-300">
      <button
        onClick={onToggleStart}
        className="h-10 px-4 bg-ink dark:bg-white text-white dark:text-black rounded-lg font-bold text-sm flex items-center gap-2 shadow-hard hover:bg-gray-800 dark:hover:bg-gray-200 transition-all active:scale-95"
      >
        <i className="fa-solid fa-layer-group"></i> <span className="hidden md:inline">Start</span>
      </button>

      <button
        onClick={toggleDarkMode}
        className="h-10 w-10 flex items-center justify-center rounded-lg hover:bg-gray-200 dark:hover:bg-gray-800 transition-colors text-ink dark:text-white"
        title="Toggle Dark Mode"
      >
        <i className={`fa-solid ${isDarkMode ? 'fa-sun' : 'fa-moon'}`}></i>
      </button>

      <div className="w-px h-8 bg-gray-300 dark:bg-gray-700 hidden md:block"></div>

      <div className="flex-1 flex items-end gap-2 overflow-x-auto h-full pb-1 no-scrollbar" id="task-dock">
        {openApps.map(id => {
            if (processedIds.has(id)) return null;

            // Render Split Group
            if (splitMode && splitApps.length === 2 && splitApps.includes(id)) {
                const leftId = splitApps[0];
                const rightId = splitApps[1];
                const isActive = activeApp === leftId || activeApp === rightId;

                processedIds.add(leftId);
                processedIds.add(rightId);

                return (
                    <div
                        key={`${leftId}-${rightId}`}
                        onClick={() => focusApp(leftId)}
                        className={`h-10 px-3 flex items-center gap-2 transition-all cursor-pointer border-t-2 border-transparent rounded-t-lg mr-1 relative group ${isActive ? 'bg-white border-ink shadow-sm -translate-y-[2px]' : 'bg-gray-200/50 text-gray-500 hover:bg-gray-200'}`}
                    >
                        <div className="flex items-center text-xs gap-0 bg-gray-100 px-2 py-1 rounded-full border border-gray-200 relative">
                             <i className={`fa-solid ${APPS[leftId].icon} z-10 relative`}></i>
                             <div className="w-4 h-[1px] bg-gray-400 mx-1"></div>
                             <i className={`fa-solid ${APPS[rightId].icon} z-10 relative`}></i>
                        </div>
                        <span className="text-xs font-bold hidden md:inline ml-1 text-ink">Context Group</span>
                        {isActive && <div className="absolute bottom-0 left-2 right-2 h-[2px] bg-accent-blue"></div>}
                    </div>
                );
            }

            // Render Single App
            processedIds.add(id);
            const isActive = activeApp === id;
            const isMinimized = minimizedApps.includes(id);

            return (
                <button
                    key={id}
                    onClick={() => focusApp(id)}
                    className={`task-tab h-10 px-3 md:px-4 rounded-t-lg flex items-center gap-2 text-sm font-medium transition-all flex-shrink-0 relative
                        ${isActive ? 'active text-ink font-bold bg-white border-t-2 border-ink shadow-sm -translate-y-[2px]' :
                            isMinimized ? 'opacity-50 hover:opacity-100 bg-gray-100 border-t-2 border-transparent hover:bg-white/50' :
                            'text-gray-500 hover:bg-white/50 border-t-2 border-transparent'}`}
                >
                    <i className={`fa-solid ${APPS[id].icon} ${isActive ? 'text-accent-blue' : ''}`}></i>
                    <span className="hidden md:inline">{APPS[id].title}</span>
                    {isActive && <div className="absolute bottom-0 left-2 right-2 h-[2px] bg-accent-blue"></div>}
                    {isMinimized && <div className="absolute bottom-1 left-1/2 transform -translate-x-1/2 w-1 h-1 bg-gray-400 rounded-full"></div>}
                </button>
            );
        })}
      </div>
    </div>
  );
};
