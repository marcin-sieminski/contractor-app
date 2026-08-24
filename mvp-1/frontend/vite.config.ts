import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: process.env.PORT ? parseInt(process.env.PORT) : 5173,
    proxy: {
      // Czat AI / asystent — osobny host ContractorApp.Mcp.Api.
      // Musi być PRZED ogólną regułą '/api', bo Vite dopasowuje po prefiksie.
      '/api/v1/ai': {
        target: 'http://localhost:5040',
        changeOrigin: true
      },
      '/api': {
        target: 'http://localhost:5030',
        changeOrigin: true
      }
    }
  }
})
