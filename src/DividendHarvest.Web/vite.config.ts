import { defineConfig } from "vite"
import react from "@vitejs/plugin-react"
import tailwindcss from "@tailwindcss/vite"
import path from "node:path"

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(import.meta.dirname, "./src"),
    },
  },
  build: {
    outDir: "../DividendHarvest/wwwroot",
    emptyOutDir: true,
  },
  server: {
    port: 4173,
    fs: {
      allow: [path.resolve(import.meta.dirname), path.resolve(import.meta.dirname, "../../locales")],
    },
    proxy: {
      "/api": "http://127.0.0.1:5050",
      "/healthz": "http://127.0.0.1:5050",
      "/readyz": "http://127.0.0.1:5050",
    },
  },
})
