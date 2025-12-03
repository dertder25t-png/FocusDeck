
import { useState } from 'react'
import { Mail, Search, Edit2, Star, Trash2, Archive, Reply, MoreVertical } from 'lucide-react'
import { useEmails } from '../hooks/useEmails'
import type { Email } from '../hooks/useEmails'
import DOMPurify from 'dompurify'

export function EmailApp() {
  const { emails, isLoading, sendEmail } = useEmails()
  const [selectedEmail, setSelectedEmail] = useState<Email | null>(null)
  const [isComposing, setIsComposing] = useState(false)

  // Compose state
  const [to, setTo] = useState('')
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')

  const handleSend = async () => {
    try {
        await sendEmail({ to, subject, body });
        setIsComposing(false);
        setTo('');
        setSubject('');
        setBody('');
        // Optionally refresh list
    } catch (e) {
        alert('Failed to send email');
    }
  }

  // Helper to safely render HTML (stub for DOMPurify)
  const createMarkup = (html: string) => {
    // In a real app, use DOMPurify.sanitize(html)
    return { __html: DOMPurify.sanitize(html || '') };
  }

  return (
    <div className="flex h-full bg-surface-100 text-ink">
      {/* Sidebar List */}
      <div className="w-96 border-r border-surface-200 flex flex-col bg-surface-50">
        <div className="p-4 border-b border-surface-200">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-semibold text-lg flex items-center gap-2">
              <Mail className="w-5 h-5 text-primary" />
              Inbox
            </h2>
            <button
              onClick={() => setIsComposing(true)}
              className="p-2 bg-primary text-white rounded-lg hover:bg-primary/90 transition-colors"
            >
              <Edit2 className="w-4 h-4" />
            </button>
          </div>
          <div className="relative">
            <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" />
            <input
              type="text"
              placeholder="Search emails..."
              className="w-full pl-9 pr-4 py-2 bg-surface text-sm rounded-lg border border-surface-200 focus:border-primary focus:ring-1 focus:ring-primary outline-none"
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto">
            {isLoading ? (
                <div className="p-8 text-center text-ink-muted">Loading emails...</div>
            ) : (
                <div className="divide-y divide-surface-200">
                    {emails.map((email: Email) => (
                    <div
                        key={email.id}
                        onClick={() => setSelectedEmail(email)}
                        className={`p-4 cursor-pointer hover:bg-surface-200/50 transition-colors ${
                        selectedEmail?.id === email.id ? 'bg-surface-200 border-l-4 border-primary pl-3' : 'pl-4'
                        } ${!email.isRead ? 'bg-surface-50 font-medium' : 'opacity-80'}`}
                    >
                        <div className="flex items-start justify-between mb-1">
                        <h3 className="text-sm truncate pr-2 font-semibold text-ink">
                            {email.sender}
                        </h3>
                        <span className="text-[10px] text-ink-muted whitespace-nowrap">
                            {email.date}
                        </span>
                        </div>
                        <p className="text-sm text-ink mb-1 truncate">{email.subject}</p>
                        <p className="text-xs text-ink-muted line-clamp-2">
                        {email.snippet}
                        </p>
                    </div>
                    ))}
                </div>
            )}
        </div>
      </div>

      {/* Reading Pane */}
      <div className="flex-1 flex flex-col bg-surface h-full overflow-hidden">
        {selectedEmail ? (
          <>
            {/* Email Toolbar */}
            <div className="px-6 py-3 border-b border-surface-200 flex items-center justify-between bg-surface-50">
              <div className="flex items-center gap-1">
                <button className="p-2 text-ink-muted hover:text-ink hover:bg-surface-200 rounded-lg transition-colors" title="Archive">
                  <Archive className="w-4 h-4" />
                </button>
                <button className="p-2 text-ink-muted hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors" title="Delete">
                  <Trash2 className="w-4 h-4" />
                </button>
                <div className="w-px h-4 bg-surface-300 mx-1" />
                <button className="p-2 text-ink-muted hover:text-yellow-500 hover:bg-yellow-50 rounded-lg transition-colors" title="Star">
                  <Star className="w-4 h-4" />
                </button>
              </div>
              <div className="flex items-center gap-2">
                <button className="p-2 text-ink-muted hover:text-ink hover:bg-surface-200 rounded-lg transition-colors">
                   <Reply className="w-4 h-4" />
                </button>
                <button className="p-2 text-ink-muted hover:text-ink hover:bg-surface-200 rounded-lg transition-colors">
                   <MoreVertical className="w-4 h-4" />
                </button>
              </div>
            </div>

            {/* Email Content */}
            <div className="flex-1 overflow-y-auto p-8">
              <div className="max-w-3xl mx-auto">
                <h1 className="text-2xl font-bold text-ink mb-6">{selectedEmail.subject}</h1>

                <div className="flex items-start justify-between mb-8 pb-6 border-b border-surface-200">
                  <div className="flex items-center gap-4">
                    <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center text-primary font-bold text-lg">
                      {selectedEmail.sender[0]}
                    </div>
                    <div>
                      <div className="font-medium text-ink">{selectedEmail.sender}</div>
                      <div className="text-xs text-ink-muted">To: You</div>
                    </div>
                  </div>
                  <div className="text-xs text-ink-muted">
                    {selectedEmail.date}
                  </div>
                </div>

                <div
                    className="prose prose-sm max-w-none text-ink"
                    dangerouslySetInnerHTML={createMarkup(selectedEmail.bodyHtml || selectedEmail.snippet)}
                />
              </div>
            </div>
          </>
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center text-ink-muted bg-surface-50">
            <div className="w-16 h-16 bg-surface-200 rounded-2xl flex items-center justify-center mb-4">
              <Mail className="w-8 h-8 opacity-50" />
            </div>
            <p className="text-lg font-medium mb-1">Select an email to read</p>
            <p className="text-sm opacity-70">Or compose a new message</p>
          </div>
        )}
      </div>

      {/* Compose Modal */}
      {isComposing && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center">
          <div className="bg-surface w-full max-w-2xl rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
            <div className="px-6 py-4 border-b border-surface-200 flex items-center justify-between bg-surface-50">
              <h3 className="font-semibold text-lg">New Message</h3>
              <button
                onClick={() => setIsComposing(false)}
                className="text-ink-muted hover:text-ink"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="p-6 flex-1 overflow-y-auto">
              <div className="space-y-4">
                <input
                  type="text"
                  placeholder="To"
                  value={to}
                  onChange={(e) => setTo(e.target.value)}
                  className="w-full px-4 py-2 bg-surface text-sm rounded-lg border border-surface-200 focus:border-primary focus:ring-1 focus:ring-primary outline-none"
                />
                <input
                  type="text"
                  placeholder="Subject"
                  value={subject}
                  onChange={(e) => setSubject(e.target.value)}
                  className="w-full px-4 py-2 bg-surface text-sm rounded-lg border border-surface-200 focus:border-primary focus:ring-1 focus:ring-primary outline-none"
                />
                <textarea
                  placeholder="Write your message..."
                  value={body}
                  onChange={(e) => setBody(e.target.value)}
                  className="w-full h-64 px-4 py-3 bg-surface text-sm rounded-lg border border-surface-200 focus:border-primary focus:ring-1 focus:ring-primary outline-none resize-none"
                />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-surface-200 bg-surface-50 flex justify-end gap-3">
              <button
                onClick={() => setIsComposing(false)}
                className="px-4 py-2 text-sm font-medium text-ink-muted hover:text-ink transition-colors"
              >
                Discard
              </button>
              <button
                onClick={handleSend}
                className="px-6 py-2 text-sm font-medium bg-primary text-white rounded-lg hover:bg-primary/90 transition-colors shadow-sm"
              >
                Send Message
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function X({ className }: { className?: string }) {
    return (
        <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
        </svg>
    )
}
