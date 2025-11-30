import React, { useState, useRef, useEffect } from 'react';
import { MessageBubble } from '../components/Jarvis/MessageBubble';
import type { Message } from '../components/Jarvis/MessageBubble';
import { cn } from '../lib/utils';

type JarvisContext = 'general' | 'coding' | 'email' | 'brainstorming';

const CONTEXTS: Record<JarvisContext, { label: string; icon: string; description: string }> = {
  'general': { label: 'General Chat', icon: 'fa-comments', description: 'Helpful assistant for any task' },
  'coding': { label: 'Code Companion', icon: 'fa-code', description: 'Optimized for programming queries' },
  'email': { label: 'Email Drafter', icon: 'fa-envelope-open-text', description: 'Assistance with writing emails' },
  'brainstorming': { label: 'Brainstorming', icon: 'fa-lightbulb', description: 'Generate creative ideas' },
};

export const JarvisApp: React.FC = () => {
  const [messages, setMessages] = useState<Message[]>([
    { id: '1', role: 'system', content: 'Hello! I am Jarvis. How can I help you today?', timestamp: new Date() }
  ]);
  const [inputValue, setInputValue] = useState('');
  const [currentContext, setCurrentContext] = useState<JarvisContext>('general');
  const [isContextOpen, setIsContextOpen] = useState(true); // Default open on desktop
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

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
        content: `**[${CONTEXTS[currentContext].label}]** I received: "${newMessage.content}". \n\nHere is a *markdown* example:\n\`\`\`javascript\nconsole.log("Hello World");\n\`\`\``,
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

  return (
    <div className="flex h-full bg-paper dark:bg-gray-900 text-ink dark:text-white">
      {/* Context Sidebar */}
      <div className={cn(
          "w-64 bg-surface-50 dark:bg-gray-800 border-r border-border dark:border-gray-700 flex flex-col transition-all duration-300",
          !isContextOpen && "w-0 overflow-hidden border-0"
      )}>
          <div className="p-4 border-b border-border dark:border-gray-700 bg-subtle dark:bg-gray-800">
             <h3 className="font-bold text-sm uppercase text-gray-500 tracking-wider">Context Mode</h3>
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-1">
              {(Object.keys(CONTEXTS) as JarvisContext[]).map(ctx => (
                  <button
                    key={ctx}
                    onClick={() => setCurrentContext(ctx)}
                    className={cn(
                        "w-full flex items-center gap-3 px-3 py-3 rounded-lg text-left transition-colors",
                        currentContext === ctx
                            ? "bg-accent-blue/10 text-accent-blue border border-accent-blue/20"
                            : "hover:bg-surface-100 dark:hover:bg-gray-700 text-ink dark:text-gray-300"
                    )}
                  >
                      <div className={cn(
                          "w-8 h-8 rounded-full flex items-center justify-center shrink-0",
                          currentContext === ctx ? "bg-accent-blue text-white" : "bg-gray-200 dark:bg-gray-600 text-gray-500"
                      )}>
                          <i className={`fa-solid ${CONTEXTS[ctx].icon} text-xs`}></i>
                      </div>
                      <div>
                          <div className="font-bold text-sm leading-none mb-1">{CONTEXTS[ctx].label}</div>
                          <div className="text-[10px] opacity-70 leading-tight">{CONTEXTS[ctx].description}</div>
                      </div>
                  </button>
              ))}
          </div>
      </div>

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col relative">
         {/* Toggle Context Sidebar Button (Absolute) */}
         <button
            onClick={() => setIsContextOpen(!isContextOpen)}
            className="absolute top-4 left-4 z-10 p-2 bg-white/50 dark:bg-black/50 backdrop-blur rounded-md hover:bg-white dark:hover:bg-gray-700 shadow-sm border border-border transition-all"
            title="Toggle Context Menu"
         >
             <i className={`fa-solid ${isContextOpen ? 'fa-chevron-left' : 'fa-list'}`}></i>
         </button>

         {/* Messages */}
         <div className="flex-1 overflow-y-auto p-4 md:p-8 custom-scrollbar">
             <div className="max-w-3xl mx-auto">
                 {messages.map(msg => (
                     <MessageBubble key={msg.id} message={msg} />
                 ))}
                 <div ref={messagesEndRef} />
             </div>
         </div>

         {/* Input Area */}
         <div className="p-4 border-t border-border bg-surface-50 dark:bg-gray-800">
             <div className="max-w-3xl mx-auto flex gap-2">
                 <div className="flex-1 relative">
                     <textarea
                        value={inputValue}
                        onChange={(e) => setInputValue(e.target.value)}
                        onKeyDown={handleKeyDown}
                        placeholder={`Message Jarvis in ${CONTEXTS[currentContext].label}...`}
                        className="w-full pl-4 pr-12 py-3 rounded-xl border border-border bg-white dark:bg-gray-900 focus:outline-none focus:ring-2 focus:ring-accent-blue/50 resize-none h-[52px] max-h-32"
                        style={{ minHeight: '52px' }}
                     />
                     <button className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-ink">
                         <i className="fa-solid fa-paperclip"></i>
                     </button>
                 </div>
                 <button
                    onClick={handleSend}
                    disabled={!inputValue.trim()}
                    className="w-[52px] h-[52px] rounded-xl bg-accent-blue text-white flex items-center justify-center hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed shadow-md"
                 >
                     <i className="fa-solid fa-paper-plane"></i>
                 </button>
             </div>
             <div className="text-center mt-2 text-xs text-gray-400">
                 Jarvis can make mistakes. Please verify important information.
             </div>
         </div>
      </div>
    </div>
  );
};
