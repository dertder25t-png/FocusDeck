import React, { createContext, useContext, useState, ReactNode } from 'react';

export type WindowId =
  | 'win-dashboard'
  | 'win-email'
  | 'win-notes'
  | 'win-kanban'
  | 'win-whiteboard'
  | 'win-jarvis'
  | 'win-files';

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
};

export interface FileItem {
  name: string;
  type: 'note' | 'board' | 'canvas' | 'flashcard';
  targetContent: WindowId; // Which app opens this
}

interface WindowManagerContextType {
  openApps: WindowId[];
  activeApp: WindowId | null;
  splitMode: boolean;
  splitApps: [WindowId, WindowId] | [];
  currentWorkspace: WorkspaceType;

  launchApp: (id: WindowId) => void;
  closeApp: (id: WindowId) => void;
  focusApp: (id: WindowId) => void;
  snapPair: (leftId: WindowId, rightId: WindowId) => void;
  openWorkspace: (type: WorkspaceType) => void;

  // Modal State
  pendingLaunchId: WindowId | null;
  setPendingLaunchId: (id: WindowId | null) => void;
  showPlacementModal: boolean;
  setShowPlacementModal: (show: boolean) => void;
  confirmPlacement: (location: 'left' | 'right' | 'over') => void;
}

const WindowManagerContext = createContext<WindowManagerContextType | undefined>(undefined);

export const WindowManagerProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [openApps, setOpenApps] = useState<WindowId[]>(['win-dashboard']);
  const [activeApp, setActiveApp] = useState<WindowId | null>('win-dashboard');
  const [splitMode, setSplitMode] = useState(false);
  const [splitApps, setSplitApps] = useState<[WindowId, WindowId] | []>([]);
  const [currentWorkspace, setCurrentWorkspace] = useState<WorkspaceType>('work');

  const [pendingLaunchId, setPendingLaunchId] = useState<WindowId | null>(null);
  const [showPlacementModal, setShowPlacementModal] = useState(false);

  const launchApp = (id: WindowId) => {
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
    setActiveApp(id);

    // If focusing an app in the split pair
    if (splitApps.includes(id)) {
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

    if (splitApps.includes(id)) {
      const other = splitApps.find(a => a !== id);
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

  const snapPair = (leftId: WindowId, rightId: WindowId) => {
    setSplitMode(true);
    setSplitApps([leftId, rightId]);
    setActiveApp(leftId);

    // Ensure both are open
    setOpenApps(prev => {
        const newSet = new Set(prev);
        newSet.add(leftId);
        newSet.add(rightId);
        return Array.from(newSet) as WindowId[];
    });
  };

  const confirmPlacement = (location: 'left' | 'right' | 'over') => {
    setShowPlacementModal(false);
    if (!pendingLaunchId) return;

    if (!openApps.includes(pendingLaunchId)) {
        setOpenApps(prev => [...prev, pendingLaunchId]);
    }

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
      openApps, activeApp, splitMode, splitApps, currentWorkspace,
      launchApp, closeApp, focusApp, snapPair, openWorkspace,
      pendingLaunchId, setPendingLaunchId, showPlacementModal, setShowPlacementModal, confirmPlacement
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
