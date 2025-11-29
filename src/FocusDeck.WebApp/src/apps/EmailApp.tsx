import React, { useState } from 'react';
import { cn } from '../lib/utils';

interface Email {
  id: string;
  sender: string;
  subject: string;
  preview: string;
  time: string;
  isRead: boolean;
  folder: 'inbox' | 'sent' | 'trash';
  body: string; // Full content
}

const MOCK_EMAILS: Email[] = [
  { id: '1', sender: 'Prof. Dumbledore', subject: 'End of Term Feast', preview: 'Please ensure all students are present...', time: '10:30 AM', isRead: false, folder: 'inbox', body: 'Please ensure all students are present in the Great Hall by 6 PM for the End of Term Feast. House points will be awarded.' },
  { id: '2', sender: 'Hagrid', subject: 'Urgent: Dragon Egg', preview: 'Can you come down to the hut?', time: 'Yesterday', isRead: true, folder: 'inbox', body: 'Can you come down to the hut? I think it is hatching soon. Don\'t tell anyone!' },
  { id: '3', sender: 'Ministry of Magic', subject: 'Internship Application', preview: 'We have received your application...', time: 'Mon', isRead: true, folder: 'sent', body: 'We have received your application for the Department of Mysteries internship. We will be in touch.' },
];

export const EmailApp: React.FC = () => {
  const [activeFolder, setActiveFolder] = useState<'inbox' | 'sent' | 'trash'>('inbox');
  const [selectedEmailId, setSelectedEmailId] = useState<string | null>(null);
  const [emails, setEmails] = useState(MOCK_EMAILS);

  // Compose Modal State
  const [isComposeOpen, setIsComposeOpen] = useState(false);
  const [composeTo, setComposeTo] = useState('');
  const [composeSubject, setComposeSubject] = useState('');
  const [composeBody, setComposeBody] = useState('');

  const filteredEmails = emails.filter(e => e.folder === activeFolder);
  const selectedEmail = emails.find(e => e.id === selectedEmailId);

  const handleSend = () => {
      const newEmail: Email = {
          id: Date.now().toString(),
          sender: 'Me',
          subject: composeSubject,
          preview: composeBody.substring(0, 30) + '...',
          time: 'Just now',
          isRead: true,
          folder: 'sent',
          body: composeBody
      };
      setEmails(prev => [newEmail, ...prev]);
      setIsComposeOpen(false);
      setComposeTo('');
      setComposeSubject('');
      setComposeBody('');
      setActiveFolder('sent');
      setSelectedEmailId(newEmail.id);
  };

  return (
    <div className="flex-1 flex overflow-hidden bg-white h-full relative">

      {/* 1. Sidebar */}
      <div className="w-16 md:w-48 border-r border-border bg-gray-50 flex flex-col shrink-0">
          <div className="p-4">
              <button
                onClick={() => setIsComposeOpen(true)}
                className="w-full bg-accent-blue text-white rounded-lg py-3 px-2 flex items-center justify-center gap-2 hover:bg-blue-600 transition-colors shadow-sm"
              >
                  <i className="fa-solid fa-pen-to-square"></i> <span className="hidden md:inline font-bold text-sm">Compose</span>
              </button>
          </div>
          <div className="flex-1 overflow-y-auto px-2 space-y-1">
              <button onClick={() => setActiveFolder('inbox')} className={cn("w-full text-left px-3 py-2 rounded-lg flex items-center gap-3 text-sm font-medium", activeFolder === 'inbox' ? "bg-white text-ink shadow-sm ring-1 ring-black/5" : "text-gray-500 hover:bg-gray-100")}>
                  <i className="fa-solid fa-inbox w-4"></i> <span className="hidden md:inline">Inbox</span>
                  {emails.filter(e => e.folder === 'inbox' && !e.isRead).length > 0 && <span className="hidden md:flex ml-auto bg-accent-blue text-white text-[10px] font-bold px-1.5 py-0.5 rounded-full">{emails.filter(e => e.folder === 'inbox' && !e.isRead).length}</span>}
              </button>
              <button onClick={() => setActiveFolder('sent')} className={cn("w-full text-left px-3 py-2 rounded-lg flex items-center gap-3 text-sm font-medium", activeFolder === 'sent' ? "bg-white text-ink shadow-sm ring-1 ring-black/5" : "text-gray-500 hover:bg-gray-100")}>
                  <i className="fa-solid fa-paper-plane w-4"></i> <span className="hidden md:inline">Sent</span>
              </button>
              <button onClick={() => setActiveFolder('trash')} className={cn("w-full text-left px-3 py-2 rounded-lg flex items-center gap-3 text-sm font-medium", activeFolder === 'trash' ? "bg-white text-ink shadow-sm ring-1 ring-black/5" : "text-gray-500 hover:bg-gray-100")}>
                  <i className="fa-solid fa-trash w-4"></i> <span className="hidden md:inline">Trash</span>
              </button>
          </div>
      </div>

      {/* 2. Email List */}
      <div className={cn("w-full md:w-80 border-r border-border bg-white flex flex-col transition-all duration-300", selectedEmailId ? "hidden md:flex" : "flex")}>
          <div className="p-4 border-b border-border">
              <h2 className="font-bold text-lg capitalize">{activeFolder}</h2>
          </div>
          <div className="flex-1 overflow-y-auto">
              {filteredEmails.length === 0 ? (
                  <div className="p-8 text-center text-gray-400 text-sm">Folder is empty</div>
              ) : (
                  filteredEmails.map(email => (
                      <div
                        key={email.id}
                        onClick={() => setSelectedEmailId(email.id)}
                        className={cn(
                            "p-4 border-b border-gray-100 cursor-pointer hover:bg-gray-50 transition-colors",
                            selectedEmailId === email.id ? "bg-blue-50/50" : "",
                            !email.isRead && "bg-white"
                        )}
                      >
                          <div className="flex justify-between mb-1">
                              <span className={cn("text-sm text-ink truncate max-w-[70%]", !email.isRead && "font-bold")}>{email.sender}</span>
                              <span className="text-xs text-gray-400 whitespace-nowrap ml-2">{email.time}</span>
                          </div>
                          <div className={cn("text-xs mb-1 truncate", !email.isRead ? "font-bold text-ink" : "text-gray-700")}>{email.subject}</div>
                          <div className="text-xs text-gray-400 line-clamp-2">{email.preview}</div>
                      </div>
                  ))
              )}
          </div>
      </div>

      {/* 3. Reading Pane */}
      <div className={cn("flex-1 bg-gray-50 flex flex-col transition-all duration-300", !selectedEmailId ? "hidden md:flex items-center justify-center" : "flex absolute md:relative inset-0 z-20")}>
          {!selectedEmail ? (
              <div className="text-center text-gray-400">
                  <i className="fa-regular fa-envelope-open text-4xl mb-4 opacity-50"></i>
                  <p>Select an email to read</p>
              </div>
          ) : (
              <div className="flex flex-col h-full bg-white md:bg-transparent">
                  {/* Mobile Header for Reading Pane */}
                  <div className="md:hidden h-14 border-b border-gray-200 flex items-center px-4 bg-white shrink-0">
                      <button onClick={() => setSelectedEmailId(null)} className="text-gray-500 mr-4"><i className="fa-solid fa-arrow-left"></i></button>
                      <span className="font-bold text-sm">Read</span>
                  </div>

                  <div className="p-6 md:p-8 overflow-y-auto flex-1 bg-white md:m-4 md:rounded-xl md:shadow-sm md:border md:border-gray-200">
                      <div className="flex justify-between items-start mb-6">
                          <h1 className="text-2xl font-bold text-ink">{selectedEmail.subject}</h1>
                          <div className="text-xs text-gray-400">{selectedEmail.time}</div>
                      </div>

                      <div className="flex items-center gap-3 mb-8 pb-6 border-b border-gray-100">
                          <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-400 to-indigo-500 flex items-center justify-center text-white font-bold">
                              {selectedEmail.sender[0]}
                          </div>
                          <div>
                              <div className="text-sm font-bold text-ink">{selectedEmail.sender}</div>
                              <div className="text-xs text-gray-500">to me</div>
                          </div>
                      </div>

                      <div className="prose prose-sm max-w-none text-ink leading-relaxed whitespace-pre-wrap">
                          {selectedEmail.body}
                      </div>
                  </div>
              </div>
          )}
      </div>

      {/* Compose Modal Overlay */}
      {isComposeOpen && (
          <div className="absolute inset-0 z-50 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
              <div className="bg-white w-full max-w-2xl rounded-xl shadow-2xl flex flex-col max-h-[90%] border border-gray-200 animate-in fade-in zoom-in duration-200">
                  <div className="flex items-center justify-between p-4 border-b border-gray-100">
                      <h3 className="font-bold text-ink">New Message</h3>
                      <button onClick={() => setIsComposeOpen(false)} className="text-gray-400 hover:text-red-500"><i className="fa-solid fa-xmark"></i></button>
                  </div>
                  <div className="p-4 flex flex-col gap-4 flex-1 overflow-y-auto">
                      <input
                        type="text"
                        placeholder="To"
                        value={composeTo}
                        onChange={(e) => setComposeTo(e.target.value)}
                        className="w-full px-0 py-2 border-b border-gray-200 outline-none text-sm font-medium placeholder-gray-400 focus:border-accent-blue transition-colors"
                      />
                      <input
                        type="text"
                        placeholder="Subject"
                        value={composeSubject}
                        onChange={(e) => setComposeSubject(e.target.value)}
                        className="w-full px-0 py-2 border-b border-gray-200 outline-none text-sm font-medium placeholder-gray-400 focus:border-accent-blue transition-colors"
                      />
                      <textarea
                        placeholder="Write something..."
                        value={composeBody}
                        onChange={(e) => setComposeBody(e.target.value)}
                        className="w-full flex-1 resize-none outline-none text-sm leading-relaxed placeholder-gray-300 min-h-[200px]"
                      />
                  </div>
                  <div className="p-4 border-t border-gray-100 flex justify-end gap-2 bg-gray-50 rounded-b-xl">
                      <button onClick={() => setIsComposeOpen(false)} className="px-4 py-2 text-sm font-bold text-gray-500 hover:bg-gray-200 rounded-lg transition-colors">Discard</button>
                      <button
                        onClick={handleSend}
                        disabled={!composeTo || !composeSubject}
                        className="px-6 py-2 text-sm font-bold bg-accent-blue text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shadow-sm"
                      >
                          Send
                      </button>
                  </div>
              </div>
          </div>
      )}

    </div>
  );
};
