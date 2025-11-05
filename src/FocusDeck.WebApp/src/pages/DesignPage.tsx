import { useState, useEffect } from 'react'
import { EmptyState } from '../components/States'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '../components/Card'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../components/Dialog'
import { Button } from '../components/Button'
import { Input } from '../components/Input'
import { Badge } from '../components/Badge'

interface DesignProject {
  id: string
  title: string
  goalsText?: string
  vibes: string[]
  requirementsText?: string
  brandKeywords: string[]
  createdAt: string
  ideaCount: number
  pinnedCount: number
}

interface DesignIdea {
  id: string
  type: 'Thumbnail' | 'Prompt' | 'Moodboard' | 'Reference'
  content: string
  assetId?: string
  score?: number
  isPinned: boolean
  createdAt: string
}

export function DesignPage() {
  const [projects, setProjects] = useState<DesignProject[]>([])
  const [selectedProject, setSelectedProject] = useState<DesignProject | null>(null)
  const [ideas, setIdeas] = useState<DesignIdea[]>([])
  const [loading, setLoading] = useState(false)
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [showIdeaDialog, setShowIdeaDialog] = useState(false)
  const [selectedIdea, setSelectedIdea] = useState<DesignIdea | null>(null)
  const [viewMode, setViewMode] = useState<'list' | 'board'>('list')
  const [filterType, setFilterType] = useState<string>('all')
  const [generating, setGenerating] = useState(false)

  // Form state
  const [title, setTitle] = useState('')
  const [goals, setGoals] = useState('')
  const [vibesInput, setVibesInput] = useState('')
  const [vibes, setVibes] = useState<string[]>([])
  const [requirements, setRequirements] = useState('')
  const [keywordsInput, setKeywordsInput] = useState('')
  const [keywords, setKeywords] = useState<string[]>([])

  useEffect(() => {
    loadProjects()
  }, [])

  useEffect(() => {
    if (selectedProject) {
      loadIdeas(selectedProject.id)
    }
  }, [selectedProject])

  const loadProjects = async () => {
    try {
      setLoading(true)
      // Mock data for now
      setProjects([])
    } catch (error) {
      console.error('Failed to load projects:', error)
    } finally {
      setLoading(false)
    }
  }

  const loadIdeas = async (projectId: string) => {
    try {
      // Mock API call
      const response = await fetch(`/v1/design/projects/${projectId}/ideas`)
      if (response.ok) {
        const data = await response.json()
        setIdeas(data)
      }
    } catch (error) {
      console.error('Failed to load ideas:', error)
    }
  }

  const createProject = async () => {
    try {
      setLoading(true)
      const response = await fetch('/v1/design/projects', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title,
          goalsText: goals,
          vibes,
          requirementsText: requirements,
          brandKeywords: keywords
        })
      })

      if (response.ok) {
        const data = await response.json()
        await loadProjects()
        setShowCreateDialog(false)
        resetForm()
        // Open the newly created project
        const newProject = projects.find(p => p.id === data.id)
        if (newProject) setSelectedProject(newProject)
      }
    } catch (error) {
      console.error('Failed to create project:', error)
    } finally {
      setLoading(false)
    }
  }

  const generateIdeas = async (mode: string) => {
    if (!selectedProject) return

    try {
      setGenerating(true)
      const response = await fetch(`/v1/design/projects/${selectedProject.id}/generate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ mode })
      })

      if (response.ok) {
        await loadIdeas(selectedProject.id)
      }
    } catch (error) {
      console.error('Failed to generate ideas:', error)
    } finally {
      setGenerating(false)
    }
  }

  const togglePin = async (ideaId: string) => {
    try {
      const response = await fetch(`/v1/design/ideas/${ideaId}/pin`, { method: 'POST' })
      if (response.ok) {
        setIdeas(ideas.map(i => i.id === ideaId ? { ...i, isPinned: !i.isPinned } : i))
      }
    } catch (error) {
      console.error('Failed to toggle pin:', error)
    }
  }

  const addVibeChip = () => {
    if (vibesInput.trim() && !vibes.includes(vibesInput.trim())) {
      setVibes([...vibes, vibesInput.trim()])
      setVibesInput('')
    }
  }

  const addKeywordChip = () => {
    if (keywordsInput.trim() && !keywords.includes(keywordsInput.trim())) {
      setKeywords([...keywords, keywordsInput.trim()])
      setKeywordsInput('')
    }
  }

  const removeVibeChip = (vibe: string) => {
    setVibes(vibes.filter(v => v !== vibe))
  }

  const removeKeywordChip = (keyword: string) => {
    setKeywords(keywords.filter(k => k !== keyword))
  }

  const resetForm = () => {
    setTitle('')
    setGoals('')
    setVibes([])
    setVibesInput('')
    setRequirements('')
    setKeywords([])
    setKeywordsInput('')
  }

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'Thumbnail': return 'warning'
      case 'Prompt': return 'info'
      case 'Reference': return 'default'
      case 'Moodboard': return 'success'
      default: return 'default'
    }
  }

  const filteredIdeas = filterType === 'all' 
    ? ideas 
    : ideas.filter(i => i.type.toLowerCase() === filterType.toLowerCase())

  const pinnedIdeas = filteredIdeas.filter(i => i.isPinned)

  if (!selectedProject) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">Design Projects</h1>
            <p className="text-sm text-gray-400 mt-1">
              Generate design ideas, concepts, and references for your projects
            </p>
          </div>
          <Button onClick={() => setShowCreateDialog(true)}>
            + New Project
          </Button>
        </div>

        {projects.length === 0 ? (
          <Card>
            <CardContent className="py-12">
              <EmptyState
                icon="🎨"
                title="No design projects yet"
                description="Create a design project to generate thumbnails, concepts, and references"
                action={{
                  label: 'Create Project',
                  onClick: () => setShowCreateDialog(true),
                }}
              />
            </CardContent>
          </Card>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {projects.map(project => (
              <Card key={project.id} className="cursor-pointer hover:border-primary/50 transition-colors"
                onClick={() => setSelectedProject(project)}>
                <CardHeader>
                  <CardTitle>{project.title}</CardTitle>
                  <CardDescription>
                    {project.vibes.length > 0 && (
                      <div className="flex flex-wrap gap-1 mt-2">
                        {project.vibes.map((vibe, i) => (
                          <Badge key={i} variant="default">{vibe}</Badge>
                        ))}
                      </div>
                    )}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="flex justify-between text-sm text-gray-400">
                    <span>{project.ideaCount} ideas</span>
                    {project.pinnedCount > 0 && <span>📌 {project.pinnedCount} pinned</span>}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}

        {/* Create Project Dialog */}
        <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>Create Design Project</DialogTitle>
            </DialogHeader>
            <div className="space-y-4 py-4">
              <div>
                <label className="block text-sm font-medium mb-2">Project Title</label>
                <Input
                  placeholder="e.g., Website Redesign 2024"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-2">Goals</label>
                <textarea
                  className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
                  rows={3}
                  placeholder="What do you want to achieve with this design?"
                  value={goals}
                  onChange={(e) => setGoals(e.target.value)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-2">Vibes</label>
                <div className="flex gap-2 mb-2">
                  <Input
                    placeholder="Add vibe (e.g., minimalist, bold, playful)"
                    value={vibesInput}
                    onChange={(e) => setVibesInput(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addVibeChip())}
                  />
                  <Button type="button" variant="secondary" onClick={addVibeChip}>Add</Button>
                </div>
                <div className="flex flex-wrap gap-2">
                  {vibes.map((vibe, i) => (
                    <Badge key={i} variant="default">
                      {vibe}
                      <button
                        onClick={() => removeVibeChip(vibe)}
                        className="ml-2 text-xs hover:text-red-400"
                      >
                        ×
                      </button>
                    </Badge>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium mb-2">Requirements</label>
                <textarea
                  className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
                  rows={3}
                  placeholder="Any specific requirements or constraints?"
                  value={requirements}
                  onChange={(e) => setRequirements(e.target.value)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-2">Brand Keywords</label>
                <div className="flex gap-2 mb-2">
                  <Input
                    placeholder="Add keyword (e.g., modern, trustworthy, innovative)"
                    value={keywordsInput}
                    onChange={(e) => setKeywordsInput(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addKeywordChip())}
                  />
                  <Button type="button" variant="secondary" onClick={addKeywordChip}>Add</Button>
                </div>
                <div className="flex flex-wrap gap-2">
                  {keywords.map((keyword, i) => (
                    <Badge key={i} variant="info">
                      {keyword}
                      <button
                        onClick={() => removeKeywordChip(keyword)}
                        className="ml-2 text-xs hover:text-red-400"
                      >
                        ×
                      </button>
                    </Badge>
                  ))}
                </div>
              </div>
            </div>
            <DialogFooter>
              <Button variant="ghost" onClick={() => {
                setShowCreateDialog(false)
                resetForm()
              }}>
                Cancel
              </Button>
              <Button onClick={createProject} disabled={!title.trim() || loading}>
                Create Project
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>
    )
  }

  // Project Detail View
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <button
            onClick={() => setSelectedProject(null)}
            className="text-sm text-gray-400 hover:text-white mb-2"
          >
            ← Back to Projects
          </button>
          <h1 className="text-2xl font-semibold">{selectedProject.title}</h1>
          <div className="flex flex-wrap gap-2 mt-2">
            {selectedProject.vibes.map((vibe, i) => (
              <Badge key={i} variant="default">{vibe}</Badge>
            ))}
          </div>
        </div>
        <div className="flex gap-2">
          <Button
            variant="secondary"
            onClick={() => setViewMode(viewMode === 'list' ? 'board' : 'list')}
          >
            {viewMode === 'list' ? '📌 Board View' : '📋 List View'}
          </Button>
        </div>
      </div>

      {/* Generate Ideas */}
      <Card>
        <CardContent className="py-6">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-medium">Generate Ideas</h3>
              <p className="text-sm text-gray-400 mt-1">
                Generate AI-powered design concepts, thumbnails, and references
              </p>
            </div>
            <div className="flex gap-2">
              <Button
                variant="secondary"
                onClick={() => generateIdeas('thumbnails')}
                disabled={generating}
              >
                {generating ? 'Generating...' : '🎨 Thumbnails'}
              </Button>
              <Button
                variant="secondary"
                onClick={() => generateIdeas('concepts')}
                disabled={generating}
              >
                {generating ? 'Generating...' : '💡 Concepts'}
              </Button>
              <Button
                variant="secondary"
                onClick={() => generateIdeas('references')}
                disabled={generating}
              >
                {generating ? 'Generating...' : '📚 References'}
              </Button>
              <Button
                onClick={() => generateIdeas('all')}
                disabled={generating}
              >
                {generating ? 'Generating...' : '✨ Generate All'}
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Filter */}
      {ideas.length > 0 && (
        <div className="flex gap-2">
          {['all', 'thumbnail', 'prompt', 'reference'].map(type => (
            <button
              key={type}
              onClick={() => setFilterType(type)}
              className={`px-4 py-2 rounded-md transition-colors ${
                filterType === type
                  ? 'bg-primary text-white'
                  : 'bg-gray-800 text-gray-300 hover:bg-gray-700'
              }`}
            >
              {type.charAt(0).toUpperCase() + type.slice(1)}
            </button>
          ))}
        </div>
      )}

      {/* Ideas */}
      {ideas.length === 0 ? (
        <Card>
          <CardContent className="py-12">
            <EmptyState
              icon="💡"
              title="No ideas yet"
              description="Click Generate to create design ideas, concepts, and references"
            />
          </CardContent>
        </Card>
      ) : viewMode === 'board' && pinnedIdeas.length > 0 ? (
        <div>
          <h2 className="text-lg font-semibold mb-4">📌 Pinned Board ({pinnedIdeas.length})</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
            {pinnedIdeas.map(idea => (
              <Card key={idea.id} className="cursor-pointer hover:border-primary/50 transition-colors"
                onClick={() => {
                  setSelectedIdea(idea)
                  setShowIdeaDialog(true)
                }}>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <Badge variant={getTypeColor(idea.type)}>{idea.type}</Badge>
                    <button
                      onClick={(e) => {
                        e.stopPropagation()
                        togglePin(idea.id)
                      }}
                      className="text-xl"
                    >
                      📌
                    </button>
                  </div>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-gray-300 line-clamp-4">{idea.content}</p>
                  {idea.score && (
                    <div className="mt-2 text-xs text-gray-400">
                      Confidence: {Math.round(idea.score * 100)}%
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>

          <h2 className="text-lg font-semibold mb-4">All Ideas ({filteredIdeas.length})</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {filteredIdeas.filter(i => !i.isPinned).map(idea => (
              <Card key={idea.id} className="cursor-pointer hover:border-primary/50 transition-colors"
                onClick={() => {
                  setSelectedIdea(idea)
                  setShowIdeaDialog(true)
                }}>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <Badge variant={getTypeColor(idea.type)}>{idea.type}</Badge>
                    <button
                      onClick={(e) => {
                        e.stopPropagation()
                        togglePin(idea.id)
                      }}
                      className="text-xl opacity-50 hover:opacity-100"
                    >
                      📌
                    </button>
                  </div>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-gray-300 line-clamp-4">{idea.content}</p>
                  {idea.score && (
                    <div className="mt-2 text-xs text-gray-400">
                      Confidence: {Math.round(idea.score * 100)}%
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      ) : (
        <div className="space-y-3">
          {filteredIdeas.map(idea => (
            <Card key={idea.id} className="cursor-pointer hover:border-primary/50 transition-colors"
              onClick={() => {
                setSelectedIdea(idea)
                setShowIdeaDialog(true)
              }}>
              <CardContent className="py-4">
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <Badge variant={getTypeColor(idea.type)}>{idea.type}</Badge>
                      {idea.score && (
                        <span className="text-xs text-gray-400">
                          {Math.round(idea.score * 100)}% confidence
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-gray-300">{idea.content.substring(0, 150)}...</p>
                  </div>
                  <button
                    onClick={(e) => {
                      e.stopPropagation()
                      togglePin(idea.id)
                    }}
                    className={`ml-4 text-xl ${idea.isPinned ? '' : 'opacity-50 hover:opacity-100'}`}
                  >
                    📌
                  </button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Idea Detail Dialog */}
      <Dialog open={showIdeaDialog} onOpenChange={setShowIdeaDialog}>
        <DialogContent className="max-w-3xl">
          {selectedIdea && (
            <>
              <DialogHeader>
                <div className="flex items-center justify-between">
                  <DialogTitle>
                    <Badge variant={getTypeColor(selectedIdea.type)}>{selectedIdea.type}</Badge>
                  </DialogTitle>
                  <button
                    onClick={() => togglePin(selectedIdea.id)}
                    className="text-2xl"
                  >
                    {selectedIdea.isPinned ? '📌' : '📍'}
                  </button>
                </div>
              </DialogHeader>
              <div className="py-4">
                <div className="prose prose-invert max-w-none">
                  <pre className="whitespace-pre-wrap text-sm text-gray-300 bg-gray-800 p-4 rounded-md">
                    {selectedIdea.content}
                  </pre>
                </div>
                {selectedIdea.score && (
                  <div className="mt-4 text-sm text-gray-400">
                    Confidence Score: {Math.round(selectedIdea.score * 100)}%
                  </div>
                )}
              </div>
              <DialogFooter>
                <Button variant="ghost" onClick={() => setShowIdeaDialog(false)}>
                  Close
                </Button>
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
