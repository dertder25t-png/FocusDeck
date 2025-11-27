import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../Card';

export function QuickActionsWidget() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Quick Actions</CardTitle>
        <CardDescription>Get started with these common tasks</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <button className="flex items-center gap-3 p-4 rounded-lg border border-gray-700 hover:bg-surface-50 transition-colors text-left">
            <span className="text-2xl">🎓</span>
            <div>
              <div className="font-medium">New Lecture</div>
              <div className="text-sm text-gray-400">Upload and process a lecture</div>
            </div>
          </button>
          <button className="flex items-center gap-3 p-4 rounded-lg border border-gray-700 hover:bg-surface-50 transition-colors text-left">
            <span className="text-2xl">⚡</span>
            <div>
              <div className="font-medium">Start Focus Session</div>
              <div className="text-sm text-gray-400">Begin a timed focus session</div>
            </div>
          </button>
          <button className="flex items-center gap-3 p-4 rounded-lg border border-gray-700 hover:bg-surface-50 transition-colors text-left">
            <span className="text-2xl">📝</span>
            <div>
              <div className="font-medium">Verify Notes</div>
              <div className="text-sm text-gray-400">Check note completeness with AI</div>
            </div>
          </button>
          <button className="flex items-center gap-3 p-4 rounded-lg border border-gray-700 hover:bg-surface-50 transition-colors text-left">
            <span className="text-2xl">🎨</span>
            <div>
              <div className="font-medium">New Design Project</div>
              <div className="text-sm text-gray-400">Start a design ideation session</div>
            </div>
          </button>
        </div>
      </CardContent>
    </Card>
  );
}
