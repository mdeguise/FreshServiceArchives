import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev: `npm run dev` serves the SPA and proxies API calls to the ASP.NET Core
// dev server (dotnet run, http profile). Prod: `npm run build` emits the static
// assets straight into the API project's wwwroot so IIS serves one site.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5028',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: '../FreshServiceArchives.Api/wwwroot',
    emptyOutDir: true,
  },
})
