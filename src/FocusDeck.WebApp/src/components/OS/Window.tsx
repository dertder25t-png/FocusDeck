import React, { useState, useEffect, useRef } from 'react';
import { useWindowManager, APPS } from '../../contexts/WindowManagerContext';
import type { WindowId } from '../../contexts/WindowManagerContext';

interface WindowProps {
  id: WindowId;
  children: React.ReactNode;
  headerChips?: React.ReactNode;
}

export const Window: React.FC<WindowProps> = ({ id, children, headerChips }) => {
  const { openApps, activeApp, splitMode, splitApps, closeApp, focusApp, snapPair } = useWindowManager();
  const [showSplitMenu, setShowSplitMenu] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  const isOpen = openApps.includes(id);
  const isActive = activeApp === id;
  const isSplitLeft = splitMode && splitApps[0] === id;
  const isSplitRight = splitMode && splitApps[1] === id;

  // Class Logic
  let className = "window-app flex flex-col bg-paper dark:bg-gray-900 border-2 border-ink shadow-hard rounded-xl overflow-hidden absolute transition-all duration-300 ";

  if (!isOpen) {
    className += " hidden-app opacity-0 pointer-events-none z-[-1]";
  } else {
    if (isSplitLeft) {
      className += " split-left z-20 w-[calc(50%-8px)] left-0 top-0 bottom-0 rounded-l-xl rounded-r-none border-r border-border";
    } else if (isSplitRight) {
      className += " split-right z-20 w-[calc(50%-8px)] right-0 top-0 bottom-0 rounded-r-xl rounded-l-none border-l border-border";
    } else {
      // Maximized (or 'Over')
      // If split mode is ON but this window is NOT in the split pair, it must be 'Over' (z-30)
      // If split mode is OFF, it is just maximized (z-10 or z-20 if active)

      const isOver = splitMode && (splitApps.length === 0 || !splitApps.includes(id));

      className += " maximized w-full h-full left-0 top-0 ";
      className += isOver ? " z-30" : (isActive ? " z-10" : " z-10");
      // Note: Original HTML z-index logic: maximized=30 (above split), split=20.
      // If we are just browsing single apps, z-index depends on activity.

      if (isActive && !isOver) className += " z-20"; // Bring active to front in normal mode
      else if (!isActive && !isOver) className += " z-10";
    }
  }

  // Handle Split Menu Click Outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setShowSplitMenu(false);
      }
    }
    if (showSplitMenu) document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [showSplitMenu]);

  const handleSnap = (otherId: WindowId) => {
    snapPair(id, otherId);
    setShowSplitMenu(false);
  };

  const availableForSnap = openApps.filter(oid => {
    if (splitApps.length === 0) return oid !== id;
    return oid !== id && !splitApps.includes(oid as WindowId);
  });

  return (
    <div id={id} className={className} onClick={() => focusApp(id)} style={!isOpen ? { display: 'none' } : {}}>
      {/* Smart Header */}
      <div className="h-12 border-b-2 border-border bg-subtle flex items-center justify-between px-4 shrink-0 select-none">
        <div className="flex items-center gap-3 font-bold text-sm text-ink">
          <i className={`fa-solid ${APPS[id].icon} ${id === 'win-dashboard' ? 'text-accent-blue' : (id === 'win-notes' ? 'text-accent-yellow' : '')}`}></i>
          {APPS[id].title}
        </div>

        {/* Smart Chips Container */}
        <div className="flex-1 flex justify-center gap-2 overflow-hidden px-4">
          {headerChips}
        </div>

        <div className="flex gap-2 items-center relative">
          <button className="hover:text-accent-blue px-2 hidden md:inline" onClick={(e) => { e.stopPropagation(); setShowSplitMenu(!showSplitMenu); }}>
            <i className="fa-solid fa-table-columns"></i>
          </button>
          <button className="hover:text-accent-red px-2" onClick={(e) => { e.stopPropagation(); closeApp(id); }}>
            <i className="fa-solid fa-xmark"></i>
          </button>

          {/* Split Menu Dropdown */}
          {showSplitMenu && (
             <div ref={menuRef} className="split-dropdown open absolute top-8 right-0 bg-white border-2 border-ink rounded-lg shadow-hard z-50 w-48">
                <div className="p-1">
                    {availableForSnap.length === 0 ? (
                        <div className="text-center text-gray-400 italic p-2 text-xs">No other free apps</div>
                    ) : (
                        availableForSnap.map(oid => (
                            <button key={oid} onClick={() => handleSnap(oid)} className="w-full text-left flex items-center gap-2 p-2 hover:bg-gray-100 rounded text-xs font-medium text-ink">
                                <i className={`fa-solid ${APPS[oid].icon} text-gray-500`}></i> {APPS[oid].title}
                            </button>
                        ))
                    )}
                </div>
             </div>
          )}
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-hidden relative">
        {children}
      </div>
    </div>
  );
};
