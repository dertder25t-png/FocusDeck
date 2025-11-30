
import React, { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../components/Dialog';
import { Mail, Send, Paperclip, Trash2, Archive, Star, Inbox } from 'lucide-react';

interface Email {
    id: string;
    from: string;
    subject: string;
    preview: string;
    date: string;
    isUnread: boolean;
    body: string;
}

const MOCK_EMAILS: Email[] = [
    { id: '1', from: 'Alice Smith', subject: 'Project Update: Q4 Roadmap', preview: 'Hey team, just wanted to share the latest updates...', date: '10:42 AM', isUnread: true, body: 'Hey team,\n\nJust wanted to share the latest updates on the Q4 roadmap. We are making good progress on the backend integration.' },
    { id: '2', from: 'GitHub Notifications', subject: 'New Pull Request: Feature/Kanban', preview: '@jules opened a new pull request...', date: 'Yesterday', isUnread: false, body: 'You have a new pull request.' },
    { id: '3', from: 'Newsletter', subject: 'Weekly Tech Digest', preview: 'Top stories in tech this week...', date: 'Mon', isUnread: false, body: 'Here are the top stories...' },
];

export const EmailApp: React.FC = () => {
    const [emails] = useState(MOCK_EMAILS);
    const [selectedEmail, setSelectedEmail] = useState<Email | null>(MOCK_EMAILS[0]);
    const [composeOpen, setComposeOpen] = useState(false);

    return (
        <div className="flex h-full bg-white dark:bg-gray-900 overflow-hidden">
            {/* Sidebar */}
            <div className="w-48 bg-gray-50 dark:bg-gray-950 border-r border-gray-200 dark:border-gray-700 flex flex-col shrink-0">
                <div className="p-4">
                    <button
                        onClick={() => setComposeOpen(true)}
                        className="w-full py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm flex items-center justify-center gap-2 transition-colors"
                    >
                        <Send size={16} /> Compose
                    </button>
                </div>
                <div className="flex-1 overflow-y-auto px-2 space-y-1">
                    <button className="w-full text-left px-3 py-2 rounded-md bg-white dark:bg-gray-800 shadow-sm text-blue-600 font-medium text-sm flex items-center gap-3">
                        <Inbox size={16} /> Inbox <span className="ml-auto text-xs bg-blue-100 text-blue-700 px-1.5 rounded-full">4</span>
                    </button>
                    <button className="w-full text-left px-3 py-2 rounded-md hover:bg-gray-200 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300 text-sm flex items-center gap-3">
                        <Star size={16} /> Starred
                    </button>
                    <button className="w-full text-left px-3 py-2 rounded-md hover:bg-gray-200 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300 text-sm flex items-center gap-3">
                        <Send size={16} /> Sent
                    </button>
                    <button className="w-full text-left px-3 py-2 rounded-md hover:bg-gray-200 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300 text-sm flex items-center gap-3">
                        <Trash2 size={16} /> Trash
                    </button>
                </div>
            </div>

            {/* Email List */}
            <div className="w-80 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-white dark:bg-gray-900 shrink-0">
                <div className="p-3 border-b border-gray-200 dark:border-gray-700">
                    <input className="w-full bg-gray-100 dark:bg-gray-800 px-3 py-1.5 rounded-md text-sm outline-none" placeholder="Search mail..." />
                </div>
                <div className="flex-1 overflow-y-auto">
                    {emails.map(email => (
                        <div
                            key={email.id}
                            onClick={() => setSelectedEmail(email)}
                            className={`p-4 border-b border-gray-100 dark:border-gray-800 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors ${selectedEmail?.id === email.id ? 'bg-blue-50 dark:bg-blue-900/20 border-l-4 border-l-blue-500' : 'border-l-4 border-l-transparent'}`}
                        >
                            <div className="flex justify-between items-start mb-1">
                                <span className={`text-sm ${email.isUnread ? 'font-bold text-gray-900 dark:text-white' : 'font-medium text-gray-700 dark:text-gray-300'}`}>{email.from}</span>
                                <span className="text-xs text-gray-400 whitespace-nowrap ml-2">{email.date}</span>
                            </div>
                            <div className={`text-sm mb-1 truncate ${email.isUnread ? 'font-bold text-gray-800 dark:text-gray-200' : 'text-gray-600 dark:text-gray-400'}`}>{email.subject}</div>
                            <div className="text-xs text-gray-400 truncate">{email.preview}</div>
                        </div>
                    ))}
                </div>
            </div>

            {/* Reading Pane */}
            <div className="flex-1 flex flex-col bg-white dark:bg-gray-900">
                {selectedEmail ? (
                    <>
                        <div className="p-6 border-b border-gray-200 dark:border-gray-700 flex justify-between items-start">
                            <div>
                                <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-2">{selectedEmail.subject}</h2>
                                <div className="flex items-center gap-3">
                                    <div className="w-10 h-10 rounded-full bg-purple-100 text-purple-600 flex items-center justify-center font-bold text-lg">
                                        {selectedEmail.from[0]}
                                    </div>
                                    <div>
                                        <div className="font-medium text-sm text-gray-900 dark:text-white">{selectedEmail.from}</div>
                                        <div className="text-xs text-gray-500">to me</div>
                                    </div>
                                </div>
                            </div>
                            <div className="flex gap-2 text-gray-400">
                                <button className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-full"><Archive size={18} /></button>
                                <button className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-full"><Trash2 size={18} /></button>
                            </div>
                        </div>
                        <div className="p-8 flex-1 overflow-y-auto prose dark:prose-invert max-w-none">
                            <p className="whitespace-pre-wrap">{selectedEmail.body}</p>
                        </div>
                    </>
                ) : (
                    <div className="flex-1 flex items-center justify-center text-gray-400">
                        <div className="text-center">
                            <Mail size={48} className="mx-auto mb-4 opacity-50" />
                            <p>Select an email to read</p>
                        </div>
                    </div>
                )}
            </div>

            {/* Compose Modal */}
            <Dialog open={composeOpen} onOpenChange={setComposeOpen}>
                <DialogContent className="sm:max-w-[600px]">
                    <DialogHeader>
                        <DialogTitle>New Message</DialogTitle>
                    </DialogHeader>
                    <div className="flex flex-col gap-4 py-4">
                        <input className="w-full px-3 py-2 border-b border-gray-200 dark:border-gray-700 bg-transparent outline-none" placeholder="To" />
                        <input className="w-full px-3 py-2 border-b border-gray-200 dark:border-gray-700 bg-transparent outline-none" placeholder="Subject" />
                        <textarea className="w-full h-64 p-3 bg-transparent outline-none resize-none" placeholder="Write your message..."></textarea>
                    </div>
                    <DialogFooter className="justify-between items-center w-full sm:justify-between">
                        <button className="text-gray-400 hover:text-gray-600"><Paperclip size={20} /></button>
                        <div className="flex gap-2">
                             <button onClick={() => setComposeOpen(false)} className="px-4 py-2 text-sm font-medium hover:bg-gray-100 dark:hover:bg-gray-800 rounded">Discard</button>
                             <button onClick={() => setComposeOpen(false)} className="px-6 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700">Send</button>
                        </div>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
};
