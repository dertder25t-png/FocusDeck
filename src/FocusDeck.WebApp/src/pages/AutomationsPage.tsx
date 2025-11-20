import { useEffect, useState } from 'react'
import { Card, CardContent } from '../components/Card'
import { Button } from '../components/Button'
import { Badge } from '../components/Badge'
import { Link } from 'react-router-dom'

interface Automation {
  id: string
  name: string
  description: string | null
  isEnabled: boolean
  lastRunAt: string | null
}

export function AutomationsPage() {
  const [automations, setAutomations] = useState<Automation[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchAutomations()
  }, [])

  const fetchAutomations = async () => {
    try {
      const res = await fetch('/v1/automations')
      if (res.ok) {
        const data = await res.json()
        setAutomations(data)
      }
    } catch (error) {
      console.error('Failed to fetch automations', error)
    } finally {
      setLoading(false)
    }
  }

  const toggleAutomation = async (id: string) => {
    try {
      const res = await fetch(`/v1/automations/${id}/toggle`, { method: 'PATCH' })
      if (res.ok) {
        const data = await res.json()
        setAutomations(prev => prev.map(a =>
          a.id === id ? { ...a, isEnabled: data.isEnabled } : a
        ))
      }
    } catch (error) {
      console.error('Failed to toggle automation', error)
    }
  }

  const deleteAutomation = async (id: string) => {
    if (!confirm('Are you sure you want to delete this automation?')) return

    try {
      const res = await fetch(`/v1/automations/${id}`, { method: 'DELETE' })
      if (res.ok) {
        setAutomations(prev => prev.filter(a => a.id !== id))
      }
    } catch (error) {
      console.error('Failed to delete automation', error)
    }
  }

  if (loading) return <div className="p-8 text-center text-gray-400">Loading automations...</div>

  return (
    <div className="space-y-6 max-w-6xl">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-semibold">Active Automations</h1>
          <p className="text-sm text-gray-400 mt-1">
            Manage your running automation rules.
          </p>
        </div>
        <Link to="/automations/proposals">
          <Button variant="secondary">View Proposals</Button>
        </Link>
      </div>

      {automations.length === 0 ? (
        <div className="text-center py-12 border border-gray-800 rounded-lg bg-gray-900/20">
          <p className="text-gray-400 mb-4">No active automations.</p>
          <Link to="/automations/proposals">
            <Button>Check Proposals</Button>
          </Link>
        </div>
      ) : (
        <div className="space-y-4">
          {automations.map(automation => (
            <Card key={automation.id} className="overflow-hidden">
              <CardContent className="p-0">
                <div className="flex items-center justify-between p-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                      <h3 className="font-medium text-white">{automation.name}</h3>
                      {!automation.isEnabled && (
                        <Badge variant="secondary" className="text-xs">Disabled</Badge>
                      )}
                    </div>
                    <p className="text-sm text-gray-400">{automation.description || "No description"}</p>
                    <div className="mt-2 flex items-center gap-4 text-xs text-gray-500">
                      <span>
                        Last run: {automation.lastRunAt
                          ? new Date(automation.lastRunAt).toLocaleString()
                          : 'Never'}
                      </span>
                    </div>
                  </div>

                  <div className="flex items-center gap-4">
                    <label className="flex items-center cursor-pointer">
                      <div className="relative">
                        <input
                          type="checkbox"
                          className="sr-only"
                          checked={automation.isEnabled}
                          onChange={() => toggleAutomation(automation.id)}
                        />
                        <div className={`block w-10 h-6 rounded-full transition-colors ${
                          automation.isEnabled ? 'bg-primary' : 'bg-gray-700'
                        }`}></div>
                        <div className={`dot absolute left-1 top-1 bg-white w-4 h-4 rounded-full transition-transform ${
                          automation.isEnabled ? 'transform translate-x-4' : ''
                        }`}></div>
                      </div>
                    </label>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="text-red-400 hover:text-red-300 hover:bg-red-900/20"
                      onClick={() => deleteAutomation(automation.id)}
                    >
                      Delete
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
