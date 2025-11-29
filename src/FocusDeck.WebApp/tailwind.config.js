/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
        display: ['Space Grotesk', 'sans-serif'],
      },
      colors: {
        paper: '#f8f7f4',
        surface: '#ffffff',
        subtle: '#f1f0eb',
        ink: '#1a1a1a',
        border: '#e2e0d9',
        accent: {
          blue: '#3b82f6',
          yellow: '#fbbf24',
          pink: '#f472b6',
          green: '#34d399',
          purple: '#a78bfa',
          red: '#ef4444',
          orange: '#f97316',
          teal: '#14b8a6'
        }
      },
      boxShadow: {
        'hard': '4px 4px 0px 0px #1a1a1a',
      },
      backgroundImage: {
        'dot-pattern': 'radial-gradient(#cbd5e1 1px, transparent 1px)',
      }
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
  ],
}
