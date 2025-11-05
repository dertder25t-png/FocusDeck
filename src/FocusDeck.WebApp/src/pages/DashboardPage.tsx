import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card'

export function DashboardPage() {
  return (
    <div className="space-y-6">
      {/* Welcome Card */}
      <Card className="bg-gradient-to-r from-primary/20 to-purple-900/20 border-primary/20">
        <CardHeader>
          <CardTitle className="text-2xl">Welcome to FocusDeck</CardTitle>
          <CardDescription>
            Your productivity companion for lectures, focus sessions, notes, and design work.
          </CardDescription>
        </CardHeader>
      </Card>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardContent className="pt-6">
            <div className="text-gray-400 text-sm mb-1">Total Lectures</div>
            <div className="text-2xl font-semibold">0</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="text-gray-400 text-sm mb-1">Focus Time</div>
            <div className="text-2xl font-semibold">0h</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="text-gray-400 text-sm mb-1">Notes Verified</div>
            <div className="text-2xl font-semibold">0</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="text-gray-400 text-sm mb-1">Design Projects</div>
            <div className="text-2xl font-semibold">0</div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Activity */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Activity</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center text-gray-400 py-8">
            No recent activity yet. Start by creating your first lecture or focus session!
          </div>
        </CardContent>
      </Card>

      {/* Quick Actions */}
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
    </div>
  )
}
