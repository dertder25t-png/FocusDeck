
import { useEditor, EditorContent } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import Underline from '@tiptap/extension-underline';
import Placeholder from '@tiptap/extension-placeholder';
import { Bold, Italic, Underline as UnderlineIcon, Strikethrough, List, ListOrdered, Quote } from 'lucide-react';
import { useEffect } from 'react';

interface TiptapEditorProps {
  content: string;
  onChange: (content: string) => void;
  editable?: boolean;
}

export function TiptapEditor({ content, onChange, editable = true }: TiptapEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit,
      Underline,
      Placeholder.configure({
        placeholder: 'Start typing your notes here...',
      }),
    ],
    content,
    editable: editable,
    onUpdate: ({ editor }) => {
      onChange(editor.getHTML());
    },
    editorProps: {
        attributes: {
            class: 'prose dark:prose-invert max-w-none focus:outline-none min-h-[500px]',
        },
    },
  });

  useEffect(() => {
    if (editor && content !== editor.getHTML()) {
        // Only update if content is significantly different to avoid cursor jumps
        // For simplicity in this demo, we might rely on the key prop in parent to force remount
        // But if we want to support external updates without remounting:
        // editor.commands.setContent(content);
    }
  }, [content, editor]);

  useEffect(() => {
    if (editor) {
        editor.setEditable(editable);
    }
  }, [editable, editor]);

  if (!editor) {
    return null;
  }

  return (
    <div className="flex flex-col h-full">
      {/* Toolbar */}
      {editable && (
      <div className="flex items-center gap-1 p-2 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 sticky top-0 z-10">
        <button
          onClick={() => editor.chain().focus().toggleBold().run()}
          className={`p-2 rounded hover:bg-gray-200 dark:hover:bg-gray-700 ${editor.isActive('bold') ? 'bg-gray-200 dark:bg-gray-700 text-blue-600 dark:text-blue-400' : 'text-gray-600 dark:text-gray-400'}`}
          title="Bold"
        >
          <Bold size={16} />
        </button>
        <button
          onClick={() => editor.chain().focus().toggleItalic().run()}
          className={`p-2 rounded hover:bg-gray-200 dark:hover:bg-gray-700 ${editor.isActive('italic') ? 'bg-gray-200 dark:bg-gray-700 text-blue-600 dark:text-blue-400' : 'text-gray-600 dark:text-gray-400'}`}
          title="Italic"
        >
          <Italic size={16} />
        </button>
        <button
            onClick={() => editor.chain().focus().toggleUnderline().run()}
            className={`p-2 rounded hover:bg-gray-200 dark:hover:bg-gray-700 ${editor.isActive('underline') ? 'bg-gray-200 dark:bg-gray-700 text-blue-600 dark:text-blue-400' : 'text-gray-600 dark:text-gray-400'}`}
            title="Underline"
        >
            <UnderlineIcon size={16} />
        </button>
        <button
            onClick={() => editor.chain().focus().toggleStrike().run()}
            className={`p-2 rounded hover:bg-gray-200 dark:hover:bg-gray-700 ${editor.isActive('strike') ? 'bg-gray-200 dark:bg-gray-700 text-blue-600 dark:text-blue-400' : 'text-gray-600 dark:text-gray-400'}`}
            title="Strikethrough"
        >
            <Strikethrough size={16} />
        </button>

        <div className="w-px h-6 bg-gray-300 dark:bg-gray-600 mx-1"></div>

        <button
            onClick={() => editor.chain().focus().toggleBulletList().run()}
            className={`p-2 rounded hover:bg-gray-200 dark:hover:bg-gray-700 ${editor.isActive('bulletList') ? 'bg-gray-200 dark:bg-gray-700 text-blue-600 dark:text-blue-400' : 'text-gray-600 dark:text-gray-400'}`}
            title="Bullet List"
        >
            <List size={16} />
        </button>
        <button
            onClick={() => editor.chain().focus().toggleOrderedList().run()}
            className={`p-2 rounded hover:bg-gray-200 dark:hover:bg-gray-700 ${editor.isActive('orderedList') ? 'bg-gray-200 dark:bg-gray-700 text-blue-600 dark:text-blue-400' : 'text-gray-600 dark:text-gray-400'}`}
            title="Ordered List"
        >
            <ListOrdered size={16} />
        </button>
        <button
            onClick={() => editor.chain().focus().toggleBlockquote().run()}
            className={`p-2 rounded hover:bg-gray-200 dark:hover:bg-gray-700 ${editor.isActive('blockquote') ? 'bg-gray-200 dark:bg-gray-700 text-blue-600 dark:text-blue-400' : 'text-gray-600 dark:text-gray-400'}`}
            title="Blockquote"
        >
            <Quote size={16} />
        </button>
      </div>
      )}

      <div className="flex-1 overflow-y-auto p-4">
         <EditorContent editor={editor} />
      </div>
    </div>
  );
}
