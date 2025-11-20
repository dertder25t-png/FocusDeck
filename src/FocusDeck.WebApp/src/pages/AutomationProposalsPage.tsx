import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '../components/Card'
import { Button } from '../components/Button'
import { Badge } from '../components/Badge'

interface Proposal {
  id: string
  title: string
  description: string
  yamlDefinition: string
  confidenceScore: number
}

export function AutomationProposalsPage() {
  const [proposals, setProposals] = useState<Proposal[]>([])
  const [loading, setLoading] = useState(true)
  const [processing, setProcessing] = useState<string | null>(null)

  useEffect(() => {
    fetchProposals()
  }, [])

  const fetchProposals = async () => {
    try {
      const res = await fetch('/v1/automations/proposals')
      if (res.ok) {
        const data = await res.json()
        setProposals(data)
      }
    } catch (error) {
      console.error('Failed to fetch proposals', error)
    } finally {
      setLoading(false)
    }
  }

  const handleAccept = async (id: string) => {
    setProcessing(id)
    try {
      const res = await fetch(`/v1/automations/proposals/${id}/accept`, {
        method: 'POST'
      })
      if (res.ok) {
        setProposals(prev => prev.filter(p => p.id !== id))
        // Optional: Toast success
      }
    } catch (error) {
      console.error('Failed to accept proposal', error)
    } finally {
      setProcessing(null)
    }
  }

  const handleReject = async (id: string) => {
    setProcessing(id)
    try {
      const res = await fetch(`/v1/automations/proposals/${id}`, {
        method: 'DELETE'
      })
      if (res.ok) {
        setProposals(prev => prev.filter(p => p.id !== id))
      }
    } catch (error) {
      console.error('Failed to reject proposal', error)
    } finally {
      setProcessing(null)
    }
  }

  if (loading) return <div className="p-8 text-center text-gray-400">Loading proposals...</div>

  return (
    <div className="space-y-6 max-w-6xl">
      <div>
        <h1 className="text-2xl font-semibold">Automation Proposals</h1>
        <p className="text-sm text-gray-400 mt-1">
          Review automations suggested by Jarvis based on your habits.
        </p>
      </div>

      {proposals.length === 0 ? (
        <div className="text-center py-12 border border-gray-800 rounded-lg bg-gray-900/20">
          <p className="text-gray-400">No pending proposals. Jarvis is watching for patterns...</p>
        </div>
      ) : (
        <div className="grid gap-6">
          {proposals.map(proposal => (
            <Card key={proposal.id}>
              <CardHeader>
                <div className="flex justify-between items-start">
                  <div>
                    <CardTitle>{proposal.title}</CardTitle>
                    <CardDescription>{proposal.description}</CardDescription>
                  </div>
                  <Badge variant={proposal.confidenceScore > 0.8 ? 'success' : 'secondary'}>
                    {(proposal.confidenceScore * 100).toFixed(0)}% Confidence
                  </Badge>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="bg-gray-950 rounded-md p-4 overflow-x-auto border border-gray-800">
                  <pre className="text-sm font-mono text-green-400">
                    {proposal.yamlDefinition}
                  </pre>
                </div>
                <div className="flex justify-end gap-3">
                  <Button
                    variant="ghost"
                    onClick={() => handleReject(proposal.id)}
                    disabled={processing === proposal.id}
                  >
                    Reject
                  </Button>
                  <Button
                    onClick={() => handleAccept(proposal.id)}
                    disabled={processing === proposal.id}
                  >
                    {processing === proposal.id ? 'Accepting...' : 'Accept & Enable'}
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
