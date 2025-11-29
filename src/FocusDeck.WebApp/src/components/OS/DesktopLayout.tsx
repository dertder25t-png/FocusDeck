import React, { useState } from 'react';
import { useWindowManager, WindowId } from '../../contexts/WindowManagerContext';
import { Window } from './Window';
import { Taskbar } from './Taskbar';
import { StartMenu } from './StartMenu';
import { PlacementModal } from './PlacementModal';

// App Components
import { DashboardApp } from '../../apps/DashboardApp';
import { NotesApp } from '../../apps/NotesApp';
import { KanbanApp } from '../../apps/KanbanApp';
import { EmailApp } from '../../apps/EmailApp';
import { WhiteboardApp } from '../../apps/WhiteboardApp';
import { JarvisApp } from '../../apps/JarvisApp';
import { FilesApp } from '../../apps/FilesApp';

// Mock File Data
const MOCK_FILES = {
  'work': [
      { name: 'Q4_Strategy.note', type: 'note' as const, targetContent: 'win-notes' as WindowId },
      { name: 'Launch_Plan.board', type: 'board' as const, targetContent: 'win-kanban' as WindowId },
      { name: 'UI_Concepts.canvas', type: 'canvas' as const, targetContent: 'win-whiteboard' as WindowId }
  ],
  'school': [
      { name: 'History_Essay.note', type: 'note' as const, targetContent: 'win-notes' as WindowId },
      { name: 'Math_HW.canvas', type: 'canvas' as const, targetContent: 'win-whiteboard' as WindowId }
  ]
};

