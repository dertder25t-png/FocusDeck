import { useEffect, useState } from 'react'
import { Badge } from '../components/Badge'

interface HistoryProps {
  automationId: string
  onClose: () => void
}

interface Execution {
  id: string
  executedAt: string
  success: boolean
  errorMessage: string | null
  durationMs: number
  triggerData: string | null
}

export function AutomationHistory({ automationId, onClose }: HistoryProps) {
  const [history, setHistory] = useState<Execution[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchHistory()
  }, [automationId])

  const fetchHistory = async () => {
    try {
      const res = await fetch(`/v1/automations/${automationId}/history`)
      if (res.ok) {
        const data = await res.json()
        setHistory(data)
      }
    } catch (error) {
      console.error('Failed to fetch history', error)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
      <div className="bg-gray-900 border border-gray-800 rounded-xl w-full max-w-2xl max-h-[80vh] flex flex-col shadow-2xl">
        <div className="p-6 border-b border-gray-800 flex justify-between items-center">
          <h2 className="text-xl font-semibold text-white">Run History</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-white">
            ✕
          </button>
        </div>

        <div className="p-0 flex-1 overflow-y-auto">
          {loading ? (
            <div className="p-8 text-center text-gray-400">Loading history...</div>
          ) : history.length === 0 ? (
            <div className="p-8 text-center text-gray-400">No execution history found.</div>
          ) : (
            <table className="w-full text-left text-sm text-gray-400">
              <thead className="bg-gray-950 text-gray-200 font-medium border-b border-gray-800 sticky top-0">
                <tr>
                  <th className="px-6 py-3">Time</th>
                  <th className="px-6 py-3">Status</th>
                  <th className="px-6 py-3">Duration</th>
                  <th className="px-6 py-3">Trigger</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-800">
                {history.map((exec) => (
                  <tr key={exec.id} className="hover:bg-gray-800/50">
                    <td className="px-6 py-3 whitespace-nowrap">
                      {new Date(exec.executedAt).toLocaleString()}
                    </td>
                    <td className="px-6 py-3">
                      {exec.success ? (
                        <Badge variant="success" className="text-xs">Success</Badge>
                      ) : (
                        <div className="group relative cursor-help">
                          <Badge variant="danger" className="text-xs">Failed</Badge>
                          {exec.errorMessage && (
                            <div className="absolute left-0 top-full mt-1 hidden w-64 p-2 bg-red-900 text-white text-xs rounded z-10 group-hover:block shadow-lg border border-red-800">
                              {exec.errorMessage}
                            </div>
                          )}
                        </div>
                      )}
                    </td>
                    <td className="px-6 py-3">
                      {exec.durationMs}ms
                    </td>
                    <td className="px-6 py-3 truncate max-w-[200px]" title={exec.triggerData || ''}>
                      {exec.triggerData || '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  )
}
