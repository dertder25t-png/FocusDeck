import React, { useState } from 'react';

interface NoteNode {
  id: string;
  title: string;
  children?: NoteNode[];
  content?: string;
}

interface Citation {
  id: string;
  type: 'book' | 'web' | 'paper';
  title: string;
  author: string;
  year: string;
}

const MOCK_NOTE_TREE: NoteNode[] = [
  {
    id: '1',
    title: 'Lecture 1: Introduction',
    children: [
      { id: '1-1', title: 'History of Computing', content: 'Notes on history...' },
      { id: '1-2', title: 'Basic Concepts', content: 'Bits, Bytes, and Logic...' }
    ]
  },
  {
    id: '2',
    title: 'Lecture 2: Algorithms',
    children: [
      { id: '2-1', title: 'Sorting', content: 'Bubble sort, Merge sort...' },
      { id: '2-2', title: 'Searching', content: 'Binary search...' }
    ]
  }
];

const MOCK_CITATIONS: Citation[] = [
  { id: 'c1', type: 'book', title: 'The Pragmatic Programmer', author: 'Hunt & Thomas', year: '1999' },
  { id: 'c2', type: 'paper', title: 'Attention Is All You Need', author: 'Vaswani et al.', year: '2017' }
];

const TreeNode: React.FC<{ node: NoteNode; activeId: string; onSelect: (node: NoteNode) => void; depth?: number }> = ({ node, activeId, onSelect, depth = 0 }) => {
  const [expanded, setExpanded] = useState(true);
  const hasChildren = node.children && node.children.length > 0;

  return (
    <div className="select-none">
      <div 
        className={`flex items-center gap-2 py-1.5 px-2 rounded-md cursor-pointer transition-colors ${activeId === node.id ? 'bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300 font-medium' : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300'}`}
        style={{ paddingLeft: `${depth * 12 + 8}px` }}
        onClick={() => onSelect(node)}
      >
        {hasChildren ? (
          <i 
            className={`fa-solid fa-chevron-right text-[10px] w-4 transition-transform ${expanded ? 'rotate-90' : ''}`}
            onClick={(e) => { e.stopPropagation(); setExpanded(!expanded); }}
          ></i>
        ) : <span className="w-4"></span>}
        <span className="truncate text-sm">{node.title}</span>
      </div>
      {hasChildren && expanded && (
        <div>
          {node.children!.map(child => (
            <TreeNode key={child.id} node={child} activeId={activeId} onSelect={onSelect} depth={depth + 1} />
          ))}
        </div>
      )}
    </div>
  );
};

