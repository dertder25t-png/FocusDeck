import { useEffect, useState } from 'react'
import { createNotificationsConnection } from '../lib/signalr'
import { apiFetch } from '../lib/utils'

type JarvisRun = {
  runId: string
  workflowId: string
  status: string
  summary: string | null
  updatedAtUtc: string
}

type JarvisWorkflow = {
  id: string
  name: string
  description?: string | null
}

type SampleActivitySignal = {
  signalType: string
  signalValue: string
  sourceApp: string
  metadataJson?: string
}

export function JarvisPage() {
  const [workflows, setWorkflows] = useState<JarvisWorkflow[]>([])
  const [runs, setRuns] = useState<JarvisRun[]>([])
  const [isConnected, setIsConnected] = useState(false)
  const [isDisabled, setIsDisabled] = useState(false)
  const [disabledMessage, setDisabledMessage] = useState<string | null>(null)
  const [activityStatus, setActivityStatus] = useState<string | null>(null)

  useEffect(() => {
    let disposed = false

    ;(async () => {
      // Feature-flag probe – check if server-side Jarvis is enabled
      try {
        const res = await apiFetch('/v1/jarvis/workflows')
        if (res.status === 404) {
          const body = await res.json().catch(() => null as any)
          const msg = body?.error ?? 'Jarvis is not enabled for this environment.'
          setIsDisabled(true)
          setDisabledMessage(msg)
          return
        }

        if (res.ok) {
          const payload = await res.json()
          const list: JarvisWorkflow[] = Array.isArray(payload)
            ? payload.map((item: any) => ({
                id: item.id ?? item.Id ?? 'unknown',
                name: item.name ?? item.Name ?? item.id ?? item.Id ?? 'Unknown workflow',
                description: item.description ?? item.Description ?? null,
              }))
            : []
          setWorkflows(list)
        }
      } catch (e) {
        console.error('Failed to probe Jarvis feature flag', e)
      }

      try {
        const connection = await createNotificationsConnection()
        setIsConnected(true)

        connection.on('JarvisRunUpdated', (payload: any) => {
          if (!payload || disposed) return
          const runId = payload.runId ?? payload.RunId
          const workflowId = payload.workflowId ?? payload.WorkflowId ?? 'unknown'
          const status = payload.status ?? payload.Status ?? 'Unknown'
          const summary = payload.summary ?? payload.Summary ?? null
          const updatedAtUtc = payload.updatedAtUtc ?? payload.UpdatedAtUtc ?? new Date().toISOString()

          setRuns((prev) => {
            const existing = prev.find((r) => r.runId === runId)
            const next: JarvisRun = {
              runId,
              workflowId,
              status,
              summary,
              updatedAtUtc,
            }
            if (!existing) {
              return [next, ...prev].slice(0, 50)
            }
            return prev.map((r) => (r.runId === runId ? next : r))
          })
        })
      } catch (err) {
        console.error('Jarvis SignalR connection failed, will fall back to polling', err)
        setIsConnected(false)
      }
    })()

    return () => {
      disposed = true
    }
  }, [])

  const triggerRun = async (workflowId: string) => {
    if (isDisabled) return
    try {
      const response = await apiFetch('/v1/jarvis/run-workflow', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workflowId }),
      })
      if (!response.ok) {
        console.error('Failed to start Jarvis workflow', await response.text())
        return
      }

      const body = await response.json()
      const runId = body.runId ?? body.RunId
      if (!runId) return

      const initial: JarvisRun = {
        runId,
        workflowId,
        status: 'Queued',
        summary: 'Run queued',
        updatedAtUtc: new Date().toISOString(),
      }
      setRuns((prev) => [initial, ...prev].slice(0, 50))

      if (!isConnected) {
        // Fallback: poll for status a couple times
        ;(async () => {
          try {
            for (let i = 0; i < 3; i++) {
              await new Promise((r) => setTimeout(r, 1000))
              const statusRes = await apiFetch(`/v1/jarvis/runs/${runId}`)
              if (!statusRes.ok) break
              const statusBody = await statusRes.json()
              const status = statusBody.status ?? statusBody.Status ?? 'Unknown'
              const summary = statusBody.summary ?? statusBody.Summary ?? null
              setRuns((prev) =>
                prev.map((r) =>
                  r.runId === runId
                    ? {
                        ...r,
                        status,
                        summary,
                        updatedAtUtc: new Date().toISOString(),
                      }
                    : r,
                ),
              )
              if (status === 'Succeeded' || status === 'Failed') break
            }
          } catch (e) {
            console.error('Polling Jarvis run status failed', e)
          }
        })()
      }
    } catch (error) {
      console.error('Error starting Jarvis workflow', error)
    }
  }

  const emitActivitySignals = async () => {
    if (isDisabled) return

    setActivityStatus('Sending sample activity signals...')
    const now = new Date().toISOString()
    const signals: SampleActivitySignal[] = [
      {
        signalType: 'TypingBurst',
        signalValue: 'pace=fast;chars=320',
        sourceApp: 'FocusDeck.WebApp',
        metadataJson: JSON.stringify({ context: 'Jarvis page', deliveredBy: 'manual' }),
      },
      {
        signalType: 'ActiveWindow',
        signalValue: 'JarvisPage',
        sourceApp: 'FocusDeck.WebApp',
      },
    ]

    try {
      for (const signal of signals) {
        const response = await apiFetch('/v1/activity/signals', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            signalType: signal.signalType,
            signalValue: signal.signalValue,
            sourceApp: signal.sourceApp,
            capturedAtUtc: now,
            metadataJson: signal.metadataJson,
          }),
        })

        if (!response.ok) {
          const body = await response.text()
          throw new Error(body || 'Activity signal rejected')
        }
      }

      setActivityStatus('Activity signals emitted. Check jarvis.runs.* metrics.')
    } catch (error) {
      console.error('Failed to emit activity signals', error)
      setActivityStatus('Failed to emit activity signals.')
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold">Jarvis Workflows</h2>
          {isDisabled ? (
            <p className="text-sm text-amber-400">
              {disabledMessage ??
                'Jarvis is not enabled for this environment. Ask your administrator to enable Features:Jarvis.'}
            </p>
          ) : (
            <p className="text-sm text-gray-400">
              Phase 3.3 preview – trigger stubbed Jarvis workflows and watch run status update in real time.
            </p>
          )}
        </div>
        <span className="text-xs text-gray-500">
          SignalR:{' '}
          <span className={isConnected ? 'text-emerald-400' : 'text-amber-400'}>
            {isConnected ? 'Connected' : 'Not connected (polling)'}
          </span>
        </span>
      </div>

      <div className="rounded-lg border border-gray-800 bg-surface-100 p-4">
        <h3 className="text-sm font-semibold mb-3">Available Workflows</h3>
        {isDisabled ? (
          <p className="text-sm text-gray-500">Jarvis is currently disabled. No workflows can be run.</p>
        ) : workflows.length === 0 ? (
          <p className="text-sm text-gray-500">
            No Jarvis workflows were discovered. Ensure `bmad/jarvis/workflows/**/workflow.yaml` exists on the server.
          </p>
        ) : (
          <div className="flex flex-wrap gap-2">
            {workflows.map((wf) => (
              <button
                key={wf.id}
                onClick={() => triggerRun(wf.id)}
                className="px-3 py-2 rounded-md bg-primary/10 text-primary text-sm hover:bg-primary/20 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
              >
                <span className="font-medium">Run {wf.name}</span>
                {wf.description && (
                  <span className="ml-1 text-xs text-gray-400">– {wf.description}</span>
                )}
              </button>
            ))}
          </div>
        )}
        {!isDisabled && (
          <div className="mt-4 flex items-center gap-3">
            <button
              type="button"
              onClick={emitActivitySignals}
              className="px-3 py-2 rounded-md bg-emerald-500/10 text-emerald-400 text-sm font-semibold hover:bg-emerald-500/20 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-400"
            >
              Emit sample activity signals
            </button>
            {activityStatus && <span className="text-xs text-gray-400">{activityStatus}</span>}
          </div>
        )}
      </div>

      <div className="rounded-lg border border-gray-800 bg-surface-100 p-4">
        <h3 className="text-sm font-semibold mb-3">Recent Runs</h3>
        {runs.length === 0 ? (
          <p className="text-sm text-gray-500">No runs yet. Trigger a workflow above to get started.</p>
        ) : (
          <div className="space-y-2">
            {runs.map((run) => (
              <div
                key={run.runId}
                className="flex items-start justify-between rounded-md border border-gray-800/60 bg-surface px-3 py-2 text-sm"
              >
                <div>
                  <div className="font-medium">
                    {run.workflowId}{' '}
                    <span className="text-xs text-gray-500">({run.runId.slice(0, 8)}…)</span>
                  </div>
                  <div className="text-xs text-gray-400">
                    {run.status} · {new Date(run.updatedAtUtc).toLocaleTimeString()}
                  </div>
                  {run.summary && <div className="text-xs text-gray-300 mt-1">{run.summary}</div>}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
