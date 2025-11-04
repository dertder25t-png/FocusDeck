import { EmptyState } from '../components/States'
import { Card, CardContent } from '../components/Card'

export function DesignPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Design Projects</h1>
          <p className="text-sm text-gray-400 mt-1">
            Generate design ideas, concepts, and references for your projects
          </p>
        </div>
        <button className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 transition-colors">
          + New Project
        </button>
      </div>

      <Card>
        <CardContent className="py-12">
          <EmptyState
            icon="🎨"
            title="No design projects yet"
            description="Create a design project to generate thumbnails, concepts, and references"
            action={{
              label: 'Create Project',
              onClick: () => console.log('Create project'),
            }}
          />
        </CardContent>
      </Card>
    </div>
  )
}
