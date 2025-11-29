import React from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { cn } from '../../lib/utils';

export interface Message {
  id: string;
  role: 'user' | 'system';
  content: string;
  timestamp: Date;
}

interface MessageBubbleProps {
  message: Message;
}

export const MessageBubble: React.FC<MessageBubbleProps> = ({ message }) => {
  const isUser = message.role === 'user';

  return (
    <div className={cn("flex w-full mb-4", isUser ? "justify-end" : "justify-start")}>
      <div
        className={cn(
          "max-w-[80%] rounded-2xl px-4 py-3 shadow-sm text-sm",
          isUser
            ? "bg-accent-blue text-white rounded-br-none"
            : "bg-surface-100 text-ink border border-border rounded-bl-none dark:bg-gray-800 dark:text-gray-100"
        )}
      >
        <div className="markdown-body">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
                {message.content}
            </ReactMarkdown>
        </div>
        <div className={cn("text-[10px] mt-1 opacity-70", isUser ? "text-blue-100 text-right" : "text-gray-400 text-left")}>
            {message.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </div>
      </div>
    </div>
  );
};
