import { type FormEvent, useMemo, useState } from 'react'
import { startPairing, redeemPairing } from '../../lib/pake'
import { QrCode } from '../../components/QrCode'

type RedeemStatus = { type: 'success' | 'error'; message: string }

export function PairingPage() {
  const [pairingId, setPairingId] = useState<string | null>(null)
  const [code, setCode] = useState<string | null>(null)
  const [pairingError, setPairingError] = useState<string | null>(null)
  const [loadingPairing, setLoadingPairing] = useState(false)

  const [redeemPairingId, setRedeemPairingId] = useState('')
  const [redeemCode, setRedeemCode] = useState('')
  const [redeemStatus, setRedeemStatus] = useState<RedeemStatus | null>(null)
  const [loadingRedeem, setLoadingRedeem] = useState(false)

  const pairingLink = useMemo(() => {
    if (!pairingId || !code) return null
    return `focusdeck://pair?pid=${pairingId}&code=${code}`
  }, [pairingId, code])

  const start = async () => {
    setPairingError(null)
    setLoadingPairing(true)
    try {
      const data = await startPairing()
      setPairingId(data.pairingId)
      setCode(data.code)
    } catch (err: any) {
      setPairingError(err?.message || 'Error starting pairing')
    } finally {
      setLoadingPairing(false)
    }
  }

  const handleCopyLink = () => {
    if (!pairingLink) return
    navigator.clipboard.writeText(pairingLink)
  }

  const handleRedeem = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!redeemPairingId.trim() || !redeemCode.trim()) {
      setRedeemStatus({ type: 'error', message: 'Both fields are required.' })
      return
    }

    setRedeemStatus(null)
    setLoadingRedeem(true)
    try {
      const result = await redeemPairing(redeemPairingId.trim(), redeemCode.trim())
      setRedeemStatus({
        type: 'success',
        message: `Claim code accepted for ${result.userId}. Vault data is ready for the requesting device.`,
      })
    } catch (err: any) {
      setRedeemStatus({ type: 'error', message: err?.message || 'Redeem failed' })
    } finally {
      setLoadingRedeem(false)
    }
  }

  return (
    <div className="space-y-8">
      <section className="rounded-2xl border border-gray-800 bg-surface-100 p-6 shadow-lg">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-xs uppercase text-gray-400 tracking-wide">Device pairing</p>
            <h1 className="text-2xl font-semibold text-white">Generate a claim code</h1>
            <p className="text-sm text-gray-400">Start a pairing session for another device to claim.</p>
          </div>
          <button
            onClick={start}
            disabled={loadingPairing}
            className="rounded-full bg-primary px-4 py-2 text-sm font-semibold text-black transition hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {loadingPairing ? 'Generating…' : 'Start session'}
          </button>
        </div>
        {pairingError && <p className="mt-4 text-sm text-red-400">{pairingError}</p>}

        {code && pairingId && (
          <div className="mt-6 grid gap-5 rounded-2xl border border-gray-800 bg-surface p-5 md:grid-cols-[1.2fr_1fr]">
            <div className="space-y-3">
              <div className="flex items-baseline gap-2">
                <span className="text-xs uppercase text-gray-400 tracking-wide">Claim code</span>
                <span className="rounded-full bg-gray-900/70 px-3 py-1 text-xs font-bold text-gray-200">expires in 10m</span>
              </div>
              <p className="text-3xl font-semibold text-white tracking-tight">{code}</p>
              <p className="text-sm text-gray-500">Pairing ID: {pairingId}</p>
              <p className="text-sm text-gray-400">
                Use this code inside the FocusDeck desktop/mobile onboarding flow or scan the QR to publish credentials to the requesting device.
              </p>
              <button
                onClick={handleCopyLink}
                className="text-sm font-medium text-primary underline-offset-4 hover:underline"
              >
                Copy deep link
              </button>
            </div>
            {pairingLink && (
              <div className="flex flex-col items-center justify-center gap-3 rounded-2xl border border-gray-800 bg-black/40 p-4">
                <QrCode value={pairingLink} size={180} fgColor="#ffffff" bgColor="#0f172a" />
                <p className="text-xs text-gray-400">Scan or share this QR with the new device</p>
              </div>
            )}
          </div>
        )}
      </section>

      <section className="rounded-2xl border border-gray-800 bg-surface-100 p-6 shadow-lg">
        <div className="flex items-center justify-between gap-4">
          <div>
            <p className="text-xs uppercase text-gray-400 tracking-wide">Claim code</p>
            <h2 className="text-xl font-semibold text-white">Redeem another device's pairing code</h2>
            <p className="text-sm text-gray-400">Enter the pairing ID and claim code to accept a vault transfer.</p>
          </div>
        </div>
        <form className="mt-6 space-y-4" onSubmit={handleRedeem}>
          <div>
            <label className="text-xs font-semibold uppercase tracking-wide text-gray-400">Pairing ID</label>
            <input
              type="text"
              value={redeemPairingId}
              onChange={e => setRedeemPairingId(e.target.value)}
              className="mt-1 w-full rounded border border-gray-700 bg-gray-900/50 px-3 py-2 text-sm text-white outline-none focus:border-primary"
              required
              autoComplete="off"
            />
          </div>
          <div>
            <label className="text-xs font-semibold uppercase tracking-wide text-gray-400">Claim code</label>
            <input
              value={redeemCode}
              onChange={e => setRedeemCode(e.target.value)}
              type="text"
              className="mt-1 w-full rounded border border-gray-700 bg-gray-900/50 px-3 py-2 text-sm text-white outline-none focus:border-primary"
              required
              autoComplete="off"
            />
          </div>
          {redeemStatus && (
            <p className={`text-sm ${redeemStatus.type === 'success' ? 'text-green-400' : 'text-red-400'}`}>
              {redeemStatus.message}
            </p>
          )}
          <div className="flex items-center justify-end gap-3">
            <button
              type="submit"
              disabled={loadingRedeem}
              className="rounded-full bg-primary px-5 py-2 text-sm font-semibold text-black transition hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loadingRedeem ? 'Redeeming…' : 'Claim code'}
            </button>
          </div>
        </form>
      </section>
    </div>
  )
}
