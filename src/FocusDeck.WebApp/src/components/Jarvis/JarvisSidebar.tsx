import { useState, useEffect, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import { cn } from '../../lib/utils';
import { MessageBubble } from './MessageBubble';
import type { Message } from './MessageBubble';

// Placeholder function to simulate passing context to the Jarvis LLM
const sendContextToJarvis = (context: string) => {
  console.log(`Jarvis context: ${context}`);
};

export function JarvisSidebar() {
  const [isPinned, setIsPinned] = useState(false);
  const [isHovered, setIsHovered] = useState(false);
  const location = useLocation();

  const [messages, setMessages] = useState<Message[]>([
    { id: '1', role: 'system', content: 'Hi! I see you are working on something. How can I assist?', timestamp: new Date() }
  ]);
  const [inputValue, setInputValue] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    sendContextToJarvis(`I am looking at the ${location.pathname} page.`);
  }, [location]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, isPinned, isHovered]);

  const handleSend = () => {
      if (!inputValue.trim()) return;

      const newMessage: Message = {
        id: Date.now().toString(),
        role: 'user',
        content: inputValue,
        timestamp: new Date()
      };

      setMessages(prev => [...prev, newMessage]);
      setInputValue('');

      // Simulate Jarvis response
      setTimeout(() => {
        const response: Message = {
          id: (Date.now() + 1).toString(),
          role: 'system',
          content: `I am aware you are at **${location.pathname}**. Here is some help...`,
          timestamp: new Date()
        };
        setMessages(prev => [...prev, response]);
      }, 1000);
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
          e.preventDefault();
          handleSend();
        }
      };

  const isOpen = isPinned || isHovered;

  return (
    <>
      {/* Hover Trigger Area */}
      <div
        className="w-4 h-full fixed right-0 top-0 z-40"
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      />

      {/* Sidebar */}
      <aside
        className={cn(
          'fixed top-0 right-0 h-full w-96 bg-surface/95 backdrop-blur-lg border-l border-border shadow-2xl transition-transform duration-300 ease-in-out z-50 transform flex flex-col',
          isOpen ? 'translate-x-0' : 'translate-x-full'
        )}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        <div className="p-4 border-b border-border flex items-center justify-between bg-surface-100/50">
            <div className="flex items-center gap-2">
                <div className="w-2 h-2 rounded-full bg-green-500 animate-pulse"></div>
                <h2 className="text-lg font-semibold">Jarvis</h2>
            </div>
            <button
              onClick={() => setIsPinned(!isPinned)}
              className={cn("p-2 rounded-md hover:bg-surface-200 transition-colors", isPinned ? "text-accent-blue bg-blue-50" : "text-gray-500")}
              title={isPinned ? 'Unpin' : 'Pin'}
            >
              <i className="fa-solid fa-thumbtack transform rotate-45"></i>
            </button>
        </div>

        <div className="flex-1 overflow-y-auto p-4 custom-scrollbar bg-surface-50/50">
             {messages.map(msg => (
                 <MessageBubble key={msg.id} message={msg} />
             ))}
             <div ref={messagesEndRef} />
        </div>

        <div className="p-4 border-t border-border bg-surface-100/50">
            <div className="relative">
                <textarea
                    value={inputValue}
                    onChange={(e) => setInputValue(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder="Ask about this page..."
                    className="w-full pl-4 pr-10 py-3 rounded-xl border border-border bg-white focus:outline-none focus:ring-2 focus:ring-accent-blue/50 resize-none h-12 min-h-[48px] text-sm"
                />
                <button
                    onClick={handleSend}
                    disabled={!inputValue.trim()}
                    className="absolute right-2 top-1/2 -translate-y-1/2 text-accent-blue hover:text-blue-700 disabled:opacity-50"
                >
                    <i className="fa-solid fa-paper-plane"></i>
                </button>
            </div>
             <p className="text-[10px] text-gray-400 mt-2 text-center">Context: {location.pathname}</p>
        </div>
      </aside>
    </>
  );
}
