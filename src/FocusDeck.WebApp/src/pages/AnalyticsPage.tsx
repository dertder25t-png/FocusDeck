import { Card, CardContent, CardHeader, CardTitle } from '../components/Card'

export function AnalyticsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Analytics</h1>
        <p className="text-sm text-gray-400 mt-1">
          Track your productivity insights and trends
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-400">This Week</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">0h</div>
            <p className="text-xs text-gray-500 mt-1">Focus time</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-400">Current Streak</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">0</div>
            <p className="text-xs text-gray-500 mt-1">days</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-400">Lectures Processed</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-semibold">0</div>
            <p className="text-xs text-gray-500 mt-1">this month</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Activity Chart</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="h-64 flex items-center justify-center text-gray-400">
            Chart visualization will appear here
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