export const NotesApp: React.FC = () => {
  const [activeNode, setActiveNode] = useState<NoteNode>(MOCK_NOTE_TREE[0].children![0]);
  const [writingMode, setWritingMode] = useState(false);
  const [showCitations, setShowCitations] = useState(false);
  const [citations, setCitations] = useState<Citation[]>(MOCK_CITATIONS);

  const handleAddCitation = () => {
    const newCitation: Citation = {
      id: Date.now().toString(),
      type: 'web',
      title: 'New Source',
      author: 'Unknown',
      year: new Date().getFullYear().toString()
    };
    setCitations([...citations, newCitation]);
  };

  return (
    <div className="flex h-full bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 transition-colors duration-300 overflow-hidden">
      {/* Sidebar - Tree View */}
      {!writingMode && (
        <div className="w-64 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-gray-50 dark:bg-gray-950 shrink-0 transition-all duration-300">
          <div className="p-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center">
            <h2 className="font-bold text-xs uppercase tracking-wider text-gray-500 dark:text-gray-400">Chapters</h2>
            <button className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"><i className="fa-solid fa-plus"></i></button>
          </div>
          <div className="flex-1 overflow-y-auto p-2">
            {MOCK_NOTE_TREE.map(node => (
              <TreeNode key={node.id} node={node} activeId={activeNode.id} onSelect={setActiveNode} />
            ))}
          </div>
        </div>
      )}

      {/* Main Editor Area */}
      <div className="flex-1 flex flex-col relative h-full">
        {/* Toolbar */}
        <div className={`h-12 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between px-4 bg-white dark:bg-gray-900 shrink-0 transition-all duration-300 ${writingMode ? 'opacity-0 hover:opacity-100 absolute top-0 left-0 right-0 z-10' : ''}`}>
          <div className="flex items-center gap-2 text-gray-400">
            <span className="text-xs">Last edited just now</span>
          </div>
          <div className="flex items-center gap-2">
             <button 
              onClick={() => setWritingMode(!writingMode)}
              className={`p-2 rounded hover:bg-gray-100 dark:hover:bg-gray-800 ${writingMode ? 'text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20' : 'text-gray-600 dark:text-gray-400'}`}
              title="Toggle Writing Mode"
            >
              <i className="fa-solid fa-pen-nib"></i>
            </button>
            <button 
              onClick={() => setShowCitations(!showCitations)}
              className={`p-2 rounded hover:bg-gray-100 dark:hover:bg-gray-800 ${showCitations ? 'text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20' : 'text-gray-600 dark:text-gray-400'}`}
              title="Citations"
            >
              <i className="fa-solid fa-quote-right"></i>
            </button>
          </div>
        </div>

        {/* Editor */}
        <div className={`flex-1 overflow-y-auto ${writingMode ? 'p-12 max-w-3xl mx-auto w-full' : 'p-8'}`}>
          <h1 className="text-3xl font-bold mb-6 outline-none text-gray-900 dark:text-white" contentEditable suppressContentEditableWarning>{activeNode.title}</h1>
          <textarea 
            className="w-full h-[calc(100%-4rem)] resize-none outline-none text-lg bg-transparent leading-relaxed text-gray-800 dark:text-gray-200 placeholder-gray-300 dark:placeholder-gray-600 font-serif" 
            placeholder="Start typing your notes here..."
            defaultValue={activeNode.content}
            key={activeNode.id} // Force re-render on node change
          ></textarea>
        </div>
      </div>

      {/* Citation Sidebar */}
      {showCitations && (
        <div className="w-80 border-l border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-950 shrink-0 flex flex-col transition-all duration-300 h-full">
          <div className="p-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center">
            <h3 className="font-bold text-sm text-gray-700 dark:text-gray-200">Citations</h3>
            <button onClick={() => setShowCitations(false)} className="text-gray-400 hover:text-gray-600"><i className="fa-solid fa-xmark"></i></button>
          </div>
          <div className="p-4 flex-1 overflow-y-auto">
            {citations.map(citation => (
              <div key={citation.id} className="bg-white dark:bg-gray-900 p-3 rounded border border-gray-200 dark:border-gray-700 mb-3 shadow-sm hover:shadow-md transition-shadow cursor-pointer group">
                <div className="flex justify-between items-start">
                  <p className="text-[10px] uppercase tracking-wider text-gray-500 mb-1">{citation.type}</p>
                  <button className="text-gray-300 hover:text-red-500 opacity-0 group-hover:opacity-100 transition-opacity"><i className="fa-solid fa-trash"></i></button>
                </div>
                <p className="text-sm font-medium text-gray-800 dark:text-gray-200 leading-tight mb-1">{citation.title}</p>
                <p className="text-xs text-gray-500 dark:text-gray-400">{citation.author}, {citation.year}</p>
              </div>
            ))}
            
            <button 
              onClick={handleAddCitation}
              className="w-full py-3 border-2 border-dashed border-gray-300 dark:border-gray-700 rounded-lg text-gray-400 hover:border-blue-400 hover:text-blue-500 text-sm font-medium transition-all flex items-center justify-center gap-2"
            >
              <i className="fa-solid fa-plus"></i> Add Citation
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
