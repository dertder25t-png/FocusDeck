import { useEffect, useState } from 'react'
import { apiFetch } from '../lib/utils'

type Device = {
  id: string
  clientFingerprint: string
  issuedUtc: string
  expiresUtc: string
  revokedUtc?: string | null
  isActive: boolean
}

export function DevicesPage() {
  const [devices, setDevices] = useState<Device[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await apiFetch('/v1/auth/devices')
      if (!res.ok) throw new Error('Failed to load devices')
      const data = await res.json()
      setDevices(data)
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } catch (e: any) {
      setError(e?.message || 'Failed to load devices')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const revoke = async (id: string) => {
    if (!confirm('Revoke this device session?')) return
    try {
      const res = await apiFetch(`/v1/auth/devices/${id}/revoke`, { method: 'POST' })
      if (!res.ok) throw new Error('Failed to revoke device')
      await load()
    } catch {
      alert('Error revoking device')
    }
  }

  const revokeAll = async () => {
    if (!confirm('Revoke all active sessions?')) return
    try {
      const res = await apiFetch('/v1/auth/devices/revoke-all', { method: 'POST' })
      if (!res.ok) throw new Error('Failed to revoke all devices')
      await load()
    } catch {
      alert('Error revoking all devices')
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Devices</h1>
        <button className="px-3 py-2 rounded bg-red-600" onClick={revokeAll}>Revoke All</button>
      </div>
      {loading && <div className="text-sm text-gray-400">Loading…</div>}
      {error && <div className="text-sm text-red-400">{error}</div>}
      <div className="space-y-2">
        {devices.map(d => (
          <div key={d.id} className="p-3 border border-gray-700 rounded flex items-center justify-between">
            <div>
              <div className="text-sm">Fingerprint: {d.clientFingerprint.slice(0, 12)}…</div>
              <div className="text-xs text-gray-400">Issued: {new Date(d.issuedUtc).toLocaleString()}</div>
              <div className="text-xs text-gray-400">Expires: {new Date(d.expiresUtc).toLocaleString()}</div>
            </div>
            <div className="flex items-center gap-3">
              {!d.revokedUtc && d.isActive ? (
                <button className="px-3 py-2 rounded bg-gray-700 hover:bg-gray-600" onClick={() => revoke(d.id)}>Revoke</button>
              ) : (
                <span className="text-xs text-gray-400">Revoked</span>
              )}
            </div>
          </div>
        ))}
        {devices.length === 0 && !loading && (
          <div className="text-sm text-gray-500">No devices found</div>
        )}
      </div>
    </div>
  )
}

