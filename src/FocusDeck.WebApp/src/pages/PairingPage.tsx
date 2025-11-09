import { useState } from 'react'
import { startPairing } from '../lib/pake'
import { QrCode } from '../components/QrCode'

export function PairingPage() {
  const [pairingId, setPairingId] = useState<string | null>(null)
  const [code, setCode] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const start = async () => {
    setError(null)
    try {
      const data = await startPairing()
      setPairingId(data.pairingId)
      setCode(data.code)
    } catch (e: any) {
      setError(e?.message || 'Error starting pairing')
    }
  }

  const link = pairingId && code ? `focusdeck://pair?pid=${pairingId}&code=${code}` : null

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">QR Pairing</h1>
        <button onClick={start} className="px-3 py-2 rounded bg-primary">Start Pairing</button>
      </div>
      {error && <div className="text-sm text-red-400">{error}</div>}
      {code && (
        <div className="p-4 border border-gray-700 rounded">
          <div className="text-sm text-gray-400">Code</div>
          <div className="text-2xl font-bold">{code}</div>
          <div className="text-xs text-gray-500 mt-2">Pairing ID: {pairingId}</div>
          <div className="text-sm text-gray-500 mt-2">Open the Desktop app → Pair another device → enter this code / scan QR.</div>
          {link && (
            <div className="mt-3">
              <QrCode value={link} size={200} />
              <div className="text-xs text-gray-500 mt-2 break-all">{link}</div>
              <button className="mt-2 px-3 py-1 rounded bg-gray-700" onClick={() => link && navigator.clipboard.writeText(link)}>Copy Link</button>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
