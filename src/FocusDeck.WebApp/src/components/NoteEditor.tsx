import { useState } from 'react';
import { Button } from './Button';
import { Input } from './Input';
import { Card, CardContent } from './Card';

export interface AcademicSource {
  id: string;
  title: string;
  author: string;
  publisher: string;
  year: number;
  url: string;
}

interface NoteEditorProps {
  content: string;
  onChange: (content: string) => void;
  mode: 'quick' | 'paper';
  onModeChange: (mode: 'quick' | 'paper') => void;
  sources: AcademicSource[];
  onAddSource: (source: AcademicSource) => void;
  onRemoveSource: (id: string) => void;
  citationStyle: string;
  onCitationStyleChange: (style: string) => void;
}

export function NoteEditor({
  content,
  onChange,
  mode,
  onModeChange,
  sources,
  onAddSource,
  onRemoveSource,
  citationStyle,
  onCitationStyleChange
}: NoteEditorProps) {
  const [showSourceDialog, setShowSourceDialog] = useState(false);
  const [newSource, setNewSource] = useState<Partial<AcademicSource>>({});

  const handleAddSource = () => {
    if (newSource.title && newSource.author) {
      onAddSource({
        id: Math.random().toString(36).substr(2, 9),
        title: newSource.title,
        author: newSource.author,
        publisher: newSource.publisher || '',
        year: newSource.year || new Date().getFullYear(),
        url: newSource.url || ''
      });
      setNewSource({});
      setShowSourceDialog(false);
    }
  };

  const insertCitation = (source: AcademicSource) => {
    // Determine citation format based on style (Client-side stub, real formatting should ideally use a shared logic or backend)
    // For now, simple stub
    const citation = citationStyle === 'APA'
      ? `(${source.author}, ${source.year})`
      : `[${source.author}, ${source.title}]`;

    onChange(content + ' ' + citation);
  };

  return (
    <div className="flex h-full gap-4">
      <div className="flex-1 flex flex-col">
        {/* Toolbar */}
        <div className="flex justify-between items-center mb-2 bg-gray-900/50 p-2 rounded-lg border border-gray-800">
          <div className="flex gap-2">
            <button
              onClick={() => onModeChange('quick')}
              className={`px-3 py-1 text-xs rounded-md transition-colors ${
                mode === 'quick' ? 'bg-primary text-white' : 'text-gray-400 hover:text-white'
              }`}
            >
              Speed Mode
            </button>
            <button
              onClick={() => onModeChange('paper')}
              className={`px-3 py-1 text-xs rounded-md transition-colors ${
                mode === 'paper' ? 'bg-primary text-white' : 'text-gray-400 hover:text-white'
              }`}
            >
              Paper Mode
            </button>
          </div>

          {mode === 'paper' && (
             <div className="flex items-center gap-2">
                <span className="text-xs text-gray-500">Style:</span>
                <select
                  value={citationStyle}
                  onChange={(e) => onCitationStyleChange(e.target.value)}
                  className="bg-gray-800 border border-gray-700 rounded text-xs text-white p-1 focus:outline-none"
                >
                  <option value="APA">APA</option>
                  <option value="MLA">MLA</option>
                  <option value="Chicago">Chicago</option>
                </select>
             </div>
          )}
        </div>

        {/* Editor Area */}
        <div className="flex-1 relative overflow-hidden">
          {mode === 'quick' ? (
            <textarea
              className="w-full h-full p-4 bg-gray-950 border border-gray-800 rounded-lg text-sm font-mono resize-none focus:outline-none focus:ring-1 focus:ring-primary text-gray-300"
              value={content}
              onChange={(e) => onChange(e.target.value)}
              placeholder="Start typing your notes..."
            />
          ) : (
            <div className="w-full h-full bg-gray-200 overflow-y-auto p-8 flex justify-center rounded-lg">
              {/* Paper Simulation */}
              <div
                className="bg-white text-black w-[210mm] min-h-[297mm] p-[25mm] shadow-xl"
                style={{ fontFamily: 'Times New Roman, serif', lineHeight: '2' }}
              >
                {/* This is a very basic editable div for the MVP. Use a real rich text editor for production. */}
                <textarea
                    className="w-full h-full bg-transparent border-none resize-none focus:outline-none"
                    value={content}
                    onChange={(e) => onChange(e.target.value)}
                    placeholder="Start writing your paper..."
                />
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Sources Sidebar (Visible in Paper Mode) */}
      {mode === 'paper' && (
        <div className="w-72 border-l border-gray-800 pl-4 flex flex-col">
          <div className="flex justify-between items-center mb-4">
            <h3 className="font-semibold text-sm">Sources</h3>
            <Button size="sm" variant="ghost" onClick={() => setShowSourceDialog(true)}>+</Button>
          </div>

          <div className="flex-1 overflow-y-auto space-y-3">
            {sources.map(source => (
              <Card key={source.id} className="bg-gray-900 border-gray-800">
                <CardContent className="p-3">
                  <div className="flex justify-between items-start">
                    <div>
                      <div className="font-medium text-sm text-white">{source.title}</div>
                      <div className="text-xs text-gray-400">{source.author} ({source.year})</div>
                    </div>
                    <button
                      onClick={() => onRemoveSource(source.id)}
                      className="text-gray-600 hover:text-red-400"
                    >
                      ×
                    </button>
                  </div>
                  <div className="mt-2 flex justify-end">
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-6 text-xs"
                      onClick={() => insertCitation(source)}
                    >
                      Cite
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      )}

      {/* Add Source Dialog (Inline for simplicity) */}
      {showSourceDialog && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-gray-900 p-6 rounded-xl border border-gray-800 w-96">
            <h3 className="text-lg font-semibold mb-4">Add Source</h3>
            <div className="space-y-3">
              <Input
                placeholder="Title"
                value={newSource.title || ''}
                onChange={e => setNewSource({...newSource, title: e.target.value})}
              />
              <Input
                placeholder="Author"
                value={newSource.author || ''}
                onChange={e => setNewSource({...newSource, author: e.target.value})}
              />
              <Input
                placeholder="Publisher"
                value={newSource.publisher || ''}
                onChange={e => setNewSource({...newSource, publisher: e.target.value})}
              />
              <Input
                placeholder="Year"
                type="number"
                value={newSource.year || ''}
                onChange={e => setNewSource({...newSource, year: parseInt(e.target.value)})}
              />
              <Input
                placeholder="URL"
                value={newSource.url || ''}
                onChange={e => setNewSource({...newSource, url: e.target.value})}
              />
            </div>
            <div className="mt-6 flex justify-end gap-2">
              <Button variant="ghost" onClick={() => setShowSourceDialog(false)}>Cancel</Button>
              <Button onClick={handleAddSource}>Add</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
