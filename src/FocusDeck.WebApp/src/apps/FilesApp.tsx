import React from 'react';

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
            <div className="bg-gray-100 px-4 py-2 border-b border-border text-sm flex gap-2 text-gray-500 shrink-0">
                <span>Computer</span> <i className="fa-solid fa-chevron-right text-[10px] mt-1"></i> <span className="text-black font-medium">{currentWorkspace === 'work' ? 'Work' : 'School'}</span>
                {activeFile && (
                    <>
                        <i className="fa-solid fa-chevron-right text-[10px] mt-1"></i>
                        <span className="text-black font-medium">{activeFile.name}</span>
                    </>
                )}
            </div>

            <div className="flex-1 bg-white relative overflow-hidden">
                {!activeFile ? (
                    <div className="p-4 overflow-y-auto h-full">
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                            <div onClick={onCreateNew} className="flex flex-col items-center justify-center p-4 bg-white border-2 border-dashed border-gray-300 hover:border-accent-blue hover:bg-blue-50 rounded-xl cursor-pointer transition-all group h-32">
                                <div className="w-10 h-10 rounded-full bg-gray-100 flex items-center justify-center mb-2 group-hover:bg-blue-200 group-hover:text-blue-700">
                                    <i className="fa-solid fa-plus text-xl text-gray-400 group-hover:text-blue-600"></i>
                                </div>
                                <span className="text-xs font-bold text-gray-400 group-hover:text-blue-600">Create New</span>
                            </div>
                            {files[currentWorkspace].map((f, i) => (
                                <div key={i} onClick={() => onOpenFile(f)} className="flex flex-col items-center p-4 bg-white border border-transparent hover:border-gray-200 hover:shadow-md rounded-xl cursor-pointer transition-all group h-32 justify-center relative">
                                    <i className={`fa-solid ${getFileIcon(f.type)} text-4xl mb-2 group-hover:scale-110 transition-transform`}></i>
                                    <span className="text-xs font-bold text-gray-800 text-center break-all line-clamp-2">{f.name}</span>
                                </div>
                            ))}
                        </div>
                    </div>
                ) : (
                    <div className="h-full w-full absolute top-0 left-0 bg-white z-10 flex flex-col">
                        {/* Tool View */}
                        <div className="flex items-center gap-2 p-2 border-b border-border bg-subtle">
                            <button onClick={onBack} className="px-3 py-1 bg-white border border-border rounded text-xs font-bold hover:bg-gray-50">
                                <i className="fa-solid fa-chevron-left mr-1"></i> Back
                            </button>
                            <span className="text-xs font-bold">{activeFile.name}</span>
                        </div>
                        <div className="flex-1 overflow-auto">
                            {/* In a real app, render the component based on activeFile.targetContent */}
                            {activeFile.targetContent === 'win-notes' && <div className="p-8 bg-yellow-50 h-full"><textarea className="w-full h-full resize-none outline-none text-lg p-4 bg-white shadow-sm" placeholder={`Editing ${activeFile.name}...`}></textarea></div>}
                            {activeFile.targetContent === 'win-kanban' && <div className="p-4 h-full">Kanban: {activeFile.name}</div>}
                            {activeFile.targetContent === 'win-whiteboard' && <div className="p-4 h-full">Canvas: {activeFile.name}</div>}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};
