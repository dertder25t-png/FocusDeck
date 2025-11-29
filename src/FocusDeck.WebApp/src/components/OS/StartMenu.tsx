import React from 'react';
import { useWindowManager, WindowId } from '../../contexts/WindowManagerContext';

interface StartMenuProps {
  isOpen: boolean;
  onClose: () => void;
}

export const StartMenu: React.FC<StartMenuProps> = ({ isOpen, onClose }) => {
  const { launchApp, openWorkspace } = useWindowManager();

  // Handle click outside is usually better done in Layout, but for now relying on styling/modals
  // We can add a click listener to document if needed, but existing logic might suffice

  const menuClass = `fixed bottom-14 left-0 w-full md:bottom-16 md:left-4 md:w-[650px] bg-white border-t-2 md:border-2 border-ink shadow-hard rounded-t-xl md:rounded-lg overflow-hidden flex flex-col z-50 h-[70vh] md:h-[550px] transition-all duration-200 origin-bottom-left ${isOpen ? 'scale-100 opacity-100 pointer-events-auto translate-y-0' : 'scale-95 opacity-0 pointer-events-none translate-y-5'}`;

  const handleLaunch = (id: WindowId) => {
    launchApp(id);
    onClose();
  };

  const handleWorkspace = (type: 'work' | 'school') => {
      openWorkspace(type);
      onClose();
  };

  return (
    <div id="start-menu" className={menuClass}>
      <div className="h-16 border-b border-gray-200 p-4 flex items-center gap-3 shrink-0 bg-white">
        <i className="fa-solid fa-magnifying-glass text-gray-400"></i>
        <input type="text" placeholder="Search apps, files..." className="flex-1 outline-none text-sm font-medium placeholder-gray-400 h-full" />
        <div className="md:hidden"><button onClick={onClose}><i className="fa-solid fa-chevron-down"></i></button></div>
      </div>

      <div className="flex flex-1 overflow-hidden flex-col md:flex-row">
        {/* Sidebar */}
        <div className="w-full md:w-48 bg-ink text-gray-300 flex flex-row md:flex-col py-4 px-4 items-center md:items-start shrink-0 md:border-r md:border-gray-800">
          <div className="flex items-center gap-3 mb-0 md:mb-6">
            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-accent-purple to-accent-blue flex items-center justify-center text-white font-bold text-lg">JD</div>
            <div className="text-white font-bold text-sm">John Doe</div>
          </div>
          <div className="flex md:flex-col gap-1 ml-auto md:ml-0 w-full md:w-auto">
             <button onClick={() => handleLaunch('win-dashboard')} className="w-full text-left px-3 py-2 rounded hover:bg-gray-800 text-xs font-medium flex items-center gap-3"><i className="fa-solid fa-grid-2 w-4"></i> <span className="hidden md:inline">Dashboard</span></button>
             <button onClick={() => handleLaunch('win-jarvis')} className="w-full text-left px-3 py-2 rounded hover:bg-gray-800 text-xs font-medium flex items-center gap-3"><i className="fa-solid fa-robot w-4"></i> <span className="hidden md:inline">Jarvis</span></button>
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 bg-subtle overflow-y-auto p-2 md:p-4">
            <div className="px-2 py-2 text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-2">Your Workspaces</div>

            <div className="mb-2 bg-white rounded-lg border border-transparent hover:border-gray-200 shadow-sm overflow-hidden transition-all cursor-pointer hover:bg-gray-50" onClick={() => handleWorkspace('work')}>
                <div className="flex items-center px-4 py-3">
                    <div className="w-8 h-8 rounded bg-blue-100 flex items-center justify-center text-blue-600 mr-3"><i className="fa-solid fa-briefcase"></i></div>
                    <div className="flex-1"><div className="text-sm font-bold text-ink">Work Mode</div></div>
                </div>
            </div>

            <div className="mb-2 bg-white rounded-lg border border-transparent hover:border-gray-200 shadow-sm overflow-hidden transition-all cursor-pointer hover:bg-gray-50" onClick={() => handleWorkspace('school')}>
                <div className="flex items-center px-4 py-3">
                    <div className="w-8 h-8 rounded bg-yellow-100 flex items-center justify-center text-yellow-600 mr-3"><i className="fa-solid fa-graduation-cap"></i></div>
                    <div className="flex-1"><div className="text-sm font-bold text-ink">School Mode</div></div>
                </div>
            </div>
        </div>
      </div>
    </div>
  );
};