export const DesktopLayout: React.FC = () => {
  const { currentWorkspace, launchApp, openApps } = useWindowManager();
  const [startMenuOpen, setStartMenuOpen] = useState(false);
  const [toolPickerOpen, setToolPickerOpen] = useState(false);

  // File System State (Local for now, can move to Context if needed globally)
  const [currentFiles, setCurrentFiles] = useState(MOCK_FILES);
  const [activeFile, setActiveFile] = useState<{name: string, type: string, targetContent: WindowId} | null>(null);

  const handleCreateNew = (type: 'note' | 'board' | 'canvas' | 'flashcard') => {
      setToolPickerOpen(false);
      const map = {
          'note': { name: 'Untitled', targetContent: 'win-notes' as WindowId },
          'board': { name: 'New Board', targetContent: 'win-kanban' as WindowId },
          'canvas': { name: 'New Canvas', targetContent: 'win-whiteboard' as WindowId },
          'flashcard': { name: 'Study Set', targetContent: 'win-notes' as WindowId }
      };

      const newFile = {
          name: `${map[type].name} ${currentFiles[currentWorkspace].length + 1}`,
          type: type,
          targetContent: map[type].targetContent
      };

      setCurrentFiles(prev => ({
          ...prev,
          [currentWorkspace]: [...prev[currentWorkspace], newFile]
      }));

      // Open the file immediately
      setActiveFile(newFile);
  };

  const handleOpenFile = (file: typeof MOCK_FILES['work'][0]) => {
      setActiveFile(file);
  };

  const handleBackFiles = () => {
      setActiveFile(null);
  };

  return (
    <div className="flex flex-col h-screen w-full font-sans overflow-hidden bg-paper text-ink">
        <main className="flex-1 relative bg-paper bg-dot-pattern overflow-hidden p-0 md:p-4" id="desktop-area">

            <PlacementModal />

            {/* Tool Picker Overlay */}
            {toolPickerOpen && (
                <div className="absolute inset-0 z-[60] flex flex-col items-center justify-center p-8 backdrop-blur-sm bg-paper/80">
                     <div className="bg-white border-2 border-ink rounded-xl p-6 shadow-hard max-w-lg w-full">
                        <h2 className="text-xl font-display font-bold mb-4">Create New</h2>
                        <div className="grid grid-cols-2 gap-4">
                            <button onClick={() => handleCreateNew('note')} className="p-4 border border-border rounded-lg hover:bg-yellow-50 hover:border-yellow-400 flex flex-col items-center gap-2 transition-colors"><i className="fa-solid fa-note-sticky text-2xl text-accent-yellow"></i><span className="font-bold text-sm">Note</span></button>
                            <button onClick={() => handleCreateNew('board')} className="p-4 border border-border rounded-lg hover:bg-teal-50 hover:border-teal-400 flex flex-col items-center gap-2 transition-colors"><i className="fa-solid fa-list-check text-2xl text-accent-teal"></i><span className="font-bold text-sm">Kanban</span></button>
                            <button onClick={() => handleCreateNew('canvas')} className="p-4 border border-border rounded-lg hover:bg-purple-50 hover:border-purple-400 flex flex-col items-center gap-2 transition-colors"><i className="fa-solid fa-pen-nib text-2xl text-accent-purple"></i><span className="font-bold text-sm">Canvas</span></button>
                            <button onClick={() => handleCreateNew('flashcard')} className="p-4 border border-border rounded-lg hover:bg-blue-50 hover:border-blue-400 flex flex-col items-center gap-2 transition-colors"><i className="fa-solid fa-layer-group text-2xl text-accent-blue"></i><span className="font-bold text-sm">Cards</span></button>
                        </div>
                        <button onClick={() => setToolPickerOpen(false)} className="w-full mt-4 py-3 text-sm text-gray-500 font-bold hover:text-black">Cancel</button>
                     </div>
                </div>
            )}

            <div id="windows-container" className="w-full h-full relative">

                {/* 1. DASHBOARD */}
                <Window id="win-dashboard" headerChips={
                    <>
                        <div className="smart-chip hidden md:flex items-center gap-2 px-3 py-1 bg-red-100 text-red-700 rounded-full text-xs font-bold cursor-pointer border border-red-200">
                            <i className="fa-solid fa-stopwatch animate-pulse"></i> 24:59
                        </div>
                        <div className="smart-chip hidden md:flex items-center gap-2 px-3 py-1 bg-green-100 text-green-700 rounded-full text-xs font-bold cursor-pointer border border-green-200">
                            <i className="fa-brands fa-spotify"></i> Lo-Fi Beats
                        </div>
                    </>
                }>
                    <DashboardApp />
                </Window>

                {/* 2. EMAIL */}
                <Window id="win-email" headerChips={
                    <div className="smart-chip hidden md:flex items-center gap-2 px-3 py-1 bg-gray-200 text-gray-700 rounded-full text-xs font-bold cursor-pointer border border-gray-300">
                        <i className="fa-solid fa-filter"></i> Unread: 4
                    </div>
                }>
                     <EmailApp />
                </Window>

                {/* 3. NOTES */}
                <Window id="win-notes" headerChips={
                    <div className="smart-chip hidden md:flex items-center gap-2 px-3 py-1 bg-yellow-100 text-yellow-800 rounded-full text-xs font-bold cursor-pointer border border-yellow-200">
                        <i className="fa-solid fa-check"></i> Saved
                    </div>
                }>
                    <NotesApp />
                </Window>

                {/* 4. KANBAN */}
                <Window id="win-kanban">
                    <KanbanApp />
                </Window>

                {/* 5. WHITEBOARD */}
                <Window id="win-whiteboard">
                    <WhiteboardApp />
                </Window>

                {/* 6. JARVIS */}
                <Window id="win-jarvis">
                    <JarvisApp />
                </Window>

                {/* 7. FILES (Workspaces) */}
                <Window id="win-files">
                    <FilesApp
                        files={currentFiles}
                        currentWorkspace={currentWorkspace}
                        onOpenFile={handleOpenFile}
                        onBack={handleBackFiles}
                        activeFile={activeFile}
                        onCreateNew={() => setToolPickerOpen(true)}
                    />
                </Window>

            </div>
        </main>

        <StartMenu isOpen={startMenuOpen} onClose={() => setStartMenuOpen(false)} />
        <Taskbar onToggleStart={() => setStartMenuOpen(!startMenuOpen)} />
    </div>
  );
};
