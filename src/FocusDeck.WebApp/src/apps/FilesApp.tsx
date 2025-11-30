import React, { useState } from 'react';
import { NotesApp } from './NotesApp';
import { KanbanApp } from './KanbanApp';
import { WhiteboardApp } from './WhiteboardApp';
import { FlashcardsApp } from './FlashcardsApp';
import * as ContextMenu from '@radix-ui/react-context-menu';

interface FilesAppProps {
    files: Record<string, any[]>;
    currentWorkspace: string;
    onOpenFile: (file: any) => void;
    onBack: () => void;
    activeFile: any;
    onCreateNew: () => void;
}

const getFileIcon = (type: string) => {
    const map: any = { 'note': 'fa-note-sticky', 'board': 'fa-list-check', 'canvas': 'fa-pen-nib', 'flashcard': 'fa-layer-group' };
    return map[type] || 'fa-file';
};

export const FilesApp: React.FC<FilesAppProps> = ({ files, currentWorkspace, onOpenFile, onBack, activeFile, onCreateNew }) => {
    const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');

    const handleRename = (file: any) => {
        const newName = prompt("Rename file:", file.name);
        if (newName) {
            // In a real app, this would dispatch an action or call an API
            file.name = newName;
            // Force update (in a real app, use state/context)
        }
    };

    const handleDelete = (file: any) => {
        if (confirm(`Are you sure you want to delete ${file.name}?`)) {
            // Delete logic
            console.log("Delete", file);
        }
    };

    return (
        <div className="flex flex-col h-full">
            <div className="bg-subtle px-4 py-2 border-b border-border text-sm flex gap-2 text-gray-500 dark:text-gray-400 shrink-0 items-center justify-between">
                <div className="flex gap-2 items-center">
                    <span>Computer</span> <i className="fa-solid fa-chevron-right text-[10px] mt-1"></i> <span className="text-ink font-medium">{currentWorkspace === 'work' ? 'Work' : 'School'}</span>
                    {activeFile && (
                        <>
                            <i className="fa-solid fa-chevron-right text-[10px] mt-1"></i>
                            <span className="text-ink font-medium">{activeFile.name}</span>
                        </>
                    )}
                </div>
                {!activeFile && (
                    <div className="flex bg-gray-200 dark:bg-gray-800 rounded-lg p-0.5">
                        <button
                            onClick={() => setViewMode('grid')}
                            className={`p-1.5 rounded-md text-xs transition-colors ${viewMode === 'grid' ? 'bg-white dark:bg-gray-700 shadow-sm text-ink' : 'text-gray-500 hover:text-ink'}`}
                            title="Grid View"
                        >
                            <i className="fa-solid fa-border-all"></i>
                        </button>
                        <button
                            onClick={() => setViewMode('list')}
                            className={`p-1.5 rounded-md text-xs transition-colors ${viewMode === 'list' ? 'bg-white dark:bg-gray-700 shadow-sm text-ink' : 'text-gray-500 hover:text-ink'}`}
                            title="List View"
                        >
                            <i className="fa-solid fa-list"></i>
                        </button>
                    </div>
                )}
            </div>

            <div className="flex-1 bg-paper relative overflow-hidden">
                {!activeFile ? (
                    <div className="p-4 overflow-y-auto h-full">
                        <div className={viewMode === 'grid' ? "grid grid-cols-2 md:grid-cols-4 gap-4" : "flex flex-col gap-2"}>
                            <div onClick={onCreateNew} className={`flex ${viewMode === 'grid' ? 'flex-col items-center justify-center h-32' : 'flex-row items-center h-16'} p-4 bg-surface border-2 border-dashed border-border hover:border-accent-blue hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-xl cursor-pointer transition-all group`}>
                                <div className={`w-10 h-10 rounded-full bg-subtle flex items-center justify-center ${viewMode === 'grid' ? 'mb-2' : 'mr-4'} group-hover:bg-blue-200 dark:group-hover:bg-blue-800 group-hover:text-blue-700 dark:group-hover:text-blue-200`}>
                                    <i className="fa-solid fa-plus text-xl text-gray-400 group-hover:text-blue-600 dark:group-hover:text-blue-300"></i>
                                </div>
                                <span className="text-xs font-bold text-gray-400 group-hover:text-blue-600 dark:group-hover:text-blue-300">Create New</span>
                            </div>

                            {files[currentWorkspace].map((f, i) => (
                                <ContextMenu.Root key={i}>
                                    <ContextMenu.Trigger>
                                        <div
                                            onClick={() => onOpenFile(f)}
                                            className={`flex ${viewMode === 'grid' ? 'flex-col items-center justify-center h-32' : 'flex-row items-center h-16 px-4'} p-4 bg-surface border border-transparent hover:border-border hover:shadow-md rounded-xl cursor-pointer transition-all group relative`}
                                        >
                                            <i className={`fa-solid ${getFileIcon(f.type)} ${viewMode === 'grid' ? 'text-4xl mb-2' : 'text-2xl mr-4'} group-hover:scale-110 transition-transform text-ink`}></i>
                                            <div className="flex-1 min-w-0">
                                                <span className="text-xs font-bold text-ink break-all line-clamp-2">{f.name}</span>
                                                {viewMode === 'list' && <div className="text-[10px] text-gray-500 mt-0.5 capitalize">{f.type} • Modified just now</div>}
                                            </div>
                                        </div>
                                    </ContextMenu.Trigger>
                                    <ContextMenu.Portal>
                                        <ContextMenu.Content className="min-w-[160px] bg-white dark:bg-gray-800 rounded-md overflow-hidden p-[5px] shadow-[0px_10px_38px_-10px_rgba(22,_23,_24,_0.35),_0px_10px_20px_-15px_rgba(22,_23,_24,_0.2)] border border-gray-200 dark:border-gray-700 z-50">
                                            <ContextMenu.Item
                                                className="group text-[13px] leading-none text-violet11 rounded-[3px] flex items-center h-[25px] px-[5px] relative pl-[25px] select-none outline-none data-[highlighted]:bg-violet9 data-[highlighted]:text-violet1 cursor-pointer hover:bg-blue-50 dark:hover:bg-blue-900/20 hover:text-blue-600"
                                                onSelect={() => handleRename(f)}
                                            >
                                                <i className="fa-solid fa-pen-to-square absolute left-[5px] w-4 text-center"></i>
                                                Rename
                                            </ContextMenu.Item>
                                            <ContextMenu.Item
                                                className="group text-[13px] leading-none text-red-500 rounded-[3px] flex items-center h-[25px] px-[5px] relative pl-[25px] select-none outline-none data-[highlighted]:bg-red-100 data-[highlighted]:text-red-700 cursor-pointer hover:bg-red-50 dark:hover:bg-red-900/20"
                                                onSelect={() => handleDelete(f)}
                                            >
                                                <i className="fa-solid fa-trash absolute left-[5px] w-4 text-center"></i>
                                                Delete
                                            </ContextMenu.Item>
                                        </ContextMenu.Content>
                                    </ContextMenu.Portal>
                                </ContextMenu.Root>
                            ))}
                        </div>
                    </div>
                ) : (
                    <div className="h-full w-full absolute top-0 left-0 bg-paper z-10 flex flex-col">
                        {/* Tool View */}
                        <div className="flex items-center gap-2 p-2 border-b border-border bg-subtle">
                            <button onClick={onBack} className="px-3 py-1 bg-surface border border-border rounded text-xs font-bold hover:bg-subtle text-ink">
                                <i className="fa-solid fa-chevron-left mr-1"></i> Back
                            </button>
                            <span className="text-xs font-bold text-ink">{activeFile.name}</span>
                        </div>
                        <div className="flex-1 relative overflow-hidden">
                            {/* Render the actual App components */}
                            {activeFile.targetContent === 'win-notes' && <NotesApp />}
                            {activeFile.targetContent === 'win-kanban' && <KanbanApp />}
                            {activeFile.targetContent === 'win-whiteboard' && <WhiteboardApp />}
                            {activeFile.targetContent === 'win-flashcards' && <FlashcardsApp />}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};
