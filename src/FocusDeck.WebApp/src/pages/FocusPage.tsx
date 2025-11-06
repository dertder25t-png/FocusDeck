import { useState, useEffect } from 'react'
import { Button } from '../components/Button'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../components/Card'
import { Badge } from '../components/Badge'
import { EmptyState } from '../components/States'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '../components/Dialog'
import { Input } from '../components/Input'
import { apiFetch } from '../lib/utils'

interface FocusSession {
  id: string
  userId: string
  startTime: string
  endTime: string | null
  status: string
  policy: FocusPolicy
  distractionsCount: number
  createdAt: string
  updatedAt: string
}

interface FocusPolicy {
  strict: boolean
  autoBreak: boolean
  autoDim: boolean
  notifyPhone: boolean
}

interface FocusPolicyTemplate {
  id: string
  name: string
  strict: boolean
  autoBreak: boolean
  autoDim: boolean
  notifyPhone: boolean
  targetDurationMinutes: number | null
}

interface Distraction {
  reason: string
  at: string
}

// Simulated distractions array for demo purposes
const mockDistractions: Distraction[] = []

export function FocusPage() {
  const [activeSession, setActiveSession] = useState<FocusSession | null>(null)
  const [sessions, setSessions] = useState<FocusSession[]>([])
  const [policies, setPolicies] = useState<FocusPolicyTemplate[]>([])
  const [isStartDialogOpen, setIsStartDialogOpen] = useState(false)
  const [isPolicyDialogOpen, setIsPolicyDialogOpen] = useState(false)
  const [elapsedSeconds, setElapsedSeconds] = useState(0)
  
  // Stats
  const [todayMinutes, setTodayMinutes] = useState(0)
  const [distractionsPerHour, setDistractionsPerHour] = useState(0)
  const [currentStreak, setCurrentStreak] = useState(0)

  // New session form
  const [newSessionMode, setNewSessionMode] = useState<'strict' | 'soft'>('soft')
  const [targetDuration, setTargetDuration] = useState(25)

  // New policy form
  const [newPolicyName, setNewPolicyName] = useState('')
  const [newPolicyStrict, setNewPolicyStrict] = useState(false)
  const [newPolicyAutoBreak, setNewPolicyAutoBreak] = useState(true)

  useEffect(() => {
    fetchActiveSession()
    fetchSessions()
    fetchPolicies()
    // In a real app, connect to SignalR here for real-time updates
  }, [])

  useEffect(() => {
    if (activeSession && activeSession.status === 'Active') {
      const interval = setInterval(() => {
        const start = new Date(activeSession.startTime).getTime()
        const now = Date.now()
        setElapsedSeconds(Math.floor((now - start) / 1000))
      }, 1000)
      return () => clearInterval(interval)
    }
  }, [activeSession])

  const fetchActiveSession = async () => {
    try {
      const response = await apiFetch('/v1/focus/sessions/active')
      if (response.ok) {
        const data = await response.json()
        setActiveSession(data)
      }
    } catch (error) {
      console.error('Failed to fetch active session:', error)
    }
  }

  const fetchSessions = async () => {
    try {
      const response = await apiFetch('/v1/focus/sessions?limit=10')
      if (response.ok) {
        const data = await response.json()
        setSessions(data)
        
        // Calculate stats
        const today = new Date()
        today.setHours(0, 0, 0, 0)
        
        const todaySessions = data.filter((s: FocusSession) => 
          new Date(s.startTime) >= today
        )
        
        const totalMinutes = todaySessions.reduce((sum: number, s: FocusSession) => {
          if (s.endTime) {
            const duration = new Date(s.endTime).getTime() - new Date(s.startTime).getTime()
            return sum + (duration / 60000)
          }
          return sum
        }, 0)
        
        setTodayMinutes(Math.round(totalMinutes))
        
        // Calculate distractions per hour
        const totalDistractions = todaySessions.reduce((sum: number, s: FocusSession) => 
          sum + s.distractionsCount, 0
        )
        const totalHours = totalMinutes / 60
        setDistractionsPerHour(totalHours > 0 ? Math.round(totalDistractions / totalHours) : 0)
        
        // Calculate streak (consecutive days with sessions)
        setCurrentStreak(3) // Placeholder
      }
    } catch (error) {
      console.error('Failed to fetch sessions:', error)
    }
  }

  const fetchPolicies = async () => {
    try {
      const response = await apiFetch('/v1/focus/policies')
      if (response.ok) {
        const data = await response.json()
        setPolicies(data)
      }
    } catch (error) {
      console.error('Failed to fetch policies:', error)
    }
  }

  const startSession = async () => {
    try {
      const response = await apiFetch('/v1/focus/sessions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          policy: {
            strict: newSessionMode === 'strict',
            autoBreak: true,
            autoDim: false,
            notifyPhone: false
          }
        })
      })
      
      if (response.ok) {
        const data = await response.json()
        setActiveSession(data)
        setIsStartDialogOpen(false)
        fetchSessions()
      }
    } catch (error) {
      console.error('Failed to start session:', error)
    }
  }

  const endSession = async () => {
    if (!activeSession) return
    
    try {
      const response = await apiFetch(`/v1/focus/sessions/${activeSession.id}/end`, {
        method: 'POST'
      })
      
      if (response.ok) {
        setActiveSession(null)
        setElapsedSeconds(0)
        fetchSessions()
      }
    } catch (error) {
      console.error('Failed to end session:', error)
    }
  }

  const createPolicy = async () => {
    try {
      const response = await apiFetch('/v1/focus/policies', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: newPolicyName,
          strict: newPolicyStrict,
          autoBreak: newPolicyAutoBreak,
          autoDim: false,
          notifyPhone: false,
          targetDurationMinutes: null
        })
      })
      
      if (response.ok) {
        setNewPolicyName('')
        setNewPolicyStrict(false)
        setNewPolicyAutoBreak(true)
        setIsPolicyDialogOpen(false)
        fetchPolicies()
      }
    } catch (error) {
      console.error('Failed to create policy:', error)
    }
  }

  const formatTime = (seconds: number) => {
    const hrs = Math.floor(seconds / 3600)
    const mins = Math.floor((seconds % 3600) / 60)
    const secs = seconds % 60
    
    if (hrs > 0) {
      return `${hrs}:${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
    }
    return `${mins}:${secs.toString().padStart(2, '0')}`
  }

  const formatDateTime = (dateStr: string) => {
    const date = new Date(dateStr)
    return date.toLocaleString('en-US', { 
      month: 'short', 
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    })
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Focus Sessions</h1>
        <p className="text-sm text-gray-400 mt-1">
          Start focus sessions and track your productivity
        </p>
      </div>

      {/* Analytics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card>
          <CardContent className="pt-6">
            <div className="text-2xl font-bold text-primary">{todayMinutes}m</div>
            <div className="text-sm text-gray-400">Today's Focus Time</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="text-2xl font-bold text-primary">{distractionsPerHour}</div>
            <div className="text-sm text-gray-400">Distractions/Hour</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="text-2xl font-bold text-primary">{currentStreak} days</div>
            <div className="text-sm text-gray-400">Current Streak</div>
          </CardContent>
        </Card>
      </div>

      {/* Active Session */}
      {activeSession ? (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>Active Session</CardTitle>
                <CardDescription>
                  Started {formatDateTime(activeSession.startTime)}
                </CardDescription>
              </div>
              <Badge variant={activeSession.policy.strict ? 'warning' : 'info'}>
                {activeSession.policy.strict ? 'Strict Mode' : 'Soft Mode'}
              </Badge>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="text-center">
              <div className="text-5xl font-bold text-primary">{formatTime(elapsedSeconds)}</div>
              <div className="text-sm text-gray-400 mt-2">Elapsed Time</div>
            </div>
            
            <div className="flex items-center justify-center gap-4">
              <div className="text-center">
                <div className="text-2xl font-bold">{activeSession.distractionsCount}</div>
                <div className="text-xs text-gray-400">Distractions</div>
              </div>
            </div>

            <div className="flex justify-center gap-2">
              <Button onClick={endSession} variant="danger">
                End Session
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="py-12">
            <EmptyState
              icon="⚡"
              title="No active focus session"
              description="Start a focus session to track your productivity and minimize distractions"
              action={{
                label: 'Start Session',
                onClick: () => setIsStartDialogOpen(true),
              }}
            />
          </CardContent>
        </Card>
      )}

      {/* Event Log - Ready for SignalR */}
      {false && ( // Placeholder for future real-time distraction events
        <Card>
          <CardHeader>
            <CardTitle>Event Log</CardTitle>
            <CardDescription>Recent distractions and events</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {mockDistractions.map((d, i) => (
                <div key={i} className="flex items-center justify-between p-3 bg-red-500/10 rounded-lg border border-red-500/20">
                  <div className="flex items-center gap-2">
                    <span className="text-red-400">🚨</span>
                    <span className="text-sm">{d.reason}</span>
                  </div>
                  <span className="text-xs text-gray-400">{formatDateTime(d.at)}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Policies */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Focus Policies</CardTitle>
              <CardDescription>Manage your focus session templates</CardDescription>
            </div>
            <Button onClick={() => setIsPolicyDialogOpen(true)} size="sm">
              New Policy
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {policies.length === 0 ? (
            <div className="text-center py-8 text-gray-400 text-sm">
              No policies created yet. Create one to save your preferred settings.
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {policies.map((policy) => (
                <div
                  key={policy.id}
                  className="p-4 border border-gray-700 rounded-lg hover:border-gray-600 transition-colors"
                >
                  <div className="flex items-center justify-between mb-2">
                    <h4 className="font-semibold">{policy.name}</h4>
                    <Badge variant={policy.strict ? 'warning' : 'default'}>
                      {policy.strict ? 'Strict' : 'Soft'}
                    </Badge>
                  </div>
                  <div className="text-sm text-gray-400 space-y-1">
                    {policy.autoBreak && <div>• Auto-break suggestions</div>}
                    {policy.autoDim && <div>• Auto-dim display</div>}
                    {policy.notifyPhone && <div>• Phone notifications</div>}
                    {policy.targetDurationMinutes && (
                      <div>• Target: {policy.targetDurationMinutes}m</div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Recent Sessions */}
      {sessions.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Recent Sessions</CardTitle>
            <CardDescription>Your focus session history</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {sessions.filter(s => s.status !== 'Active').slice(0, 5).map((session) => {
                const duration = session.endTime 
                  ? Math.round((new Date(session.endTime).getTime() - new Date(session.startTime).getTime()) / 60000)
                  : 0
                
                return (
                  <div
                    key={session.id}
                    className="flex items-center justify-between p-3 border border-gray-700 rounded-lg"
                  >
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-semibold">{formatDateTime(session.startTime)}</span>
                        <Badge variant={session.status === 'Completed' ? 'success' : 'default'}>
                          {session.status}
                        </Badge>
                      </div>
                      <div className="text-sm text-gray-400 mt-1">
                        {duration}m • {session.distractionsCount} distractions
                      </div>
                    </div>
                    {session.policy.strict && (
                      <Badge variant="warning">Strict</Badge>
                    )}
                  </div>
                )
              })}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Start Session Dialog */}
      <Dialog open={isStartDialogOpen} onOpenChange={setIsStartDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Start Focus Session</DialogTitle>
            <DialogDescription>
              Choose your focus mode and duration
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2">Focus Mode</label>
              <div className="grid grid-cols-2 gap-2">
                <button
                  onClick={() => setNewSessionMode('soft')}
                  className={`p-4 border rounded-lg transition-colors ${
                    newSessionMode === 'soft'
                      ? 'border-primary bg-primary/10'
                      : 'border-gray-700 hover:border-gray-600'
                  }`}
                >
                  <div className="font-semibold">Soft Mode</div>
                  <div className="text-xs text-gray-400 mt-1">
                    Track without strict enforcement
                  </div>
                </button>
                <button
                  onClick={() => setNewSessionMode('strict')}
                  className={`p-4 border rounded-lg transition-colors ${
                    newSessionMode === 'strict'
                      ? 'border-primary bg-primary/10'
                      : 'border-gray-700 hover:border-gray-600'
                  }`}
                >
                  <div className="font-semibold">Strict Mode</div>
                  <div className="text-xs text-gray-400 mt-1">
                    Detect phone distractions
                  </div>
                </button>
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                Target Duration (minutes)
              </label>
              <Input
                type="number"
                value={targetDuration}
                onChange={(e) => setTargetDuration(parseInt(e.target.value) || 25)}
                min={5}
                max={180}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="secondary" onClick={() => setIsStartDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={startSession}>Start Session</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Create Policy Dialog */}
      <Dialog open={isPolicyDialogOpen} onOpenChange={setIsPolicyDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create Focus Policy</DialogTitle>
            <DialogDescription>
              Save a policy template for quick session starts
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2">Policy Name</label>
              <Input
                value={newPolicyName}
                onChange={(e) => setNewPolicyName(e.target.value)}
                placeholder="e.g., Deep Work, Quick Focus"
              />
            </div>

            <div className="space-y-2">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={newPolicyStrict}
                  onChange={(e) => setNewPolicyStrict(e.target.checked)}
                  className="rounded border-gray-600 bg-gray-800 text-primary focus:ring-2 focus:ring-primary"
                />
                <span className="text-sm">Strict Mode (detect phone activity)</span>
              </label>

              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={newPolicyAutoBreak}
                  onChange={(e) => setNewPolicyAutoBreak(e.target.checked)}
                  className="rounded border-gray-600 bg-gray-800 text-primary focus:ring-2 focus:ring-primary"
                />
                <span className="text-sm">Auto-break suggestions</span>
              </label>
            </div>
          </div>

          <DialogFooter>
            <Button variant="secondary" onClick={() => setIsPolicyDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={createPolicy} disabled={!newPolicyName.trim()}>
              Create Policy
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
