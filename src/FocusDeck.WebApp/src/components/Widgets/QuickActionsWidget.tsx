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
    if (isMobile) {
        navigate('/lectures/new');
    } else {
        // Desktop fallback: Open Jarvis
        launchApp('win-jarvis');
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
