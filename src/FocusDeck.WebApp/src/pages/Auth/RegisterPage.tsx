import React, { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { pakeRegister } from '../../lib/pake'
import { storeTokens } from '../../lib/utils'

export const RegisterPage: React.FC = () => {
  const navigate = useNavigate()
  const [userId, setUserId] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  function mapError(raw: string): string {
    switch (raw) {
      case 'User already exists':
        return 'An account with this ID already exists.'
      case 'Invalid verifier':
        return 'Verifier generation failed — retry.'
      case 'Password too weak':
        return 'Choose a stronger password.'
      case 'PAKE register start failed':
        return 'Registration handshake could not start.'
      case 'PAKE register finish failed':
        return 'Registration handshake failed to finalize.'
      default:
        return raw || 'Registration failed'
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSuccess(null)

    if (password !== confirm) {
      setError('Passwords do not match')
      return
    }
    if (password.length < 8) {
      setError('Password must be at least 8 characters')
      return
    }

    setLoading(true)
    try {
      const res: any = await pakeRegister(userId.trim(), password)
      if (res?.accessToken && res?.refreshToken) {
        storeTokens(res.accessToken, res.refreshToken, userId.trim())
        navigate('/', { replace: true })
        return
      }
      setSuccess('Account created. Redirecting to login…')
      setTimeout(() => navigate('/login', { replace: true, state: { registeredUserId: userId.trim() } }), 1200)
    } catch (err: any) {
      const msg = mapError(err?.message)
      setError(msg)
      console.error('Register error:', err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen w-full flex items-center justify-center bg-paper dark:bg-gray-900 relative px-4 py-10">
      <div className="absolute inset-0 pointer-events-none opacity-10 mix-blend-overlay" style={{ backgroundImage: 'radial-gradient(circle at 1px 1px, var(--ink) 1px, transparent 0)', backgroundSize: '28px 28px' }} />
      <div className="max-w-md w-full relative z-10">
        <div className="bg-surface border-2 border-ink rounded-xl shadow-hard overflow-hidden flex flex-col">
          <div className="px-6 py-5 border-b-2 border-ink bg-subtle flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-ink text-white rounded-lg flex items-center justify-center shadow-inner">
                <i className="fa-solid fa-user-plus"></i>
              </div>
              <div>
                <h1 className="font-display text-xl font-bold leading-none">Create Account</h1>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-1 tracking-wide">Secure SRP / PAKE Provisioning</p>
              </div>
            </div>
            <div className="hidden md:flex gap-1">
              <div className="w-3 h-3 rounded-full bg-red-500" />
              <div className="w-3 h-3 rounded-full bg-yellow-400" />
              <div className="w-3 h-3 rounded-full bg-green-500" />
            </div>
          </div>
          <form onSubmit={handleSubmit} className="p-6 space-y-5">
            {error && (
              <div className="border-2 border-red-600 bg-red-100 text-red-800 rounded-lg p-3 text-sm flex items-start gap-3 shadow-sm">
                <i className="fa-solid fa-triangle-exclamation mt-0.5"></i>
                <div className="flex-1">
                  <div className="font-bold mb-0.5">Registration failed</div>
                  <div>{error}</div>
                </div>
                <button type="button" onClick={() => setError(null)} className="text-red-700 hover:text-red-900">
                  <i className="fa-solid fa-xmark"></i>
                </button>
              </div>
            )}
            {success && (
              <div className="border-2 border-green-600 bg-green-100 text-green-800 rounded-lg p-3 text-sm flex items-start gap-3 shadow-sm">
                <i className="fa-solid fa-circle-check mt-0.5"></i>
                <div className="flex-1">
                  <div className="font-bold mb-0.5">Success</div>
                  <div>{success}</div>
                </div>
                <button type="button" onClick={() => setSuccess(null)} className="text-green-700 hover:text-green-900">
                  <i className="fa-solid fa-xmark"></i>
                </button>
              </div>
            )}
            <div className="space-y-2">
              <label className="text-xs font-bold tracking-wide uppercase text-gray-600 dark:text-gray-300">User ID or Email</label>
              <input
                autoComplete="username"
                required
                value={userId}
                onChange={(e) => setUserId(e.target.value)}
                placeholder="you@example.com"
                className="w-full px-4 py-2.5 rounded-lg bg-white dark:bg-gray-950 border-2 border-ink focus:outline-none focus:ring-2 focus:ring-primary/30 font-mono text-sm"
              />
            </div>
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-xs font-bold tracking-wide uppercase text-gray-600 dark:text-gray-300">Password</label>
                <button type="button" onClick={() => setShowPassword(!showPassword)} className="text-xs font-semibold text-primary hover:underline">
                  {showPassword ? 'Hide' : 'Show'}
                </button>
              </div>
              <input
                type={showPassword ? 'text' : 'password'}
                autoComplete="new-password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full px-4 py-2.5 rounded-lg bg-white dark:bg-gray-950 border-2 border-ink focus:outline-none focus:ring-2 focus:ring-primary/30 font-mono text-sm"
              />
            </div>
            <div className="space-y-2">
              <label className="text-xs font-bold tracking-wide uppercase text-gray-600 dark:text-gray-300">Confirm Password</label>
              <input
                type={showPassword ? 'text' : 'password'}
                autoComplete="new-password"
                required
                value={confirm}
                onChange={(e) => setConfirm(e.target.value)}
                placeholder="••••••••"
                className="w-full px-4 py-2.5 rounded-lg bg-white dark:bg-gray-950 border-2 border-ink focus:outline-none focus:ring-2 focus:ring-primary/30 font-mono text-sm"
              />
            </div>
            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 rounded-lg bg-ink text-white font-bold text-sm tracking-wide flex items-center justify-center gap-2 hover:opacity-90 transition disabled:opacity-50"
            >
              {loading && <i className="fa-solid fa-circle-notch fa-spin"></i>}
              {loading ? 'Provisioning…' : 'Create Account Securely'}
            </button>
            <div className="text-center text-xs text-gray-500 space-y-1 pt-2">
              <p>Your password is never sent — SRP verifier stored.</p>
              <p>
                Already have an account?{' '}
                <Link to="/login" className="text-primary font-semibold hover:underline">Login</Link>
              </p>
            </div>
          </form>
          <div className="px-6 py-4 border-t-2 border-ink bg-subtle flex items-center justify-between text-xs text-gray-500">
            <span className="font-mono">v{import.meta.env.VITE_APP_VERSION || '0.1.0'}</span>
            <span>© FocusDeck</span>
          </div>
        </div>
      </div>
    </div>
  )
}
