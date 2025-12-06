import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../Card';
import { FocusSessionModal } from '../FocusSessionModal';
import { useWindowManager } from '../../contexts/WindowManagerContext';
import { useIsMobile } from '../../hooks/useIsMobile';

export function QuickActionsWidget() {
  const [showFocusModal, setShowFocusModal] = useState(false);
  const navigate = useNavigate();
  const { launchApp } = useWindowManager();
  const isMobile = useIsMobile();

  const handleNewLecture = () => {
    navigate('/lectures/new');
    if (!isMobile) {
        // Fallback or explicit open for desktop environment
        // Assuming 'win-lectures' or using 'win-jarvis' if lectures are part of it,
        // but strictly navigating is the primary request.
        // There is no explicit 'win-lectures' in DesktopLayout, so we rely on the route
        // or perhaps 'win-notes' if that's where lectures live.
        // However, the user asked to "Navigate to /lectures/new".
        // The previous code opened 'win-jarvis'.
        // If we want to be safe, we can try to open a relevant window if it exists.
        // For now, let's just navigate, as that updates the URL and DesktopLayout might not react
        // unless mapped. But the user was specific.

        // Actually, looking at DesktopLayout, there is no 'win-lectures'.
        // Lectures might be a sub-feature or a modal.
        // Let's stick to just navigate, or if we must launch an app,
        // maybe 'win-dashboard' is the safest fallback if it's a page.
    }
  };

  const handleVerifyNotes = () => {
    navigate('/notes?action=verify');
    if (!isMobile) {
        launchApp('win-notes');
    }
  };

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Quick Actions</CardTitle>
          <CardDescription>Get started with these common tasks</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <button
              onClick={handleNewLecture}
              className="flex items-center gap-3 p-4 rounded-lg border border-gray-700 hover:bg-surface-50 transition-colors text-left"
            >
              <span className="text-2xl">🎓</span>
              <div>
                <div className="font-medium">New Lecture</div>
                <div className="text-sm text-gray-400">Upload and process a lecture</div>
              </div>
            </button>
            <button
              onClick={() => setShowFocusModal(true)}
              className="flex items-center gap-3 p-4 rounded-lg border border-gray-700 hover:bg-surface-50 transition-colors text-left"
            >
              <span className="text-2xl">⚡</span>
              <div>
                <div className="font-medium">Start Focus Session</div>
                <div className="text-sm text-gray-400">Begin a timed focus session</div>
              </div>
            </button>
            <button
              onClick={handleVerifyNotes}
              className="flex items-center gap-3 p-4 rounded-lg border border-gray-700 hover:bg-surface-50 transition-colors text-left"
            >
              <span className="text-2xl">📝</span>
              <div>
                <div className="font-medium">Verify Notes</div>
                <div className="text-sm text-gray-400">Check note completeness with AI</div>
              </div>
            </button>
            <button
              onClick={() => isMobile ? navigate('/design/new') : launchApp('win-whiteboard')}
              className="flex items-center gap-3 p-4 rounded-lg border border-gray-700 hover:bg-surface-50 transition-colors text-left"
            >
              <span className="text-2xl">🎨</span>
              <div>
                <div className="font-medium">New Design Project</div>
                <div className="text-sm text-gray-400">Start a design ideation session</div>
              </div>
            </button>
          </div>
        </CardContent>
      </Card>

      <FocusSessionModal open={showFocusModal} onOpenChange={setShowFocusModal} />
    </>
  );
}
