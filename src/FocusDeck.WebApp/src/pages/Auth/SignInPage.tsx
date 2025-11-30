
import React, { useState } from 'react'
import { useLocation } from 'react-router-dom'
import { pakeLogin } from '../../lib/pake'
import { storeTokens } from '../../lib/utils'

export const SignInPage: React.FC = () => {
    const location = useLocation()

    const [userId, setUserId] = useState('')
    const [password, setPassword] = useState('')
    const [showPassword, setShowPassword] = useState(false)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const from = (location.state as any)?.from?.pathname || '/'

    function mapError(raw: string): string {
        switch (raw) {
            case 'Missing KDF salt':
                return 'Account upgrade required — reset password or contact support.'
            case 'Invalid KDF salt':
                return 'Credential metadata mismatch — please retry or contact support.'
            case 'User not found':
                return 'No account found. Check your user ID or register.'
            case 'Login failed':
                return 'Authentication failed — verify credentials.'
            default:
                return raw || 'Authentication failed'
        }
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault()
        setError(null)
        setLoading(true)
        try {
            console.log('[SignIn] Starting login for user:', userId.trim());
            const res = await pakeLogin(userId.trim(), password)
            console.log('[SignIn] Login successful, storing tokens');
            storeTokens(res.accessToken, res.refreshToken, userId.trim())
            console.log('[SignIn] Tokens stored, reloading page');
            // Force a full page reload to ensure all state is fresh
            window.location.href = from
        } catch (err: any) {
            const msg = mapError(err?.message)
            setError(msg)
            console.error('[SignIn] Login error:', err)
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="min-h-screen w-full flex items-center justify-center bg-paper dark:bg-gray-900 relative px-4 py-10">
            {/* Decorative grid / retro accents */}
            <div className="absolute inset-0 pointer-events-none opacity-10 mix-blend-overlay" style={{ backgroundImage: 'radial-gradient(circle at 1px 1px, var(--ink) 1px, transparent 0)', backgroundSize: '28px 28px' }} />

            <div className="max-w-md w-full relative z-10">
                <div className="bg-surface border-2 border-ink rounded-xl shadow-hard overflow-hidden flex flex-col">
                    {/* Header */}
                    <div className="px-6 py-5 border-b-2 border-ink bg-subtle flex items-center justify-between">
                        <div className="flex items-center gap-3">
                            <div className="w-10 h-10 bg-ink text-white rounded-lg flex items-center justify-center shadow-inner">
                                <i className="fa-solid fa-layer-group"></i>
                            </div>
                            <div>
                                <h1 className="font-display text-xl font-bold leading-none">FocusDeck Login</h1>
                                <p className="text-xs text-gray-500 dark:text-gray-400 mt-1 tracking-wide">Secure SRP / PAKE Authentication</p>
                            </div>
                        </div>
                        <div className="hidden md:flex gap-1">
                            <div className="w-3 h-3 rounded-full bg-red-500" />
                            <div className="w-3 h-3 rounded-full bg-yellow-400" />
                            <div className="w-3 h-3 rounded-full bg-green-500" />
                        </div>
                    </div>

                    {/* Body */}
                    <form onSubmit={handleSubmit} className="p-6 space-y-5">
                        {error && (
                            <div className="border-2 border-red-600 bg-red-100 text-red-800 rounded-lg p-3 text-sm flex items-start gap-3 shadow-sm">
                                <i className="fa-solid fa-triangle-exclamation mt-0.5"></i>
                                <div className="flex-1">
                                    <div className="font-bold mb-0.5">Sign in failed</div>
                                    <div>{error}</div>
                                </div>
                                <button type="button" onClick={() => setError(null)} className="text-red-700 hover:text-red-900">
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
                            <div className="relative">
                                <input
                                    type={showPassword ? 'text' : 'password'}
                                    autoComplete="current-password"
                                    required
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    placeholder="••••••••"
                                    className="w-full px-4 py-2.5 rounded-lg bg-white dark:bg-gray-950 border-2 border-ink focus:outline-none focus:ring-2 focus:ring-primary/30 font-mono text-sm pr-10"
                                />
                                <i className="fa-solid fa-key absolute right-3 top-1/2 -translate-y-1/2 text-gray-400"></i>
                            </div>
                        </div>

                        <button
                            type="submit"
                            disabled={loading}
                            className="w-full py-3 rounded-lg bg-ink text-white font-bold text-sm tracking-wide flex items-center justify-center gap-2 hover:opacity-90 transition disabled:opacity-50"
                        >
                            {loading && <i className="fa-solid fa-circle-notch fa-spin"></i>}
                            {loading ? 'Authenticating…' : 'Sign In Securely'}
                        </button>

                        <div className="text-center text-xs text-gray-500 space-y-1 pt-2">
                            <p>Encrypted SRP handshake protects your password.</p>
                            <p className="italic">Need an account? <a href="/register" className="text-primary font-semibold hover:underline">Register</a></p>
                        </div>
                    </form>

                    {/* Footer */}
                    <div className="px-6 py-4 border-t-2 border-ink bg-subtle flex items-center justify-between text-xs text-gray-500">
                        <span className="font-mono">v{import.meta.env.VITE_APP_VERSION || '0.1.0'}</span>
                        <span>© FocusDeck</span>
                    </div>
                </div>
            </div>
        </div>
    )
}
