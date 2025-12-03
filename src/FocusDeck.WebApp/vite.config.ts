
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    outDir: '../FocusDeck.Server/wwwroot',
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
        '/v1': {
            target: 'https://localhost:7066', // Adjust port if needed, assuming default HTTPS for .NET 8
            changeOrigin: true,
            secure: false
        },
        '/hubs': {
            target: 'https://localhost:7066',
            changeOrigin: true,
            secure: false,
            ws: true
        }
    }
  }
})
