import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

function stubArgon2Wasm() {
  return {
    name: 'stub-argon2-wasm',
    enforce: 'pre' as const,
    load(id: string) {
      if (id.endsWith('argon2-browser/dist/argon2.wasm')) {
        return 'const wasm = \"\"; export default wasm;'
      }
      return null
    },
  }
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [stubArgon2Wasm(), react()],
  base: '/',
  build: {
    outDir: 'dist',
    emptyOutDir: true,
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
