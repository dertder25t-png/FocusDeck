import React, { createContext, useContext, useState } from 'react';

export type WindowId =
  | 'win-dashboard'
  | 'win-email'
  | 'win-notes'
  | 'win-kanban'
  | 'win-whiteboard'
  | 'win-jarvis'
  | 'win-files'
  | 'win-flashcards'
  | 'win-account-settings';

export type WorkspaceType = 'work' | 'school';

export interface AppDefinition {
  id: WindowId;
  title: string;
  icon: string;
}

export const APPS: Record<WindowId, AppDefinition> = {
  'win-dashboard': { id: 'win-dashboard', title: 'Dashboard', icon: 'fa-grid-2' },
  'win-email': { id: 'win-email', title: 'Email', icon: 'fa-envelope' },
  'win-notes': { id: 'win-notes', title: 'Notes', icon: 'fa-note-sticky' },
  'win-kanban': { id: 'win-kanban', title: 'Kanban', icon: 'fa-list-check' },
  'win-whiteboard': { id: 'win-whiteboard', title: 'Canvas', icon: 'fa-pen-nib' },
  'win-jarvis': { id: 'win-jarvis', title: 'Jarvis', icon: 'fa-robot' },
  'win-files': { id: 'win-files', title: 'Files', icon: 'fa-folder-open' },
  'win-flashcards': { id: 'win-flashcards', title: 'Flashcards', icon: 'fa-layer-group' },
  'win-account-settings': { id: 'win-account-settings', title: 'Account', icon: 'fa-user-shield' },
};

export interface FileItem {
  name: string;
  type: 'note' | 'board' | 'canvas' | 'flashcard';
  targetContent: WindowId; // Which app opens this
}

interface WindowManagerContextType {
  openApps: WindowId[];
  minimizedApps: WindowId[];
  activeApp: WindowId | null;
  splitMode: boolean;
  splitApps: [WindowId, WindowId] | [];
  currentWorkspace: WorkspaceType;

  launchApp: (id: WindowId) => void;
  closeApp: (id: WindowId) => void;
  minimizeApp: (id: WindowId) => void;
  focusApp: (id: WindowId) => void;
  snapPair: (leftId: WindowId, rightId: WindowId) => void;
  openWorkspace: (type: WorkspaceType) => void;

  // Modal State
  pendingLaunchId: WindowId | null;
  setPendingLaunchId: (id: WindowId | null) => void;
  showPlacementModal: boolean;
  setShowPlacementModal: (show: boolean) => void;
  confirmPlacement: (location: 'left' | 'right' | 'over') => void;

  // Theme
  isDarkMode: boolean;
  toggleDarkMode: () => void;
}

const WindowManagerContext = createContext<WindowManagerContextType | undefined>(undefined);

