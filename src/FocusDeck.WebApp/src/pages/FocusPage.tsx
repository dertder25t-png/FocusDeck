import { EmptyState } from '../components/States'
import { Card, CardContent } from '../components/Card'

export function FocusPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Focus Sessions</h1>
        <p className="text-sm text-gray-400 mt-1">
          Start focus sessions and track your productivity
        </p>
      </div>

      <Card>
        <CardContent className="py-12">
          <EmptyState
            icon="⚡"
            title="No active focus session"
            description="Start a focus session to track your productivity and minimize distractions"
            action={{
              label: 'Start Session',
              onClick: () => console.log('Start focus'),
            }}
          />
        </CardContent>
      </Card>
    </div>
  )
}
