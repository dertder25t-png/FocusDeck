import { useState, useEffect } from 'react'
import { Button } from '../components/Button'
import { Input } from '../components/Input'
import { Card, CardContent } from '../components/Card'

interface VisualBuilderProps {
  initialYaml: string
  onYamlChange: (yaml: string) => void
}

interface Trigger {
  type: string
  settings: Record<string, string>
}

interface Action {
  type: string
  settings: Record<string, string>
}

// Basic YAML parser/dumper for the visual builder
// Note: In a production app, use a real YAML library like js-yaml
const parseYaml = (yaml: string): { trigger: Trigger; actions: Action[] } => {
  try {
    const lines = yaml.split('\n')
    const trigger: Trigger = { type: 'Time', settings: {} }
    const actions: Action[] = []
    let currentSection = ''
    let currentActionIndex = -1

    // Very naive parsing for MVP
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i].trim()
      if (line.startsWith('trigger:')) {
        currentSection = 'trigger'
        continue
      }
      if (line.startsWith('actions:')) {
        currentSection = 'actions'
        continue
      }

      if (currentSection === 'trigger') {
        if (line.startsWith('type:')) {
           trigger.type = line.split(':')[1].trim()
        } else if (line.startsWith('time:')) { // time settings
           // Fix: handle colons in time values (e.g., "09:30")
           trigger.settings['time'] = line.split(':').slice(1).join(':').trim().replace(/"/g, '')
        } else if (line.startsWith('minutes:')) { // interval settings
           trigger.settings['minutes'] = line.split(':')[1].trim()
        } else if (line.startsWith('app:') || line.startsWith('name:')) { // app settings
           trigger.settings['app'] = line.split(':').slice(1).join(':').trim().replace(/"/g, '')
        }
      } else if (currentSection === 'actions') {
        if (line.startsWith('- type:')) {
          actions.push({ type: line.split(':')[1].trim(), settings: {} })
          currentActionIndex++
        } else if (currentActionIndex >= 0) {
           // Naive settings parsing
           const parts = line.split(':')
           if (parts.length >= 2 && !line.startsWith('settings:')) {
             const key = parts[0].trim()
             const val = parts.slice(1).join(':').trim().replace(/"/g, '')
             actions[currentActionIndex].settings[key] = val
           }
        }
      }
    }

    return { trigger, actions }
  } catch (e) {
    console.error("Failed to parse YAML visually", e)
    return { trigger: { type: 'Time', settings: {} }, actions: [] }
  }
}

const generateYaml = (trigger: Trigger, actions: Action[]): string => {
  let yaml = `trigger:
  type: ${trigger.type}
  settings:
`
  Object.entries(trigger.settings).forEach(([k, v]) => {
    yaml += `    ${k}: "${v}"\n`
  })

  yaml += `
actions:
`
  actions.forEach(action => {
    yaml += `  - type: ${action.type}
    settings:
`
    Object.entries(action.settings).forEach(([k, v]) => {
      yaml += `      ${k}: "${v}"\n`
    })
  })

  return yaml
}

export function AutomationVisualBuilder({ initialYaml, onYamlChange }: VisualBuilderProps) {
  const [trigger, setTrigger] = useState<Trigger>({ type: 'Time', settings: { time: '09:00' } })
  const [actions, setActions] = useState<Action[]>([])

  useEffect(() => {
    if (initialYaml) {
      const parsed = parseYaml(initialYaml)
      // Only update if we successfully parsed something useful, otherwise keep default
      if (parsed.actions.length > 0 || parsed.trigger.type) {
          setTrigger(parsed.trigger)
          setActions(parsed.actions)
      }
    }
  }, [])

  // Sync back to YAML whenever state changes
  useEffect(() => {
    const newYaml = generateYaml(trigger, actions)
    if (newYaml !== initialYaml) {
        onYamlChange(newYaml)
    }
  }, [trigger, actions])

  const addAction = () => {
    setActions([...actions, { type: 'focusdeck.ShowNotification', settings: { title: 'New Notification', message: 'Hello' } }])
  }

  const removeAction = (index: number) => {
    const newActions = [...actions]
    newActions.splice(index, 1)
    setActions(newActions)
  }

  const updateAction = (index: number, field: string, value: string) => {
    const newActions = [...actions]
    if (field === 'type') {
        newActions[index].type = value
    } else {
        // Settings update
        newActions[index].settings[field] = value
    }
    setActions(newActions)
  }

  const updateTrigger = (field: string, value: string) => {
    if (field === 'type') {
        setTrigger({ ...trigger, type: value, settings: {} })
    } else {
        setTrigger({ ...trigger, settings: { ...trigger.settings, [field]: value } })
    }
  }

  return (
    <div className="space-y-6 p-4 bg-gray-950 rounded-lg border border-gray-800">
      {/* Trigger Section */}
      <div>
        <h3 className="text-sm font-medium text-gray-400 uppercase tracking-wider mb-3">When... (Trigger)</h3>
        <Card className="bg-gray-900 border-gray-800">
          <CardContent className="p-4 space-y-4">
            <div>
              <label className="block text-xs text-gray-500 mb-1">Trigger Type</label>
              <select
                value={trigger.type}
                onChange={(e) => updateTrigger('type', e.target.value)}
                className="w-full bg-gray-800 border border-gray-700 rounded p-2 text-sm text-white focus:outline-none focus:border-primary"
              >
                <option value="Time">Time of Day</option>
                <option value="Interval">Recurring Interval</option>
                <option value="AppOpen">Application Opened</option>
              </select>
            </div>

            {trigger.type === 'Time' && (
               <div>
                 <label className="block text-xs text-gray-500 mb-1">Time (24h)</label>
                 <Input
                   type="time"
                   value={trigger.settings['time'] || '09:00'}
                   onChange={(e) => updateTrigger('time', e.target.value)}
                 />
               </div>
            )}

            {trigger.type === 'Interval' && (
               <div>
                 <label className="block text-xs text-gray-500 mb-1">Every X Minutes</label>
                 <Input
                   type="number"
                   value={trigger.settings['minutes'] || '60'}
                   onChange={(e) => updateTrigger('minutes', e.target.value)}
                 />
               </div>
            )}

            {trigger.type === 'AppOpen' && (
               <div>
                 <label className="block text-xs text-gray-500 mb-1">Application Name / Title</label>
                 <Input
                   placeholder="e.g. VS Code"
                   value={trigger.settings['app'] || ''}
                   onChange={(e) => updateTrigger('app', e.target.value)}
                 />
               </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Actions Section */}
      <div>
        <h3 className="text-sm font-medium text-gray-400 uppercase tracking-wider mb-3">Then Do... (Actions)</h3>
        <div className="space-y-3">
            {actions.map((action, idx) => (
                <Card key={idx} className="bg-gray-900 border-gray-800 relative group">
                    <button
                        onClick={() => removeAction(idx)}
                        className="absolute top-2 right-2 text-gray-600 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-opacity"
                    >
                        ✕
                    </button>
                    <CardContent className="p-4 space-y-3">
                        <div>
                           <label className="block text-xs text-gray-500 mb-1">Action Type</label>
                           <select
                             value={action.type}
                             onChange={(e) => updateAction(idx, 'type', e.target.value)}
                             className="w-full bg-gray-800 border border-gray-700 rounded p-2 text-sm text-white focus:outline-none focus:border-primary"
                           >
                             <option value="focusdeck.ShowNotification">Show Notification</option>
                             <option value="focusdeck.PlaySound">Play Sound</option>
                             <option value="focusdeck.StartTimer">Start Timer</option>
                             <option value="general.OpenURL">Open Website</option>
                             <option value="email.Send">Send Email</option>
                             <option value="github.OpenBrowser">Open GitHub</option>
                             <option value="storage.SaveFile">Save File</option>
                           </select>
                        </div>

                        {/* Dynamic fields based on type */}
                        {action.type === 'focusdeck.ShowNotification' && (
                            <>
                                <div>
                                    <label className="block text-xs text-gray-500 mb-1">Title</label>
                                    <Input
                                        value={action.settings['title'] || ''}
                                        onChange={(e) => updateAction(idx, 'title', e.target.value)}
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs text-gray-500 mb-1">Message</label>
                                    <Input
                                        value={action.settings['message'] || ''}
                                        onChange={(e) => updateAction(idx, 'message', e.target.value)}
                                    />
                                </div>
                            </>
                        )}
                         {action.type === 'general.OpenURL' && (
                            <div>
                                <label className="block text-xs text-gray-500 mb-1">URL</label>
                                <Input
                                    value={action.settings['url'] || ''}
                                    onChange={(e) => updateAction(idx, 'url', e.target.value)}
                                />
                            </div>
                        )}
                        {action.type === 'focusdeck.StartTimer' && (
                            <div>
                                <label className="block text-xs text-gray-500 mb-1">Duration (minutes)</label>
                                <Input
                                    type="number"
                                    value={action.settings['duration'] || '25'}
                                    onChange={(e) => updateAction(idx, 'duration', e.target.value)}
                                />
                            </div>
                        )}
                        {action.type === 'email.Send' && (
                            <>
                                <div>
                                    <label className="block text-xs text-gray-500 mb-1">To</label>
                                    <Input
                                        value={action.settings['to'] || ''}
                                        onChange={(e) => updateAction(idx, 'to', e.target.value)}
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs text-gray-500 mb-1">Subject</label>
                                    <Input
                                        value={action.settings['subject'] || ''}
                                        onChange={(e) => updateAction(idx, 'subject', e.target.value)}
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs text-gray-500 mb-1">Body</label>
                                    <Input
                                        value={action.settings['body'] || ''}
                                        onChange={(e) => updateAction(idx, 'body', e.target.value)}
                                    />
                                </div>
                            </>
                        )}
                        {action.type === 'github.OpenBrowser' && (
                            <div>
                                <label className="block text-xs text-gray-500 mb-1">URL</label>
                                <Input
                                    value={action.settings['url'] || 'https://github.com'}
                                    onChange={(e) => updateAction(idx, 'url', e.target.value)}
                                />
                            </div>
                        )}
                        {action.type === 'storage.SaveFile' && (
                            <>
                                <div>
                                    <label className="block text-xs text-gray-500 mb-1">Provider</label>
                                    <select
                                        value={action.settings['provider'] || 'GoogleDrive'}
                                        onChange={(e) => updateAction(idx, 'provider', e.target.value)}
                                        className="w-full bg-gray-800 border border-gray-700 rounded p-2 text-sm text-white focus:outline-none focus:border-primary"
                                    >
                                        <option value="GoogleDrive">Google Drive</option>
                                        <option value="OneDrive">OneDrive</option>
                                    </select>
                                </div>
                                <div>
                                    <label className="block text-xs text-gray-500 mb-1">Path</label>
                                    <Input
                                        value={action.settings['path'] || '/'}
                                        onChange={(e) => updateAction(idx, 'path', e.target.value)}
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs text-gray-500 mb-1">Content</label>
                                    <Input
                                        value={action.settings['content'] || ''}
                                        onChange={(e) => updateAction(idx, 'content', e.target.value)}
                                    />
                                </div>
                            </>
                        )}
                    </CardContent>
                </Card>
            ))}
        </div>
        <div className="flex gap-2 mt-3">
            <Button
                variant="outline"
                className="flex-1 border-dashed border-gray-700 hover:border-gray-500 text-gray-400"
                onClick={addAction}
            >
                + Add Action
            </Button>
        </div>
      </div>
    </div>
  )
}
