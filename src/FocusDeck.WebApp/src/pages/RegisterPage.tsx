import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { pakeRegister, pakeLogin } from '../lib/pake'
import { storeTokens } from '../lib/utils'

export function RegisterPage() {
  const navigate = useNavigate()
  const [userId, setUserId] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (password !== confirmPassword) {
      setError('Passwords do not match')
      return
    }

    setLoading(true)
    setError(null)
    try {
      await pakeRegister(userId.trim(), password)
      const loginResult = await pakeLogin(userId.trim(), password)
      storeTokens(loginResult.accessToken, loginResult.refreshToken, userId.trim())
      navigate('/', { replace: true })
    } catch (err: any) {
      setError(err?.message || 'Registration failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-surface text-white px-4">
      <form onSubmit={onSubmit} className="w-full max-w-sm p-6 border border-gray-700 rounded-lg bg-surface-100 shadow-lg">
        <h1 className="text-2xl font-semibold mb-2">Create your FocusDeck ID</h1>
        <p className="text-sm text-gray-400 mb-6">Register with a unique username or email.</p>

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
          className="w-full mb-3 p-2 rounded bg-gray-900 border border-gray-700 focus:outline-none"
          value={password}
          onChange={e => setPassword(e.target.value)}
          required
          minLength={8}
        />

        <label className="block text-sm mb-1">Confirm password</label>
        <input
          type="password"
          className="w-full mb-4 p-2 rounded bg-gray-900 border border-gray-700 focus:outline-none"
          value={confirmPassword}
          onChange={e => setConfirmPassword(e.target.value)}
          required
        />

        {error && <div className="text-red-400 text-sm mb-3">{error}</div>}

        <button type="submit" disabled={loading} className="w-full py-2 rounded bg-primary disabled:opacity-50">
          {loading ? 'Creating account…' : 'Register'}
        </button>

        <div className="text-sm text-gray-400 mt-6 text-center">
          Already have an account?{' '}
          <Link to="/login" className="text-primary hover:underline">
            Sign in
          </Link>
        </div>
      </form>
    </div>
  )
}
