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
          DEFAULT: '#512BD4',
          50: '#E8E1FB',
          100: '#D5C6F7',
          200: '#AF91EF',
          300: '#895CE7',
          400: '#6327DF',
          500: '#512BD4',
          600: '#3F22A6',
          700: '#2D1978',
          800: '#1B104A',
          900: '#09061C',
        },
        surface: {
          DEFAULT: '#0F0F10',
          50: '#2A2A2C',
          100: '#1F1F21',
          200: '#141416',
          300: '#0F0F10',
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
