import { EmptyState } from '../components/States'
import { Card, CardContent } from '../components/Card'

export function NotesPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Notes</h1>
        <p className="text-sm text-gray-400 mt-1">
          Verify and enhance your study notes with AI suggestions
        </p>
      </div>

      <Card>
        <CardContent className="py-12">
          <EmptyState
            icon="📝"
            title="No notes to verify"
            description="Create lecture notes and use AI to verify completeness and get suggestions"
          />
        </CardContent>
      </Card>
    </div>
  )
}
