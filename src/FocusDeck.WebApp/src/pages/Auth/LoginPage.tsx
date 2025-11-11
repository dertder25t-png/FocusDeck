import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { pakeLogin } from '../../lib/pake'
import { storeTokens } from '../../lib/utils'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [userId, setUserId] = useState('test@gmail.com')
  const [password, setPassword] = useState('123456789')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const res = await pakeLogin(userId, password)
      storeTokens(res.accessToken, res.refreshToken, userId)
      const from = (location.state as { from?: string } | undefined)?.from || '/'
      navigate(from, { replace: true })
    } catch (err: any) {
      setError(err?.message || 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-surface text-white px-4">
      <form onSubmit={onSubmit} className="w-full max-w-sm p-6 border border-gray-700 rounded-lg bg-surface-100 shadow-lg">
        <h1 className="text-2xl font-semibold mb-2">Welcome back</h1>
        <p className="text-sm text-gray-400 mb-6">Sign in with your FocusDeck ID to continue.</p>
        <p className="text-xs uppercase tracking-wide text-gray-500">Test users may use <strong>test@gmail.com</strong> / <strong>123456789</strong>.</p>

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

        <div className="text-sm text-gray-400 mt-6 text-center">
          Need an account?{' '}
          <Link to="/register" className="text-primary hover:underline">
            Register
          </Link>
        </div>
      </form>
    </div>
  )
}