export const WindowManagerProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [openApps, setOpenApps] = useState<WindowId[]>(['win-dashboard']);
  const [minimizedApps, setMinimizedApps] = useState<WindowId[]>([]);
  const [activeApp, setActiveApp] = useState<WindowId | null>('win-dashboard');
  const [splitMode, setSplitMode] = useState(false);
  const [splitApps, setSplitApps] = useState<[WindowId, WindowId] | []>([]);
  const [currentWorkspace, setCurrentWorkspace] = useState<WorkspaceType>('work');
  
  // Theme State
  const [isDarkMode, setIsDarkMode] = useState(false);

  const toggleDarkMode = () => {
    setIsDarkMode(prev => {
      const newVal = !prev;
      if (newVal) {
        document.documentElement.classList.add('dark');
      } else {
        document.documentElement.classList.remove('dark');
      }
      return newVal;
    });
  };

  const [pendingLaunchId, setPendingLaunchId] = useState<WindowId | null>(null);
  const [showPlacementModal, setShowPlacementModal] = useState(false);

  const launchApp = (id: WindowId) => {
    if (minimizedApps.includes(id)) {
      setMinimizedApps(prev => prev.filter(appId => appId !== id));
      focusApp(id);
      return;
    }

    if (openApps.includes(id)) {
      focusApp(id);
      return;
    }

    if (splitMode && splitApps.length > 0) {
      setPendingLaunchId(id);
      setShowPlacementModal(true);
      return;
    }

    setOpenApps(prev => [...prev, id]);
    focusApp(id);
  };

  const focusApp = (id: WindowId) => {
    // If it's minimized, restore it
    if (minimizedApps.includes(id)) {
      setMinimizedApps(prev => prev.filter(appId => appId !== id));
    }

    // Auto-minimize the previously active app when switching tabs in split mode
    if (activeApp && activeApp !== id && splitMode && splitApps.length === 2) {
      // If we're switching to a different tab and we're in split mode,
      // minimize the previous active app if it's not in the split pair
      if (!splitApps.includes(activeApp)) {
        setMinimizedApps(prev => [...prev, activeApp]);
      }
    }

    setActiveApp(id);

    // If focusing an app in the split pair
    if (splitApps.length > 0 && (splitApps as WindowId[]).includes(id)) {
      setSplitMode(true);
    } else {
      // Focusing an app NOT in split pair (Open Over)
      // We don't necessarily need to clear splitMode,
      // but in the HTML logic, z-index handles visibility.
      // Here, state will drive class names.
    }
  };

  const closeApp = (id: WindowId) => {
    setOpenApps(prev => prev.filter(appId => appId !== id));
    setMinimizedApps(prev => prev.filter(appId => appId !== id));

    if (splitApps.length > 0 && (splitApps as WindowId[]).includes(id)) {
      const other = (splitApps as WindowId[]).find(a => a !== id);
      setSplitApps([]);
      setSplitMode(false);
      if (other) {
        // The other app becomes maximized
        setActiveApp(other);
      } else if (openApps.length > 1) {
          // Fallback if somehow both closed or just one left?
          // Actually openApps isn't updated in this closure yet.
          // We'll rely on effect or next render, but for now:
          const remaining = openApps.filter(a => a !== id);
          if (remaining.length > 0) setActiveApp(remaining[remaining.length - 1]);
          else setActiveApp(null);
      }
    } else {
       // Standard close
       const remaining = openApps.filter(a => a !== id);
       if (activeApp === id && remaining.length > 0) {
         setActiveApp(remaining[remaining.length - 1]);
       }
    }
  };

  const minimizeApp = (id: WindowId) => {
    if (!minimizedApps.includes(id)) {
      setMinimizedApps(prev => [...prev, id]);
    }
    // If we minimize the active app, we should focus the next available one
    if (activeApp === id) {
      const remaining = openApps.filter(a => a !== id && !minimizedApps.includes(a));
      // Note: minimizedApps isn't updated in this closure yet, so we have to account for that
      const actuallyRemaining = remaining.filter(a => a !== id); // Redundant check for safety

      if (actuallyRemaining.length > 0) {
        setActiveApp(actuallyRemaining[actuallyRemaining.length - 1]);
      } else {
        setActiveApp(null);
      }
    }
  };

  const snapPair = (leftId: WindowId, rightId: WindowId) => {
    setSplitMode(true);
    setSplitApps([leftId, rightId] as [WindowId, WindowId]);
    setActiveApp(leftId);

    // Ensure both are open and not minimized
    setOpenApps(prev => {
        const newSet = new Set(prev);
        newSet.add(leftId);
        newSet.add(rightId);
        return Array.from(newSet) as WindowId[];
    });
    setMinimizedApps(prev => prev.filter(id => id !== leftId && id !== rightId));
  };

  const confirmPlacement = (location: 'left' | 'right' | 'over') => {
    setShowPlacementModal(false);
    if (!pendingLaunchId) return;

    if (!openApps.includes(pendingLaunchId)) {
        setOpenApps(prev => [...prev, pendingLaunchId]);
    }
    // Ensure not minimized
    setMinimizedApps(prev => prev.filter(id => id !== pendingLaunchId));


    if (location === 'over') {
        focusApp(pendingLaunchId);
    } else if (location === 'left') {
        const oldRight = splitApps[1];
        if (oldRight) snapPair(pendingLaunchId, oldRight);
        else console.warn("No right app to snap to");
    } else if (location === 'right') {
        const oldLeft = splitApps[0];
        if (oldLeft) snapPair(oldLeft, pendingLaunchId);
        else console.warn("No left app to snap to");
    }

    setPendingLaunchId(null);
  };

  const openWorkspace = (type: WorkspaceType) => {
      setCurrentWorkspace(type);
      launchApp('win-files');
  };

  return (
    <WindowManagerContext.Provider value={{
      openApps, minimizedApps, activeApp, splitMode, splitApps, currentWorkspace,
      launchApp, closeApp, minimizeApp, focusApp, snapPair, openWorkspace,
      pendingLaunchId, setPendingLaunchId, showPlacementModal, setShowPlacementModal, confirmPlacement,
      isDarkMode, toggleDarkMode
    }}>
      {children}
    </WindowManagerContext.Provider>
  );
};

export const useWindowManager = () => {
  const context = useContext(WindowManagerContext);
  if (!context) throw new Error("useWindowManager must be used within a WindowManagerProvider");
  return context;
};
