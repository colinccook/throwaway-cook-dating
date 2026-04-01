import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Find the BFF URL from Aspire-injected env vars.
// With multi-tenancy, the resource is named {tenantId}-bff so the env var
// is services__{tenantId}-bff__http__0. Find whichever one is set.
function findBffUrl(): string {
  const env = process.env;
  for (const key of Object.keys(env)) {
    if (key.match(/^services__.*-bff__https?__0$/) && env[key]) {
      return env[key]!;
    }
  }
  return env.BFF_URL || 'http://localhost:5000';
}

const bffUrl = findBffUrl();

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
