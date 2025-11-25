import { useState, useEffect } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { pakeLogin } from '../../lib/pake'
import { storeTokens } from '../../lib/utils'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [userId, setUserId] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({})

  // Get redirect URL from query param or location state
  const redirectUrl = new URLSearchParams(location.search).get('redirectUrl') ||
    (location.state as { from?: string } | undefined)?.from ||
    '/'

  useEffect(() => {
    // Clear error when user starts typing
    if (error) {
      setError(null)
    }
  }, [userId, password])

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {}

    if (!userId.trim()) {
      errors.userId = 'Email or User ID is required'
    } else if (userId.length < 3) {
      errors.userId = 'Please enter a valid email or user ID'
    }

    if (!password) {
      errors.password = 'Password is required'
    } else if (password.length < 6) {
      errors.password = 'Password must be at least 6 characters'
    }

    setValidationErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!validateForm()) {
      return
    }

    setError(null)
    setLoading(true)

    try {
      const res = await pakeLogin(userId, password)
      storeTokens(res.accessToken, res.refreshToken, userId)
      
      // Redirect to original page or dashboard
      navigate(redirectUrl, { replace: true })
    } catch (err) {
      const error = err as { message?: string };
      const rawMessage = error?.message || 'Authentication failed'
      // Map server-side auth errors to friendly UI messages
      let friendly = rawMessage
      if (rawMessage === 'Missing KDF salt') {
        friendly = 'Account needs an upgrade — please reset your password or contact support.'
      } else if (rawMessage === 'Invalid KDF salt') {
        friendly = 'Authentication could not proceed due to credential metadata. Please try again or contact support.'
      } else if (rawMessage === 'User not found') {
        friendly = 'Account not found. Please check your email or create an account.'
      }

      setError(friendly)
      console.error('Login error:', err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gradient-to-br from-surface via-surface-100 to-surface text-white px-4 py-12">
      {/* Background accent */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-primary/5 rounded-full blur-3xl" />
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-primary/5 rounded-full blur-3xl" />
      </div>

      {/* Logo and branding */}
      <div className="mb-8 text-center relative z-10">
        <div className="text-5xl mb-3">🎯</div>
        <h1 className="text-3xl font-bold bg-gradient-to-r from-primary to-primary/70 bg-clip-text text-transparent">
          FocusDeck
        </h1>
        <p className="text-gray-400 text-sm mt-1">Productivity Suite for Deep Work</p>
      </div>

      {/* Login Card */}
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-md relative z-10 p-8 rounded-xl bg-surface-100/80 backdrop-blur-sm border border-gray-700/50 shadow-2xl"
      >
        <div className="mb-6">
          <h2 className="text-2xl font-semibold">Welcome back</h2>
          <p className="text-gray-400 text-sm mt-1">Sign in to continue to your workspace</p>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="mb-4 p-3 rounded-lg bg-red-950/30 border border-red-700/50 text-red-200 text-sm flex items-start gap-3">
            <span className="text-lg mt-0.5">⚠️</span>
            <div>
              <div className="font-semibold">Sign in failed</div>
              <div className="text-sm mt-1">{error}</div>
            </div>
          </div>
        )}

        {/* Email/ID Field */}
        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-200 mb-2">
            Email or User ID
          </label>
          <input
            type="text"
            value={userId}
            onChange={(e) => setUserId(e.target.value)}
            placeholder="you@example.com"
            autoComplete="email"
            disabled={loading}
            className={`w-full px-4 py-3 rounded-lg bg-gray-900/50 border transition-all ${
              validationErrors.userId
                ? 'border-red-500/50 focus:border-red-500'
                : 'border-gray-700/50 focus:border-primary/50'
            } text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-primary/20 disabled:opacity-50 disabled:cursor-not-allowed`}
          />
          {validationErrors.userId && (
            <p className="text-red-400 text-xs mt-1.5">{validationErrors.userId}</p>
          )}
        </div>

        {/* Password Field */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-2">
            <label className="block text-sm font-medium text-gray-200">
              Password
            </label>
            <a href="#" className="text-xs text-primary hover:text-primary/80 transition-colors">
              Forgot?
            </a>
          </div>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            autoComplete="current-password"
            disabled={loading}
            className={`w-full px-4 py-3 rounded-lg bg-gray-900/50 border transition-all ${
              validationErrors.password
                ? 'border-red-500/50 focus:border-red-500'
                : 'border-gray-700/50 focus:border-primary/50'
            } text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-primary/20 disabled:opacity-50 disabled:cursor-not-allowed`}
          />
          {validationErrors.password && (
            <p className="text-red-400 text-xs mt-1.5">{validationErrors.password}</p>
          )}
        </div>

        {/* Submit Button */}
        <button
          type="submit"
          disabled={loading}
          className="w-full py-3 rounded-lg bg-gradient-to-r from-primary to-primary/80 font-semibold transition-all hover:shadow-lg hover:shadow-primary/20 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
        >
          {loading && (
            <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
          )}
          {loading ? 'Signing in...' : 'Sign in'}
        </button>

        {/* Divider */}
        <div className="my-6 flex items-center gap-3">
          <div className="flex-1 h-px bg-gray-700/50" />
          <span className="text-xs text-gray-400">New user?</span>
          <div className="flex-1 h-px bg-gray-700/50" />
        </div>

        {/* Register Link */}
        <div className="text-center">
          <Link
            to="/register"
            className="inline-flex items-center justify-center gap-1 text-sm font-medium text-primary hover:text-primary/80 transition-colors"
          >
            Create account
            <span>→</span>
          </Link>
        </div>

        {/* Footer */}
        <div className="mt-6 pt-6 border-t border-gray-700/30 text-xs text-gray-500 text-center">
          <p>By signing in, you agree to our</p>
          <div className="flex items-center justify-center gap-3 mt-1">
            <a href="#" className="hover:text-gray-300 transition-colors">Terms</a>
            <span>•</span>
            <a href="#" className="hover:text-gray-300 transition-colors">Privacy</a>
          </div>
        </div>
      </form>

      {/* Development Notice - Remove in production */}
      {process.env.NODE_ENV === 'development' && (
        <div className="mt-6 text-xs text-gray-600 text-center relative z-10">
          <p>Demo: Use test@gmail.com / 123456789</p>
        </div>
      )}
    </div>
  )
}

