/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: 'hsl(260, 100%, 75%)', // Vibrant Lavender
          foreground: 'hsl(0, 0%, 100%)',
        },
        secondary: {
          DEFAULT: 'hsl(210, 40%, 96.1%)',
          foreground: 'hsl(210, 40%, 9.8%)',
        },
        background: 'hsl(224, 71%, 4%)', // Deep Midnight Blue
        foreground: 'hsl(213, 31%, 91%)',
        surface: {
          DEFAULT: 'hsla(224, 71%, 10%, 0.7)', // Glassmorphism Surface
          100: 'hsla(224, 71%, 15%, 0.7)',
          200: 'hsla(224, 71%, 20%, 0.7)',
        },
        border: 'hsla(213, 31%, 85%, 0.1)',
        accent: {
          DEFAULT: 'hsl(190, 100%, 50%)', // Cyan Glow
          foreground: 'hsl(224, 71%, 4%)',
        },
        destructive: {
          DEFAULT: 'hsl(0, 84%, 60%)',
          foreground: 'hsl(0, 0%, 100%)',
        },
      },
      spacing: {
        '0.5': '0.125rem',  // 2px
        '1': '0.25rem',     // 4px
        '2': '0.5rem',      // 8px
        '3': '0.75rem',     // 12px
        '4': '1rem',        // 16px
        '5': '1.25rem',     // 20px
        '6': '1.5rem',      // 24px
        '8': '2rem',        // 32px
        '10': '2.5rem',     // 40px
        '12': '3rem',       // 48px
        '16': '4rem',       // 64px
        '20': '5rem',       // 80px
        '24': '6rem',       // 96px
      },
      borderRadius: {
        'DEFAULT': '12px',
        'sm': '8px',
        'md': '12px',
        'lg': '16px',
        'xl': '24px',
      },
      boxShadow: {
        '0': 'none',
        '1': '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
        '2': '0 2px 4px 0 rgba(0, 0, 0, 0.1)',
        '4': '0 4px 8px 0 rgba(0, 0, 0, 0.15)',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      fontSize: {
        'xs': '12px',
        'sm': '14px',
        'base': '16px',
        'lg': '22px',
      },
      fontWeight: {
        'normal': '400',
        'semibold': '600',
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
  ],
}
