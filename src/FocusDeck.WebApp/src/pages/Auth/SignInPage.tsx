
import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';

export const SignInPage: React.FC = () => {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();
    const location = useLocation();

    // Mock login for now
    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);

        // Simulate API call
        setTimeout(() => {
            // Set mock token
            localStorage.setItem('focusdeck_access_token', 'mock_token_123');
            localStorage.setItem('focusdeck_user', JSON.stringify({ name: 'User', email }));

            // Dispatch storage event to notify other tabs/components if listening
            window.dispatchEvent(new Event('storage'));

            setLoading(false);

            // Redirect
            const from = (location.state as any)?.from?.pathname || '/';
            navigate(from, { replace: true });
        }, 1000);
    };

    return (
        <div className="min-h-screen w-full flex items-center justify-center bg-gray-100 dark:bg-gray-900 bg-cover bg-center relative" style={{ backgroundImage: 'url(https://images.unsplash.com/photo-1497294815431-9365093b7331?ixlib=rb-1.2.1&auto=format&fit=crop&w=1950&q=80)' }}>
            <div className="absolute inset-0 bg-black/40 backdrop-blur-sm"></div>

            <div className="relative z-10 w-full max-w-md bg-white/90 dark:bg-gray-950/90 backdrop-blur-md p-8 rounded-2xl shadow-2xl border border-white/20">
                <div className="text-center mb-8">
                    <div className="w-12 h-12 bg-blue-600 rounded-xl mx-auto flex items-center justify-center mb-4 shadow-lg rotate-3">
                         <i className="fa-solid fa-layer-group text-white text-2xl"></i>
                    </div>
                    <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Welcome back</h1>
                    <p className="text-sm text-gray-500 dark:text-gray-400 mt-2">Sign in to your FocusDeck Workspace</p>
                </div>

                <form onSubmit={handleLogin} className="space-y-4">
                    <div>
                        <label className="block text-xs font-bold text-gray-500 uppercase tracking-wide mb-1.5">Email</label>
                        <input
                            type="email"
                            required
                            value={email}
                            onChange={e => setEmail(e.target.value)}
                            className="w-full px-4 py-2.5 rounded-lg bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-800 outline-none focus:ring-2 focus:ring-blue-500 transition-all text-gray-900 dark:text-white placeholder-gray-400"
                            placeholder="name@company.com"
                        />
                    </div>
                    <div>
                        <label className="block text-xs font-bold text-gray-500 uppercase tracking-wide mb-1.5 flex justify-between">
                            Password
                            <a href="#" className="text-blue-600 hover:text-blue-500 normal-case font-medium">Forgot?</a>
                        </label>
                        <input
                            type="password"
                            required
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            className="w-full px-4 py-2.5 rounded-lg bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-800 outline-none focus:ring-2 focus:ring-blue-500 transition-all text-gray-900 dark:text-white"
                            placeholder="••••••••"
                        />
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        className="w-full py-3 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-lg shadow-lg hover:shadow-blue-500/30 transition-all flex items-center justify-center gap-2"
                    >
                        {loading ? <i className="fa-solid fa-circle-notch fa-spin"></i> : 'Sign In'}
                    </button>
                </form>

                <div className="mt-8 pt-6 border-t border-gray-200 dark:border-gray-800 text-center">
                    <p className="text-sm text-gray-500">Don't have an account? <a href="#" className="text-blue-600 font-bold hover:underline">Create Account</a></p>
                </div>
            </div>
        </div>
    );
};
