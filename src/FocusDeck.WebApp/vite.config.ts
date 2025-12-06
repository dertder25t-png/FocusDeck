import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  base: '/',
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    rollupOptions: {
      output: {
        manualChunks: {
          jarvis: [
            './src/apps/JarvisApp.tsx',
            './src/pages/JarvisPage.tsx',
            './src/components/Jarvis/JarvisSidebar.tsx'
          ],
          whiteboard: [
            './src/apps/WhiteboardApp.tsx'
          ],
          dashboard: [
            './src/apps/DashboardApp.tsx',
            './src/pages/DashboardPage.tsx'
          ]
        }
      }
    }
  },
  server: {
    port: 5173,
    proxy: {
      '/v1': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        ws: true,
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      'react-qr-reader': 'react-qr-reader/dist/cjs/index.js',
    },
  },
  optimizeDeps: {
    exclude: ['argon2-browser'],
  },
})
