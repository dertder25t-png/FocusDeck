import { useState, useEffect } from 'react'
import { Button } from '../components/Button'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../components/Card'
import { Badge } from '../components/Badge'
import { EmptyState } from '../components/States'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '../components/Dialog'
import { Input } from '../components/Input'
import { apiFetch } from '../lib/utils'
import { createNotificationsConnection, type ActivityState as LiveActivityState } from '../lib/signalr'

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
  
  // Live context
  const [liveContext, setLiveContext] = useState<LiveActivityState | null>(null)
  const [events, setEvents] = useState<string[]>([])
  const [timeline, setTimeline] = useState<any[]>([])
  const [activeSessions, setActiveSessions] = useState<number>(0)
  const [lastIssued, setLastIssued] = useState<string | null>(null)

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
    
    // Connect to SignalR for real-time context updates
    ;(async () => {
      try {
        const conn = await createNotificationsConnection()
        conn.on('ContextUpdated', (state: LiveActivityState) => {
          setLiveContext(state)
          setEvents(prev => [`Context: ${state.focusedAppName ?? '(none)'} (${state.activityIntensity})`, ...prev].slice(0, 50))
        })
        conn.on('FocusRecoverySuggested', (suggestion: string) => {
          setEvents(prev => [`Recovery: ${suggestion}`, ...prev].slice(0, 50))
        })
      } catch (err) {
        console.error('SignalR connection failed', err)
      }
    })()

    // Initial timeline load
    fetchTimeline()
    fetchSessionsOverview()
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

  const fetchTimeline = async () => {
    try {
      const res = await apiFetch('/v1/context/timeline?limit=20')
      if (res.ok) {
        const data = await res.json()
        setTimeline(data)
      }
    } catch (e) {
      console.error('Failed to fetch timeline', e)
    }
  }

  const fetchSessionsOverview = async () => {
    try {
      const res = await apiFetch('/v1/auth/devices')
      if (res.ok) {
        const data = await res.json()
        const active = data.filter((d: any) => d.isActive && !d.revokedUtc).length
        setActiveSessions(active)
        if (data.length > 0) {
          const issued = data.reduce((max: number, d: any) => Math.max(max, new Date(d.issuedUtc).getTime()), 0)
          setLastIssued(new Date(issued).toLocaleString())
        }
      }
    } catch {}
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

      {/* Live Context */}
      <Card>
        <CardHeader>
          <CardTitle>Live Context</CardTitle>
          <CardDescription>Real-time activity across devices</CardDescription>
        </CardHeader>
        <CardContent>
          {liveContext ? (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <div className="text-sm text-gray-400">Focused App</div>
                <div className="text-lg font-semibold">{liveContext.focusedAppName ?? '—'}</div>
                <div className="text-xs text-gray-500 truncate">{liveContext.focusedWindowTitle}</div>
              </div>
              <div>
                <div className="text-sm text-gray-400">Intensity</div>
                <div className="text-lg font-semibold">{liveContext.activityIntensity}</div>
                <div className="text-xs text-gray-500">{liveContext.isIdle ? 'Idle' : 'Active'}</div>
              </div>
              <div>
                <div className="text-sm text-gray-400">Upcoming</div>
                <div className="space-y-1">
                  {liveContext.openContexts.filter(c => c.type === 'canvas_assignment').slice(0,3).map((c, i) => (
                    <div key={i} className="text-sm">{c.title}</div>
                  ))}
                  {liveContext.openContexts.filter(c => c.type === 'canvas_assignment').length === 0 && (
                    <div className="text-sm text-gray-500">No upcoming assignments</div>
                  )}
                </div>
              </div>
            </div>
          ) : (
            <EmptyState title="No context yet" description="Connect a device or start a session to see live updates." />
          )}
        </CardContent>
      </Card>

      {/* Sessions Overview */}
      <Card>
        <CardHeader>
          <CardTitle>Sessions Overview</CardTitle>
          <CardDescription>Active device sessions</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <div className="text-2xl font-bold text-primary">{activeSessions}</div>
              <div className="text-sm text-gray-400">Active Sessions</div>
            </div>
            <div className="text-right">
              <div className="text-sm">Last Issued</div>
              <div className="text-xs text-gray-400">{lastIssued ?? '—'}</div>
            </div>
          </div>
          <div className="mt-3">
            <a href="/app/devices" className="text-sm text-primary">Manage Devices →</a>
          </div>
        </CardContent>
      </Card>

      {/* Context Timeline */}
      <Card>
        <CardHeader>
          <CardTitle>Context Timeline</CardTitle>
          <CardDescription>Recent activity snapshots</CardDescription>
        </CardHeader>
        <CardContent>
          {timeline.length === 0 ? (
            <div className="text-sm text-gray-500">No recent activity</div>
          ) : (
            <div className="space-y-2">
              {timeline.map((item, idx) => (
                <div key={idx} className="flex items-center justify-between p-3 border border-gray-700 rounded-lg">
                  <div className="flex-1">
                    <div className="font-semibold">{item.focusedAppName ?? '—'}</div>
                    <div className="text-xs text-gray-400 truncate">{item.focusedWindowTitle}</div>
                  </div>
                  <div className="text-right">
                    <div className="text-sm">{item.activityIntensity}</div>
                    <div className="text-xs text-gray-500">{new Date(item.timestamp).toLocaleString()}</div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

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

      {/* Event Log */}
      <Card>
        <CardHeader>
          <CardTitle>Event Log</CardTitle>
          <CardDescription>Latest context updates and suggestions</CardDescription>
        </CardHeader>
        <CardContent>
          {events.length === 0 ? (
            <div className="text-sm text-gray-500">No events yet</div>
          ) : (
            <ul className="space-y-1 text-sm">
              {events.map((e, i) => (
                <li key={i} className="text-gray-300">{e}</li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

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
