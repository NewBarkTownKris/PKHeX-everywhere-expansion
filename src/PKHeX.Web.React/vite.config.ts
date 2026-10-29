import { join, resolve } from 'node:path'
import { defaultClientConditions, defineConfig, type Plugin } from 'vite'
import react from '@vitejs/plugin-react'
import pkhexEngine from '../PKHeX.Everywhere.Engine.Sdk/packages/engine/src/vite'
import { readFileSync, writeFileSync } from 'node:fs'

const host = resolve(import.meta.dirname, '../PKHeX.Everywhere.Engine.Host/bin')
const debugFramework = join(host, 'Debug/net10.0/wwwroot/_framework')
const publishedFramework = join(host, 'Release/net10.0/publish/wwwroot/_framework')
let siteBase = '/'

export default defineConfig(() => ({
  base: process.env.PKHEX_BASE_PATH ?? '/',
  plugins: [
    react(),
    {
      name: 'pages-route-fallback',
      configResolved(config) { siteBase = config.base },
      writeBundle({ dir }) {
        writeFileSync(join(dir!, '404.html'), readFileSync(join(import.meta.dirname, 'public/404.html'), 'utf8').replaceAll('%BASE_URL%', siteBase))
      },
    },
    {
      name: 'private-local-fixture',
      configureServer(server) {
        const fixture = process.env.PKHEX_TEST_SAVE
        if (fixture) server.middlewares.use('/__private_fixture.sav', (_req, res) => {
          res.setHeader('Content-Type', 'application/octet-stream')
          res.setHeader('Cache-Control', 'no-store')
          res.end(readFileSync(fixture))
        })
      },
    },
    // The SDK and app can resolve different Vite patch versions.
    pkhexEngine({ frameworkDir: process.env.PKHEX_FRAMEWORK === 'debug' ? debugFramework : publishedFramework }) as unknown as Plugin,
  ],
  resolve: { preserveSymlinks: true, conditions: ['source', ...defaultClientConditions] },
  server: { port: 5173, strictPort: true },
}))
