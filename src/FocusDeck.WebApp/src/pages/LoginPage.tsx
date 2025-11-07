import { useState } from 'react'
import { pakeLogin } from '../lib/pake'
import { storeTokens } from '../lib/utils'

export function LoginPage() {
  const [userId, setUserId] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const res = await pakeLogin(userId, password)
      storeTokens(res.accessToken, res.refreshToken, userId)
      window.location.href = '/app/focus'
    } catch (err: any) {
      setError(err?.message || 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-surface text-white">
      <form onSubmit={onSubmit} className="w-full max-w-sm p-6 border border-gray-700 rounded-lg bg-surface-100">
        <h1 className="text-xl font-semibold mb-4">Sign in</h1>

        <label className="block text-sm mb-1">Email or User ID</label>
        <input
          className="w-full mb-3 p-2 rounded bg-gray-900 border border-gray-700 focus:outline-none"
          value={userId}
          onChange={e => setUserId(e.target.value)}
          required
          autoFocus
        />

        <label className="block text-sm mb-1">Password</label>
        <input
          type="password"
          className="w-full mb-4 p-2 rounded bg-gray-900 border border-gray-700 focus:outline-none"
          value={password}
          onChange={e => setPassword(e.target.value)}
          required
        />

        {error && <div className="text-red-400 text-sm mb-3">{error}</div>}

        <button
          type="submit"
          disabled={loading}
          className="w-full py-2 rounded bg-primary disabled:opacity-50"
        >
          {loading ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}

