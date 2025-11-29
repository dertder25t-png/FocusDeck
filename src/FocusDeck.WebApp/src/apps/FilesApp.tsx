import React from 'react';
import { NotesApp } from './NotesApp';
import { KanbanApp } from './KanbanApp';
import { WhiteboardApp } from './WhiteboardApp';
import { FlashcardsApp } from './FlashcardsApp';

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
    return (
        <div className="flex flex-col h-full">
            <div className="bg-subtle px-4 py-2 border-b border-border text-sm flex gap-2 text-gray-500 dark:text-gray-400 shrink-0">
                <span>Computer</span> <i className="fa-solid fa-chevron-right text-[10px] mt-1"></i> <span className="text-ink font-medium">{currentWorkspace === 'work' ? 'Work' : 'School'}</span>
                {activeFile && (
                    <>
                        <i className="fa-solid fa-chevron-right text-[10px] mt-1"></i>
                        <span className="text-ink font-medium">{activeFile.name}</span>
                    </>
                )}
            </div>

            <div className="flex-1 bg-paper relative overflow-hidden">
                {!activeFile ? (
                    <div className="p-4 overflow-y-auto h-full">
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                            <div onClick={onCreateNew} className="flex flex-col items-center justify-center p-4 bg-surface border-2 border-dashed border-border hover:border-accent-blue hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-xl cursor-pointer transition-all group h-32">
                                <div className="w-10 h-10 rounded-full bg-subtle flex items-center justify-center mb-2 group-hover:bg-blue-200 dark:group-hover:bg-blue-800 group-hover:text-blue-700 dark:group-hover:text-blue-200">
                                    <i className="fa-solid fa-plus text-xl text-gray-400 group-hover:text-blue-600 dark:group-hover:text-blue-300"></i>
                                </div>
                                <span className="text-xs font-bold text-gray-400 group-hover:text-blue-600 dark:group-hover:text-blue-300">Create New</span>
                            </div>
                            {files[currentWorkspace].map((f, i) => (
                                <div key={i} onClick={() => onOpenFile(f)} className="flex flex-col items-center p-4 bg-surface border border-transparent hover:border-border hover:shadow-md rounded-xl cursor-pointer transition-all group h-32 justify-center relative">
                                    <i className={`fa-solid ${getFileIcon(f.type)} text-4xl mb-2 group-hover:scale-110 transition-transform text-ink`}></i>
                                    <span className="text-xs font-bold text-ink text-center break-all line-clamp-2">{f.name}</span>
                                </div>
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
