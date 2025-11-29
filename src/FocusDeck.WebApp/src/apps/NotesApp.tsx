import React from 'react';

export const NotesApp: React.FC = () => {
    return (
        <div className="flex-1 bg-yellow-50 p-8 overflow-y-auto h-full">
            <div className="max-w-2xl mx-auto bg-white p-8 shadow-sm border border-gray-200 h-full">
                <textarea className="w-full h-full resize-none outline-none text-lg" placeholder="Start typing..."></textarea>
            </div>
        </div>
    );
};
