import { useState, useEffect } from 'react'
import { EmptyState } from '../components/States'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '../components/Card'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose } from '../components/Dialog'
import { Button } from '../components/Button'
import { Input } from '../components/Input'
import { Badge } from '../components/Badge'
import { FileUpload } from '../components/FileUpload'

interface Course {
  id: string
  name: string
  code?: string
  instructor?: string
}

interface Lecture {
  id: string
  courseId: string
  title: string
  description?: string
  recordedAt: string
  createdAt: string
  audioAssetId?: string
  status: string
  transcriptionText?: string
  summaryText?: string
  generatedNoteId?: string
  durationSeconds?: number
}

export function LecturesPage() {
  const [lectures, setLectures] = useState<Lecture[]>([])
  const [courses, setCourses] = useState<Course[]>([])
  const [selectedLecture, setSelectedLecture] = useState<Lecture | null>(null)
  const [isNewLectureOpen, setIsNewLectureOpen] = useState(false)
  const [isUploadOpen, setIsUploadOpen] = useState(false)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [loading, setLoading] = useState(false)

  // New lecture form
  const [newLecture, setNewLecture] = useState({
    courseId: '',
    title: '',
    description: '',
    recordedAt: new Date().toISOString().split('T')[0]
  })

  useEffect(() => {
    loadCourses()
    loadLectures()
  }, [])

  const loadCourses = async () => {
    try {
      const response = await fetch('/v1/courses', {
        headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
      })
      if (response.ok) {
        const data = await response.json()
        setCourses(data)
      }
    } catch (error) {
      console.error('Failed to load courses:', error)
    }
  }

  const loadLectures = async () => {
    // For now, return empty array - in real app, would fetch from API
    // The /lectures endpoint would need to be added to list all lectures
    setLectures([])
  }

  const createLecture = async () => {
    setLoading(true)
    try {
      const response = await fetch('/v1/lectures', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({
          ...newLecture,
          recordedAt: new Date(newLecture.recordedAt).toISOString()
        })
      })

      if (response.ok) {
        const lecture = await response.json()
        setLectures(prev => [lecture, ...prev])
        setIsNewLectureOpen(false)
        setSelectedLecture(lecture)
        setIsUploadOpen(true)
        // Reset form
        setNewLecture({
          courseId: '',
          title: '',
          description: '',
          recordedAt: new Date().toISOString().split('T')[0]
        })
      }
    } catch (error) {
      console.error('Failed to create lecture:', error)
    } finally {
      setLoading(false)
    }
  }

  const uploadAudio = async (file: File) => {
    if (!selectedLecture) return

    const formData = new FormData()
    formData.append('audio', file)

    try {
      const response = await fetch(`/v1/lectures/${selectedLecture.id}/audio`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: formData
      })

      if (response.ok) {
        const result = await response.json()
        setLectures(prev => prev.map(l =>
          l.id === selectedLecture.id
            ? { ...l, audioAssetId: result.audioAssetId, status: result.status }
            : l
        ))
        setSelectedLecture(prev => prev ? { ...prev, audioAssetId: result.audioAssetId, status: result.status } : null)
        setIsUploadOpen(false)
      }
    } catch (error) {
      console.error('Failed to upload audio:', error)
      throw error
    }
  }

  const processLecture = async (lectureId: string) => {
    try {
      const response = await fetch(`/v1/lectures/${lectureId}/process`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (response.ok) {
        setLectures(prev => prev.map(l =>
          l.id === lectureId ? { ...l, status: 'Transcribing' } : l
        ))
      }
    } catch (error) {
      console.error('Failed to process lecture:', error)
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Created': return 'default'
      case 'AudioUploaded': return 'info'
      case 'Transcribing': case 'Summarizing': case 'GeneratingNotes': return 'warning'
      case 'Transcribed': case 'Summarized': case 'Completed': return 'success'
      case 'Failed': return 'danger'
      default: return 'default'
    }
  }

  const formatDuration = (seconds?: number) => {
    if (!seconds) return 'Unknown'
    const mins = Math.floor(seconds / 60)
    const secs = seconds % 60
    return `${mins}:${secs.toString().padStart(2, '0')}`
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Lectures</h1>
          <p className="text-sm text-gray-400 mt-1">
            Upload, transcribe, and process your lecture recordings
          </p>
        </div>
        <Button onClick={() => setIsNewLectureOpen(true)}>
          + New Lecture
        </Button>
      </div>

      {lectures.length === 0 ? (
        <Card>
          <CardContent className="py-12">
            <EmptyState
              icon="🎓"
              title="No lectures yet"
              description="Upload your first lecture recording to get started with AI transcription and summarization"
              action={{
                label: 'Create Lecture',
                onClick: () => setIsNewLectureOpen(true),
              }}
            />
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4">
          {lectures.map(lecture => (
            <Card key={lecture.id} className="hover:border-gray-700 transition-colors cursor-pointer"
              onClick={() => { setSelectedLecture(lecture); setIsDetailOpen(true); }}>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <CardTitle>{lecture.title}</CardTitle>
                    {lecture.description && (
                      <CardDescription className="mt-1">{lecture.description}</CardDescription>
                    )}
                  </div>
                  <Badge variant={getStatusColor(lecture.status)}>{lecture.status}</Badge>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex items-center gap-6 text-sm text-gray-400">
                  <span>📅 {new Date(lecture.recordedAt).toLocaleDateString()}</span>
                  {lecture.durationSeconds && (
                    <span>⏱️ {formatDuration(lecture.durationSeconds)}</span>
                  )}
                  {lecture.audioAssetId && <span>🎵 Audio uploaded</span>}
                  {lecture.transcriptionText && <span>📝 Transcribed</span>}
                  {lecture.summaryText && <span>📋 Summarized</span>}
                </div>
                {lecture.audioAssetId && lecture.status === 'AudioUploaded' && (
                  <Button
                    variant="primary"
                    size="sm"
                    className="mt-4"
                    onClick={(e) => { e.stopPropagation(); processLecture(lecture.id); }}
                  >
                    Process Lecture
                  </Button>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* New Lecture Dialog */}
      <Dialog open={isNewLectureOpen} onOpenChange={setIsNewLectureOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create New Lecture</DialogTitle>
            <DialogDescription>
              Enter the lecture details. You'll be able to upload audio next.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-1">Course</label>
              <select
                value={newLecture.courseId}
                onChange={(e) => setNewLecture(prev => ({ ...prev, courseId: e.target.value }))}
                className="w-full px-3 py-2 bg-[#1a1a1c] border border-gray-800 rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="">Select a course</option>
                {courses.map(course => (
                  <option key={course.id} value={course.id}>
                    {course.code ? `${course.code} - ` : ''}{course.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">Title</label>
              <Input
                value={newLecture.title}
                onChange={(e) => setNewLecture(prev => ({ ...prev, title: e.target.value }))}
                placeholder="e.g., Week 5 - Introduction to Neural Networks"
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">Description (optional)</label>
              <Input
                value={newLecture.description}
                onChange={(e) => setNewLecture(prev => ({ ...prev, description: e.target.value }))}
                placeholder="Brief description of the lecture content"
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">Recorded Date</label>
              <Input
                type="date"
                value={newLecture.recordedAt}
                onChange={(e) => setNewLecture(prev => ({ ...prev, recordedAt: e.target.value }))}
              />
            </div>
          </div>

          <DialogFooter>
            <DialogClose asChild>
              <Button variant="secondary">Cancel</Button>
            </DialogClose>
            <Button
              onClick={createLecture}
              disabled={!newLecture.courseId || !newLecture.title || loading}
            >
              {loading ? 'Creating...' : 'Create & Upload Audio'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Upload Audio Dialog */}
      <Dialog open={isUploadOpen} onOpenChange={setIsUploadOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Upload Audio</DialogTitle>
            <DialogDescription>
              Upload the audio recording for "{selectedLecture?.title}"
            </DialogDescription>
          </DialogHeader>

          <FileUpload
            accept="audio/*"
            maxSize={50 * 1024 * 1024} // 50MB
            onUpload={uploadAudio}
          />

          <DialogFooter>
            <DialogClose asChild>
              <Button variant="secondary">Skip for Now</Button>
            </DialogClose>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Lecture Detail Dialog */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-3xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{selectedLecture?.title}</DialogTitle>
            <DialogDescription>
              {selectedLecture?.description}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="flex items-center gap-4">
              <Badge variant={selectedLecture ? getStatusColor(selectedLecture.status) : 'default'}>
                {selectedLecture?.status}
              </Badge>
              <span className="text-sm text-gray-400">
                📅 {selectedLecture && new Date(selectedLecture.recordedAt).toLocaleDateString()}
              </span>
              {selectedLecture?.durationSeconds && (
                <span className="text-sm text-gray-400">
                  ⏱️ {formatDuration(selectedLecture.durationSeconds)}
                </span>
              )}
            </div>

            {selectedLecture?.transcriptionText && (
              <div>
                <h3 className="font-semibold mb-2">Transcript</h3>
                <div className="p-4 bg-[#1a1a1c] border border-gray-800 rounded-md text-sm">
                  {selectedLecture.transcriptionText}
                </div>
              </div>
            )}

            {selectedLecture?.summaryText && (
              <div>
                <h3 className="font-semibold mb-2">Summary</h3>
                <div className="p-4 bg-[#1a1a1c] border border-gray-800 rounded-md text-sm">
                  {selectedLecture.summaryText}
                </div>
              </div>
            )}

            {selectedLecture?.generatedNoteId && (
              <div>
                <h3 className="font-semibold mb-2">Generated Note</h3>
                <Button variant="secondary" size="sm">
                  View Note
                </Button>
              </div>
            )}

            {selectedLecture?.audioAssetId && !selectedLecture.transcriptionText && selectedLecture.status === 'AudioUploaded' && (
              <Button onClick={() => selectedLecture && processLecture(selectedLecture.id)}>
                Process Lecture (Transcribe & Summarize)
              </Button>
            )}
          </div>

          <DialogFooter>
            <DialogClose asChild>
              <Button variant="secondary">Close</Button>
            </DialogClose>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
