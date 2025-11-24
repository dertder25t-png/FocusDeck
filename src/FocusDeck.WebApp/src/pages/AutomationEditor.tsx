import { useState, useEffect } from 'react'
import Editor from '@monaco-editor/react'
import { Button } from '../components/Button'
import { Input } from '../components/Input'
import { AutomationVisualBuilder } from './AutomationVisualBuilder'

interface AutomationEditorProps {
  automationId?: string | null
  proposalId?: string | null
  initialYaml?: string
  onClose: () => void
  onSaved: () => void
}

export function AutomationEditor({ automationId, proposalId, initialYaml, onClose, onSaved }: AutomationEditorProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [yaml, setYaml] = useState('')
  const [mode, setMode] = useState<'visual' | 'yaml'>('visual')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (automationId) {
      loadAutomation(automationId)
    } else if (proposalId && initialYaml) {
      // Pre-fill from proposal
      setYaml(initialYaml)
      // We don't have name/desc in this context unless passed props, but we can default
      setName("From Proposal")
      setDescription("Edited proposal")
    } else {
      // Default template
      setYaml(`trigger:
  type: Time
  settings:
    time: "09:00"

actions:
  - type: focusdeck.ShowNotification
    settings:
      title: "Good Morning"
      message: "Time to focus!"`)
    }
  }, [automationId, proposalId, initialYaml])

  const loadAutomation = async (id: string) => {
    setLoading(true)
    try {
      const res = await fetch(`/v1/automations/${id}`)
      if (res.ok) {
        const data = await res.json()
        setName(data.name)
        setDescription(data.description || '')
        setYaml(data.yamlDefinition)
      }
    } catch (err) {
      setError('Failed to load automation')
    } finally {
      setLoading(false)
    }
  }

  const handleSave = async () => {
    setLoading(true)
    setError(null)

    try {
      // If we have a proposal ID, we want to "accept" it but with the *new* YAML
      // The accept endpoint creates the automation. However, standard accept doesn't take body.
      // So for editing a proposal, we should probably just Create a New Automation and Delete the Proposal?
      // OR: We call Accept on the proposal, and then Update the automation immediately?
      // Simpler: Treat it as creating a new automation from scratch, and then (optionally) delete the proposal.
      // For MVP: Just Create New Automation. The user can delete the proposal later or we can add a 'fromProposal' query param to the create endpoint to handle cleanup.

      let url = automationId ? `/v1/automations/${automationId}` : '/v1/automations'
      const method = automationId ? 'PUT' : 'POST'

      const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, description, yamlDefinition: yaml })
      })

      if (res.ok) {
        // If we saved successfully and came from a proposal, we should probably delete/accept the proposal to clean up.
        if (proposalId) {
            await fetch(`/v1/automations/proposals/${proposalId}/accept`, { method: 'POST' })
        }
        onSaved()
      } else {
        const data = await res.json()
        setError(data.details || data.error || 'Failed to save')
      }
    } catch (err) {
      setError('An unexpected error occurred')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
      <div className="bg-gray-900 border border-gray-800 rounded-xl w-full max-w-2xl max-h-[90vh] flex flex-col shadow-2xl">
        <div className="p-6 border-b border-gray-800 flex justify-between items-center">
          <h2 className="text-xl font-semibold text-white">
            {automationId ? 'Edit Automation' : 'New Automation'}
          </h2>
          <button onClick={onClose} className="text-gray-400 hover:text-white">
            ✕
          </button>
        </div>

        <div className="p-6 flex-1 overflow-y-auto space-y-4">
          {error && (
            <div className="bg-red-900/20 border border-red-800 text-red-200 p-3 rounded-lg text-sm">
              {error}
            </div>
          )}

          <div>
            <label className="block text-sm text-gray-400 mb-1">Name</label>
            <Input
              value={name}
              onChange={e => setName(e.target.value)}
              placeholder="My Automation"
              className="w-full"
            />
          </div>

          <div>
            <label className="block text-sm text-gray-400 mb-1">Description</label>
            <Input
              value={description}
              onChange={e => setDescription(e.target.value)}
              placeholder="What does this do?"
              className="w-full"
            />
          </div>

          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="block text-sm text-gray-400">Automation Logic</label>
              <div className="flex bg-gray-800 rounded-lg p-1">
                <button
                  onClick={() => setMode('visual')}
                  className={`px-3 py-1 text-xs rounded-md transition-colors ${
                    mode === 'visual' ? 'bg-gray-700 text-white' : 'text-gray-400 hover:text-white'
                  }`}
                >
                  Visual
                </button>
                <button
                  onClick={() => setMode('yaml')}
                  className={`px-3 py-1 text-xs rounded-md transition-colors ${
                    mode === 'yaml' ? 'bg-gray-700 text-white' : 'text-gray-400 hover:text-white'
                  }`}
                >
                  YAML
                </button>
              </div>
            </div>

            {mode === 'visual' ? (
              <AutomationVisualBuilder
                initialYaml={yaml}
                onYamlChange={setYaml}
              />
            ) : (
              <div className="flex flex-col min-h-[400px] border border-gray-800 rounded-lg overflow-hidden">
                <Editor
                  height="400px"
                  defaultLanguage="yaml"
                  theme="vs-dark"
                  value={yaml}
                  onChange={(value) => setYaml(value || '')}
                  options={{
                    minimap: { enabled: false },
                    fontSize: 14,
                    lineNumbers: 'on',
                    scrollBeyondLastLine: false,
                    automaticLayout: true,
                    padding: { top: 16, bottom: 16 }
                  }}
                />
                <div className="bg-gray-900 p-2 border-t border-gray-800">
                  <p className="text-xs text-gray-500">
                    Advanced mode: Edit the raw YAML definition directly.
                  </p>
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="p-6 border-t border-gray-800 flex justify-end gap-3">
          <Button variant="ghost" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={loading || !name || !yaml}>
            {loading ? 'Saving...' : 'Save Automation'}
          </Button>
        </div>
      </div>
    </div>
  )
}
