import { EmptyState } from '../components/States'
import { Card, CardContent } from '../components/Card'

export function LecturesPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Lectures</h1>
          <p className="text-sm text-gray-400 mt-1">
            Upload, transcribe, and process your lecture recordings
          </p>
        </div>
        <button className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 transition-colors">
          + New Lecture
        </button>
      </div>

      <Card>
        <CardContent className="py-12">
          <EmptyState
            icon="🎓"
            title="No lectures yet"
            description="Upload your first lecture recording to get started with AI transcription and summarization"
            action={{
              label: 'Upload Lecture',
              onClick: () => console.log('Upload lecture'),
            }}
          />
        </CardContent>
      </Card>
    </div>
  )
}
