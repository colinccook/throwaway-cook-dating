import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const bffUrl = process.env.services__bff__http__0
  || process.env.services__bff__https__0
  || 'http://localhost:5000';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: bffUrl,
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: bffUrl,
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
})
